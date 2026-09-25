using StardewModdingAPI;
using StardewValley;
using StardewVoices.Dialogue;
using StardewVoices.Utils;
using StardewVoices.FileGeneration.Models;
using StardewVoices.Utils.ParsingHelpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace StardewVoices.Utils.GameDialogueExtraction
{
    /// <summary>
    /// A fork/switchEvent command found in an event script, resolved to the location file
    /// its target actually lives in.
    /// </summary>
    /// <param name="ParentEventKey">"Location/eventId" of the script containing the command.</param>
    /// <param name="TargetEventKey">"Location/key" the command jumps to.</param>
    /// <param name="IsSwitchEvent">True for switchEvent (always jumps), false for fork (conditional).</param>
    /// <param name="QuestionKey">The preceding "question forkN" key this fork answers (e.g. "fork1"), or null.</param>
    /// <param name="AnswerIndex">Index into <paramref name="Answers"/> of the answer that jumps to the target.
    /// Every other answer continues the parent script past the fork. Null when not question-driven.</param>
    /// <param name="Answers">All player answer texts of that question.</param>
    /// <param name="LineIndex">Index into the parent's EventLine list where the fork executes — lines from
    /// here on are the parent's main continuation. -1 when found outside line extraction (fork graph).</param>
    /// <param name="RequiredId">For "fork {requiredId} {key}": the mail flag / answered dialogue id that
    /// decides the jump. Null for a plain "fork {key}" (decided by the event's fork flag) and switchEvent.</param>
    public record EventFork(
        string ParentEventKey,
        string TargetEventKey,
        bool IsSwitchEvent,
        string QuestionKey,
        int? AnswerIndex,
        IReadOnlyList<string> Answers,
        int LineIndex = -1,
        string RequiredId = null);

    /// <summary>
    /// A player question whose replies are inline in the same event script — a "question"
    /// followed by "splitSpeak A~B", or a "quickQuestion" with one command branch per
    /// answer — with the EventLines each answer leads to. Only one branch plays per scene.
    /// </summary>
    /// <param name="Source">"splitSpeak" or "quickQuestion".</param>
    /// <param name="QuestionText">The prompt shown with the answers (often blank for splitSpeak).</param>
    /// <param name="Answers">Every answer the player can pick, in order.</param>
    /// <param name="Branches">One per answer, in answer order.</param>
    public record EventQuestionBranches(
        string Source,
        string QuestionText,
        IReadOnlyList<string> Answers,
        IReadOnlyList<EventAnswerBranch> Branches);

    /// <param name="AnswerIndex">Index into the question's Answers.</param>
    /// <param name="AnswerText">That answer's text, or null if the question didn't list one.</param>
    /// <param name="FirstLineIndex">Index into the event's EventLine list of this answer's first reply
    /// line (for a branch with no lines, where it would have been).</param>
    /// <param name="LineCount">How many lines this answer leads to — 0 if it's actions only.
    /// Includes lines of any question nested inside this branch.</param>
    /// <param name="Note">Direction shown after the branch's lines, e.g. which scene it leads to. Null = none.</param>
    public record EventAnswerBranch(int AnswerIndex, string AnswerText, int FirstLineIndex, int LineCount, string Note = null);

    /// <summary>
    /// An event speak line that asks a $q dialogue question. Unlike splitSpeak/quickQuestion,
    /// the replies aren't in the event: each answer names a key in the speaker's own dialogue
    /// file (Characters/Dialogue/{speaker}), which is where the game reads the reply from.
    /// </summary>
    /// <param name="QuestionLineIndex">Index into the event's EventLine list of the question's
    /// last line — the replies follow it.</param>
    /// <param name="Speaker">The character asking (and replying).</param>
    /// <param name="Responses">Every player answer, in order, with its reply key.</param>
    /// <param name="RepliesByKey">Each reply key's lines, with the same ids the character's own
    /// dialogue extraction gives them. A key missing from the dialogue file has no entry.</param>
    /// <param name="ForkingReplyKeys">Reply keys whose text contains "%fork" — showing that reply sets
    /// the event's fork flag, so a following plain "fork {key}" jumps (Dialogue.checkForSpecialCharacters).</param>
    public record EventDialogueQuestion(
        int QuestionLineIndex,
        string Speaker,
        IReadOnlyList<DialogueQuestionResponse> Responses,
        IReadOnlyDictionary<string, IReadOnlyList<EventLine>> RepliesByKey,
        IReadOnlyCollection<string> ForkingReplyKeys = null);

    public class EventDialogueExtractor
    {
        private readonly IModHelper helper;
        private readonly bool isEnglish =
            LocalizationHelpers.GetCurrentLanguage() == LocalizedContentManager.LanguageCode.en.ToString();

        // locationName -> English event dict, loaded lazily since most locations are
        // never visited by a given extraction. Only populated on non-English locales.
        private readonly Dictionary<string, Dictionary<string, string>> _englishEventDataCache = new();

        // Every location name that has a "Data/Events/{name}" content asset, discovered
        // dynamically instead of a fixed list — see GetEventLocationNames. Computed once
        // per extractor instance and reused, since it touches disk.
        private List<string> _eventLocationNames;

        // locationName -> raw event dict, memoized so repeated per-character callers
        // (GetAllEventsForCharacter is called once per character in both Pass 1 and
        // again in GenerateEventDialogueFiles) don't reload the same ~45 locations'
        // content from SMAPI over and over.
        private readonly Dictionary<string, Dictionary<string, string>> _gameEventDataCache = new();

        private readonly BookPlaceholderResolver bookPlaceholderResolver;

        // "Location/eventId" -> every fork/switchEvent in that event's script. Built once
        // across every location on first use — see GetForkGraph.
        private Dictionary<string, List<EventFork>> _forkGraph;

        // "Location/eventId" -> fork points recorded while extracting that event's lines,
        // with LineIndex into its EventLine list. See ForkPointsByEvent.
        private readonly Dictionary<string, List<EventFork>> _forkPointsByEvent = new();

        // "{parentEventKey}/{key}" for forks already warned about, so an unresolvable fork
        // logs once instead of once per character per pass.
        private readonly HashSet<string> _warnedUnresolvedForks = new();

        /// <summary>
        /// Fork points found while extracting each event's lines (via GetEventDialogue), keyed
        /// "Location/eventId", each with the player answer that leads into its branch and the
        /// LineIndex where the parent's main continuation starts.
        /// Stored for Phase 2b (question branches side by side); not rendered yet.
        /// </summary>
        public IReadOnlyDictionary<string, List<EventFork>> ForkPointsByEvent => _forkPointsByEvent;

        // "Location/eventId" -> inline question branches recorded while extracting that
        // event's lines. See QuestionBranchesByEvent.
        private readonly Dictionary<string, List<EventQuestionBranches>> _questionBranchesByEvent = new();

        /// <summary>
        /// Inline question branches (splitSpeak / quickQuestion) found while extracting each
        /// event's lines (via GetEventDialogue), keyed "Location/eventId", with the range of
        /// that event's EventLines each player answer leads to.
        /// Stored for Phase 2b (question branches side by side); not rendered yet.
        /// </summary>
        public IReadOnlyDictionary<string, List<EventQuestionBranches>> QuestionBranchesByEvent => _questionBranchesByEvent;

        // "Location/eventId" -> $q dialogue questions asked in that event. See DialogueQuestionsByEvent.
        private readonly Dictionary<string, List<EventDialogueQuestion>> _dialogueQuestionsByEvent = new();

        /// <summary>
        /// $q dialogue questions asked by speak lines in each event (via GetEventDialogue), keyed
        /// "Location/eventId", with the replies loaded from the speaker's dialogue file.
        /// </summary>
        public IReadOnlyDictionary<string, List<EventDialogueQuestion>> DialogueQuestionsByEvent => _dialogueQuestionsByEvent;

        // Speaker -> Characters/Dialogue/{speaker} (null if it has none), and its English
        // counterpart on non-English locales. Loaded lazily for $q reply lookups.
        private readonly Dictionary<string, Dictionary<string, string>> _characterDialogueCache = new();
        private readonly Dictionary<string, Dictionary<string, string>> _englishCharacterDialogueCache = new();

        public EventDialogueExtractor(IModHelper helper)
        {
            this.helper = helper;
            this.bookPlaceholderResolver = new BookPlaceholderResolver(helper);
        }


        // Returns (Text, Context) pairs where Context is "{locationName}/{eventId}/{characterName}".
        // Line text is passed through GetEventLinesForEntry so any "%book" placeholder is
        // resolved (or expanded into one entry per genre) the same way as GetEventDialogue.
        public List<(string Text, string Context)> GetAllEventDialogue()
        {
            var results = new List<(string Text, string Context)>();

            foreach (string locationName in GetEventLocationNames())
            {
                Dictionary<string, string> gameEventData = LoadGameEventData(locationName);
                if (gameEventData == null) continue;

                foreach (var kvp in gameEventData)
                {
                    string eventId = kvp.Key.Split('/')[0];
                    foreach (EventLine eventLine in GetEventLinesForEntry(locationName, eventId, kvp))
                        results.Add((eventLine.Dialogue, $"{locationName}/{eventId}/{eventLine.Character}"));
                }
            }

            return results;
        }

        // Returns (Text, Context) pairs where Context is "{locationName}/{eventId}".
        // Line text is passed through GetEventLinesForEntry so any "%book" placeholder is
        // resolved (or expanded into one entry per genre) the same way as GetEventDialogue.
        public List<(string Text, string Context)> GetEventDialogueForCharacter(string characterName)
        {
            var results = new List<(string Text, string Context)>();

            foreach (string locationName in GetEventLocationNames())
            {
                Dictionary<string, string> gameEventData = LoadGameEventData(locationName);
                if (gameEventData == null) continue;

                foreach (var kvp in gameEventData)
                {
                    if (!kvp.Value.Contains($"speak {characterName} \"")
                        && !kvp.Value.Contains($"textAboveHead {characterName} \"")
                        && !kvp.Value.Contains($"end dialogue {characterName} \""))
                        continue;

                    string eventId = kvp.Key.Split('/')[0];
                    foreach (EventLine eventLine in GetEventLinesForEntry(locationName, eventId, kvp))
                    {
                        if (eventLine.Character != characterName) continue;
                        results.Add((eventLine.Dialogue, $"{locationName}/{eventId}"));
                    }
                }
            }

            return results;
        }

        public List<(string Location, string EventId)> GetAllEventsForCharacter(string characterName)
        {
            var results = new List<(string Location, string EventId)>();

            foreach (string locationName in GetEventLocationNames())
            {
                Dictionary<string, string> gameEventData = LoadGameEventData(locationName);
                if (gameEventData == null) continue;

                foreach (var kvp in gameEventData)
                {
                    string eventId = kvp.Key.Split('/')[0];
                    if (!kvp.Value.Contains($"speak {characterName} \"")) continue;

                    results.Add((locationName, eventId));
                }
            }

            return results;
        }

        /// <summary>
        /// Returns events for a character grouped and ordered for script generation.
        /// "heart_events" — this character's own heart events, sorted lowest to highest friendship level,
        ///                   with any response events (fork/switchEvent branches) inserted directly after
        ///                   the parent event that references them.
        /// "guest_events"  — every other event in which this character speaks, in discovery order,
        ///                   also with responses inserted after their parent.
        /// </summary>
        public List<(string locationName, string eventId)> GetEventOrderForCharacter(
            string characterName,
            List<(string locationName, string eventId)> allEvents)
        {
            var heartEvents = new List<(int friendshipLevel, string locationName, string eventId)>();
            var guestEvents = new List<(string locationName, string eventId)>();
            HashSet<string> appendedBranches = GetBranchesReachableFrom(allEvents);

            foreach (var (locationName, eventId) in allEvents)
            {
                // Find the full raw key (e.g. "423502/f Elliott 1500") for this event
                var entry = FindEventEntry(locationName, eventId);
                if (entry.Key == null) continue;

                // A key with no '/' is usually a fork/switchEvent response — those are
                // appended after their parent below, so skip them here. But some locations
                // (e.g. Temp, Tent) are triggered directly by game code rather than through
                // the normal precondition/fork system, so ALL their keys lack '/' and none
                // of them are ever a fork target either. And a branch whose parent this
                // character never speaks in has no parent here to be appended after. Only
                // skip when this key will actually be appended — otherwise it's a
                // standalone event that would vanish entirely if skipped.
                if (!entry.Key.Contains('/') && appendedBranches.Contains($"{locationName}/{eventId}"))
                    continue;

                Match heartMatch = Regex.Match(entry.Key, $@"/f {Regex.Escape(characterName)} (\d+)");
                if (heartMatch.Success)
                    heartEvents.Add((int.Parse(heartMatch.Groups[1].Value), locationName, eventId));
                else
                    guestEvents.Add((locationName, eventId));
            }

            heartEvents.Sort((a, b) => a.friendshipLevel.CompareTo(b.friendshipLevel));

            var heartList = new List<(string locationName, string eventId)>();
            var guestList = new List<(string locationName, string eventId)>();
            // Keyed "Location/eventId" — branches can now come from another location's
            // file, where a bare id could collide with an unrelated event.
            var seen = new HashSet<string>();

            foreach (var (_, locationName, eventId) in heartEvents)
            {
                string eventKey = $"{locationName}/{eventId}";
                if (!seen.Add(eventKey)) continue;
                heartList.Add((locationName, eventId));
                AppendResponseEvents(eventKey, heartList, seen);
            }

            foreach (var (locationName, eventId) in guestEvents)
            {
                string eventKey = $"{locationName}/{eventId}";
                if (!seen.Add(eventKey)) continue;
                guestList.Add((locationName, eventId));
                AppendResponseEvents(eventKey, guestList, seen);
            }
            heartList.Add((null, "STARTING GUEST LIST"));
            heartList.AddRange(guestList);

            return heartList;
        }

        // Inserts the event's fork/switchEvent targets directly after it in the list —
        // from whichever location file each one resolved to — recursing for nested responses.
        private void AppendResponseEvents(
            string eventKey,
            List<(string locationName, string eventId)> target,
            HashSet<string> seen)
        {
            foreach (EventFork fork in GetForks(eventKey))
            {
                if (!seen.Add(fork.TargetEventKey)) continue;

                var (locationName, eventId) = SplitEventKey(fork.TargetEventKey);
                target.Add((locationName, eventId));
                AppendResponseEvents(fork.TargetEventKey, target, seen);
            }
        }

        // Every branch ("Location/key") that AppendResponseEvents will insert after one of
        // these events — i.e. reachable through fork/switchEvent from an event in the list
        // that is itself a real entry point (a '/'-precondition key, or a key nothing forks to).
        private HashSet<string> GetBranchesReachableFrom(List<(string locationName, string eventId)> events)
        {
            var allForkTargets = new HashSet<string>(
                GetForkGraph().Values.SelectMany(forks => forks).Select(fork => fork.TargetEventKey));

            var reachable = new HashSet<string>();
            foreach (var (locationName, eventId) in events)
            {
                string eventKey = $"{locationName}/{eventId}";
                var entry = FindEventEntry(locationName, eventId);
                if (entry.Key == null) continue;
                if (!entry.Key.Contains('/') && allForkTargets.Contains(eventKey)) continue;

                CollectBranches(eventKey, reachable);
            }
            return reachable;
        }

        private void CollectBranches(string eventKey, HashSet<string> reached)
        {
            foreach (EventFork fork in GetForks(eventKey))
            {
                if (reached.Add(fork.TargetEventKey))
                    CollectBranches(fork.TargetEventKey, reached);
            }
        }

        // ── Fork / switchEvent resolution ────────────────────────────────────────

        /// <summary>
        /// Every fork/switchEvent in one event's script ("Location/eventId"), each resolved
        /// to the location file its target lives in. Unresolvable ones are left out (and
        /// warned about once).
        /// </summary>
        public IReadOnlyList<EventFork> GetForks(string eventKey)
            => GetForkGraph().TryGetValue(eventKey, out var forks) ? forks : new List<EventFork>();

        // Builds the fork graph for every event in every location once, and reuses it —
        // ordering runs per character and the reconciler walks it again, so resolving
        // forks (which loads other locations' data) on every call would repeat a lot.
        private Dictionary<string, List<EventFork>> GetForkGraph()
        {
            if (_forkGraph != null) return _forkGraph;

            _forkGraph = new Dictionary<string, List<EventFork>>();
            foreach (string locationName in GetEventLocationNames())
            {
                Dictionary<string, string> locationData = LoadGameEventData(locationName);
                if (locationData == null) continue;

                foreach (var kvp in locationData)
                {
                    string eventId = kvp.Key.Split('/')[0];
                    string eventKey = $"{locationName}/{eventId}";

                    var tracker = new ForkTracker(this, locationName, eventId);
                    var forks = new List<EventFork>();
                    foreach (var (command, _, _) in ExpandCommandStream(kvp.Value, '/'))
                    {
                        if (tracker.Handle(command) is EventFork fork)
                            forks.Add(fork);
                    }

                    if (forks.Count == 0) continue;
                    if (_forkGraph.TryGetValue(eventKey, out var existing))
                        existing.AddRange(forks);
                    else
                        _forkGraph[eventKey] = forks;
                }
            }
            return _forkGraph;
        }

        /// <summary>
        /// Follows one event script's commands in order, tracking what the game needs to
        /// resolve a fork/switchEvent: the event's current location (the game looks the key
        /// up in Data/Events/{current location}, which changes on changeLocation — not the
        /// start location), and the most recent "question forkN" (whose answer N sets the
        /// flag a following plain "fork key" checks).
        /// </summary>
        private sealed class ForkTracker
        {
            // question forkN "Question text#Answer0#Answer1#..."
            private static readonly Regex ForkQuestionRegex = new Regex(@"^question (fork(\d+)) ""(.*)""");

            private readonly EventDialogueExtractor extractor;
            private readonly string startLocation;
            private readonly string parentEventKey;
            private string currentLocation;

            private string questionKey;
            private int? answerIndex;
            private IReadOnlyList<string> answers;

            public ForkTracker(EventDialogueExtractor extractor, string startLocation, string eventId)
            {
                this.extractor = extractor;
                this.startLocation = startLocation;
                currentLocation = startLocation;
                parentEventKey = $"{startLocation}/{eventId}";
            }

            /// <summary>
            /// Feed every command in order. Returns the resolved fork for fork/switchEvent
            /// commands; null for anything else (including forks that can't be resolved).
            /// </summary>
            public EventFork Handle(string command)
            {
                string[] args = command.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (args.Length == 0) return null;

                switch (args[0])
                {
                    case "changeLocation" when args.Length > 1:
                        currentLocation = args[1];
                        return null;

                    // The game builds the temporary map as a location literally named "Temp".
                    case "changeToTemporaryMap":
                        currentLocation = "Temp";
                        return null;

                    case "resetVariable":
                        ClearQuestion();
                        return null;

                    case "question":
                        Match questionMatch = ForkQuestionRegex.Match(command);
                        if (questionMatch.Success)
                        {
                            // Index 0 is the question text itself (often blank); answers follow.
                            string[] parts = questionMatch.Groups[3].Value.Split('#').Select(part => part.Trim()).ToArray();
                            questionKey = questionMatch.Groups[1].Value;
                            answerIndex = int.Parse(questionMatch.Groups[2].Value);
                            answers = parts.Skip(1).ToList();
                        }
                        return null;

                    // "fork <key>" jumps when the question flag is set;
                    // "fork <requiredId> <key> [isTranslationKey]" jumps on a mail flag/answered
                    // dialogue question instead, so it isn't tied to the preceding question.
                    case "fork" when args.Length == 2:
                        return Resolve(args[1], isSwitchEvent: false, questionDriven: true);
                    case "fork" when args.Length >= 3:
                        if (args.Length >= 4 && args[3].Equals("true", StringComparison.OrdinalIgnoreCase))
                        {
                            ErrorLogger.Info($"[Event forks] {parentEventKey}: skipping fork to translation key '{args[2]}' — not event data.");
                            return null;
                        }
                        return Resolve(args[2], isSwitchEvent: false, questionDriven: false, requiredId: args[1]);

                    case "switchEvent" when args.Length > 1:
                        return Resolve(args[1], isSwitchEvent: true, questionDriven: false);
                }

                return null;
            }

            private void ClearQuestion()
            {
                questionKey = null;
                answerIndex = null;
                answers = null;
            }

            // Current location first, as the game does; the start location as a fallback.
            private EventFork Resolve(string key, bool isSwitchEvent, bool questionDriven, string requiredId = null)
            {
                string[] locationsToCheck = new[] { currentLocation, startLocation }.Distinct().ToArray();

                foreach (string location in locationsToCheck)
                {
                    if (extractor.LoadGameEventData(location)?.ContainsKey(key) != true) continue;

                    return new EventFork(
                        parentEventKey,
                        $"{location}/{key}",
                        isSwitchEvent,
                        questionDriven ? questionKey : null,
                        questionDriven ? answerIndex : null,
                        questionDriven ? answers : null,
                        RequiredId: requiredId);
                }

                if (extractor._warnedUnresolvedForks.Add($"{parentEventKey}/{key}"))
                {
                    string command = isSwitchEvent ? "switchEvent" : "fork";
                    string checkedList = string.Join(", ", locationsToCheck.Select(location => $"Data/Events/{location}"));
                    ErrorLogger.Warn($"[Event forks] Event '{parentEventKey}': {command} target '{key}' not found in {checkedList} — its dialogue was not extracted.");
                }
                return null;
            }
        }

        private KeyValuePair<string, string> FindEventEntry(string locationName, string eventId)
            => LoadGameEventData(locationName)?.FirstOrDefault(kvp => kvp.Key.Split('/')[0] == eventId) ?? default;

        private static (string locationName, string eventId) SplitEventKey(string eventKey)
        {
            string[] parts = eventKey.Split('/', 2);
            return (parts[0], parts[1]);
        }

        public List<EventLine> GetEventDialogue(string locationName, string eventId)
        {
            Dictionary<string, string> gameEventData = LoadGameEventData(locationName);
            if (gameEventData == null) return null;
            List<EventLine> results = new List<EventLine>();
            var forkPoints = new List<EventFork>();
            var questionBranches = new List<EventQuestionBranches>();
            var dialogueQuestions = new List<EventDialogueQuestion>();

            foreach (var kvp in gameEventData)
            {
                string id = kvp.Key.Split('/')[0];
                if (eventId != id) continue;

                var entryForkPoints = new List<EventFork>();
                var entryQuestionBranches = new List<EventQuestionBranches>();
                var entryDialogueQuestions = new List<EventDialogueQuestion>();
                List<EventLine> entryLines = GetEventLinesForEntry(
                    locationName, eventId, kvp, entryForkPoints, entryQuestionBranches, entryDialogueQuestions);

                // Line indexes are relative to this entry — shift them past any earlier entry
                // sharing the same id so they index the combined list returned here.
                int offset = results.Count;
                forkPoints.AddRange(entryForkPoints.Select(fork => fork with { LineIndex = fork.LineIndex + offset }));
                questionBranches.AddRange(entryQuestionBranches.Select(question => question with
                {
                    Branches = question.Branches.Select(branch => branch with { FirstLineIndex = branch.FirstLineIndex + offset }).ToList()
                }));
                dialogueQuestions.AddRange(entryDialogueQuestions.Select(question => question with
                {
                    QuestionLineIndex = question.QuestionLineIndex + offset
                }));
                results.AddRange(entryLines);
            }

            _forkPointsByEvent[$"{locationName}/{eventId}"] = forkPoints;
            _questionBranchesByEvent[$"{locationName}/{eventId}"] = questionBranches;
            _dialogueQuestionsByEvent[$"{locationName}/{eventId}"] = dialogueQuestions;
            return results;
        }

        // Parses one raw event-script entry (one kvp from LoadGameEventData) into fully
        // resolved EventLines — real line ids via GenerateEventLineId, with any "%book"
        // placeholder already resolved. Shared by GetEventDialogue (single event) and the
        // simpler bulk extractors below, so line ids and %book resolution stay consistent
        // across every caller instead of drifting between two parsing paths.
        // Pass forkPointsOut / questionBranchesOut / dialogueQuestionsOut to also collect the
        // script's fork points, inline question branches and $q dialogue questions (see
        // EventFork.LineIndex, EventAnswerBranch, EventDialogueQuestion).
        private List<EventLine> GetEventLinesForEntry(
            string locationName, string eventId, KeyValuePair<string, string> kvp,
            List<EventFork> forkPointsOut = null, List<EventQuestionBranches> questionBranchesOut = null,
            List<EventDialogueQuestion> dialogueQuestionsOut = null)
        {
            bool isResponseEvent = !kvp.Key.Contains('/');

            // The raw key's last precondition often carries a value that the scene name
            // needs to preserve (e.g. "733330/f Sam 750/.../y 1" -> "733330_1").
            // eventId alone (the part before the first '/') drops that segment, so
            // rebuild sceneName from the full key here rather than reusing eventId.
            string[] keyTokens = kvp.Key.Split(' ');
            string sceneName = keyTokens.Length > 1
                ? $"{eventId}_{keyTokens[^1]}"
                : eventId;

            // English counterpart of this same event entry, positionally aligned to
            // ParseSpeakingLines(kvp.Value) (same script, same command order — only the
            // spoken text differs by locale). Used so ids derived from response-embedded
            // text (see ParseYDialogueWithIds) resolve to the English source instead of
            // whatever language the game is currently loaded with. Null on English
            // locales or if the English asset/key can't be found — GetScriptDialogue then
            // falls back to deriving ids from the localized text itself.
            string englishScript = null;
            if (!isEnglish
                && GetEnglishGameEventData(locationName) is Dictionary<string, string> englishEventData
                && englishEventData.TryGetValue(kvp.Key, out string es))
            {
                englishScript = es;
            }

            ForkTracker forkTracker = forkPointsOut != null ? new ForkTracker(this, locationName, eventId) : null;
            return GetScriptDialogue(locationName, sceneName, kvp.Value, englishScript, isResponseEvent,
                forkTracker, forkPointsOut, questionBranchesOut, dialogueQuestionsOut);
        }

        /// <summary>
        /// Parses a raw event-script string (the "/"-delimited speak/textAboveHead/end
        /// dialogue command stream shared by Data/Events and Data/Festivals) into fully
        /// resolved EventLines — real line ids via GenerateEventLineId, with any "%book"
        /// placeholder already resolved. Shared by GetEventLinesForEntry (Data/Events) and
        /// GetFestivalScriptDialogue (Data/Festivals) so line-id generation, #$b# splitting,
        /// and gender/book-placeholder resolution stay identical between the two instead of
        /// drifting between two parsing paths.
        /// </summary>
        /// <param name="forkTracker">
        /// When set (with <paramref name="forkPointsOut"/>), fork/switchEvent commands are resolved
        /// as the script is walked and added to <paramref name="forkPointsOut"/> with LineIndex set
        /// to how many lines were produced before them.
        /// </param>
        /// <param name="questionBranchesOut">
        /// When set, gets every inline question (splitSpeak / quickQuestion) with the range of
        /// returned lines each answer leads to.
        /// </param>
        /// <param name="dialogueQuestionsOut">
        /// When set, gets every speak line that asks a $q dialogue question, with its replies
        /// loaded from the speaker's dialogue file.
        /// </param>
        private List<EventLine> GetScriptDialogue(
            string locationName, string sceneName, string script, string englishScript, bool isResponseEvent,
            ForkTracker forkTracker = null, List<EventFork> forkPointsOut = null,
            List<EventQuestionBranches> questionBranchesOut = null,
            List<EventDialogueQuestion> dialogueQuestionsOut = null)
        {
            var results = new List<EventLine>();
            string contextLabel = $"{locationName}/{sceneName}";

            Dictionary<string, int> speakerLineCount = new Dictionary<string, int>();
            // Includes speak, textAboveHead, end-dialogue, and expanded splitSpeak/question-
            // answer lines together, in the order they appear in the raw script, so the
            // shared per-character counter below advances across all types instead of
            // resetting per type. A "resolvedLine" entry (from a hardcoded question answer)
            // already carries its own fixed id and bypasses the counter entirely — see below.
            var forkMarkers = new List<(int speakLineIndex, EventFork fork)>();
            var branchTags = new List<BranchTag>();
            List<(string line, string characterName, EventActionType actionType, object chainGroup, EventLine resolvedLine)> speakLines =
                ParseSpeakingLines(script, contextLabel, forkTracker, forkMarkers, branchTags);

            // lineStarts[i] = results.Count when speakLines[i] started producing lines, so
            // speakLines[i] produced results[lineStarts[i] .. lineStarts[i + 1]).
            var lineStarts = new int[speakLines.Count + 1];

            // A fork marker sits before speakLines[speakLineIndex]; each speak line can expand
            // into several EventLines, so translate to a results index as lines are produced.
            int nextMarker = 0;
            void RecordForkPointsBefore(int speakLineIndex)
            {
                for (; nextMarker < forkMarkers.Count && forkMarkers[nextMarker].speakLineIndex <= speakLineIndex; nextMarker++)
                    forkPointsOut?.Add(forkMarkers[nextMarker].fork with { LineIndex = results.Count });
            }

            if (speakLines.Count == 0)
            {
                RecordForkPointsBefore(int.MaxValue);
                return results;
            }

            List<(string line, string characterName, EventActionType actionType, object chainGroup, EventLine resolvedLine)> englishSpeakLines =
                englishScript != null ? ParseSpeakingLines(englishScript, contextLabel) : null;

            // Tracks the previous speak/textAboveHead/end-dialogue command's chain
            // group and the next id to use if the next command shares that same
            // group (i.e. came from the same quickQuestion branch) — so several
            // speak commands in one branch continue one _sv chain instead of each
            // grabbing a new event id. Top-level commands never share a group, so
            // they always fall through to GenerateEventLineId as before.
            object previousChainGroup = null;
            string chainId = null;

            for (int i = 0; i < speakLines.Count; i++)
            {
                (string line, string characterName, EventActionType actionType, object chainGroup, EventLine resolvedLine) = speakLines[i];
                RecordForkPointsBefore(i);
                lineStarts[i] = results.Count;

                // A hardcoded question-answer line already has its own fixed line id
                // (from Strings/StringsFromCsFiles) — add it as-is and reset the chain,
                // since it never participates in the per-character _eventN counter.
                if (resolvedLine != null)
                {
                    results.Add(resolvedLine);
                    previousChainGroup = null;
                    chainId = null;
                    continue;
                }

                bool continuesChain = chainGroup != null && chainGroup == previousChainGroup;

                // Generate the base ID first so ParseDialogueWithIds can append
                // gender suffixes (_male_farmer / _female_farmer) when needed.
                // Note: GenerateEventLineId handles speakerLineCount initialisation
                // internally — do NOT pre-initialise here or the first line gets _event2.
                string baseId = (continuesChain && chainId != null)
                    ? chainId
                    : GenerateEventLineId(locationName, sceneName, characterName, speakerLineCount, actionType);
                string englishLine = englishSpeakLines != null && i < englishSpeakLines.Count
                    ? englishSpeakLines[i].line
                    : null;
                var parsedPairs = CharacterDialogueParser.ParseDialogueWithIds(line, baseId, englishLine);
                if (parsedPairs != null && parsedPairs.Length > 0)
                {
                    foreach (var (lineId, dialogue) in parsedPairs)
                    {
                        // Event keys with no "/" are branch events reached only after
                        // a player made a dialogue choice (e.g. "DadWeird", "beatGame").
                        // All lines from such events are prefixed with "response_".
                        string finalId = isResponseEvent ? "response_" + lineId : lineId;
                        foreach (var (resolvedId, resolvedDialogue) in bookPlaceholderResolver.Resolve(finalId, dialogue))
                            results.Add(new EventLine(resolvedId, characterName, resolvedDialogue));
                    }
                    chainId = CharacterDialogueParser.AdvancePastLineId(parsedPairs[^1].LineId);
                }
                else
                {
                    ErrorLogger.Warn($"Error parsing event dialogue: {line}");
                    chainId = null;
                }

                // A $q question's replies live in the speaker's dialogue file, not the event —
                // record them so the script can show them under the question.
                if (dialogueQuestionsOut != null && results.Count > lineStarts[i])
                {
                    List<DialogueQuestionResponse> responses = CharacterDialogueParser.GetQuestionResponses(line);
                    if (responses.Count >= 2)
                    {
                        Dictionary<string, string> speakerDialogue = LoadCharacterDialogue(characterName, english: false);
                        List<string> forkingReplyKeys = responses
                            .Select(response => response.ReplyKey)
                            .Distinct()
                            .Where(replyKey => speakerDialogue != null
                                && speakerDialogue.TryGetValue(replyKey, out string rawReply)
                                && rawReply.Contains("%fork"))
                            .ToList();

                        dialogueQuestionsOut.Add(new EventDialogueQuestion(
                            results.Count - 1, characterName, responses,
                            LoadDialogueReplies(characterName, responses, contextLabel), forkingReplyKeys));
                    }
                }

                previousChainGroup = chainGroup;
            }

            RecordForkPointsBefore(int.MaxValue);
            lineStarts[speakLines.Count] = results.Count;
            questionBranchesOut?.AddRange(BuildQuestionBranches(branchTags, lineStarts));
            return results;
        }

        // Each distinct reply key's lines from the speaker's dialogue file, parsed exactly as the
        // character's own dialogue extraction parses them (CharacterDialogueExtractor.BuildLabelledDialogue),
        // so they carry the same line ids.
        private Dictionary<string, IReadOnlyList<EventLine>> LoadDialogueReplies(
            string speaker, List<DialogueQuestionResponse> responses, string contextLabel)
        {
            var replies = new Dictionary<string, IReadOnlyList<EventLine>>();
            Dictionary<string, string> dialogue = LoadCharacterDialogue(speaker, english: false);
            Dictionary<string, string> englishDialogue = isEnglish ? null : LoadCharacterDialogue(speaker, english: true);

            foreach (string replyKey in responses.Select(response => response.ReplyKey).Distinct())
            {
                if (dialogue == null || !dialogue.TryGetValue(replyKey, out string rawReply))
                {
                    ErrorLogger.Warn($"$q reply key '{replyKey}' in {contextLabel} was not found in Characters/Dialogue/{speaker}.");
                    continue;
                }

                string englishRawReply = englishDialogue != null && englishDialogue.TryGetValue(replyKey, out string er) ? er : null;
                var lines = new List<EventLine>();
                foreach (LabelledDialogueLine parsed in CharacterDialogueParser.ParseRawValueToLines(rawReply, replyKey, DialogueType.Dialogue, englishRawReply))
                    foreach (var (resolvedId, resolvedText) in bookPlaceholderResolver.Resolve(parsed.LineId, parsed.Text))
                        lines.Add(new EventLine(resolvedId, speaker, resolvedText));

                replies[replyKey] = lines;
            }
            return replies;
        }

        private Dictionary<string, string> LoadCharacterDialogue(string speaker, bool english)
        {
            var cache = english ? _englishCharacterDialogueCache : _characterDialogueCache;
            if (cache.TryGetValue(speaker, out var cached))
                return cached;

            Dictionary<string, string> data;
            try
            {
                data = english
                    ? Game1.content.Load<Dictionary<string, string>>($"Characters/Dialogue/{speaker}", LocalizedContentManager.LanguageCode.en)
                    : helper.GameContent.Load<Dictionary<string, string>>($"Characters/Dialogue/{speaker}");
            }
            catch
            {
                ErrorLogger.Warn($"Could not load {(english ? "English " : "")}dialogue for '{speaker}' to look up $q replies.");
                data = null;
            }

            cache[speaker] = data;
            return data;
        }

        // Turns each speaking command's answer-branch tag plus its range of produced lines
        // into one EventQuestionBranches per inline question, in the order they appear.
        private static List<EventQuestionBranches> BuildQuestionBranches(List<BranchTag> branchTags, int[] lineStarts)
        {
            var rangesByQuestion = new Dictionary<InlineQuestion, Dictionary<int, (int first, int end)>>();
            var questionOrder = new List<InlineQuestion>();

            for (int i = 0; i < branchTags.Count; i++)
            {
                int first = lineStarts[i];
                int end = lineStarts[i + 1];
                if (end == first) continue;

                // A line inside a nested question also belongs to every enclosing branch.
                // Walk outermost first so an enclosing question is listed before its nested ones.
                var chain = new List<BranchTag>();
                for (BranchTag tag = branchTags[i]; tag != null; tag = tag.Parent)
                    chain.Insert(0, tag);

                foreach (BranchTag tag in chain)
                {
                    if (!rangesByQuestion.TryGetValue(tag.Question, out var byAnswer))
                    {
                        rangesByQuestion[tag.Question] = byAnswer = new Dictionary<int, (int first, int end)>();
                        questionOrder.Add(tag.Question);
                    }

                    byAnswer[tag.AnswerIndex] = byAnswer.TryGetValue(tag.AnswerIndex, out var range)
                        ? (Math.Min(range.first, first), Math.Max(range.end, end))
                        : (first, end);
                }
            }

            var results = new List<EventQuestionBranches>();
            foreach (InlineQuestion question in questionOrder)
            {
                var byAnswer = rangesByQuestion[question];
                var branches = new List<EventAnswerBranch>();

                // Branches run back to back, so an answer with no lines sits where the
                // previous answer's lines ended.
                int position = byAnswer.Values.Min(range => range.first);
                for (int answer = 0; answer < question.BranchCount; answer++)
                {
                    string answerText = answer < question.Answers.Count ? question.Answers[answer] : null;
                    if (byAnswer.TryGetValue(answer, out var range))
                    {
                        branches.Add(new EventAnswerBranch(answer, answerText, range.first, range.end - range.first));
                        position = range.end;
                    }
                    else
                    {
                        branches.Add(new EventAnswerBranch(answer, answerText, position, 0));
                    }
                }

                results.Add(new EventQuestionBranches(question.Source, question.QuestionText, question.Answers, branches));
            }
            return results;
        }

        /// <summary>
        /// Parses one Data/Festivals event-script value (e.g. "governorReaction6") the same
        /// way as a regular event: same speak/textAboveHead/end-dialogue extraction, #$b#
        /// splitting, and per-character-per-key counters. Unlike Data/Events entries,
        /// festival script keys never carry the "/"-precondition format used to distinguish
        /// top-level events from fork-response branches, so isResponseEvent is always false
        /// here — no "response_" id prefix.
        /// </summary>
        public List<EventLine> GetFestivalScriptDialogue(string festivalDate, string jsonKey, string script, string englishScript)
            => GetScriptDialogue(festivalDate, jsonKey, script, englishScript, isResponseEvent: false);

        public enum EventActionType
        {
            Speak,
            TextAboveHead,
            EndDialogue
        }

        public static string GenerateEventLineId(
            string locationName,
            string sceneName,
            string speakerName, 
            Dictionary<string, int> speakerLineCounts,
            EventActionType actionType = EventActionType.Speak
        )
        {
            if (!speakerLineCounts.ContainsKey(speakerName))
                speakerLineCounts[speakerName] = 1;
            else
                speakerLineCounts[speakerName]++;

            int n = speakerLineCounts[speakerName];

            string suffix = actionType switch
            {
                EventActionType.Speak => $"_event{n}",
                EventActionType.TextAboveHead => $"_textAboveHeadEvent{n}",
                EventActionType.EndDialogue => $"_endDialogue{n}",
                _ => throw new ArgumentOutOfRangeException(nameof(actionType))
            };

            return $"{locationName}_{sceneName}_{speakerName}{suffix}".Replace('/', '_');
        }

        /// <summary>
        /// Returns the raw event-script dictionary for a location.
        /// Used by Pass 2 (EventDialogueReconciler) to build the fork graph.
        /// </summary>
        public Dictionary<string, string> GetLocationData(string locationName)
            => LoadGameEventData(locationName);

        // Builds the on-disk path to the game's Content/Data/Events folder.
        //
        // StardewModdingAPI.Constants.GamePath points at the folder containing the game's
        // executable/DLLs, which is where Content lives directly on Windows and Linux. On
        // the Mac Steam build, though, GamePath resolves to .../Stardew Valley.app/Contents/MacOS
        // (the .app bundle's executable folder), while the actual Content folder lives one
        // level up in the bundle at .../Contents/Resources/Content instead of alongside the
        // executable. Without accounting for that bundle layout, the naive Windows-style
        // "GamePath/Content" join misses the folder entirely on Mac.
        private string GetEventsContentPath()
        {
            string gamePath = StardewModdingAPI.Constants.GamePath;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                string contentsDir = Directory.GetParent(gamePath)?.FullName ?? gamePath;
                return Path.Combine(contentsDir, "Resources", "Content", "Data", "Events");
            }

            return Path.Combine(gamePath, "Content", "Data", "Events");
        }

        // Every location name that has a "Data/Events/{name}" content asset.
        // Computed once per extractor instance and reused, since it touches disk.
        private List<string> GetEventLocationNames()
        {
            if (_eventLocationNames != null)
                return _eventLocationNames;

            string eventsContentPath = GetEventsContentPath();
            ErrorLogger.Info($"Resolved game event content folder path: {eventsContentPath}");

            var locationNames = new List<string>();
            try
            {
                if (Directory.Exists(eventsContentPath))
                {
                    foreach (string file in Directory.EnumerateFiles(eventsContentPath, "*.xnb"))
                    {
                        string nameWithoutExtension = Path.GetFileNameWithoutExtension(file);
                        if (!string.IsNullOrWhiteSpace(nameWithoutExtension) && !nameWithoutExtension.Contains('.'))
                            locationNames.Add(nameWithoutExtension);
                    }
                }
                else
                {
                    ErrorLogger.Warn($"Could not find game event content folder at {eventsContentPath}.");
                }
            }
            catch (Exception ex)
            {
                ErrorLogger.Error($"Error scanning game event content folder at {eventsContentPath}. Falling back to whatever was found before the error.", ex);
            }

            ErrorLogger.Info($"Discovered {locationNames.Count} event location(s) from {eventsContentPath}: {string.Join(", ", locationNames)}");

            _eventLocationNames = locationNames;
            return locationNames;
        }

        private Dictionary<string, string> LoadGameEventData(string locationName)
        {
            if (string.IsNullOrEmpty(locationName))
            {
                return null;
            }

            if (_gameEventDataCache.TryGetValue(locationName, out var cached))
                return cached;

            Dictionary<string, string> data;
            try
            {
                data = helper.GameContent.Load<Dictionary<string, string>>($"Data/Events/{locationName}");
            }
            catch
            {
                ErrorLogger.Warn($"Could not load event data for location: {locationName}");
                data = null;
            }

            _gameEventDataCache[locationName] = data;
            return data;
        }

        // Loads the English version of a location's event data regardless of current game
        // language, so line ids derived from player-facing text (e.g. $y response option
        // labels — see ParseYDialogueWithIds) stay identical across all languages, the
        // same "resolve to English source" approach used for character/festival dialogue.
        private Dictionary<string, string> GetEnglishGameEventData(string locationName)
        {
            if (_englishEventDataCache.TryGetValue(locationName, out var cached))
                return cached;

            Dictionary<string, string> data;
            try
            {
                data = Game1.content.Load<Dictionary<string, string>>(
                    $"Data/Events/{locationName}", LocalizedContentManager.LanguageCode.en);
            }
            catch
            {
                ErrorLogger.Warn($"Could not load English event data for location: {locationName}");
                data = null;
            }

            _englishEventDataCache[locationName] = data;
            return data;
        }


        // Yields each command in order, inlining quickQuestion branches (skipping the
        // options text) so callers see one flat stream. Each command also carries a
        // chainGroup marker: every command directly inside the same quickQuestion
        // branch shares one marker (by reference), while every top-level command and
        // every other branch gets its own distinct marker. This lets a branch with
        // several speak commands be numbered as one continuing chain (_sv1, _sv2, ...)
        // instead of each grabbing a new event id — a plain "is this the first token"
        // flag doesn't work here since a branch's first token is usually a non-speak
        // command (emote, jump, ...), not the first speak.
        //
        // Each command also carries the quickQuestion answer branch it sits in (null at
        // the top level) — see BranchTag.
        private static List<(string command, object chainGroup, BranchTag branch)> ExpandCommandStream(
            string script, char delimiter, bool chain = false, BranchTag branch = null)
        {
            var result = new List<(string, object, BranchTag)>();

            // One marker shared by every command directly in this call — i.e. every
            // command belonging to the SAME branch (or, at the top level, null so
            // top-level commands never chain with each other).
            object branchMarker = chain ? new object() : null;

            foreach (string rawCommand in script.Split(delimiter))
            {
                string trimmed = rawCommand.Trim();

                if (!trimmed.StartsWith("quickQuestion "))
                {
                    result.Add((trimmed, chain ? branchMarker : new object(), branch));
                    continue;
                }

                string[] segments = trimmed.Substring("quickQuestion ".Length)
                    .Split(new[] { "(break)" }, StringSplitOptions.None);

                // segments[0] is the '#'-delimited prompt + options list shown to the
                // player. segments[1..] are the branches, one per option, in order.
                // Each nested branch gets its own fresh marker (a new recursive call).
                // The game picks segments[1 + answer], so only as many branches as there are
                // answers can ever play — a trailing "(break)" leaves an empty extra segment.
                string[] promptAndAnswers = segments[0].Split('#').Select(part => part.Trim()).ToArray();
                List<string> answers = promptAndAnswers.Skip(1).ToList();
                var question = new InlineQuestion(
                    "quickQuestion",
                    promptAndAnswers[0],
                    answers,
                    answers.Count > 0 ? Math.Min(answers.Count, segments.Length - 1) : segments.Length - 1);

                for (int i = 1; i < segments.Length; i++)
                    result.AddRange(ExpandCommandStream(segments[i], '\\', chain: true, new BranchTag(question, i - 1, branch)));
            }

            return result;
        }

        // A player question whose replies are inline in the script (splitSpeak / quickQuestion).
        // Compared by reference: each occurrence in a script is its own question.
        private sealed class InlineQuestion
        {
            public string Source { get; }
            public string QuestionText { get; }
            public List<string> Answers { get; }
            public int BranchCount { get; }

            public InlineQuestion(string source, string questionText, List<string> answers, int branchCount)
            {
                Source = source;
                QuestionText = questionText;
                Answers = answers;
                BranchCount = branchCount;
            }
        }

        // Which answer branch of an inline question a command/line belongs to. Parent is the
        // enclosing branch when the question is itself nested inside another question's branch.
        private sealed record BranchTag(InlineQuestion Question, int AnswerIndex, BranchTag Parent);

        // One regex per command type, keyed by the same EventActionType returned to callers.
        // textAboveHead can carry trailing numeric args after the quoted text (display
        // duration, pre-timer, width), so its text capture isn't end-anchored like the others.
        private static readonly Dictionary<EventActionType, Regex> SpeakingLineRegexes = new()
        {
            { EventActionType.Speak,         new Regex(@"^speak (\S+) ""(.+)""$") },
            { EventActionType.TextAboveHead, new Regex(@"^textAboveHead (\S+) ""(.*?)""") },
            { EventActionType.EndDialogue,   new Regex(@"^end dialogue (\S+) ""(.+)""$") },
        };

        private static readonly Regex SplitSpeakRegex = new Regex(@"^splitSpeak (\S+) ""(.+)""$");
        private static readonly Regex QuestionRegex = new Regex(@"^question (\S+)");
        // question <key> "Prompt#Answer0#Answer1#..."
        private static readonly Regex QuestionTextRegex = new Regex(@"^question \S+ ""(.*)""");

        // Vanilla `question <key>` commands whose answer dialogue for each option is NOT
        // present in the event script itself — Event.answerDialogue hardcodes these replies
        // in the game's C# source rather than in Data/Events, keyed by the same question key.
        // Not reflectively callable here (it mutates a live Event/Game1.CurrentEvent, which
        // doesn't exist during offline extraction), so these are hardcoded from
        // Strings/StringsFromCsFiles by hand. Kept under their "File.cs.Line" id rather than
        // an event-scoped id, since the same hardcoded reply could be reused verbatim by other
        // question keys/events, unlike ordinary event dialogue which is scoped to one event.
        private static readonly Dictionary<string, (string Character, string[] CsFileKeys)> QuestionAnswerTable = new()
        {
            // "Are you going to jump?" cliffside scene (Forest event 3910975).
            ["shaneCliffs"] = ("Shane", new[] { "Event.cs.1760", "Event.cs.1761", "Event.cs.1763", "Event.cs.1764" }),
        };

        private Dictionary<string, string> _stringsFromCsFilesCache;

        private Dictionary<string, string> LoadStringsFromCsFiles()
        {
            if (_stringsFromCsFilesCache != null) return _stringsFromCsFilesCache;

            try
            {
                _stringsFromCsFilesCache = helper.GameContent.Load<Dictionary<string, string>>("Strings/StringsFromCsFiles");
            }
            catch
            {
                ErrorLogger.Warn("Could not load 'Strings/StringsFromCsFiles'.");
                _stringsFromCsFilesCache = new Dictionary<string, string>();
            }

            return _stringsFromCsFilesCache;
        }

        // Extracts speak, textAboveHead, end-dialogue, splitSpeak, and question-answer
        // commands together, in the order they appear in the raw script. Used by
        // GetScriptDialogue so the per-character line counter in GenerateEventLineId advances
        // across all types instead of resetting per type. Each command's text is kept whole
        // (including any "#$b#" page breaks) so CharacterDialogueParser.ParseDialogueWithIds
        // can split it and assign the _sv{n} continuation suffixes itself, same as regular
        // dialogue. chainGroup is carried through from ExpandCommandStream so GetScriptDialogue
        // can tell which consecutive commands came from the same quickQuestion branch (continue
        // the _sv chain) vs. which should grab a fresh event id.
        //
        // When forkTracker is set, every command is also fed to it, and each resolved
        // fork/switchEvent is added to forkMarkers with the number of speaking lines found
        // before it — GetScriptDialogue turns that into a fork point's EventLine index.
        //
        // When branchTags is set, it gets one entry per returned line (same order): the
        // inline-question answer branch that line belongs to, or null.
        private List<(string line, string characterName, EventActionType actionType, object chainGroup, EventLine resolvedLine)> ParseSpeakingLines(
            string eventScript, string contextLabel,
            ForkTracker forkTracker = null, List<(int speakLineIndex, EventFork fork)> forkMarkers = null,
            List<BranchTag> branchTags = null)
        {
            var lines = new List<(string line, string characterName, EventActionType actionType, object chainGroup, EventLine resolvedLine)>();

            void AddLine((string, string, EventActionType, object, EventLine) entry, BranchTag branch)
            {
                lines.Add(entry);
                branchTags?.Add(branch);
            }

            // A question's answer dialogue is handled either by a following splitSpeak
            // (key "null") or is already covered by fork/switchEvent commands elsewhere in
            // this same script (the forked event gets extracted on its own when that key is
            // reached) — in either case, no lookup or warning is needed. Checked once per
            // script rather than per question command.
            bool scriptHasForkOrSwitch = eventScript.Contains("fork ") || eventScript.Contains("switchEvent ");

            // Prompt and answers of the most recent question command — a following
            // splitSpeak's "~" branches answer it, in order (the game picks the branch
            // by the answer's index).
            string lastQuestionText = null;
            List<string> lastAnswers = new List<string>();

            foreach (var (trimmed, chainGroup, branch) in ExpandCommandStream(eventScript, '/'))
            {
                if (forkTracker?.Handle(trimmed) is EventFork fork)
                {
                    forkMarkers?.Add((lines.Count, fork));
                    continue;
                }

                // The question's own text is the farmer's answer choices, never NPC
                // dialogue — this command itself never emits a line. If its answers are
                // hardcoded (not "null" and not fork-resolved), look them up and emit them
                // here, using their own fixed ids rather than the counter below.
                if (trimmed.StartsWith("question "))
                {
                    Match questionTextMatch = QuestionTextRegex.Match(trimmed);
                    if (questionTextMatch.Success)
                    {
                        string[] promptAndAnswers = questionTextMatch.Groups[1].Value.Split('#').Select(part => part.Trim()).ToArray();
                        lastQuestionText = promptAndAnswers[0];
                        lastAnswers = promptAndAnswers.Skip(1).ToList();
                    }

                    Match questionMatch = QuestionRegex.Match(trimmed);
                    string questionKey = questionMatch.Success ? questionMatch.Groups[1].Value : null;

                    if (string.IsNullOrEmpty(questionKey) || questionKey == "null" || scriptHasForkOrSwitch)
                        continue;

                    if (!QuestionAnswerTable.TryGetValue(questionKey, out var mapping))
                    {
                        ErrorLogger.Warn($"Unmapped question key '{questionKey}' in {contextLabel} — its answer dialogue was not extracted.");
                        continue;
                    }

                    Dictionary<string, string> csFileStrings = LoadStringsFromCsFiles();
                    foreach (string csKey in mapping.CsFileKeys)
                    {
                        if (csKey == null)
                        {
                            ErrorLogger.Warn($"Question '{questionKey}' in {contextLabel} has an answer slot with no mapped Strings/StringsFromCsFiles key.");
                            continue;
                        }
                        if (!csFileStrings.TryGetValue(csKey, out string rawText))
                        {
                            ErrorLogger.Warn($"Answer key '{csKey}' for question '{questionKey}' in {contextLabel} was not found in Strings/StringsFromCsFiles.");
                            continue;
                        }

                        foreach (LabelledDialogueLine parsed in CharacterDialogueParser.ParseRawValueToLines(rawText, csKey, DialogueType.Event))
                            AddLine((null, mapping.Character, EventActionType.Speak, null,
                                new EventLine(parsed.LineId, mapping.Character, parsed.Text)), branch);
                    }

                    continue;
                }

                if (trimmed.StartsWith("splitSpeak "))
                {
                    Match splitMatch = SplitSpeakRegex.Match(trimmed);
                    if (!splitMatch.Success) continue;

                    string splitCharacterName = splitMatch.Groups[1].Value;
                    // Each branch is a fresh, independent reply — never a continuation of
                    // another branch or of whatever came before — so each gets its own
                    // object() marker rather than sharing chainGroup with the splitSpeak
                    // command itself or with one another.
                    string[] replies = splitMatch.Groups[2].Value.Split('~');
                    var question = new InlineQuestion("splitSpeak", lastQuestionText, lastAnswers, replies.Length);
                    for (int i = 0; i < replies.Length; i++)
                        AddLine((replies[i], splitCharacterName, EventActionType.Speak, new object(), null), new BranchTag(question, i, branch));

                    continue;
                }

                EventActionType? actionType = trimmed switch
                {
                    var s when s.StartsWith("speak ") => EventActionType.Speak,
                    var s when s.StartsWith("textAboveHead ") => EventActionType.TextAboveHead,
                    var s when s.StartsWith("end dialogue ") => EventActionType.EndDialogue,
                    _ => null,
                };
                if (actionType == null) continue;

                Match m = SpeakingLineRegexes[actionType.Value].Match(trimmed);
                if (!m.Success) continue;

                string characterName = m.Groups[1].Value;
                AddLine((m.Groups[2].Value, characterName, actionType.Value, chainGroup, null), branch);
            }

            return lines;
        }
    }
}
