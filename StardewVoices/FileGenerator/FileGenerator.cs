using StardewModdingAPI;
using StardewVoices.FileGeneration.Models;
using StardewVoices.Pathing;
using StardewVoices.Utils.GameDialogueExtraction;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using StardewVoices.Dialogue;
using StardewValley;
using StardewVoices.Utils;
using StardewVoices.Utils.ParsingHelpers;
using StardewVoices.Config;

namespace StardewVoices.FileGeneration
{
    internal class FileGenerator
    {
        private IModHelper Helper;
        private ManagerHub Managers;
        private Dictionary<string, DialogueJson> Jsons = new Dictionary<string, DialogueJson>();
        private Dictionary<string, EventDialogueJson> EventJsons = new Dictionary<string, EventDialogueJson>();

        private Dictionary<string, Script> Scripts = new Dictionary<string, Script>();
        private Dictionary<string, EventScript> EventScripts = new Dictionary<string, EventScript>();

        private CharacterDialogueExtractor characterDialogueExtractor { get; set; }
        private FestivalDialogueExtractor festivalDialogueExtractor { get; set; }
        private GossipDialogueExtractor gossipDialogueExtractor { get; set; }
        private EventDialogueExtractor eventDialogueExtractor { get; set; }

        private List<DialogueType> MarriageDialogueTypes = new() { 
            DialogueType.Marriage, DialogueType.GenericMarriage, DialogueType.Engagement 
        };



        public FileGenerator(IModHelper helper, ManagerHub managers)
        {
            Helper = helper;
            Managers = managers;
            FileManipulationHelpers.Init(helper);

        }

        private List<string> SeparateJsonFiles = new List<string>() { "EngagementDialogue", "ExtraDialogue", "rainy", "Marriage Dialogue Generic"};

        private Dictionary<DialogueType, string> typeMap = new Dictionary<DialogueType, string>()
        {
            {DialogueType.Marriage, "marriage_dialogue" },
            {DialogueType.Dialogue, "dialogue" },
            {DialogueType.Schedule, "schedule" },
            {DialogueType.Generic, "misc" },
            {DialogueType.Gossip, "gossip" },
            {DialogueType.GiftTastes, "gift_tastes" },
            {DialogueType.SpeechBubble, "misc" },
            {DialogueType.Engagement, "EngagementDialogue" },
            {DialogueType.Extra, "ExtraDialogue" },
            {DialogueType.Rainy, "rainy" },
            {DialogueType.GenericMarriage, "Marriage Dialogue Generic" },
            {DialogueType.Quest, "quest" },
        };

        public void GenerateFiles(bool generateJson, bool generateScript, bool generateGossipDialogue, string scriptFormat, ModConfig config)
        {
            // Abort if the output folder for this language already exists so we don't
            // re-generate over a completed run. Other language folders are left untouched.
            string outputRoot = Managers.PathManager.GeneratedFilesRoot.FullPath;
            if (Directory.Exists(Path.Combine(outputRoot, "Characters")))
            {
                ErrorLogger.Warn($"Generated files for language '{LocalizationHelpers.GetCurrentLanguage()}' already exist at '{outputRoot}'. Skipping generation.");
                return;
            }

            Managers.TestingManager.ValidationIsRunning = true;

            // Clear state from any previous run in this session so re-running the
            // generation command doesn't try to re-add already-populated keys.
            Jsons.Clear();
            EventJsons.Clear();
            Scripts.Clear();
            EventScripts.Clear();

            // Setup extractor class instances
            characterDialogueExtractor= new CharacterDialogueExtractor(Helper);
            eventDialogueExtractor = new EventDialogueExtractor(Helper);
            festivalDialogueExtractor = new FestivalDialogueExtractor(Helper);      
            gossipDialogueExtractor = new GossipDialogueExtractor();

            if (generateJson)
            {
                // Initialize dialogue JSONs for generic files
                foreach (string jsonFileName in SeparateJsonFiles)
                {
                    Jsons.Add(jsonFileName, new DialogueJson(jsonFileName, DialogueParentFolder.Generic, Managers.PathManager));
                }
            }

            // ── Pass 1: collect raw EventLine records for every event ─────────────
            // One entry per unique "LocationName/eventId" across all characters.
            // Fork/response events are included because GetEventOrderForCharacter
            // appends them after their root via AppendResponseEvents.
            var rawEventLines = new Dictionary<string, List<EventLine>>();
            if (generateJson || generateScript)
            {
                foreach (string characterName in config.CharactersToGenerate)
                {
                    string englishName = LocalizationHelpers.GetName(characterName);
                    var allEvents = eventDialogueExtractor.GetAllEventsForCharacter(englishName);
                    var ordered  = eventDialogueExtractor.GetEventOrderForCharacter(englishName, allEvents);

                    foreach ((string locationName, string eventId) in ordered)
                    {
                        if (locationName == null) continue;  // null = section-header marker
                        string key = $"{locationName}/{eventId}";
                        if (rawEventLines.ContainsKey(key)) continue;

                        rawEventLines[key] = eventDialogueExtractor.GetEventDialogue(locationName, eventId)
                            ?? new List<EventLine>();
                    }
                }
            }

            // ── Pass 2: reconcile duplicate names across forked branches ──────────
            var reconciledEventLines = rawEventLines;
            if (rawEventLines.Count > 0)
            {
                var (reconciled, collapseLog) = EventDialogueReconciler.Reconcile(
                    rawEventLines,
                    locationName => eventDialogueExtractor.GetLocationData(locationName),
                    eventDialogueExtractor.GetForks);

                reconciledEventLines = reconciled;

                if (collapseLog.Count > 0)
                {
                    ErrorLogger.Info($"[Pass 2] Collapsed {collapseLog.Count} duplicate name(s):");
                    foreach (var (oldId, canonicalId) in collapseLog)
                        ErrorLogger.Info($"  {oldId}  →  {canonicalId}");
                }
                else
                {
                    ErrorLogger.Info("[Pass 2] No duplicate names found.");
                }
            }

            // ── Phase 3: generate per-character files using reconciled data ───────
            // CharactersToGenerate may hold English or localized names. Jsons are keyed and
            // named by the English name (the runtime loads them by SupportedCharacters);
            // scripts by the localized name, which is what they display and what festival
            // lines look them up by.
            foreach(string configName in config.CharactersToGenerate)
            {
                string englishName = LocalizationHelpers.GetName(configName);
                string localizedName = LocalizationHelpers.GetLocalizedName(englishName);

                if(generateJson)
                {
                    Jsons.Add(englishName, new DialogueJson(englishName, DialogueParentFolder.Characters, Managers.PathManager));
                }
                if(generateScript)
                {
                    Scripts.Add(localizedName, new Script(localizedName, Managers.PathManager));
                    EventScripts.Add(localizedName, new EventScript(localizedName, Managers.PathManager));

                }

                GenerateCharacterDialogueFiles(englishName, generateJson, generateScript, generateGossipDialogue);

                GenerateEventDialogueFiles(englishName, generateJson, generateScript, reconciledEventLines);
            }


            GenerateFestivalDialogueFiles(generateJson, generateScript);


            WriteOutAllFiles(scriptFormat);
        }

        /*
        // might be obsoltete if i do it in GenerateFilesForCharacter
        private void GenerateEventDialogueJsons(EventDialogueExtractor eventDialogueExtractor)
        {
            // Context is "{locationName}/{eventId}/{characterName}".
            List<(string Text, string Context)> eventLines = eventDialogueExtractor.GetAllEventDialogue();
            string lastEventId = "";

            foreach((string Text, string Context) in eventLines)
            {
                string[] c = Context.Split("/");
                if (c[1] != lastEventId)
                {
                    lastEventId = c[1];
                    EventJsons.Add(lastEventId, new EventDialogueJson(lastEventId, c[0], Managers.PathManager));
                }

                EventJsons[lastEventId].AddLine(GetLineId(Text), Text);
            }
        }*/
        private void WriteOutAllFiles(string scriptFormat)
        {
            foreach (string key in Jsons.Keys)
            {
                Jsons[key].WriteOut();
            }

            foreach (string key in EventJsons.Keys)
            {
                EventJsons[key].WriteOut();
            }

            foreach (string key in Scripts.Keys)
            {
                Scripts[key].WriteOut(scriptFormat);
            }

            foreach (string key in EventScripts.Keys)
            {
                EventScripts[key].WriteOut(scriptFormat);
            }
        }
        private void GenerateCharacterDialogueFiles(string englishCharacterName, bool generateJson, bool generateScript, bool generateGossipDialogue)
        {
            string localizedCharacterName = LocalizationHelpers.GetLocalizedName(englishCharacterName);
            NPC character = Game1.getCharacterFromName(englishCharacterName);

            // Get all character dialogue
            Dictionary<DialogueType, List<LabelledDialogue>> characterDialogue
                = characterDialogueExtractor.GetAllDialogueForCharacter(character);
            if (generateGossipDialogue)
            {
                characterDialogue.Add(DialogueType.Gossip,
              gossipDialogueExtractor.GetAllPossibleGossipDialogues(character));
            }

            bool IsSkippedType(DialogueType type) => MarriageDialogueTypes.Contains(type) && !character.GetData().CanBeRomanced;

            // $q questions: the game plays the reply to the chosen answer from the
            // character's own dialogue file, so look reply keys up there. Replies shown
            // under their question — here or in an event script — aren't repeated on their
            // own (the json still gets every line).
            Dictionary<string, LabelledDialogue> dialogueByKey = new Dictionary<string, LabelledDialogue>();
            if (characterDialogue.TryGetValue(DialogueType.Dialogue, out var ownDialogue))
            {
                foreach (LabelledDialogue entry in ownDialogue)
                    if (entry != null) dialogueByKey.TryAdd(entry.OriginalKey, entry);
            }

            HashSet<string> repliesShownUnderQuestion = new HashSet<string>(characterDialogue
                .Where(kvp => !IsSkippedType(kvp.Key))
                .SelectMany(kvp => kvp.Value)
                .Where(entry => entry != null && entry.QuestionResponses.Count >= 2)
                .SelectMany(entry => entry.QuestionResponses)
                .Select(response => response.ReplyKey)
                .Where(dialogueByKey.ContainsKey));

            // Replies to $q questions asked by this character in events come from this same
            // dialogue file and are already shown under the question in the event script
            // (Pass 1 has extracted every event by now) — don't repeat them here either.
            foreach (EventDialogueQuestion question in eventDialogueExtractor.DialogueQuestionsByEvent.Values.SelectMany(questions => questions))
            {
                if (question.Speaker == englishCharacterName)
                    repliesShownUnderQuestion.UnionWith(question.RepliesByKey.Keys);
            }

            foreach (DialogueType type in characterDialogue.Keys)
            {
                if (IsSkippedType(type))
                {
                    continue;
                }

                if (generateScript)
                {
                    Scripts[localizedCharacterName].AppendSectionHeader(type + " lines!");
                }

                foreach (LabelledDialogue line in characterDialogue[type])
                {
                    if (line != null)
                    {
                        foreach (LabelledDialogueLine dialogue in line.Lines)
                        {
                            if (generateJson)
                            {
                                typeMap.TryGetValue(type, out string jsonTypeLabel);

                                if(SeparateJsonFiles.Contains(jsonTypeLabel))
                                {
                                    Jsons[jsonTypeLabel].AddLine(dialogue);
                                } else
                                {
                                    // AddLine already takes care of adding the dialogue type to the json correctly
                                    Jsons[englishCharacterName].AddLine(dialogue, jsonTypeLabel);
                                }

                                if (dialogue.IsContinuation)
                                {
                                    Jsons[englishCharacterName].AddContinuationDialogue(dialogue.LineId);
                                }
                            }
                        }

                        if (generateScript)
                        {
                            if (line.QuestionResponses.Count >= 2)
                            {
                                Scripts[localizedCharacterName].AddQuestion(
                                    line.Lines,
                                    line.QuestionResponses,
                                    replyKey => dialogueByKey.TryGetValue(replyKey, out LabelledDialogue reply)
                                        ? reply.Lines
                                        : new List<LabelledDialogueLine>());
                            }
                            else if (!(type == DialogueType.Dialogue && repliesShownUnderQuestion.Contains(line.OriginalKey)))
                            {
                                Scripts[localizedCharacterName].AddLines(line.Lines);
                            }
                        }
                    }
                }
            }
        }

        private void GenerateFestivalDialogueFiles(bool generateJson, bool generateScript)
        {
            string[] festivalDates = festivalDialogueExtractor.GetAllFestivalDates();
            bool isEnglish = LocalizationHelpers.GetCurrentLanguage() == LocalizedContentManager.LanguageCode.en.ToString();

            foreach (string date in festivalDates)
            {
                Dictionary<string, string> festivalDict = festivalDialogueExtractor.LoadGameFestivalData(date);
                Dictionary<string, string> englishFestivalDict = isEnglish
                    ? null
                    : festivalDialogueExtractor.LoadEnglishFestivalData(date);

                if (generateScript)
                {
                    foreach (Script script in Scripts.Values)
                    {
                        script.AppendSectionHeader(festivalDict["name"]);
                    }
                }

                if (generateJson)
                {
                    Jsons.Add(date, new DialogueJson(date, DialogueParentFolder.Festivals, Managers.PathManager));
                }

                foreach (string lineId in festivalDict.Keys)
                {
                    // Pure staging script with no dialogue of its own.
                    if (lineId == "set-up") continue;

                    string rawValue = festivalDict[lineId];
                    string englishRawValue = (englishFestivalDict != null && englishFestivalDict.TryGetValue(lineId, out string ev))
                        ? ev : null;

                    // Event-script values (e.g. "governorReaction6", "afterEggHunt") are
                    // detected structurally, not by key name, so fork targets like
                    // "AbbyWin" get the same treatment as top-level keys.
                    if (FestivalDialogueExtractor.IsEventScript(rawValue))
                    {
                        List<EventLine> eventLines =
                            eventDialogueExtractor.GetFestivalScriptDialogue(date, lineId, rawValue, englishRawValue);

                        if (generateJson)
                        {
                            foreach (EventLine eventLine in eventLines)
                                Jsons[date].AddMiscLine(eventLine.LineId, eventLine.Dialogue);
                        }

                        if (generateScript)
                        {
                            List<ScriptLine> scriptLines = eventLines
                                .Select(eventLine => new ScriptLine(eventLine.LineId, eventLine.Dialogue, eventLine.Character))
                                .ToList();

                            foreach (ScriptEntry entry in VariantGroupBuilder.Build(scriptLines))
                            {
                                if (Scripts.TryGetValue(LocalizationHelpers.GetLocalizedName(entry.Speaker), out Script targetScript))
                                    targetScript.AddEntry(entry);
                            }
                        }

                        continue;
                    }

                    if (DialogueHelpers.StringContainsCharacterToGenerate(lineId))
                    {
                        List<LabelledDialogueLine> parsedLines =
                            CharacterDialogueParser.ParseRawValueToLines(rawValue, lineId, DialogueType.Festival, englishRawValue);


                        foreach (LabelledDialogueLine dialogueLine in parsedLines)
                        {

                            dialogueLine.LineId = date + "_" + dialogueLine.LineId;
                        }

                        if (generateJson)
                        {
                            foreach (LabelledDialogueLine line in parsedLines)
                            {
                                Jsons[date].AddMiscLine(line);
                            }
                        }

                        if (generateScript)
                        {
                            string characterName = lineId.Split("_")[0];

                            if (Scripts.TryGetValue(LocalizationHelpers.GetLocalizedName(characterName), out Script targetScript))
                                targetScript.AddLines(parsedLines);
                        }
                    }
                }
            }
        }

        private void GenerateEventDialogueFiles(
            string englishName,
            bool generateJson,
            bool generateScript,
            Dictionary<string, List<EventLine>> reconciledEventLines)
        {
            string localizedName = LocalizationHelpers.GetLocalizedName(englishName);

            // Get events for character
            List<(string locationName, string eventId)> events
                = eventDialogueExtractor.GetAllEventsForCharacter(englishName);

            // We only need to order the events if it's a script
            if (generateScript)
            {
                events = eventDialogueExtractor.GetEventOrderForCharacter(englishName, events);
                EventScripts.TryGetValue(localizedName, out EventScript eventScript);
                eventScript.AppendSectionHeader("<3 HEART EVENTS <3");
            }

            // Iterate through events.
            foreach ((string locationName, string eventId) in events)
            {
                // Look up Pass 2 reconciled lines; fall back to a fresh extraction only
                // if somehow this event wasn't collected in Phase 1 (shouldn't happen).
                string key = locationName != null ? $"{locationName}/{eventId}" : null;
                List<EventLine> eventLines = (key != null && reconciledEventLines.TryGetValue(key, out var cached))
                    ? cached
                    : eventDialogueExtractor.GetEventDialogue(locationName, eventId) ?? new List<EventLine>();

                // Create event json — if the file's already been created: skip
                if (generateJson && locationName != null && !EventJsons.ContainsKey(key))
                {
                    EventJsons.Add(key, new EventDialogueJson(eventId, locationName, Managers.PathManager));
                    EventJsons.TryGetValue(key, out EventDialogueJson eventJson);
                    foreach (EventLine e in eventLines)
                        eventJson.AddLine(e.LineId, e.Dialogue);
                }

                // Add to event script for character — never skip
                if (generateScript)
                {
                    EventScripts.TryGetValue(localizedName, out EventScript eventScript);

                    if (locationName == null)
                    {
                        eventScript.AppendSectionHeader("GUEST EVENTS!!!");
                    }
                    else
                    {
                        eventScript.ChangeEvent(eventId);
                        eventScript.AddLocationChange(locationName);

                        // Recorded alongside these lines when they were extracted; Pass 2 only
                        // renames ids, so the line indexes still line up with eventLines.
                        eventDialogueExtractor.QuestionBranchesByEvent.TryGetValue(key, out var questionBranches);
                        eventDialogueExtractor.DialogueQuestionsByEvent.TryGetValue(key, out var dialogueQuestions);
                        eventDialogueExtractor.ForkPointsByEvent.TryGetValue(key, out var forks);
                        eventScript.AddEventLines(eventLines, questionBranches, dialogueQuestions, forks);
                    }
                }
            }
        }
    }
}
