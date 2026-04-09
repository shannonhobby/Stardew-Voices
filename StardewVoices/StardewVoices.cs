using System;
using System.IO;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Audio;
using Newtonsoft.Json;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewModdingAPI.Framework.StateTracking;
using StardewModdingAPI.Framework.Input;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Locations;
using System.Text.Json.Serialization;
using System.Linq;
using System.Text.RegularExpressions;
using System.ComponentModel.DataAnnotations;
using StardewValley.Minigames;
using Microsoft.Xna.Framework.Input;
using StardewValley.Tools;
using System.Diagnostics.Tracing;
using System.Reflection;
using System.Linq.Expressions;
using StardewValley.Audio;


namespace StardewVoices
{

    public class Character
    {
        public string Name { get; }
        public SoundBank SoundBank { get; set; }
        private WaveBank WaveBank { get; set; }
        public NPC InGameNPC { get; set; }
        public Dictionary<string, Dictionary<string, string[]>> DialogueDictionary { get; set; }
        public Character(string name) {
            this.Name = name;
        }

        public void SetupAudioBanks(AudioEngine audioEngine, string audioPath)
        {
            this.WaveBank = new WaveBank(audioEngine, Path.Combine(audioPath, this.Name +  ".xwb"));
            this.SoundBank = new SoundBank(audioEngine, Path.Combine(audioPath, this.Name + ".xsb"));
        }


    }

    /// <summary>The mod entry point.</summary>
    public class StardewVoices : Mod
    {

        private string modPath;

        private string audioPath;
        private string inverseDialogueFolderPath;
        private string eventDialogueFolderPath;
        private string characterDialogueFolderPath;
        private string festivalDialogueFolderPath;
        private string festivalDatesJsonPath;
        private string genericDialogueFolderPath;
        private string engagementDialogueJsonPath;
        private string extraDialogueJsonPath;
        private string genericMarriageDialogueJsonPath;
        private string rainyDialogueJsonPath;


        private AudioEngine audioEngine;
        private AudioCategory dialogueCategory;

        private Cue currentCue;
        private Cue lastCue;
        private string currentLineId;
        private string lastLineId;

        private string currentLanguage;


        /// <summary>The in-game dialogue line detected on the last update tick.</summary>
        private Stack<Dialogue> LastDialogue;
        /// <summary>The in-game dialogue line detected on the last update tick.</summary>
        private string LastDialogueBoxString = "";

        /// <summary>The in-game event detected on the last update tick.</summary>
        private Event lastEvent = null;
        private Dictionary<string, string> currentEventDict = null;
        private Dictionary<string, string> eventResponseDict = null;

        /// <summary>The in-game location detected on the last update tick.</summary>
        private GameLocation lastLocation = null;
        /// <summary>The in-game festival detected on the last update tick.</summary>
        private string lastFestival = null;
        private Dictionary<string, string[]> currentFestivalDict = null;


        private List<string> eventsToTest = new List<string>();
        private Dictionary<int, string> eventCommandDict = new Dictionary<int, string>();
        private int eventsToTestIndex = 0;

        private int lastEventCommand;
        private bool eventCommandsHaveBeenLogged = false;
        private List<Dictionary<string, string>> eventsToday = new List<Dictionary<string, string>>();

        // Mod Testing world stuff
        private bool testingWorldLoaded = false;
        private NPC Shane;
        private Farmer player;

        private Dictionary<string, string> festivalNameToDateDict = new Dictionary<string, string>();

        private bool localizedContentLoaded = false;

        List<string> continuationDialogue = new List<string>();
        Dictionary<string, string> dialogueWithRandomNounOrAdj = new Dictionary<string, string>();
        bool isReplacementDialogueBox = false;

        private WorldDate lastDate = null;

        List<string> testDialogue = new List<string>();

        string characterNameRegex = "";

        public ModConfig Config = null;

        public IModHelper helper = null;

        private bool currentlyTalking;

        private List<string> supportedCharacters = new List<string>();
        private Dictionary<string, Character> characterDictionary = new Dictionary<string, Character>();
        private static Dictionary<string, Dictionary<string, string[]>> genericDialogueDictionariesDictionary = new Dictionary<string, Dictionary<string, string[]>>();

        /*********
        ** Public methods
        *********/
        /// <summary>The mod entry point, called after the mod is first loaded.</summary>
        /// <param name="helper">Provides simplified APIs for writing mods.</param>
        public override void Entry(IModHelper helper)
        {
            modPath = helper.DirectoryPath;
            string audioFolder = Path.Combine(modPath, "Audio");

            if(modPath.Contains("Resources"))
            {
                modPath.Replace("Resources", "MacOS");
            }


            audioPath = Path.GetRelativePath(Path.Combine(Path.Combine(modPath, ".."), ".."), audioFolder);
   

            this.helper = helper;

            this.Config = helper.ReadConfig<ModConfig>();

            var gmcm = helper.ModRegistry.GetApi<IGenericModConfigMenuApi>(
                "spacechase0.GenericModConfigMenu");


            if (gmcm == null)
                return;

            helper.Events.GameLoop.UpdateTicked += this.OnUpdateTicked;
            helper.Events.Input.ButtonPressed += this.OnButtonPressed;


            inverseDialogueFolderPath = Path.Combine(modPath, "Dialogue Jsons");

            
            currentLanguage = this.Config.Language;
     

            if (currentLanguage != LocalizedContentManager.LanguageCode.en.ToString())
            {
                inverseDialogueFolderPath = Path.Combine(inverseDialogueFolderPath, currentLanguage);
            }
            
            
            eventDialogueFolderPath = Path.Combine(inverseDialogueFolderPath, "Events");

            characterDialogueFolderPath = Path.Combine(inverseDialogueFolderPath, "Characters");

            festivalDialogueFolderPath = Path.Combine(inverseDialogueFolderPath, "Festivals");
            

            festivalDatesJsonPath = Path.Combine(festivalDialogueFolderPath, $"FestivalDates.{GetLanguageJsonExtension(currentLanguage)}");



            genericDialogueFolderPath = Path.Combine(inverseDialogueFolderPath, "Generic");
            engagementDialogueJsonPath = Path.Combine(genericDialogueFolderPath, "EngagementDialogue.json");
            extraDialogueJsonPath = Path.Combine(genericDialogueFolderPath, "ExtraDialogue.json");
            genericMarriageDialogueJsonPath = Path.Combine(genericDialogueFolderPath, "Marriage Dialogue Generic.json");
            rainyDialogueJsonPath = Path.Combine(genericDialogueFolderPath, "rainy.json");


            this.supportedCharacters.Add("Shane");
            this.supportedCharacters.Add("Haley");
            this.supportedCharacters.Add("Alex");
            this.supportedCharacters.Add("Penny");
            this.supportedCharacters.Add("Sam");
            this.supportedCharacters.Add("Emily");
            this.supportedCharacters.Add("Maru");
            this.supportedCharacters.Add("Harvey");
            this.supportedCharacters.Add("Elliott");
            this.supportedCharacters.Add("Leah");
            this.supportedCharacters.Add("Sebastian");
            this.supportedCharacters.Add("Abigail");



            this.setupCharacterDictionary();

            this.setupCharacterSoundBanks();

            this.setupFestivalDictionary();

            this.setupDialogueDictionaries();

            this.setupContinuationDialogue();



            // Shane
            // Night
            // this.eventsToTest.Add("611944_Forest");
            //   this.eventsToTest.Add("3910674_AnimalShop");
            // Night
            //    this.eventsToTest.Add("3910975_Forest");
            //    this.eventsToTest.Add("2118991_Farm");
            //    this.eventsToTest.Add("3910974_AnimalShop");
            //    this.eventsToTest.Add("831125_Town");
            //    this.eventsToTest.Add("3900074_AnimalShop");
            //    this.eventsToTest.Add("2128292_Farm");
            // this.eventsToTest.Add("9581348_BusStop");
            //     this.eventsToTest.Add("3917584_Town");
            //     this.eventsToTest.Add("3917585_Town");
            // this.eventsToTest.Add("3917586_Town");



            // Haley
           //   this.eventsToTest.Add("11_HaleyHouse");
            // this.eventsToTest.Add("12_HaleyHouse");
            // this.eventsToTest.Add("13_Beach");
            //  this.eventsToTest.Add("14_Forest");
            //  this.eventsToTest.Add("15_HaleyHouse");
            //  this.eventsToTest.Add("6184643_Town");
            //  this.eventsToTest.Add("8675611_FarmHouse");
            //  this.eventsToTest.Add("6184644_Town");

            // Emily
            //    this.eventsToTest.Add("471942_HaleyHouse");
            //    this.eventsToTest.Add("463391_Town");
            //    this.eventsToTest.Add("917409_HaleyHouse");
            //    this.eventsToTest.Add("2123243_ManorHouse");
            //     this.eventsToTest.Add("2123343_Woods");
            //     this.eventsToTest.Add("3917600_Farm");
            //    this.eventsToTest.Add("3917601_FarmHouse");

            // Maru
            // this.eventsToTest.Add("6_ScienceHouse");
            // this.eventsToTest.Add("7_Hospital");
            // Night
            //   this.eventsToTest.Add("8_Mountain");
            //this.eventsToTest.Add("9_ScienceHouse");
            //this.eventsToTest.Add("10_ScienceHouse");
            //this.eventsToTest.Add("3917666_FarmHouse");
            // Night
            //athis.eventsToTest.Add("5183338_Mountain");


            // Penny
            // this.eventsToTest.Add("34_Town");
            //  this.eventsToTest.Add("35_Trailer");
            //    this.eventsToTest.Add("36_Trailer");
            //    this.eventsToTest.Add("181928_Forest");
            //  this.eventsToTest.Add("38_BathHouse_Pool");
            //this.eventsToTest.Add("4325434_FarmHouse");
            //this.eventsToTest.Add("4324303_FarmHouse");

            // Abigail
            //this.eventsToTest.Add("1_SeedShop");
            // Night Rainy
            // this.eventsToTest.Add("2_Mountain");
            // nIGHT
            // this.eventsToTest.Add("4_Town");
            // nIGHT
            //  this.eventsToTest.Add("3_SeedShop");
            //  this.eventsToTest.Add("17_SeedShop");
            //  this.eventsToTest.Add("901756_Mine");
            //  this.eventsToTest.Add("6963327_Backwoods");




            // Leah

            //    this.eventsToTest.Add("50_LeahHouse");
            //    this.eventsToTest.Add("51_LeahHouse");
            //    this.eventsToTest.Add("52_Forest");
            //   this.eventsToTest.Add("55_Farm");
            //   this.eventsToTest.Add("53_Town");
            //  this.eventsToTest.Add("584059_LeahHouse");
            //  this.eventsToTest.Add("54_Forest");
            // this.eventsToTest.Add("3911124_Farm");
            //  this.eventsToTest.Add("3091462_Forest");


            // Sebastian
            //   this.eventsToTest.Add("2794460_SebastianRoom");
            //  this.eventsToTest.Add("384883_Mountain");
            //   this.eventsToTest.Add("27_SebastianRoom");
            // Rain
            //this.eventsToTest.Add("29_Beach");
            // NIght
            // this.eventsToTest.Add("384882_Mountain");
            // NIght
            // this.eventsToTest.Add("9333219_Mountain");
            //this.eventsToTest.Add("9333220_FarmHouse");
/*
            // Elliott
            this.eventsToTest.Add("39_ElliottHouse");
            this.eventsToTest.Add("40_Saloon");
            this.eventsToTest.Add("423502_ElliottHouse");
            this.eventsToTest.Add("1848481_ArchaeologyHouse");
            this.eventsToTest.Add("43_Beach");
            this.eventsToTest.Add("3912125_Farm");
            this.eventsToTest.Add("3912132_FarmHouse");


            // Sam
            this.eventsToTest.Add("44_SamHouse");
            this.eventsToTest.Add("733330_Beach");
            this.eventsToTest.Add("46_SamHouse");
            this.eventsToTest.Add("45_Town");
            this.eventsToTest.Add("47_Farm");
            this.eventsToTest.Add("4081148_BusStop");
            this.eventsToTest.Add("233104_Town");
            this.eventsToTest.Add("3918600_FarmHouse");
            this.eventsToTest.Add("3918601_FarmHouse");
            this.eventsToTest.Add("3918602_FarmHouse");
            this.eventsToTest.Add("3918603_FarmHouse");



            // Harvey
            this.eventsToTest.Add("56_JoshHouse");
            this.eventsToTest.Add("57_Hospital");
            this.eventsToTest.Add("58_SeedShop");
            this.eventsToTest.Add("571102_Hospital");
            this.eventsToTest.Add("528052_Railroad");
            this.eventsToTest.Add("3917626_FarmHouse");


            // Alex
            this.eventsToTest.Add("20_Beach");
            this.eventsToTest.Add("2481135_Town");
            this.eventsToTest.Add("21_JoshHouse");
            this.eventsToTest.Add("2119820_JoshHouse");
            this.eventsToTest.Add("288847_Beach");
            this.eventsToTest.Add("911526_Saloon");
            this.eventsToTest.Add("3917587_Farm");
            this.eventsToTest.Add("3917590_Saloon");
      
            */
            /**  

              // Cheating Events
              // Lads
              this.eventsToTest.Add("195099_Saloon");
              this.eventsToTest.Add("195013_Saloon");
            

              // Ladies
              this.eventsToTest.Add("195019_HaleyHouse");
              this.eventsToTest.Add("195012_HaleyHouse");
       


          
            */







            dialogueWithRandomNounOrAdj.Add("Last night I dreamt that my left hand had turned into a gigantic %noun. Does that mean anything?", "Dialogue-fall_Thu_chance_1");
            dialogueWithRandomNounOrAdj.Add("I did, but all I remember is that it had something to do with a %adj %noun. Weird.", "Generic Marriage Dialogue-Indoor_Day_3_sv1");

        }


        /*********
        ** Private methods
        *********/
        /// <summary>The method invoked when the game updates its state.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnUpdateTicked(object sender, EventArgs e)
        {
            //this.createModTestingWorld();

            this.UpdateCurrentDialogue();

            this.UpdateLastLocationAndEvent();

            if (this.dialogueCategory != null)
            {
                this.dialogueCategory.SetVolume(this.Config.DialogueVolume);
            }

            if (characterNameRegex.Length == 0 && Game1.characterData != null && this.testingWorldLoaded)
            {
                PopulateCharacterNamesList();
                if (characterNameRegex.Length == 0)
                {
                    Monitor.Log("Unable to populate character names :(", LogLevel.Debug);
                }
            }

            if (characterNameRegex.Length > 0 && testDialogue.Count == 0 && this.testingWorldLoaded)
            {
                PopulateTestDialogueList();
                if (testDialogue.Count == 0)
                {
                    Monitor.Log("Unable to populate test dialogue list :(", LogLevel.Debug);
                }
            }

            if (Game1.Date != null && Game1.Date != this.lastDate)
            {
                this.lastDate = Game1.Date;

                // Reset todays events
                this.eventsToday = new List<Dictionary<string, string>>();

            }
            /**
            foreach (string name in supportedCharacters)
            {
                NPC inGameCharacter = characterDictionary[name].InGameNPC;
                if (inGameCharacter != null && inGameCharacter.currentLocation?.Name != "FarmHouse" && Game1.getLocationFromName("FarmHouse") != null && this.testingWorldLoaded)
                {
                    FarmHouse farmHouse = Game1.getLocationFromName("FarmHouse") as FarmHouse;
                    Game1.warpCharacter(inGameCharacter, "FarmHouse", farmHouse.getRandomOpenPointInHouse(new Random()));
                    inGameCharacter.ignoreScheduleToday = true;
                    this.Monitor.Log($"{name} current location: {inGameCharacter.currentLocation}", LogLevel.Debug);
                }
            }*/



        }

        /// <summary>Raised after the player presses a button on the keyboard, controller, or mouse.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void OnButtonPressed(object sender, ButtonPressedEventArgs e)
        {
            // ignore if player hasn't loaded a save yet
            if (!Context.IsWorldReady)
                return;
            if (this.testingWorldLoaded && e.Button == SButton.K)
            {
                this.Monitor.Log($"K Pressed, starting event", LogLevel.Debug);

                string currentEventToTest = this.eventsToTest[eventsToTestIndex];
                string location;

                string[] currentEventSplitString = currentEventToTest.Split('_');
                if(currentEventSplitString.Length > 2)
                {
                    location = String.Join("_", currentEventSplitString.Skip(1).ToArray());
                } else
                {
                    location = currentEventSplitString[1];
                }


                this.triggerEvent(currentEventToTest.Split("_")[0], currentEventToTest.Split("_")[1]);
                this.eventsToTestIndex++;
            }

            if (this.testingWorldLoaded && e.Button == SButton.L)
            {
                this.Monitor.Log($"L Pressed, resetting character dialogue", LogLevel.Debug);

                foreach (string name in supportedCharacters)
                {
                    NPC inGameCharacter = characterDictionary[name].InGameNPC;
                    if (Game1.player != null && inGameCharacter != null)
                    {
                        Game1.player.friendshipData.TryGetValue(inGameCharacter.Name, out var friendship);
                        Game1.player.resetFriendshipsForNewDay();
                        inGameCharacter.checkForNewCurrentDialogue(friendship.Points);

                        Game1.currentLocation?.ResetCharacterDialogues();
                    }
                }

            }

            if (this.testingWorldLoaded && e.Button == SButton.B)
            {
                this.Monitor.Log($"B Pressed", LogLevel.Debug);
                if (Game1.activeClickableMenu != null)
                {
                    Game1.activeClickableMenu = null;
                    currentlyTalking = false;
                }

                // TODO: solution for testing multiple character at a time?
                //  if (Game1.player != null && this.Shane != null && Game1.activeClickableMenu == null && testDialogue.Count > 0)
                // {
                //     Game1.currentSpeaker = this.Shane;
                //     Shane.CurrentDialogue.Push(new Dialogue(Shane, "translationKey", GetCurrentTestDialogue()));
                //     Game1.drawDialogue(Shane);
                //  }
            }

         /**   if (this.Config.OpenMenuKey.JustPressed())
            {
                if (Game1.activeClickableMenu is Menu menu)
                {
                    menu.ExitIfValid();
                }
                else
                {
                    this.OpenVoicesMenu();
                }

            }*/
        }

        private string GetCurrentTestDialogue()
        {
            string currentTestDialogue = testDialogue[0];
            testDialogue.RemoveAt(0);
            return currentTestDialogue;

        }

        private void UpdateLastLocationAndEvent()
        {
            // Deal with events!

            this.lastLocation = Game1.currentLocation;

            if (Game1.CurrentEvent == null && this.lastEvent != null && this.lastLocation != null)
            {
                this.currentEventDict = null;
                this.eventResponseDict = null;
                this.lastEvent = null;
                this.eventCommandsHaveBeenLogged = false;
            }

            if (Game1.CurrentEvent != null && !Game1.CurrentEvent.isFestival && this.lastEvent == null)
            {
                string locationFilePath = Path.Combine(eventDialogueFolderPath, this.lastLocation.Name);
                string lastEventId = Game1.CurrentEvent.id;

                if (lastEventId != null && lastEventId.Length > 0)
                {
                    string filePath = Path.Combine(locationFilePath, lastEventId + ".json");

                    try
                    {
                        string jsonString = File.ReadAllText(filePath);
                        this.currentEventDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonString);
                        this.eventsToday.Add(this.currentEventDict);

                    }
                    catch (Exception ex)
                    {
                            this.Monitor.Log($"Event not found", LogLevel.Debug);
                            this.Monitor.Log($"Attempted to read from filePath: {filePath}", LogLevel.Debug);

                    }

                }

            }

            // Festivals are just special events, so get the current festival from the current event.
            if (Game1.CurrentEvent != null && Game1.CurrentEvent.isFestival && this.lastFestival == null)
            {

                this.lastFestival = Game1.CurrentEvent.FestivalName;

                try
                {
                    // Load dialogue dictionary for that festival
                    if (!Directory.Exists(festivalDialogueFolderPath))
                        throw new Exception("Festival dialogue folder could not be found");

                    string filePath = Path.Combine(festivalDialogueFolderPath, (string)festivalNameToDateDict[this.lastFestival] + $".{GetLanguageJsonExtension(currentLanguage)}");

                    string jsonString = File.ReadAllText(filePath);
                    this.currentEventDict = null;
                    this.currentFestivalDict = JsonConvert.DeserializeObject<Dictionary<string, string[]>>(jsonString);

                } catch (Exception ex)
                {
                    this.Monitor.Log($"Couldn't find festival: {this.lastFestival}, {ex}", LogLevel.Debug);
                }


            }

            this.lastEvent = Game1.CurrentEvent;

            // Unset Dialogue Dictionary when necessary
            if ((this.lastEvent == null || this.lastEvent.isFestival) && this.currentEventDict != null)
            {
                this.currentEventDict = null;
            }


            // Remember to unset the festival once the event changes!!
            if ((this.lastEvent == null || !this.lastEvent.isFestival) && this.lastFestival != null)
            {
                this.lastFestival = null;
                this.currentFestivalDict = null;
            }

        }

        private Cue getCueFromDialogueId(string lineId, string characterName)
        {
            try
            {
                Character character = this.characterDictionary[characterName];
                SoundBank characterSoundBank = character.SoundBank;

                // TODO: Check if i actually do need the dialogue context?
                return characterSoundBank.GetCue(lineId);

            } catch (Exception error)
            {
                this.Monitor.Log($"Error getting cue for {characterName}: {lineId} Sound Bank: {error}", LogLevel.Debug);
                return null;
            }

        }


        private void CheckForTextAboveHead()
        {
            if (!eventCommandsHaveBeenLogged && Game1.CurrentEvent != null)
            {
                int count = 0;
                eventCommandDict = new Dictionary<int, string>();
                foreach (string eventCommand in Game1.CurrentEvent.eventCommands)
                {
                    // Sometimes event commands have multiple commands within them which is fun and exciting so I have to split it up 
                    if (eventCommand.Contains("\\"))
                    {
                        string[] splitEventCommand = eventCommand.Split('\\');
                        foreach (string command in splitEventCommand)
                        {
                            eventCommandDict.Add(count, command);
                            count++;
                        }
                    } else
                    {
                        eventCommandDict.Add(count, eventCommand);
                        count++;
                    }

               
                }
                eventCommandsHaveBeenLogged = true;
            }


            if (Game1.CurrentEvent != null && this.lastEventCommand != Game1.CurrentEvent.currentCommand)
            {

                string currentCommand = "";
                if (Game1.CurrentEvent.currentCommand - this.lastEventCommand > 1)
                {
                    for (int i = 0; i <= Game1.CurrentEvent.currentCommand - this.lastEventCommand; i++)
                    {
                        try
                        {
                            currentCommand = eventCommandDict[this.lastEventCommand + i];
                        }
                        catch (Exception e)
                        {
                            continue;
                        }

                        if (currentCommand.Contains("textAboveHead") && (StringContainsSupportedCharacter(currentCommand) || currentCommand.Contains("ClothesTherapyCharacters")))
                        {
                            break;
                        }

                    }
                }
                else
                {
                    try
                    {
                        currentCommand = eventCommandDict[Game1.CurrentEvent.currentCommand];
                    } catch (Exception e)
                    {
                        this.lastEventCommand = Game1.CurrentEvent.currentCommand;
                        return;
                    }
                }

                if (currentCommand.Contains("textAboveHead") && (StringContainsSupportedCharacter(currentCommand) || currentCommand.Contains("ClothesTherapyCharacters")))
                {
                    Regex rx = new Regex(@"""(.*?)""");
                    string commandDialogue;

                    string currentSpeaker = GetSupportedCharacterFromString(currentCommand);

                    if (currentSpeaker.Contains("Could not find"))
                    {
                       // this.Monitor.Log($"Could not find speaker for textAboveHead command: {currentCommand}", LogLevel.Debug);
                        this.lastEventCommand = Game1.CurrentEvent.currentCommand;
                        return;
                    }

                    try
                    {
                        Match m = rx.Match(currentCommand);
                        commandDialogue = m.Value.Replace("\"", "");
                    }
                    catch (Exception e)
                    {
                      //  Monitor.Log($"Error extracting dialogue from textAboveHead event command: {e}", LogLevel.Debug);
                        this.lastEventCommand = Game1.CurrentEvent.currentCommand;
                        return;
                    }

                    
                    if (currentCommand.Contains("ClothesTherapyCharacters") && commandDialogue != "It's sweaty in here..." && currentSpeaker == "Abigail")
                    {
                        this.lastEventCommand = Game1.CurrentEvent.currentCommand;
                        return;
                    }

                    if (this.currentEventDict != null)
                    {

                        string textAboveHeadLineId = getIdFromDialogueString(this.formatDialogueString(commandDialogue), currentSpeaker);

                        if (textAboveHeadLineId.Contains("not found"))
                        {
                           // this.Monitor.Log(textAboveHeadLineId, LogLevel.Debug);
                            this.lastEventCommand = Game1.CurrentEvent.currentCommand;
                            return;
                        }

                        this.currentCue = this.getCueFromDialogueId(textAboveHeadLineId, currentSpeaker);

                        if (this.currentCue == null)
                        {
                         //   this.Monitor.Log($"Cue could not be found for textAboveHead line: {textAboveHeadLineId}", LogLevel.Debug);
                            this.lastEventCommand = Game1.CurrentEvent.currentCommand;
                            return;
                        }
                        else
                        {
                            if (this.lastCue != null && this.lastCue.IsPlaying)
                            {
                                this.lastCue.Stop(AudioStopOptions.Immediate);
                            }

                            this.currentCue.Play();

                        }
                    }
                }

                this.lastEventCommand = Game1.CurrentEvent.currentCommand;
            }
        }

        private bool DialogueHasContinuation(string dialogueId)
        {
            foreach (string id in this.continuationDialogue)
            {
                if (dialogueId == id)
                {
                    return true;
                }
            }

            return false;
        }

        private void UpdateCurrentDialogue()
        {
            IClickableMenu currentActiveClickableMenu = Game1.activeClickableMenu;
            string currentDialogueBoxString = "";

            if (
                currentActiveClickableMenu != null
                && currentActiveClickableMenu.GetType() == typeof(DialogueBox)
               )
            {
                currentDialogueBoxString = ((DialogueBox)currentActiveClickableMenu).getCurrentString();
            }

            this.CheckForTextAboveHead();


            // If the last dialogue box string stored doesn't equal the current dialogue box string that means there's been a change! (yay!)
            if (!this.LastDialogueBoxString.Equals(currentDialogueBoxString))
            {
                // If dialogue has gone from non-empty to empty stop the audio cues.
                if (this.LastDialogueBoxString.Length > 0 && (currentActiveClickableMenu == null || currentActiveClickableMenu.GetType() != typeof(DialogueBox) || currentDialogueBoxString.Length == 0))
                {
                    this.LastDialogueBoxString = "";
                    currentlyTalking = false;


                    if (this.currentCue != null)
                    {
                        this.currentCue.Stop(AudioStopOptions.Immediate);
                        this.currentCue = null;
                    }

                    if (this.lastCue != null && this.lastCue.IsPlaying)
                    {
                        this.lastCue.Stop(AudioStopOptions.Immediate);
                    }
                }


                // If dialogue has gone from empty to not empty start the audio cues!
                // Also only check supported characters for performance purposes.
                if (Game1.currentSpeaker?.getName() != null && StringContainsSupportedCharacter(Game1.currentSpeaker?.getName())
                    && (currentActiveClickableMenu != null && currentActiveClickableMenu.GetType() == typeof(DialogueBox)) && currentDialogueBoxString.Length > 0 && currentDialogueBoxString != "...")
                {
                    if (isReplacementDialogueBox)
                    {
                        isReplacementDialogueBox = false;
                        this.LastDialogueBoxString = currentDialogueBoxString;
                        return;
                    }

                    string currentLineId = getIdFromDialogueString(currentDialogueBoxString, Game1.currentSpeaker.Name);

                    if (currentLineId.Contains("not found"))
                    {
                      //  this.Monitor.Log(currentLineId, LogLevel.Debug);
                        this.LastDialogueBoxString = currentDialogueBoxString;
                        return;
                    }

                    this.currentCue = this.getCueFromDialogueId(currentLineId, Game1.currentSpeaker?.getName());

                    if (this.currentCue == null)
                    {
                       // this.Monitor.Log($"Cue could not be found for current line: {this.currentLineId}: {currentDialogueBoxString}", LogLevel.Debug);
                        this.LastDialogueBoxString = currentDialogueBoxString;
                        return;
                    }
                    else
                    {

                        if (this.lastCue != null && this.lastCue.IsPlaying)
                        {
                            this.lastCue.Stop(AudioStopOptions.Immediate);
                        }

                        // If the last cue is still playing current cue probably isn't ready to play yet
                        if (this.lastCue == null || (this.lastCue != null && !this.lastCue.IsPlaying) && !currentlyTalking)
                        {
                            currentlyTalking = true;
                            this.currentCue.Play();

                            // i THINK events are the only place the right after anotehr dialogue can happen 
                            if ((Game1.CurrentEvent != null && !Game1.CurrentEvent.isFestival)
                                || this.currentCue.Name.Contains("Gift")
                                || this.DialogueHasContinuation(this.currentCue.Name)
                                || this.currentCue.Name.Contains("endDialogue")
                                || this.currentCue.Name.ToLower().Contains("event"))
                            {
                                currentlyTalking = false;
                            }
                        }

                    }

                }


                // Remember, the dialogue box has changed, keep track of the changes
                if (this.currentLineId != null)
                {
                    this.lastLineId = this.currentLineId;
                }


                if (this.currentCue != null)
                {
                    this.lastCue = currentCue;
                }

                this.LastDialogueBoxString = currentDialogueBoxString;

            }

        }

        private void triggerEvent(string eventId, string locationName)
        {
            GameLocation gameLocation = Game1.getLocationFromName(locationName);
            Game1.warpFarmer(locationName, 0, 0, true);
            bool validEvent = true;
            Game1.PlayEvent(eventId, gameLocation, out validEvent, false, false);

            if (!validEvent)
            {
                throw new Exception($"Invalid event: {eventId}");
            }
        }

        private void createModTestingWorld()
        {
            if (!Context.IsWorldReady || this.testingWorldLoaded == true)
                return;

            if (Game1.player?.farmName?.Value == "Mod World")
            {
                foreach (string name in supportedCharacters)
                {
                    NPC inGameNPC = Game1.getCharacterFromName(name);
                    characterDictionary[name].InGameNPC = inGameNPC;
                    FarmHouse farmHouse = Game1.getLocationFromName("FarmHouse") as FarmHouse;
                    Game1.warpCharacter(inGameNPC, "FarmHouse", farmHouse.getRandomOpenPointInHouse(new Random()));
                    inGameNPC.ignoreScheduleToday = true;
                    //this.Monitor.Log($"{name} current location: {inGameNPC.currentLocation}", LogLevel.Debug);
                }


                this.testingWorldLoaded = true;
            }
        }

        private void setupDialogueDictionaries()
        {
            try
            {
                if (!Directory.Exists(characterDialogueFolderPath))
                    throw new Exception("Character dialogue folder could not be found");

                for (int i = 0; i < this.supportedCharacters.Count; i++)
                {
                    string characterDialoguePath = Path.Combine(characterDialogueFolderPath, supportedCharacters[i].ToLower() + ".json");
                    string jsonString = File.ReadAllText(characterDialoguePath);
                    characterDictionary[supportedCharacters[i]].DialogueDictionary = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, string[]>>>(jsonString);

                    string genericDialogueJsonString = File.ReadAllText(engagementDialogueJsonPath);
                    characterDictionary[supportedCharacters[i]].DialogueDictionary.Add("engagement", JsonConvert.DeserializeObject<Dictionary<string, string[]>>(genericDialogueJsonString));

                    genericDialogueJsonString = File.ReadAllText(extraDialogueJsonPath);
                    characterDictionary[supportedCharacters[i]].DialogueDictionary.Add("extra", JsonConvert.DeserializeObject<Dictionary<string, string[]>>(genericDialogueJsonString));

                    genericDialogueJsonString = File.ReadAllText(genericMarriageDialogueJsonPath);
                    characterDictionary[supportedCharacters[i]].DialogueDictionary.Add("generic_marriage", JsonConvert.DeserializeObject<Dictionary<string, string[]>>(genericDialogueJsonString));

                    genericDialogueJsonString = File.ReadAllText(rainyDialogueJsonPath);
                    characterDictionary[supportedCharacters[i]].DialogueDictionary.Add("rainy", JsonConvert.DeserializeObject<Dictionary<string, string[]>>(genericDialogueJsonString));
                }

            }
            catch (Exception ex)
            {
                this.Monitor.Log($"Error setting up dialogue dictionaries: {ex}. Audio will not play.", LogLevel.Error);
            }

        }

        private void setupFestivalDictionary()
        {
            try
            {
                string jsonString = File.ReadAllText(festivalDatesJsonPath);
                this.festivalNameToDateDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonString);
            } catch (Exception ex)
            {
                this.Monitor.Log($"Error setting up festival dictionaries: {ex}. Audio will not play.", LogLevel.Error);

            }

        }

        private void setupCharacterDictionary()
        {
            for (int i = 0; i < this.supportedCharacters.Count; i++)
            {
                this.characterDictionary.Add(supportedCharacters[i], new Character(supportedCharacters[i]));
            }
        }

        private void setupContinuationDialogue()
        {
            for (int i = 0; i < this.supportedCharacters.Count; i++)
            {
                Dictionary<string, Dictionary<string, string[]>> dialogueDictionary = this.characterDictionary[this.supportedCharacters[i]].DialogueDictionary;
                Dictionary<string, string[]> continuationDialogue = dialogueDictionary["continuation_dialogue"];
                string[] continuationLineIds = continuationDialogue["continuation_dialogue"];
                this.continuationDialogue.AddRange(continuationLineIds);
            }
        }

        private void setupCharacterSoundBanks()
        {
            try
            {
                this.audioEngine = new AudioEngine(Path.Combine(audioPath, "Stardew Voices.xgs"));
                this.dialogueCategory = this.audioEngine.GetCategory("Default");

                foreach (string character in this.supportedCharacters)
                {
                    this.characterDictionary[character].SetupAudioBanks(audioEngine, audioPath);
                }

            }
            catch (Exception e)
            {
                Monitor.Log($"Error loading sound bank, audio cannot play: {e}", LogLevel.Error);
            }
        }


        private void changeCurrentDialogueBoxString(string newDialogueString)
        {
            if (Game1.activeClickableMenu != null && Game1.activeClickableMenu.GetType() == typeof(DialogueBox))
            {
                //Game1.activeClickableMenu = new DialogueBox(newDialogueString);
                Shane.CurrentDialogue.Push(new Dialogue(Shane, "translationKey", newDialogueString));
                Game1.drawDialogue(Shane);
            }
        }

        private string formatDialogueString(string dialogueString)
        {
            dialogueString = dialogueString.Replace(Game1.player.Name, "@");
            dialogueString = dialogueString.Replace(Game1.getFarm().Name, "%farm");
            if (this.currentEventDict == null )
            {
                dialogueString = dialogueString.Replace("the second baby", "%kid2");
                dialogueString = dialogueString.Replace("baby", "%kid1");
            }
    


            /// TODO: remove hardcoded Abigail stuff!
            // Hardcode for that abigail sprit board scene
            if (dialogueString.Contains("It's spelling out...") || dialogueString.Contains("I <"))
            {
                dialogueString = dialogueString.Replace(Game1.player.Name.Substring(0, Math.Max(0, Game1.player.Name.Length / 2)), "%firstnameletter");
            }

            for (int i = 0; i < Game1.player.getChildren().Count; i++)
            {
                dialogueString = dialogueString.Replace(Game1.player.getChildren()[i].Name, $"%kid{i+1}");
            }

            return dialogueString;
        }

        private bool IdHasSeasonInIt(string id)
        {
            return id.ToLower().Contains("fall") || id.ToLower().Contains("winter") || id.ToLower().Contains("spring") || id.ToLower().Contains("summer");
        }

        private bool IdHasDayOfWeekInIt(string id)
        {
            return id.ToLower().Contains("mon") || id.ToLower().Contains("tue") || id.ToLower().Contains("wed") || id.ToLower().Contains("thur") || id.ToLower().Contains("fri") || id.ToLower().Contains("sat") || id.ToLower().Contains("sun");
        }

        // Abigail has two instances of this: event_grave (I've lived in the valley my whole life, but I've never really done anything memorable. I want to go on an adventure!) 
        // and PurchasedItem_Abigail_QualityLow vs high
        // We're gonna hard code for these situations a lil bit :(
        private string getCorrectIdFromListOfLineIds(string[] dialogueIdStrings, string currentSpeaker, string dialogueContext)
        {
            foreach (string id in dialogueIdStrings)
            {
                if (dialogueContext == "Extra Dialogue-" || dialogueContext == "Generic Marriage Dialogue")
                {
                    // Right now just hard code quality low vs high to high
                    if (id.Contains("QualityHigh") && id.Contains(currentSpeaker))
                    {
                        return id;
                    }
                }

                // If the id has a day of the week and/or season we should check to see if it's right
                if (Game1.Date != null && (this.IdHasDayOfWeekInIt(id) || this.IdHasSeasonInIt(id)))
                {
                    if (this.IdHasSeasonInIt(id))
                    {
                        if (!id.Contains(Game1.Date.SeasonKey))
                        {
                            continue;
                        }
                    }

                    if (this.IdHasDayOfWeekInIt(id))
                    {
                         if (!id.Contains(Game1.Date.DayOfWeek.ToString().Substring(0, 3)) && !id.Contains(Game1.Date.DayOfWeek.ToString().Substring(0, 4)))
                        {
                            continue;
                        }

                    }

                    return id;
                }

                // The last line id will show which event choice was made last and then we can just choose
                // The new id that continues that event.
                if (this.lastLineId != null && id.Contains(this.lastLineId))
                {
                    return id;
                }
            }

            this.Monitor.Log("Correct line id not found from list of line ids taking first one: " + String.Join(", ", dialogueIdStrings), LogLevel.Debug);

            return dialogueIdStrings[0];

        }

        private string checkEventsThatHappenedTodayForDialogue(string dialogueString, string currentSpeaker)
        {
            try
            {
                foreach (Dictionary<string, string> dictionary in this.eventsToday)
                {
                    if (dictionary.ContainsKey(dialogueString) && dictionary[dialogueString].Contains(currentSpeaker))
                    {
                        return dictionary[dialogueString];
                    }
                }

                return $"Event end dialogue not found for {currentSpeaker}: {dialogueString}";
            } catch (Exception ex)
            {
                return $"Event end dialogue not found for {currentSpeaker}: {dialogueString}, Exception: {ex}";
            }

        }

        private string checkForDialogueInNearbyEvents(string dialogueString, string currentSpeaker, string currentEventId)
        {

            try
            {
                if (Game1.currentLocation != null)
                {
                    string eventPath = Path.Combine(eventDialogueFolderPath, Game1.currentLocation.Name);
                    if (!Directory.Exists(eventPath))
                    {
                        return $"Directory not found at {eventPath}";
                    }

                    string[] eventFilePaths = Directory.GetFiles(eventPath);


                    foreach (string file in eventFilePaths)
                    {

                        string jsonString = File.ReadAllText(file);
                        Dictionary<string, string> tempEventDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonString);

                        // It needs to contain the dialogue line AND it has to be said by the right person
                        if ( tempEventDict.ContainsKey(dialogueString) && tempEventDict[dialogueString].Contains(currentSpeaker))
                        {
                            this.eventResponseDict = tempEventDict;
                            return tempEventDict[dialogueString];
                        }
                    }

                    return $"Event dialogue not found for: {dialogueString} for {currentSpeaker}";
                }

                return $"Event dialogue not found for: {dialogueString} for {currentSpeaker}. No location.";
            } catch (Exception ex)
            {
                return $"Event dialogue not found for: {dialogueString} for {currentSpeaker}. Exception: {ex}";
            }

        }

        private string getIdFromDialogueString(string dialogueString, string currentSpeaker)
        {

            if (!Directory.Exists(inverseDialogueFolderPath))
                return "Character dialogue folder could not be found";

            if (currentSpeaker == null)
            {
                return $"Current speaker is null for {dialogueString}";
            }

            Dictionary<string, Dictionary<string, string[]>> currentSpeakerDialogue;
            Dictionary<string, string[]> dialogueDict;
            Dictionary<string, string[]> miscDialogueDict;


            try
            {
                currentSpeakerDialogue = this.characterDictionary[currentSpeaker].DialogueDictionary;

                dialogueDict = currentSpeakerDialogue["dialogue"];
                miscDialogueDict = currentSpeakerDialogue["misc"];

            }
            catch (Exception ex)
            {
                return $"Dialogue dictionaries not found for {currentSpeaker}. Exception: {ex}";
            }

            string formattedDialogueString = this.formatDialogueString(dialogueString);

            // if in event
            try
            {
                if (Game1.CurrentEvent != null && this.currentEventDict != null)
                {
                    if (this.currentEventDict.ContainsKey(formattedDialogueString))
                    {
                        return this.currentEventDict[formattedDialogueString];
                    }
                    else
                    {
                        string closeEnoughKey = ContainsCloseEnoughLine(this.currentEventDict, formattedDialogueString);
                        if(!closeEnoughKey.Contains("Not found"))
                        {
                            return this.currentEventDict[closeEnoughKey];
                        }

                        // Check surrounding event files and also the dialogue file to see if the dialogue is there:
                        if (this.eventResponseDict != null && this.eventResponseDict.ContainsKey(formattedDialogueString))
                        {
                            return eventResponseDict[formattedDialogueString];

                        }

                        string eventDialogue = this.checkForDialogueInNearbyEvents(formattedDialogueString, currentSpeaker, Game1.CurrentEvent.id);

                        // If it's not found in a nearby event it could be in dialogue
                        if (eventDialogue.Contains("not found"))
                        {
                            if (dialogueDict.ContainsKey(formattedDialogueString))
                            {

                                if (dialogueDict[formattedDialogueString].Length > 1)
                                {
                                    return this.getCorrectIdFromListOfLineIds(dialogueDict[formattedDialogueString], currentSpeaker, "Dialogue-");
                                }

                                return dialogueDict[formattedDialogueString][0];
                            }
                            else if (miscDialogueDict.ContainsKey(formattedDialogueString))
                            {
                                return this.getCorrectIdFromListOfLineIds(miscDialogueDict[formattedDialogueString], currentSpeaker, "Internal Dialogue-");
                            }
                            /**else
                            {
                                closeEnoughKey = DialogueContainsCloseEnoughLine(dialogueDict, formattedDialogueString);
                                 if (!closeEnoughKey.Contains("Not found"))
                                {
                                    if (dialogueDict[closeEnoughKey].Length > 1)
                                    {
                                        return this.getCorrectIdFromListOfLineIds(dialogueDict[closeEnoughKey], currentSpeaker, "Dialogue-");
                                    }else
                                    {
                                        return dialogueDict[closeEnoughKey][0];

                                    }
                                 }
                            } */
                          
                        }
                        else
                        {
                            return eventDialogue;
                        }

                        return $"Event dialogue not found for {formattedDialogueString}";

                    }
                }

                else if (Game1.CurrentEvent != null && Game1.CurrentEvent.isFestival && this.currentFestivalDict != null)
                {
                    if (this.currentFestivalDict.ContainsKey(formattedDialogueString))
                    {
                        if (this.currentFestivalDict[formattedDialogueString].Length > 1)
                        {
                            return this.getCorrectIdFromListOfLineIds(this.currentFestivalDict[formattedDialogueString], currentSpeaker, "Festival Dialogue-");
                        }

                        return this.currentFestivalDict[formattedDialogueString][0];
                    }
                    else
                    {
                        if (dialogueDict.ContainsKey(formattedDialogueString))
                        {
                            if (dialogueDict[formattedDialogueString].Length > 1)
                            {
                                return this.getCorrectIdFromListOfLineIds(dialogueDict[formattedDialogueString], currentSpeaker, "Dialogue-");
                            }

                            return dialogueDict[formattedDialogueString][0];
                        }

                        if (miscDialogueDict.ContainsKey(formattedDialogueString))
                        {
                            if (miscDialogueDict[formattedDialogueString].Length > 1)
                            {
                                return this.getCorrectIdFromListOfLineIds(miscDialogueDict[formattedDialogueString], currentSpeaker, "Internal Dialogue-");
                            }

                            return miscDialogueDict[formattedDialogueString][0];
                        }

                        return $"Festival dialogue not found for {formattedDialogueString}";
                    }
                }

                // If not currently in event or festival check all dialogue dictionaries
                else
                {
                    List<Dictionary<string, string[]>> characterDialogueDictionaries = new List<Dictionary<string, string[]>>();
                    List<string> dialogueDictionaryNames = new List<string>();


                    Dictionary<string, string[]> engagementDialogueDict = currentSpeakerDialogue["engagement"];
                    Dictionary<string, string[]> genericMarriageDialogueDict = currentSpeakerDialogue["generic_marriage"];
                    Dictionary<string, string[]> extraDialogueDict = currentSpeakerDialogue["extra"];
                    Dictionary<string, string[]> rainyDialogueDict = currentSpeakerDialogue["rainy"];


                    Dictionary<string, string[]> marriageDialogueDict = currentSpeakerDialogue["marriage_dialogue"];
                    Dictionary<string, string[]> npcGiftTastesDialogueDict = currentSpeakerDialogue["gift_tastes"];

                    if (Game1.player.isEngaged() || testingWorldLoaded)
                    {
                        characterDialogueDictionaries.Add(engagementDialogueDict);
                        dialogueDictionaryNames.Add("Engagement_Dialogue-");
                    }

                    if (Game1.player.getSpouse()?.getName() == currentSpeaker || testingWorldLoaded)
                    {
                        characterDialogueDictionaries.Add(marriageDialogueDict);
                        dialogueDictionaryNames.Add("Marriage_Dialogue-");

                        characterDialogueDictionaries.Add(genericMarriageDialogueDict);
                        dialogueDictionaryNames.Add("Generic_Marriage_Dialogue-");
                    }

                    characterDialogueDictionaries.Add(dialogueDict);
                    dialogueDictionaryNames.Add("Dialogue-");

                    characterDialogueDictionaries.Add(extraDialogueDict);
                    dialogueDictionaryNames.Add("Extra_Dialogue-");

                    characterDialogueDictionaries.Add(rainyDialogueDict);
                    dialogueDictionaryNames.Add("Rainy-");

                    characterDialogueDictionaries.Add(npcGiftTastesDialogueDict);
                    dialogueDictionaryNames.Add("NpcGiftTastes-");

                    characterDialogueDictionaries.Add(miscDialogueDict);
                    dialogueDictionaryNames.Add("Internal_Dialogue-");

                    string[] splitDialogueString = formattedDialogueString.Split(" ");

                    for (int i = 0; i < characterDialogueDictionaries.Count; i++)
                    {
                        if (characterDialogueDictionaries[i].ContainsKey(formattedDialogueString))
                        {
                            if (characterDialogueDictionaries[i][formattedDialogueString].Length > 1)
                            {
                                return this.getCorrectIdFromListOfLineIds(characterDialogueDictionaries[i][formattedDialogueString], currentSpeaker, dialogueDictionaryNames[i]);
                            }

                            return characterDialogueDictionaries[i][formattedDialogueString][0];
                        }

                        if (dialogueDictionaryNames[i] == "Extra_Dialogue-" || dialogueDictionaryNames[i] == "Internal_Dialogue-")
                        {
                            foreach (string key in characterDialogueDictionaries[i].Keys)
                            {
                                if (!key.Contains("{") && !key.Contains("%"))
                                {
                                    continue;
                                }

                                string[] splitKey = key.Split(" ");


                                // Check to see if the amount of shared words is ALMOST equivalent to the length minus the number of substitutions possible (i've only seen up to 3)
                                if (splitDialogueString.Intersect(splitKey).Count() > 0 && splitDialogueString.Length == splitKey.Length && splitDialogueString.Length - splitDialogueString.Intersect(splitKey).Count() <= 3)
                                {
                                    // Check for dad / mom / name stuff
                                    if (splitDialogueString.Contains("Dad") && !splitKey.Contains("Dad")
                                        || splitDialogueString.Contains("Mom") && !splitKey.Contains("Mom")
                                        || (!splitDialogueString.Contains("Dad") && !splitDialogueString.Contains("Mom") && splitKey.Contains("Dad") && splitKey.Contains("Mom")))
                                    {
                                        continue;
                                    }

                                    // check for she/ he stuff her/him
                                    if ((!splitDialogueString.Contains("Dad") && !splitDialogueString.Contains("Mom"))
                                        && (
                                               formattedDialogueString.ToLower().Contains("she") && !key.ToLower().Contains("she")
                                            || formattedDialogueString.ToLower().Contains(" he ") && !key.ToLower().Contains(" he ")
                                            || formattedDialogueString.ToLower().Contains("him") && !key.ToLower().Contains("him")
                                            || formattedDialogueString.ToLower().Contains("her") && !key.ToLower().Contains("her")
                                           )
                                       )
                                    {
                                        continue;
                                    }

                                    if (characterDialogueDictionaries[i][key].Length > 1)
                                    {
                                        return this.getCorrectIdFromListOfLineIds(characterDialogueDictionaries[i][key], currentSpeaker, dialogueDictionaryNames[i]);
                                    }

                                    return characterDialogueDictionaries[i][key][0];

                                }
                            }
                        }

                    }

                    if (this.eventsToday.Count > 0)
                    {
                        return this.checkEventsThatHappenedTodayForDialogue(formattedDialogueString, currentSpeaker);
                    }

                    // check if it is a %noun %adjective situation
                    foreach (string randDialogueLine in dialogueWithRandomNounOrAdj.Keys)
                    {
                        string[] splitRandDialogueLine = randDialogueLine.Split(" ");
                        if (splitDialogueString.Length == splitRandDialogueLine.Length && splitDialogueString.Length - splitDialogueString.Intersect(splitRandDialogueLine).Count() <= 3)
                        {
                            string newRandDialogueLIne = randDialogueLine.Replace("%noun", "dragon").Replace("%adj", "giant").Replace("%year", "couple");

                            this.changeCurrentDialogueBoxString(newRandDialogueLIne);
                            isReplacementDialogueBox = true;
                            return dialogueWithRandomNounOrAdj[randDialogueLine];
                        }

                    }

                    return $"Line not found for {currentSpeaker}: {formattedDialogueString}";
                }

            } catch (Exception ex)
            {
                return $"Line not found for {currentSpeaker}: {formattedDialogueString}. Exception: {ex}";
            }
        }

        private void OpenVoicesMenu()
        {
            if(Game1.activeClickableMenu == null)
            {
                Game1.activeClickableMenu = new Menu(this.dialogueCategory, this.Config, this.helper);
            }
        }

        private void PopulateCharacterNamesList()
        {
            if (Game1.characterData == null)
            {
                Monitor.Log($"Error populating character names list: no game character data found?", LogLevel.Debug);
                return;
            }

            try
            {
                List<string> characterNames = new List<string>();
                foreach (string character in Game1.characterData.Keys)
                {
                    if (character != "???")
                    {
                        characterNames.Add(character);
                    }
                }


                characterNameRegex = String.Join("|", characterNames);
            }
            catch (Exception ex) {
                Monitor.Log($"Error populating character names list: {ex}", LogLevel.Debug);
                return;
            }
      
        }

        private Boolean StringContainsSupportedCharacter(string input)
        {
            for (int i=0; i<this.supportedCharacters.Count; i++)
            {
               if (input.Contains(this.supportedCharacters[i]))
                {
                    return true;
                }
            }

            return false;
        }

        private string GetSupportedCharacterFromString(string input)
        {
            for (int i = 0; i < this.supportedCharacters.Count; i++)
            {
                if (input.Contains(this.supportedCharacters[i]))
                {
                    return this.supportedCharacters[i];
                }
            }

            return  "Could not find supported character in string :I";
        }

        private string ContainsCloseEnoughLine(Dictionary<string, string> dictionary, string line)
        {
            if(dictionary.ContainsKey(line))
            {
                return line;
            }
            string[] splitDialogueString = line.Split(" ");
            foreach (string key in dictionary.Keys)
            {
                string[] splitKey = key.Split(" ");

                if (splitDialogueString.Length - key.Count(word => word=='@') > 1 && splitDialogueString.Intersect(splitKey).Count() > 0 && splitDialogueString.Length - splitDialogueString.Intersect(splitKey).Count() <= Math.Ceiling(splitDialogueString.Length * .25) )
                {
                    // TODO FIX 
                    /*if (!key.Contains("female_farmer") && this.player.Gender == Gender.Female)
                    {
                        return key.Replace("male", "female");
                    } else if (key.Contains("female_farmer") && this.player.Gender == Gender.Male)
                    {
                        return key.Replace("female", "male");
                    }
                    else
                    {
                        return key;
                    }*/
                }
            }
            return "Not found";
        }

        private string DialogueContainsCloseEnoughLine(Dictionary<string, string[]> dictionary, string line)
        {
            if (dictionary.ContainsKey(line))
            {
                return line;
            }
            string[] splitDialogueString = line.Split(" ");
            foreach (string key in dictionary.Keys)
            {
                string[] splitKey = key.Split(" ");

                foreach ( string value in dictionary[key])
                {
                    if (splitDialogueString.Length - key.Count(word => word == '@') > 1 && splitDialogueString.Intersect(splitKey).Count() > 0 && splitDialogueString.Length - splitDialogueString.Intersect(splitKey).Count() <= Math.Ceiling(splitDialogueString.Length * .25))
                    {
                        return key;
                    }
                }
            }
            return "Not found";
        }


        private string GetLanguageJsonExtension(string language)
        {
            if (language == "en")
            {
                return "json";
            }
            Dictionary<string, string> languageDict = new Dictionary<string, string>()
            {
                { "de", "DE" },
                { "es", "ES" },
                { "fr", "FR" },
                { "hu", "HU" },
                { "it", "IT" },
                { "ja", "JP" },
                { "ko", "KR" },
                { "pt", "BR" },
                { "ru", "RU" },
                { "tr", "TR" },
                { "zh", "CN" },
            };

            return $"{language}-{languageDict[language]}.json";

        }

        private void PopulateTestDialogueList()
        {
            List<Dictionary<string, string[]>> characterDialogueDictionaries = new List<Dictionary<string, string[]>>();
            List<string> dialogueDictionaryNames = new List<string>();

            Dictionary<string, Dictionary<string, string[]>> shaneDictionary;

            try
            {
                shaneDictionary = this.characterDictionary["Shane"].DialogueDictionary;

            }
            catch (Exception ex) {
                Monitor.Log($"Could not find dialogue for Shane in testing. {ex}", LogLevel.Debug);
                return;
            }


            characterDialogueDictionaries.Add(shaneDictionary["dialogue"]);
            dialogueDictionaryNames.Add("Dialogue-");

            characterDialogueDictionaries.Add(shaneDictionary["engagement"]);
            dialogueDictionaryNames.Add("Engagement_Dialogue-");

            characterDialogueDictionaries.Add(shaneDictionary["generic_marriage"]);
            dialogueDictionaryNames.Add("Generic_Marriage-");

            characterDialogueDictionaries.Add(shaneDictionary["rainy"]);
            dialogueDictionaryNames.Add("Rainy-");

            characterDialogueDictionaries.Add(shaneDictionary["marriage_dialogue"]);
            dialogueDictionaryNames.Add("Marriage_Dialogue-");

            characterDialogueDictionaries.Add(shaneDictionary["misc"]);
            dialogueDictionaryNames.Add("Misc-");

            characterDialogueDictionaries.Add(shaneDictionary["schedule"]);
            dialogueDictionaryNames.Add("Schedule-");

            characterDialogueDictionaries.Add(shaneDictionary["extra"]);
            dialogueDictionaryNames.Add("Extra-");

            characterDialogueDictionaries.Add(shaneDictionary["gift_tastes"]);
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
                          
                            Match characterNameMatch = Regex.Match(value, this.characterNameRegex, RegexOptions.IgnoreCase);
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