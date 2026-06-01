using StardewModdingAPI;
using StardewVoices.FileGenerator.Models;
using StardewVoices.Pathing;
using StardewVoices.Utils.GameDialogueExtraction;
using System.Collections.Generic;

namespace StardewVoices.FileGenerator
{
    internal class FileGenerator
    {
        private IModHelper Helper;
        private  ManagerHub Managers;
        private Dictionary<string, DialogueJson> Jsons = new Dictionary<string, DialogueJson>();
        private Dictionary<string, EventDialogueJson> EventJsons = new Dictionary<string, EventDialogueJson>();

        private Dictionary<string, Script> Scripts = new Dictionary<string, Script>();
        private Dictionary<string, EventScript> EventScripts = new Dictionary<string, EventScript>();

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
            CharacterDialogueExtractor characterDialogueExtractor= new CharacterDialogueExtractor(Helper);
            EventDialogueExtractor eventDialogueExtractor = new EventDialogueExtractor(Helper);
            FestivalDialogueExtractor festivalDialogueExtractor = new FestivalDialogueExtractor(Helper);
            GossipDialogueExtractor gossipDialogueExtractor = new GossipDialogueExtractor();

            // Events
            if (generateJson)
            {
                GenerateEventDialogueJsons(eventDialogueExtractor);
            }

            foreach(string characterName in Constants.supportedCharacters)
            {
                GenerateFilesForCharacter(characterName);
            }
        }

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

        private void GenerateFestivalDialogueJsons(FestivalDialogueExtractor festivalDialogueExtractor) { }

        private void GenerateGenericDialogueJsons()
        {

        }



        private void GenerateFilesForCharacter(string characterName)
        {
            // Get all character dialogue

            // Iterate through each line
                // Add to dialogue json
                // Add to script

            // Get All Festival Dialogue
                // Add to Script 
                // Add to appropriate Festival dialogue json 

            // Get All Generic Dialogue 
                // Add to Script 
                // (I've already added it  to the dialogue json if necessary

            // Events???
                // My original thought is that it would be easiest just to do the json events seperately
                // but I suppose I could iterate through the events the character shows up in
                // And then just get all lines for all characters this way?

                // THEN - iterate through each supported character in the scene and create their 
                // individual script
                    // Different method though?
        }

    }
}
