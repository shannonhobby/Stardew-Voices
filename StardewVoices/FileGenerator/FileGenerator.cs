using StardewModdingAPI;
using StardewVoices.FileGeneration.Models;
using StardewVoices.Pathing;
using StardewVoices.Utils.GameDialogueExtraction;
using System.Collections.Generic;
using StardewVoices.Dialogue;
using StardewValley;
using StardewVoices.Utils.ParsingHelpers;
using StardewVoices.Utils;

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
            {DialogueType.Gossip, "misc" },
            {DialogueType.SpeechBubble, "misc" },
            {DialogueType.Engagement, "EngagementDialogue" },
            {DialogueType.Extra, "ExtraDialogue" },
            {DialogueType.Rainy, "rainy" },
            {DialogueType.GenericMarriage, "Marriage Dialogue Generic" },
        };

        public void GenerateFiles(bool generateJson, bool generateScript, string scriptFormat)
        {
            Managers.TestingManager.ValidationIsRunning = true;

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

            foreach(string characterName in Constants.supportedCharacters)
            {
                if(generateJson)
                {
                    Jsons.Add(characterName, new DialogueJson(characterName, DialogueParentFolder.Characters, Managers.PathManager));
                }
                if(generateScript)
                {
                    Scripts.Add(characterName, new Script(characterName, Managers.PathManager));
                    EventScripts.Add(characterName, new EventScript(characterName, Managers.PathManager));

                }

                GenerateCharacterDialogueFiles(characterName, generateJson, generateScript);

                GenerateEventDialogueFiles(characterName, generateJson, generateScript);
            }


            GenerateFestivalDialogueFiles(generateJson, generateScript);


            WriteOutAllFiles();
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
        private void WriteOutAllFiles()
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
                Scripts[key].WriteOut();
            }

            foreach (string key in EventScripts.Keys)
            {
                EventScripts[key].WriteOut();
            }
        }
        private void GenerateCharacterDialogueFiles(string characterName, bool generateJson, bool generateScript)
        {
            NPC character = Game1.getCharacterFromName(characterName); 

            // Get all character dialogue
            Dictionary<DialogueType, List<LabelledDialogue>> characterDialogue
                = characterDialogueExtractor.GetAllDialogueForCharacter(character);

            foreach (DialogueType type in characterDialogue.Keys)
            {
                if (generateScript)
                {
                    Scripts[characterName].AppendSectionHeader(type + " lines!");
                }

                foreach (LabelledDialogue line in characterDialogue[type])
                {
                    if (line != null)
                    {
                        // Used to track spacing between lines.
                        bool firstInGroup = true;
                        foreach (LabelledDialogueLine dialogue in line.Lines)
                        {
                            if(type == DialogueType.Rainy)
                            {
                                dialogue.LineId = "rainy_" + dialogue.LineId;
                            }
                            if (generateJson)
                            {
                                typeMap.TryGetValue(type, out string jsonTypeLabel);

                                if(SeparateJsonFiles.Contains(jsonTypeLabel))
                                {
                                    Jsons[jsonTypeLabel].AddLine(dialogue);
                                } else
                                {
                                    // AddLine already takes care of adding the dialogue type to the json correctly
                                    Jsons[characterName].AddLine(dialogue, jsonTypeLabel);
                                }

                                if (dialogue.IsContinuation)
                                {
                                    Jsons[characterName].AddContinuationDialogue(dialogue.LineId);
                                }
                            }

                            if (generateScript)
                            {
                                Scripts[characterName].AddLine(dialogue, grouped: !firstInGroup);
                                firstInGroup = false;
                            }
                        }
                    }
                }
            }
        }

        private void GenerateFestivalDialogueFiles(bool generateJson, bool generateScript)
        {
            string[] festivalDates = festivalDialogueExtractor.GetAllFestivalDates();
            foreach (string date in festivalDates)
            {
                Dictionary<string, string> festivalDict = festivalDialogueExtractor.LoadGameFestivalData(date);
                
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

                foreach (string lineId in festivalDict.Keys )
                {
                    if (DialogueHelpers.StringContainsSupportedCharacter(lineId))
                    {
                        List<LabelledDialogueLine> parsedLines = 
                            CharacterDialogueParser.ParseRawValueToLines(festivalDict[lineId], lineId, DialogueType.Festival);
                        
                        foreach (LabelledDialogueLine dialogueLine in parsedLines)
                        {
                            if (dialogueLine.Contains("_"))
                            {

                            }
                        }
                        
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
                            bool firstInGroup = true;
                            foreach (LabelledDialogueLine line in parsedLines)
                            {
                                Scripts[characterName].AddLine(line, firstInGroup);
                                firstInGroup = false;
                            }
                        }
                    }
                }
            }
        }

        private void GenerateEventDialogueFiles
            (string characterName, 
            bool generateJson,
            bool generateScript)
        {
            // Get events for character
            List<(string locationName, string eventId)> events
                = eventDialogueExtractor.GetAllEventsForCharacter(characterName);


            // Iterate through events. 
            foreach ((string locationName, string eventId) in events)
            {
                List<EventLine> eventDict = eventDialogueExtractor.GetEventDialogue(locationName, eventId);
             
                // Create event json
                // if the files already been created: skippy poo
                if (generateJson)
                {
                    string eventLocationAndId = $"{locationName}/{eventId}";
                    if (!EventJsons.ContainsKey(eventLocationAndId))
                    {
                        EventJsons.Add(eventLocationAndId, new EventDialogueJson(eventId, locationName, Managers.PathManager));
                        EventJsons.TryGetValue(eventLocationAndId, out EventDialogueJson eventJson);
                        // TODO - refactor so only one for loop for script and events
                        foreach (EventLine e  in eventDict)
                        {
                            eventJson.AddLine(e.LineId, e.Dialogue);
                        }
                    }
                }

                // Add to event script for character
                // Never skippy poo if generating script
                if (generateScript)
                {
                    EventScripts.TryGetValue(characterName, out EventScript eventScript);
                    eventScript.ChangeEvent(eventId);
                    eventScript.AddLocationChange(locationName);
                    foreach (EventLine e in eventDict)
                    {
                        eventScript.AddEventLine(e);
                    }
                }
            }


        }

    }
}
