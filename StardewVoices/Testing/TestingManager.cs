using StardewModdingAPI;
using StardewValley.Locations;
using StardewValley;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using StardewVoices.Characters;
using StardewVoices.Dialogue;
using System.Linq;
using StardewVoices.Utils;

namespace StardewVoices.Testing
{
    public class TestingManager
    {
        private Boolean testingWorldLoaded { get; set; }
        private List<string> eventsToTest = new List<string>();
        private int eventsToTestIndex = 0;
        List<string> testDialogue = new List<string>();

        public bool ValidationIsRunning = false;

        private CharacterManager _characterManager { get;}
        private NPC Shane;

        public TestingManager(CharacterManager characterManager) {
            _characterManager = characterManager;
            testingWorldLoaded = false;
        }

        private string GetCurrentTestDialogue()
        {
            string currentTestDialogue = testDialogue[0];
            testDialogue.RemoveAt(0);
            return currentTestDialogue;

        }

        public Boolean IsTestingWorldLoaded()
        {
            return testingWorldLoaded;
        }

        public void createModTestingWorld()
        {
            if (!Context.IsWorldReady || testingWorldLoaded == true)
                return;

            if (Game1.player?.farmName?.Value == "Mod World")
            {
                foreach (string name in Constants.LoveInterests)
                {
                    NPC inGameNPC = _characterManager.GetCharacter(name).GetNPC();
                    FarmHouse farmHouse = Game1.getLocationFromName("FarmHouse") as FarmHouse;
                    Game1.warpCharacter(inGameNPC, "FarmHouse", farmHouse.getRandomOpenPointInHouse(new Random()));
                    inGameNPC.ignoreScheduleToday = true;
                    //ErrorLogger.Trace($"{name} current location: {inGameNPC.currentLocation}");
                }


                testingWorldLoaded = true;
            }
        }

        public void SetupTestDialogue()
        {
            if (IsTestingWorldLoaded())
            {
                // TODO - what's the regex for? is it only for the testing world loaded?
                if (_characterManager.GetCharacterNameRegex() == null && Game1.characterData != null)
                {
                    _characterManager.PopulateCharacterNamesList();
                    if (_characterManager.GetCharacterNameRegex().Length == 0)
                    {
                        ErrorLogger.Trace("Unable to populate character names :(");
                    }
                }

                if (_characterManager.GetCharacterNameRegex().Length > 0 && testDialogue.Count == 0)
                {
                    PopulateTestDialogueList();
                    if (testDialogue.Count == 0)
                    {
                        ErrorLogger.Trace("Unable to populate test dialogue list :(");
                    }
                }

                SetupEventsToTest();
            }
        }

        public Boolean IsTestDialogueSetup()
        {
            return testDialogue.Count > 0;
        }

        public void TriggerEvent()
        {

            ErrorLogger.Trace($"K Pressed, starting event");

            string currentEventToTest = this.eventsToTest[eventsToTestIndex];
            string eventId = currentEventToTest.Split("_")[0];
            string location;

            string[] currentEventSplitString = currentEventToTest.Split('_');
            if (currentEventSplitString.Length > 1)
            {
                location = String.Join("_", currentEventSplitString.Skip(1).ToArray());
            }
            else
            {
                location = currentEventSplitString[1];
            }




            GameLocation gameLocation = Game1.getLocationFromName(location);
            Game1.warpFarmer(location, 0, 0, true);
            bool validEvent = true;
            Game1.PlayEvent(eventId, gameLocation, out validEvent, false, false);

            if (!validEvent)
            {
                throw new Exception($"Invalid event: {eventId}");
            }

            this.eventsToTestIndex++;
        }

        public void ResetCharacterDialogue()
        {
            ErrorLogger.Trace($"L Pressed, resetting character dialogue");

            foreach (string name in Constants.supportedCharacters)
            {
                NPC inGameCharacter = _characterManager.GetCharacter(name).GetNPC();
                if (Game1.player != null && inGameCharacter != null)
                {
                    Game1.player.friendshipData.TryGetValue(inGameCharacter.Name, out var friendship);
                    Game1.player.resetFriendshipsForNewDay();
                    inGameCharacter.checkForNewCurrentDialogue(friendship.Points);

                    Game1.currentLocation?.ResetCharacterDialogues();
                }
            }
        }


        private void SetupEventsToTest()
        {


            // Shane
            // Night
            eventsToTest.Add("611944_Forest");
            eventsToTest.Add("3910674_AnimalShop");
            // Night
             eventsToTest.Add("3910975_Forest");
             eventsToTest.Add("2118991_Farm");
             eventsToTest.Add("3910974_AnimalShop");
             eventsToTest.Add("831125_Town");
             eventsToTest.Add("3900074_AnimalShop");
             eventsToTest.Add("2128292_Farm");
           eventsToTest.Add("9581348_BusStop");
              eventsToTest.Add("3917584_Town");
              eventsToTest.Add("3917585_Town");
           eventsToTest.Add("3917586_Town");



            // Haley
           eventsToTest.Add("11_HaleyHouse");
           eventsToTest.Add("12_HaleyHouse");
           eventsToTest.Add("13_Beach");
           eventsToTest.Add("14_Forest");
           eventsToTest.Add("15_HaleyHouse");
           eventsToTest.Add("6184643_Town");
           eventsToTest.Add("8675611_FarmHouse");
           eventsToTest.Add("6184644_Town");

            // Emily
           eventsToTest.Add("471942_HaleyHouse");
           eventsToTest.Add("463391_Town");
           eventsToTest.Add("917409_HaleyHouse");
           eventsToTest.Add("2123243_ManorHouse");
           eventsToTest.Add("2123343_Woods");
           eventsToTest.Add("3917600_Farm");
           eventsToTest.Add("3917601_FarmHouse");

            // Maru
           eventsToTest.Add("6_ScienceHouse");
           eventsToTest.Add("7_Hospital");
            // Night
           eventsToTest.Add("8_Mountain");
           eventsToTest.Add("9_ScienceHouse");
           eventsToTest.Add("10_ScienceHouse");
           eventsToTest.Add("3917666_FarmHouse");
            // Night
            eventsToTest.Add("5183338_Mountain");


            // Penny
           eventsToTest.Add("34_Town");
           eventsToTest.Add("35_Trailer");
           eventsToTest.Add("36_Trailer");
           eventsToTest.Add("181928_Forest");
           eventsToTest.Add("38_BathHouse_Pool");
           eventsToTest.Add("4325434_FarmHouse");
           eventsToTest.Add("4324303_FarmHouse");

            // Abigail
           eventsToTest.Add("1_SeedShop");
            // Night Rainy
           eventsToTest.Add("2_Mountain");
            // nIGHT
           eventsToTest.Add("4_Town");
            // nIGHT
           eventsToTest.Add("3_SeedShop");
           eventsToTest.Add("17_SeedShop");
           eventsToTest.Add("901756_Mine");
           eventsToTest.Add("6963327_Backwoods");




            // Leah

           eventsToTest.Add("50_LeahHouse");
           eventsToTest.Add("51_LeahHouse");
           eventsToTest.Add("52_Forest");
           eventsToTest.Add("55_Farm");
           eventsToTest.Add("53_Town");
           eventsToTest.Add("584059_LeahHouse");
           eventsToTest.Add("54_Forest");
           eventsToTest.Add("3911124_Farm");
           eventsToTest.Add("3091462_Forest");


            // Sebastian
              eventsToTest.Add("2794460_SebastianRoom");
              eventsToTest.Add("384883_Mountain");
            eventsToTest.Add("27_SebastianRoom");
            // Rain
           eventsToTest.Add("29_Beach");
            // NIght
           eventsToTest.Add("384882_Mountain");
            // NIght
           eventsToTest.Add("9333219_Mountain");
           eventsToTest.Add("9333220_FarmHouse");
            
            
            // Elliott
            eventsToTest.Add("39_ElliottHouse");
            eventsToTest.Add("40_Saloon");
            eventsToTest.Add("423502_ElliottHouse");
            eventsToTest.Add("1848481_ArchaeologyHouse");
            eventsToTest.Add("43_Beach");
            eventsToTest.Add("3912125_Farm");
            eventsToTest.Add("3912132_FarmHouse");


            // Sam
            eventsToTest.Add("44_SamHouse");
            eventsToTest.Add("733330_Beach");
            eventsToTest.Add("46_SamHouse");
            eventsToTest.Add("45_Town");
            eventsToTest.Add("47_Farm");
            eventsToTest.Add("4081148_BusStop");
            eventsToTest.Add("233104_Town");
            eventsToTest.Add("3918600_FarmHouse");
            eventsToTest.Add("3918601_FarmHouse");
            eventsToTest.Add("3918602_FarmHouse");
            eventsToTest.Add("3918603_FarmHouse");



            // Harvey
            eventsToTest.Add("56_JoshHouse");
            eventsToTest.Add("57_Hospital");
            eventsToTest.Add("58_SeedShop");
            eventsToTest.Add("571102_Hospital");
            eventsToTest.Add("528052_Railroad");
            eventsToTest.Add("3917626_FarmHouse");


            // Alex
            eventsToTest.Add("20_Beach");
            eventsToTest.Add("2481135_Town");
            eventsToTest.Add("21_JoshHouse");
            eventsToTest.Add("2119820_JoshHouse");
            eventsToTest.Add("288847_Beach");
            eventsToTest.Add("911526_Saloon");
            eventsToTest.Add("3917587_Farm");
            eventsToTest.Add("3917590_Saloon");


              // Cheating Events
              // Lads
              eventsToTest.Add("195099_Saloon");
              eventsToTest.Add("195013_Saloon");
            

              // Ladies
              eventsToTest.Add("195019_HaleyHouse");
              eventsToTest.Add("195012_HaleyHouse");
          

        }


        private void PopulateTestDialogueList()
        {
            List<Dictionary<string, string[]>> characterDialogueDictionaries = new List<Dictionary<string, string[]>>();
            List<string> dialogueDictionaryNames = new List<string>();

            DialogueDictionary shaneDictionary;

            try
            {
                shaneDictionary = _characterManager.GetCharacter("Shane").GetDialogueDictionary();

            }
            catch (Exception ex)
            {
                ErrorLogger.Trace($"Could not find dialogue for Shane in testing. {ex}");
                return;
            }


            characterDialogueDictionaries.Add(shaneDictionary.GetType("dialogue"));
            dialogueDictionaryNames.Add("Dialogue-");

            characterDialogueDictionaries.Add(shaneDictionary.GetType("engagement"));
            dialogueDictionaryNames.Add("Engagement_Dialogue-");

            characterDialogueDictionaries.Add(shaneDictionary.GetType("generic_marriage"));
            dialogueDictionaryNames.Add("Generic_Marriage-");

            characterDialogueDictionaries.Add(shaneDictionary.GetType("rainy"));
            dialogueDictionaryNames.Add("Rainy-");

            characterDialogueDictionaries.Add(shaneDictionary.GetType("marriage_dialogue"));
            dialogueDictionaryNames.Add("Marriage_Dialogue-");

            characterDialogueDictionaries.Add(shaneDictionary.GetType("misc"));
            dialogueDictionaryNames.Add("Misc-");

            characterDialogueDictionaries.Add(shaneDictionary.GetType("schedule"));
            dialogueDictionaryNames.Add("Schedule-");

            characterDialogueDictionaries.Add(shaneDictionary.GetType("extra"));
            dialogueDictionaryNames.Add("Extra-");

            characterDialogueDictionaries.Add(shaneDictionary.GetType("gift_tastes"));
            dialogueDictionaryNames.Add("Gift_Tastes-");

            for (int i = 0; i < characterDialogueDictionaries.Count; i++)
            {
                foreach (string key in characterDialogueDictionaries[i].Keys)
                {
                    bool confirmedCharacterDialogue = true;

                    // Generic dialogue JSON contains multiple different characters' lines, make sure there is either 
                    // no name specified, or if there is it matches the character we want to test.
                    if (dialogueDictionaryNames[i] == "Generic_Marriage-"
                        && dialogueDictionaryNames[i] == "Extra-"
                        && dialogueDictionaryNames[i] == "Engagement-")
                    {
                        confirmedCharacterDialogue = false;
                        foreach (string value in characterDialogueDictionaries[i][key])
                        {

                            Match characterNameMatch = Regex.Match(value, _characterManager.GetCharacterNameRegex(), RegexOptions.IgnoreCase);
                            if ((characterNameMatch.Success && value.Contains("Shane")) || !characterNameMatch.Success)
                            {
                                confirmedCharacterDialogue = true;
                                break;
                            }
                        }
                    }

                    if (confirmedCharacterDialogue)
                    {
                        string tempKey = key;
                        while (tempKey.Contains('{'))
                        {
                            Match m = Regex.Match(tempKey, @"{.*?}", RegexOptions.IgnoreCase);

                            if (m.Success)
                            {
                                tempKey = tempKey.Replace(m.Value, "placeholder");
                            }
                        }

                        testDialogue.Add(tempKey);
                    }
                }
            }
        }

    }
}