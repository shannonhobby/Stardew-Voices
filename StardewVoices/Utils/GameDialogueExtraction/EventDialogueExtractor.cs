using StardewModdingAPI;
using StardewValley;
using StardewVoices.Utils;
using StardewVoices.FileGeneration.Models;
using StardewVoices.Utils.ParsingHelpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace StardewVoices.Utils.GameDialogueExtraction
{
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
            var locationCache = new Dictionary<string, Dictionary<string, string>>();
            var forkTargetCache = new Dictionary<string, HashSet<string>>();

            foreach (var (locationName, eventId) in allEvents)
            {
                if (!locationCache.TryGetValue(locationName, out var locationData))
                {
                    locationData = LoadGameEventData(locationName);
                    if (locationData == null) continue;
                    locationCache[locationName] = locationData;
                    forkTargetCache[locationName] = GetForkTargetIds(locationData);
                }

                // Find the full raw key (e.g. "423502/f Elliott 1500") for this event
                var entry = locationData.FirstOrDefault(kvp => kvp.Key.Split('/')[0] == eventId);
                if (entry.Key == null) continue;

                // A key with no '/' is usually a fork/switchEvent response reached only
                // from another event in this location — those are appended after their
                // parent below, so skip them here. But some locations (e.g. Temp, Tent)
                // are triggered directly by game code rather than through the normal
                // precondition/fork system, so ALL their keys lack '/' and none of them
                // are ever a fork target either. Only skip when this key is actually
                // reachable as a fork target — otherwise it's a standalone event that
                // would vanish entirely if skipped unconditionally.
                if (!entry.Key.Contains('/') && forkTargetCache[locationName].Contains(eventId))
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
            var seen = new HashSet<string>();

            foreach (var (_, locationName, eventId) in heartEvents)
            {
                if (!seen.Add(eventId)) continue;
                heartList.Add((locationName, eventId));
                var entry = locationCache[locationName].FirstOrDefault(kvp => kvp.Key.Split('/')[0] == eventId);
                if (entry.Key != null)
                    AppendResponseEvents(locationCache[locationName], locationName, entry.Value, heartList, seen);
            }

            foreach (var (locationName, eventId) in guestEvents)
            {
                if (!seen.Add(eventId)) continue;
                guestList.Add((locationName, eventId));
                var entry = locationCache[locationName].FirstOrDefault(kvp => kvp.Key.Split('/')[0] == eventId);
                if (entry.Key != null)
                    AppendResponseEvents(locationCache[locationName], locationName, entry.Value, guestList, seen);
            }
            heartList.Add((null, "STARTING GUEST LIST"));
            heartList.AddRange(guestList);

            return heartList;
        }

        // Walks a script for fork/switchEvent commands and inserts their target events
        // directly after the parent in the list, recursing for nested responses.
        private void AppendResponseEvents(
            Dictionary<string, string> locationData,
            string locationName,
            string eventScript,
            List<(string locationName, string eventId)> target,
            HashSet<string> seen)
        {
            foreach (string command in eventScript.Split('/'))
            {
                string trimmed = command.Trim();
                string responseId = null;

                if (trimmed.StartsWith("fork "))
                    responseId = trimmed.Substring(5).Trim().Split(' ')[0];
                else if (trimmed.StartsWith("switchEvent "))
                    responseId = trimmed.Substring(12).Trim().Split(' ')[0];

                if (string.IsNullOrEmpty(responseId)) continue;
                if (!locationData.TryGetValue(responseId, out string responseScript)) continue;
                if (!seen.Add(responseId)) continue;

                target.Add((locationName, responseId));
                AppendResponseEvents(locationData, locationName, responseScript, target, seen);
            }
        }

        // Every event id referenced anywhere in this location as a fork/switchEvent
        // target — i.e. every id that's only reachable as a response to some other
        // event, not a standalone/top-level one. Used by GetEventOrderForCharacter to
        // tell a genuine fork-response key (no '/', appended after its parent) apart
        // from a location whose events simply aren't gated by the usual '/'-precondition
        // format at all (e.g. Temp, Tent — triggered directly by game code).
        private static HashSet<string> GetForkTargetIds(Dictionary<string, string> locationData)
        {
            var targets = new HashSet<string>();
            foreach (string script in locationData.Values)
            {
                foreach (string command in script.Split('/'))
                {
                    string trimmed = command.Trim();
                    if (trimmed.StartsWith("fork "))
                        targets.Add(trimmed.Substring(5).Trim().Split(' ')[0]);
                    else if (trimmed.StartsWith("switchEvent "))
                        targets.Add(trimmed.Substring(12).Trim().Split(' ')[0]);
                }
            }
            return targets;
        }

        public List<EventLine> GetEventDialogue(string locationName, string eventId)
        {
            Dictionary<string, string> gameEventData = LoadGameEventData(locationName);
            if (gameEventData == null) return null;
            List<EventLine> results = new List<EventLine>();

            foreach (var kvp in gameEventData)
            {
                string id = kvp.Key.Split('/')[0];
                if (eventId == id)
                    results.AddRange(GetEventLinesForEntry(locationName, eventId, kvp));
            }

            return results;
        }

        // Parses one raw event-script entry (one kvp from LoadGameEventData) into fully
        // resolved EventLines — real line ids via GenerateEventLineId, with any "%book"
        // placeholder already resolved. Shared by GetEventDialogue (single event) and the
        // simpler bulk extractors below, so line ids and %book resolution stay consistent
        // across every caller instead of drifting between two parsing paths.
        private List<EventLine> GetEventLinesForEntry(string locationName, string eventId, KeyValuePair<string, string> kvp)
        {
            var results = new List<EventLine>();

            bool isResponseEvent = !kvp.Key.Contains('/');
            Dictionary<string, int> speakerLineCount = new Dictionary<string, int>();
            // Includes speak, textAboveHead, and end-dialogue lines together, in the
            // order they appear in the raw script, so the shared per-character counter
            // below advances across all three types instead of resetting per type.
            List<(string line, string characterName, EventActionType actionType, object chainGroup)> speakLines =
                ParseSpeakingLines(kvp.Value);
            if (speakLines.Count == 0) return results;

            // English counterpart of this same event entry, positionally aligned to
            // speakLines (same script, same command order — only the spoken text
            // differs by locale). Used so ids derived from response-embedded text
            // (see ParseYDialogueWithIds) resolve to the English source instead of
            // whatever language the game is currently loaded with. Null on English
            // locales or if the English asset/key can't be found — callers then fall
            // back to deriving ids from the localized text itself.
            List<(string line, string characterName, EventActionType actionType, object chainGroup)> englishSpeakLines = null;
            if (!isEnglish
                && GetEnglishGameEventData(locationName) is Dictionary<string, string> englishEventData
                && englishEventData.TryGetValue(kvp.Key, out string englishScript))
            {
                englishSpeakLines = ParseSpeakingLines(englishScript);
            }

            // The raw key's last precondition often carries a value that the scene name
            // needs to preserve (e.g. "733330/f Sam 750/.../y 1" -> "733330_1").
            // eventId alone (the part before the first '/') drops that segment, so
            // rebuild sceneName from the full key here rather than reusing eventId.
            string[] keyTokens = kvp.Key.Split(' ');
            string sceneName = keyTokens.Length > 1
                ? $"{eventId}_{keyTokens[^1]}"
                : eventId;

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
                (string line, string characterName, EventActionType actionType, object chainGroup) = speakLines[i];
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

                previousChainGroup = chainGroup;
            }

            return results;
        }

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

        // Every location name that has a "Data/Events/{name}" content asset.
        // Computed once per extractor instance and reused, since it touches disk.
        private List<string> GetEventLocationNames()
        {
            if (_eventLocationNames != null)
                return _eventLocationNames;

            string eventsContentPath = Path.Combine(StardewModdingAPI.Constants.GamePath, "Content", "Data", "Events");

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
                ErrorLogger.Warn($"LoadGameEventData called with a null/empty location name. Stack: {Environment.StackTrace}");
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
        private static List<(string command, object chainGroup)> ExpandCommandStream(
            string script, char delimiter, bool chain = false)
        {
            var result = new List<(string, object)>();

            // One marker shared by every command directly in this call — i.e. every
            // command belonging to the SAME branch (or, at the top level, null so
            // top-level commands never chain with each other).
            object branchMarker = chain ? new object() : null;

            foreach (string rawCommand in script.Split(delimiter))
            {
                string trimmed = rawCommand.Trim();

                if (!trimmed.StartsWith("quickQuestion "))
                {
                    result.Add((trimmed, chain ? branchMarker : new object()));
                    continue;
                }

                string[] segments = trimmed.Substring("quickQuestion ".Length)
                    .Split(new[] { "(break)" }, StringSplitOptions.None);

                // segments[0] is the '#'-delimited options list shown to the player -
                // skip it. segments[1..] are the branches, one per option, in order.
                // Each nested branch gets its own fresh marker (a new recursive call).
                for (int i = 1; i < segments.Length; i++)
                    result.AddRange(ExpandCommandStream(segments[i], '\\', chain: true));
            }

            return result;
        }

        // One regex per command type, keyed by the same EventActionType returned to callers.
        // textAboveHead can carry trailing numeric args after the quoted text (display
        // duration, pre-timer, width), so its text capture isn't end-anchored like the others.
        private static readonly Dictionary<EventActionType, Regex> SpeakingLineRegexes = new()
        {
            { EventActionType.Speak,         new Regex(@"^speak (\S+) ""(.+)""$") },
            { EventActionType.TextAboveHead, new Regex(@"^textAboveHead (\S+) ""(.*?)""") },
            { EventActionType.EndDialogue,   new Regex(@"^end dialogue (\S+) ""(.+)""$") },
        };

        // Extracts speak, textAboveHead, and end-dialogue commands together, in the order
        // they appear in the raw script. Used by GetEventDialogue so the per-character line
        // counter in GenerateEventLineId advances across all three types instead of resetting
        // per type. Each command's text is kept whole (including any "#$b#" page breaks) so
        // CharacterDialogueParser.ParseDialogueWithIds can split it and assign the _sv{n}
        // continuation suffixes itself, same as regular dialogue. chainGroup is carried
        // through from ExpandCommandStream so GetEventDialogue can tell which consecutive
        // commands came from the same quickQuestion branch (continue the _sv chain) vs.
        // which should grab a fresh event id.
        private List<(string line, string characterName, EventActionType actionType, object chainGroup)> ParseSpeakingLines(string eventScript)
        {
            var lines = new List<(string line, string characterName, EventActionType actionType, object chainGroup)>();

            foreach (var (trimmed, chainGroup) in ExpandCommandStream(eventScript, '/'))
            {
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
                lines.Add((m.Groups[2].Value, characterName, actionType.Value, chainGroup));
            }

            return lines;
        }
    }
}
