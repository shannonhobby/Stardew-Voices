using StardewModdingAPI;
using StardewVoices.FileGenerator.Models;
using StardewVoices.Pathing;
using StardewVoices.Utils.GameDialogueExtraction;
using System.Collections.Generic;
using StardewVoices.Dialogue;
using StardewValley;
using StardewVoices.Utils.ParsingHelpers;
using Microsoft.VisualBasic;

namespace StardewVoices.FileGenerator
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

        public void GenerateFiles(bool generateJson, bool generateScript, string scriptFormat)
        {
            Managers.TestingManager.ValidationIsRunning = true;

            // Setup extractor class instances
            characterDialogueExtractor= new CharacterDialogueExtractor(Helper);
            eventDialogueExtractor = new EventDialogueExtractor(Helper);
            festivalDialogueExtractor = new FestivalDialogueExtractor(Helper);
            gossipDialogueExtractor = new GossipDialogueExtractor();

            // Events
            if (generateJson)
            {
                GenerateEventDialogueJsons(eventDialogueExtractor);
            }

            foreach(string characterName in Constants.supportedCharacters)
            {
                if(generateJson)
                {
                    Jsons.Add(characterName, new DialogueJson(characterName, DialogueParentFolder.Characters, Managers.PathManager));
                }
                if(generateScript)
                {
                    Scripts.Add(characterName, new Script(characterName));
                }

                GenerateCharacterDialogueFiles(characterName, generateJson, generateScript);

                GenerateGenericDialogueFiles();

                GenerateFestivalDialogueFiles(generateJson, generateScript);

            }
        }

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
        }

        private string GetLineId(string s)
        {
            // TODO
            // also have this live in DialogueJson.cs i just dont wanna forget
            return s;
        }

        private void GenerateCharacterDialogueFiles(string characterName, bool generateJson, bool generateScript)
        {
            NPC character = Game1.getCharacterFromName(characterName);

            // Get all character dialogue
            Dictionary<DialogueType, List<StardewValley.Dialogue>> characterDialogue
                = characterDialogueExtractor.GetAllDialogueForCharacter(character);

            foreach (DialogueType type in characterDialogue.Keys)
            {
                if (generateScript)
                {
                    Scripts[characterName].AppendSectionHeader(type + "lines");
                }

                foreach (StardewValley.Dialogue line in characterDialogue[type])
                {
                    if (line != null)
                    {
                        // Used to track spacing between lines.
                        bool firstInGroup = true;
                        foreach (DialogueLine dialogue in line.dialogues)
                        {
                            if (generateJson)
                            {
                                // AddLine already takes care of adding the dialogue type to the json correctly
                                Jsons[characterName].AddLine(GetLineId(dialogue.Text), dialogue.Text, type.ToString());
                            }

                            if (generateScript)
                            {
                                Scripts[characterName].AddLine(GetLineId(dialogue.Text), dialogue.Text, grouped: !firstInGroup);
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
                        script.AppendSectionHeader(festivalDict["name"] + "Festival!!!");
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
                        string[] parsedLines = DialogueParsingHelper.ParseDialogue(festivalDict[lineId]);
                        if (generateJson)
                        {
                            Jsons[date].AddMiscLine(GetLineId(lineId), festivalDict[lineId]);
                        }

                        if (generateScript)
                        {
                            string characterName = lineId.Split("_")[0];
                            bool firstInGroup = true;
                            foreach (string line in parsedLines)
                            {
                                Scripts[characterName].AddLine(GetLineId(lineId), line, firstInGroup);
                                firstInGroup = false;
                            }
                        }
                    }
                }
            }
        }



        private void GenerateGenericDialogueFiles(string characterName)
        {

        }

        private void GenerateEventDialogueFiles()
        {

        }

    }
}
