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

namespace StardewVoices
{
    /// <summary>The mod entry point.</summary>
    public class StardewVoices : Mod
    {
        private static string modPath = GetModPath();

        private static string audioPath = Path.Combine(modPath, "Audio");
        private static string inverseDialogueFolderPath = Path.Combine(modPath, "Dialogue Jsons");

        private static string genericDialogueFolderPath = Path.Combine(inverseDialogueFolderPath, "Generic");
        private static string eventDialogueFolderPath = Path.Combine(inverseDialogueFolderPath, "Events");
        private static string festivalDialogueFolderPath = Path.Combine(inverseDialogueFolderPath, "Festivals");
        private static string characterDialogueFolderPath = Path.Combine(inverseDialogueFolderPath, "Characters");


        private static string festivalDatesJsonPath = Path.Combine(festivalDialogueFolderPath, "FestivalDates.json");

        private WaveBank abigailWaveBank;
        private SoundBank abigailSoundBank;

        private AudioEngine audioEngine;
        private AudioCategory dialogueCategory;

        private Cue currentCue;
        private Cue lastCue;
        private string currentLineId;
        private string lastLineId;

        private Dictionary<string, SoundBank> NpcSoundBankDict = new Dictionary<string, SoundBank>();
        private Dictionary<string, Dictionary<string, string[]>> GenericNpcDialogueJsonDict = new Dictionary<string, Dictionary<string, string[]>>();

        /*
         *  A dictionary of NPC names mapped to dictionary=ies of different types of dialogue: ["Dialogue", "Marriage_Dialogue", etc...] that are dictionaries that contain dialogue line -> lineId key value pairs.
         * 
         * Ex:
         *      "Abigail": {
         *          "Dialogue" : {
         *                "Oh, that's right... I heard someone new was moving onto that old farm.": [
                              "Introduction"
                            ],
         *          },
         *          
         *          "Marriage_Dialogue": {
                        "The dark... the rain... it gets me excited.": ["Rainy_Day_0"],
                    },
         *      },
         *      
         *      "Shane": {
         *          etc...
         *      }
         * 
         * 
         */

        private Dictionary<string, Dictionary<string, Dictionary<string, string[]>>> NpcDialogueJsonDict = new Dictionary<string, Dictionary<string, Dictionary<string, string[]>>>();



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
        private NPC Abigail;
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


        /*********
        ** Public methods
        *********/
        /// <summary>The mod entry point, called after the mod is first loaded.</summary>
        /// <param name="helper">Provides simplified APIs for writing mods.</param>
        public override void Entry(IModHelper helper)
        {
            this.supportedCharacters.Add("Abigail");


            Monitor.Log($"Relative mods path: {modPath}", LogLevel.Debug);

            this.helper = helper;

            this.Config = helper.ReadConfig<ModConfig>();

            helper.Events.GameLoop.UpdateTicked += this.OnUpdateTicked;
            helper.Events.Input.ButtonPressed += this.OnButtonPressed;

            this.setupFestivalDictionary();

            this.setupDialogueDictionaries();

            this.eventsToTest.Add("195019_HaleyHouse");
            this.eventsToTest.Add("195012_HaleyHouse");
            this.eventsToTest.Add("2123243_ManorHouse");


            this.eventsToTest.Add("2_Mountain");
            this.eventsToTest.Add("4_Town");
            this.eventsToTest.Add("1_SeedShop");
            this.eventsToTest.Add("3_SeedShop");
            this.eventsToTest.Add("901756_Mine");
            this.eventsToTest.Add("5_Farm");
            this.eventsToTest.Add("150938_HaleyHouse");
            this.eventsToTest.Add("6963327_Backwoods");
            this.eventsToTest.Add("17_SeedShop");


            // Question dialogue
            continuationDialogue.Add("Sun2");
            continuationDialogue.Add("Sun2_sv1");

            continuationDialogue.Add("summer_Sun");
            continuationDialogue.Add("summer_Sun_sv1");

            continuationDialogue.Add("fall_Sun");
            continuationDialogue.Add("fall_Sun_sv1");

            continuationDialogue.Add("winter_Sun");
            continuationDialogue.Add("winter_Sun_sv1");

            continuationDialogue.Add("spring13_Abigail_y2");

            // Anything w/ $b
            continuationDialogue.Add("married_Sebastian");
            continuationDialogue.Add("Resort");
            continuationDialogue.Add("FlowerDance_Accept_Spouse");
            continuationDialogue.Add("breakUp");
            continuationDialogue.Add("Thu10_sv1");
            continuationDialogue.Add("breakUp");
            continuationDialogue.Add("summer_Wed2");
            continuationDialogue.Add("fall_Tue4");
            continuationDialogue.Add("winter_Thu10");
            continuationDialogue.Add("winter_Sat6_sv1");
            continuationDialogue.Add("SummitEvent_Dialogue3_Abigail");
            continuationDialogue.Add("PurchasedItem_Abigail_QualityLow");
            continuationDialogue.Add("PurchasedItem_Abigail_QualityHigh");

            continuationDialogue.Add("NPC_cs_3972");
            continuationDialogue.Add("NPC_cs_3980");
            continuationDialogue.Add("NPC_cs_3980_sv1");
            continuationDialogue.Add("NPC_cs_3980_sv2");
            continuationDialogue.Add("NPC_cs_4427");
            continuationDialogue.Add("NPC_cs_4429");
            continuationDialogue.Add("ItemDeliveryQuest_cs_13597");
            continuationDialogue.Add("AbigailInMineFirst");
            continuationDialogue.Add("AbigailInMineFirst_sv1");
            continuationDialogue.Add("AbigailInMine1");


            dialogueWithRandomNounOrAdj.Add("Last night I dreamt that my left hand had turned into a gigantic %noun. Does that mean anything?", "Dialogue-fall_Thu_chance_1");
            dialogueWithRandomNounOrAdj.Add("I did, but all I remember is that it had something to do with a %adj %noun. Weird.", "Generic Marriage Dialogue-Indoor_Day_3_sv1");


            try
            {
                this.audioEngine = new AudioEngine(Path.Combine(audioPath, "Stardew Voices.xgs"));
                this.dialogueCategory = this.audioEngine.GetCategory("Default");

                this.abigailWaveBank = new WaveBank(audioEngine, Path.Combine(audioPath, "Abigail.xwb"));
                this.abigailSoundBank = new SoundBank(audioEngine, Path.Combine(audioPath, "Abigail.xsb"));

            } 
            catch (Exception e) 
            {
                Monitor.Log($"Error loading sound bank, audio cannot play: {e}", LogLevel.Debug);
            }


        }


        /*********
        ** Private methods
        *********/
        /// <summary>Get path to mod.</summary>
        private static string GetModPath()
        {
            string pathToAssembly = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            int indexOfModsFolder = pathToAssembly.ToLower().IndexOf("mods");
            if (indexOfModsFolder > pathToAssembly.Length - 1 || indexOfModsFolder == -1)
            {
                return Path.Combine("Mods", "StardewVoices");
            }

            return pathToAssembly.Substring(indexOfModsFolder);
        }

         
        /// <summary>The method invoked when the game updates its state.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnUpdateTicked(object sender, EventArgs e)
        {

            this.createModTestingWorld();

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

            //if (Game1.content != null && !localizedContentLoaded)
            //{
            //    Dictionary<string, string> characterStrings = Game1.content.Load<Dictionary<string, string>>("Strings\\schedules\\Shane");
            //    string outFilePath = "C:\\Users\\shann\\OneDrive\\Documents\\Stardew Voices Mod Stuff\\Localized Content\\Localized Content - Shane Schedule.txt";

            //    using (StreamWriter outputFile = new StreamWriter(outFilePath))
            //    {
            //        foreach (string key in characterStrings.Keys)
            //        {
            //            this.Monitor.Log($"Localized character string {key}: {characterStrings[key]}", LogLevel.Debug);
            //            outputFile.WriteLine($"{key}: {characterStrings[key]}");
            //        }
            //    }

            //    localizedContentLoaded = true;
            //}


            // Warp Abigail to Farm House on load when in testing world. That is her home now.
            if (this.Abigail != null && this.Abigail.currentLocation?.Name != "FarmHouse" && Game1.getLocationFromName("FarmHouse") != null && this.testingWorldLoaded)
            {
                FarmHouse farmHouse = Game1.getLocationFromName("FarmHouse") as FarmHouse;
                Game1.warpCharacter(this.Abigail, "FarmHouse", farmHouse.getRandomOpenPointInHouse(new Random()));
                this.Abigail.ignoreScheduleToday = true;
                this.Monitor.Log($"Abigail current location: {this.Abigail.currentLocation}", LogLevel.Debug);
            }

        }

        /// <summary>Raised after the player presses a button on the keyboard, controller, or mouse.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void OnButtonPressed(object sender, ButtonPressedEventArgs e)
        {
            // ignore if player hasn't loaded a save yet
            if (!Context.IsWorldReady)
                return;

            if(this.testingWorldLoaded && e.Button == SButton.K)
            {
                this.Monitor.Log($"K Pressed, starting event", LogLevel.Debug);

                string currentEventToTest = this.eventsToTest[eventsToTestIndex];

                this.triggerEvent(currentEventToTest.Split("_")[0], currentEventToTest.Split("_")[1]);
                this.eventsToTestIndex++;
            }

            if (this.testingWorldLoaded && e.Button == SButton.L)
            {
                this.Monitor.Log($"L Pressed, resetting character dialogue", LogLevel.Debug);

                if (Game1.player != null && this.Abigail != null)
                {
                    Game1.player.friendshipData.TryGetValue(this.Abigail.Name, out var friendship);
                    Game1.player.resetFriendshipsForNewDay();
                    this.Abigail.checkForNewCurrentDialogue(friendship.Points);

                    Game1.currentLocation?.ResetCharacterDialogues();
                }
            }

            if (this.testingWorldLoaded && e.Button == SButton.B)
            {
                this.Monitor.Log($"B Pressed", LogLevel.Debug);
                if(Game1.activeClickableMenu != null)
                {
                    Game1.activeClickableMenu = null;
                    currentlyTalking = false;
                }

                if (Game1.player != null && this.Abigail != null && Game1.activeClickableMenu == null && testDialogue.Count > 0)
                {
                    Game1.currentSpeaker = this.Abigail;
                    Abigail.CurrentDialogue.Push(new Dialogue(Abigail, "translationKey", GetCurrentTestDialogue()));
                    Game1.drawDialogue(Abigail);
                }
            }

            if (this.Config.OpenMenuKey.JustPressed())
            {
                if(Game1.activeClickableMenu is Menu menu)
                {
                    menu.ExitIfValid();
                }
                else
                {
                    this.OpenVoicesMenu();
                }
         
            }
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

            if (Game1.CurrentEvent != null  && !Game1.CurrentEvent.isFestival && this.lastEvent == null)
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

                    string filePath = Path.Combine(festivalDialogueFolderPath, (string)festivalNameToDateDict[this.lastFestival] + ".json");

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
            if ((this.lastEvent == null || !this.lastEvent.isFestival) && this.lastFestival != null )
            {
                this.lastFestival = null;
                this.currentFestivalDict = null;
            }

        }

        private Cue getCueFromDialogueId(string lineId, string dialogueContext, string characterName)
        {
            switch (dialogueContext)
            {
                case "Abigail":
                    try
                    {
                        return this.abigailSoundBank.GetCue(dialogueContext + "-" + lineId);
                    } catch(Exception error)
                    {
                        this.Monitor.Log($"Error getting cue for {characterName}: {lineId} in {dialogueContext} Sound Bank: {error}", LogLevel.Debug);
                        return null;
                    }

                default:
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
                    eventCommandDict.Add(count, eventCommand);
                    count++;
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

                    if(currentSpeaker.Contains("Could not find"))
                    {
                        this.Monitor.Log($"Could not find speaker for textAboveHead command: {currentCommand}", LogLevel.Debug);
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
                        Monitor.Log($"Error extracting dialogue from textAboveHead event command: {e}", LogLevel.Debug);
                        this.lastEventCommand = Game1.CurrentEvent.currentCommand;
                        return;
                    }
         

                    if (currentCommand.Contains("ClothesTherapyCharacters") && commandDialogue != "It's sweaty in here..." && currentSpeaker == "Abigail")
                    {
                        this.lastEventCommand = Game1.CurrentEvent.currentCommand;
                        return;
                    }

                    if(this.currentEventDict != null)
                    {

                        string textAboveHeadLineId = getIdFromDialogueString(this.formatDialogueString(commandDialogue), currentSpeaker);

                        if (textAboveHeadLineId.Contains("not found"))
                        {
                            this.Monitor.Log(textAboveHeadLineId, LogLevel.Debug);
                            this.lastEventCommand = Game1.CurrentEvent.currentCommand;
                            return;
                        }

                        this.currentCue = this.getCueFromDialogueId(textAboveHeadLineId.Split("-")[1], textAboveHeadLineId.Split("-")[0], currentSpeaker);

                        if (this.currentCue == null)
                        {
                            this.Monitor.Log($"Cue could not be found for textAboveHead line: {textAboveHeadLineId}", LogLevel.Debug);
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
                if (
                    StringContainsSupportedCharacter(Game1.currentSpeaker?.getName())
                    && (currentActiveClickableMenu != null && currentActiveClickableMenu.GetType() == typeof(DialogueBox)) && currentDialogueBoxString.Length > 0 && currentDialogueBoxString != "...")
                {
                    if (isReplacementDialogueBox)
                    {
                        isReplacementDialogueBox = false;
                        this.LastDialogueBoxString = currentDialogueBoxString;
                        return;
                    }

                    string currentLine = getIdFromDialogueString(currentDialogueBoxString, Game1.currentSpeaker.Name);

                    if (currentLine.Contains("not found"))
                    {
                        this.Monitor.Log(currentLine, LogLevel.Debug);
                        this.LastDialogueBoxString = currentDialogueBoxString;
                        return;
                    }

                    this.currentCue = this.getCueFromDialogueId(currentLine.Split("-")[1], currentLine.Split("-")[0], Game1.currentSpeaker?.getName());
                    this.currentLineId = currentLine.Split("-")[1];

                    if (this.currentCue == null)
                    {
                        this.Monitor.Log($"Cue could not be found for current line: {this.currentLineId}: {currentDialogueBoxString}", LogLevel.Debug);
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
                this.Abigail = Game1.getCharacterFromName("Abigail");

                FarmHouse farmHouse = Game1.getLocationFromName("FarmHouse") as FarmHouse;
                Game1.warpCharacter(this.Abigail, "FarmHouse", farmHouse.getRandomOpenPointInHouse(new Random()));
                this.Abigail.ignoreScheduleToday = true;
                this.Monitor.Log($"Abigail current location: {this.Abigail.currentLocation}", LogLevel.Debug);

                this.testingWorldLoaded = true;
            }
        }

        private void setupDialogueDictionaries()
        {
            try
            {
                if (!Directory.Exists(inverseDialogueFolderPath))
                    throw new Exception("Character dialogue folder could not be found");

                for (int i = 0; i < supportedCharacters.Count; i++)
                {
                    string characterDialogueJsonPath = Path.Combine(characterDialogueFolderPath, supportedCharacters[i].ToLower() + ".json");
                    string jsonString = File.ReadAllText(characterDialogueJsonPath);
                    Dictionary <string, Dictionary<string, string[]>> tempDictionary = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, string[]>>>(jsonString);
                   
                    if (!this.NpcDialogueJsonDict.ContainsKey(supportedCharacters[i]))
                    {
                        this.NpcDialogueJsonDict.Add(supportedCharacters[i], tempDictionary);
                    }
                    else
                    {
                        throw new Exception($"{supportedCharacters[i]} already populated in NPC Dialogue Dictionary");
                    }
                     
                }

                // Extra Dialogue

                string genericDialogueJsonPath = Path.Combine(genericDialogueFolderPath, "ExtraDialogue.json");
                string genericJsonString = File.ReadAllText(genericDialogueJsonPath);
                Dictionary<string, string[]> tempDialogueDict = JsonConvert.DeserializeObject<Dictionary<string, string[]>>(genericJsonString);

                if (!this.GenericNpcDialogueJsonDict.ContainsKey("Extra_Dialogue"))
                {
                    this.GenericNpcDialogueJsonDict.Add("Extra_Dialogue", tempDialogueDict);
                }
                else
                {
                    throw new Exception("Extra_Dialogue already populated in NPC Dialogue Dictionary");
                }

                // Engagement Dialogue

                genericDialogueJsonPath = Path.Combine(genericDialogueFolderPath, "EngagementDialogue.json");
                genericJsonString = File.ReadAllText(genericDialogueJsonPath);
                tempDialogueDict = JsonConvert.DeserializeObject<Dictionary<string, string[]>>(genericJsonString);


                if (!this.GenericNpcDialogueJsonDict.ContainsKey("Engagement_Dialogue"))
                {
                    this.GenericNpcDialogueJsonDict.Add("Engagement_Dialogue", tempDialogueDict);
                }
                else
                {
                    throw new Exception("Engagement_Dialogue already populated in NPC Dialogue Dictionary");
                }


                // Generic Marriage Dialogue

                genericDialogueJsonPath = Path.Combine(genericDialogueFolderPath, "Marriage Dialogue Generic.json");
                genericJsonString = File.ReadAllText(genericDialogueJsonPath);
                tempDialogueDict = JsonConvert.DeserializeObject<Dictionary<string, string[]>>(genericJsonString);


                if (!this.GenericNpcDialogueJsonDict.ContainsKey("Generic_Marriage_Dialogue"))
                {
                    this.GenericNpcDialogueJsonDict.Add("Generic_Marriage_Dialogue", tempDialogueDict);
                }
                else
                {
                    throw new Exception("Generic_Marriage_Dialogue already populated in NPC Dialogue Dictionary");
                }

                // Rainy Dialogue

                genericDialogueJsonPath = Path.Combine(genericDialogueFolderPath, "rainy.json");
                genericJsonString = File.ReadAllText(genericDialogueJsonPath);
                tempDialogueDict = JsonConvert.DeserializeObject<Dictionary<string, string[]>>(genericJsonString);


                if (!this.GenericNpcDialogueJsonDict.ContainsKey("Rainy"))
                {
                    this.GenericNpcDialogueJsonDict.Add("Rainy", tempDialogueDict);
                }
                else
                {
                    throw new Exception("Rainy dialogue key already populated in NPC Dialogue Dictionary");
                }


            } catch (Exception ex)
            {
                this.Monitor.Log($"Error setting up dialogue dictionaries: {ex}. Audio will not play.", LogLevel.Debug);
            }

        }

        private void setupFestivalDictionary()
        {
            try
            {
                string jsonString = File.ReadAllText(festivalDatesJsonPath);
                Monitor.Log(jsonString, LogLevel.Debug);
                this.festivalNameToDateDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonString);
            } catch (Exception ex)
            {
                this.Monitor.Log($"Error setting up festival dictionaries: {ex}. Audio will not play.", LogLevel.Debug);

            }

        }

        private void changeCurrentDialogueBoxString(string newDialogueString)
        {
            if (Game1.activeClickableMenu != null && Game1.activeClickableMenu.GetType() == typeof(DialogueBox))
            {
                //Game1.activeClickableMenu = new DialogueBox(newDialogueString);
                Abigail.CurrentDialogue.Push(new Dialogue(Abigail, "translationKey", newDialogueString));
                Game1.drawDialogue(Abigail);
            }
        }

        private string formatDialogueString(string dialogueString)
        {
            dialogueString = dialogueString.Replace(Game1.player.Name, "@");
            dialogueString = dialogueString.Replace(Game1.getFarm().Name, "%farm");
            dialogueString = dialogueString.Replace("the second baby", "%kid2");
            dialogueString = dialogueString.Replace("baby", "%kid1");


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
                        return dialogueContext + id;
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

                    return dialogueContext + id;
                }

                // The last line id will show which event choice was made last and then we can just choose
                // The new id that continues that event.
                if (this.lastLineId != null && id.Contains(this.lastLineId))
                {
                    return dialogueContext + id;
                }
            }

            this.Monitor.Log("Correct line id not found from list of line ids taking first one: " + String.Join(", ", dialogueIdStrings), LogLevel.Debug);

            return dialogueContext + dialogueIdStrings[0];

        }

        private string checkEventsThatHappenedTodayForDialogue(string dialogueString, string currentSpeaker)
        {
            try
            {
                foreach (Dictionary<string, string> dictionary in this.eventsToday)
                {
                    if (dictionary.ContainsKey(dialogueString) && dictionary[dialogueString].Contains(currentSpeaker))
                    {
                        return "Event Dialogue-" + dictionary[dialogueString];
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
                        if (tempEventDict.ContainsKey(dialogueString) && tempEventDict[dialogueString].Contains(currentSpeaker))
                        {
                            this.eventResponseDict = tempEventDict;
                            return "Event Dialogue-" + tempEventDict[dialogueString];
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
            Dictionary<string, string[]> internalDialogueDict;


            try
            {
                currentSpeakerDialogue = GetAllDialogueForCharacter(currentSpeaker);

                dialogueDict = currentSpeakerDialogue["Dialogue"];
                internalDialogueDict = currentSpeakerDialogue["Internal_Dialogue"];

            }
            catch (Exception ex)
            {
                return $"Dialogue dictionaries not found for {currentSpeaker}. Exception: {ex}";
            }

            string formattedDialogueString = this.formatDialogueString(dialogueString);

            try
            {
                if (Game1.CurrentEvent != null && this.currentEventDict != null)
                {
                    if (this.currentEventDict.ContainsKey(formattedDialogueString))
                    {
                        return "Event Dialogue-" + this.currentEventDict[formattedDialogueString];
                    }
                    else
                    {
                        // Check surrounding event files and also the dialogue file to see if the dialogue is there:
                        if (this.eventResponseDict != null && this.eventResponseDict.ContainsKey(formattedDialogueString))
                        {
                            return "Event Dialogue-" + this.eventResponseDict[formattedDialogueString];

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

                                return "Dialogue-" + dialogueDict[formattedDialogueString][0];
                            }
                            else if (internalDialogueDict.ContainsKey(formattedDialogueString))
                            {
                                return this.getCorrectIdFromListOfLineIds(internalDialogueDict[formattedDialogueString], currentSpeaker, "Internal Dialogue-");
                            }
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

                        return "Festival Dialogue-" + this.currentFestivalDict[formattedDialogueString][0];
                    }
                    else
                    {
                        if (dialogueDict.ContainsKey(formattedDialogueString))
                        {
                            if (dialogueDict[formattedDialogueString].Length > 1)
                            {
                                return this.getCorrectIdFromListOfLineIds(dialogueDict[formattedDialogueString], currentSpeaker, "Dialogue-");
                            }

                            return "Dialogue-" + dialogueDict[formattedDialogueString][0];
                        }

                        if (internalDialogueDict.ContainsKey(formattedDialogueString))
                        {
                            if (internalDialogueDict[formattedDialogueString].Length > 1)
                            {
                                return this.getCorrectIdFromListOfLineIds(internalDialogueDict[formattedDialogueString], currentSpeaker, "Internal Dialogue-");
                            }

                            return "Internal_Dialogue-" + internalDialogueDict[formattedDialogueString][0];
                        }

                        return $"Festival dialogue not found for {formattedDialogueString}";
                    }
                }

                // If not currently in event or festival check all dialogue dictionaries
                else
                {
                    List<Dictionary<string, string[]>> characterDialogueDictionaries = new List<Dictionary<string, string[]>>();
                    List<string> dialogueDictionaryNames = new List<string>();


                    Dictionary<string, string[]> engagementDialogueDict = currentSpeakerDialogue["Engagement_Dialogue"];
                    Dictionary<string, string[]> genericMarriageDialogueDict = currentSpeakerDialogue["Generic_Marriage_Dialogue"];
                    Dictionary<string, string[]> extraDialogueDict = currentSpeakerDialogue["Marriage_Dialogue"];
                    Dictionary<string, string[]> rainyDialogueDict = currentSpeakerDialogue["Rainy"];


                    Dictionary<string, string[]> marriageDialogueDict = currentSpeakerDialogue["Marriage_Dialogue"];
                    Dictionary<string, string[]> npcGiftTastesDialogueDict = currentSpeakerDialogue["NpcGiftTastes"];

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

                    characterDialogueDictionaries.Add(internalDialogueDict);
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

                            return dialogueDictionaryNames[i] + characterDialogueDictionaries[i][formattedDialogueString][0];
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

                                    return dialogueDictionaryNames[i] + characterDialogueDictionaries[i][key][0];

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
                        if (splitDialogueString.Intersect(splitRandDialogueLine).Count() > 0 && splitDialogueString.Length == splitRandDialogueLine.Length && splitDialogueString.Length - splitDialogueString.Intersect(splitRandDialogueLine).Count() <= 3)
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
            Game1.activeClickableMenu = new Menu(this.dialogueCategory, this.Config, this.helper);
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

        private String GetSupportedCharacterFromString(string input)
        {
            for (int i = 0; i < this.supportedCharacters.Count; i++)
            {
                if (input.Contains(this.supportedCharacters[i]))
                {
                    return this.supportedCharacters[i];
                }
            }

            return "Could not find supported character in string :I";
        }

        private Dictionary<string, Dictionary<string, string[]>> GetAllDialogueForCharacter(string characterName)
        {

            try
            {

                Dictionary<string, Dictionary<string, string[]>> currentSpeakerDialogue = NpcDialogueJsonDict[characterName];

                Dictionary<string, string[]> dialogueDict = currentSpeakerDialogue["Dialogue"];
                Dictionary<string, string[]> internalDialogueDict = currentSpeakerDialogue["Internal_Dialogue"];
                Dictionary<string, string[]> npcGiftTastesDialogueDict = currentSpeakerDialogue["NpcGiftTastes"];

                // TODO: Update when non-marriageable characters are added
                Dictionary<string, string[]>  marriageDialogue = currentSpeakerDialogue["Marriage_Dialogue"];


                Dictionary<string, string[]> engagementDialogueDict = this.GenericNpcDialogueJsonDict["Engagement_Dialogue"];
                Dictionary<string, string[]> genericMarriageDialogueDict = this.GenericNpcDialogueJsonDict["Generic_Marriage_Dialogue"];
                Dictionary<string, string[]> extraDialogueDict = this.GenericNpcDialogueJsonDict["Extra_Dialogue"];
                Dictionary<string, string[]> rainyDialogueDict = this.GenericNpcDialogueJsonDict["Rainy"];

                Dictionary<string, Dictionary<string, string[]>> allDialogueForCharacter = new Dictionary<string, Dictionary<string, string[]>>();
                allDialogueForCharacter.Add("Dialogue", dialogueDict);
                allDialogueForCharacter.Add("Internal_Dialogue", internalDialogueDict);
                allDialogueForCharacter.Add("NpcGiftTastes", npcGiftTastesDialogueDict);
                allDialogueForCharacter.Add("Marriage_Dialogue", marriageDialogue);
                allDialogueForCharacter.Add("Engagement_Dialogue", engagementDialogueDict);
                allDialogueForCharacter.Add("Generic_Marriage_Dialogue", genericMarriageDialogueDict);
                allDialogueForCharacter.Add("Extra_Dialogue", extraDialogueDict);
                allDialogueForCharacter.Add("Rainy", rainyDialogueDict);

                return allDialogueForCharacter;
            }
            catch (Exception ex)
            {
                Monitor.Log($"Error getting dialogue dictionaries for {characterName}: {ex}", LogLevel.Debug);
                return new Dictionary<string, Dictionary<string, string[]>>();
            }

        }

        private void PopulateTestDialogueList()
        {
            List<Dictionary<string, string[]>> characterDialogueDictionaries = new List<Dictionary<string, string[]>>();
            List<string> dialogueDictionaryNames = new List<string>();

            Dictionary<string, Dictionary<string, string[]>> abigailDictionary;

            try
            {
                abigailDictionary = GetAllDialogueForCharacter("Abigail");

            }
            catch (Exception ex) {
                Monitor.Log($"Could not find dialogue for Abigail in testing. {ex}");
                return;
            }


            characterDialogueDictionaries.Add(abigailDictionary["Extra_Dialogue"]);
            dialogueDictionaryNames.Add("Extra_Dialogue-");

            characterDialogueDictionaries.Add(abigailDictionary["Engagement_Dialogue"]);
            dialogueDictionaryNames.Add("Engagement_Dialogue-");


            characterDialogueDictionaries.Add(abigailDictionary["Generic_Marriage_Dialogue"]);
            dialogueDictionaryNames.Add("Generic_Marriage_Dialogue-");

            characterDialogueDictionaries.Add(abigailDictionary["Rainy"]);
            dialogueDictionaryNames.Add("Rainy-");

            characterDialogueDictionaries.Add(abigailDictionary["Marriage_Dialogue"]);
            dialogueDictionaryNames.Add("Marriage_Dialogue-");


            characterDialogueDictionaries.Add(abigailDictionary["Dialogue"]);
            dialogueDictionaryNames.Add("Dialogue-");

  

            for (int i = 0; i < characterDialogueDictionaries.Count; i++)
            {
                foreach (string key in characterDialogueDictionaries[i].Keys)
                {
                    bool confirmedCharacterDialogue = true;

                    // Make sure not to add non abigail dialogue
                    if (dialogueDictionaryNames[i] != "Dialogue-"
                        && dialogueDictionaryNames[i] != "Marriage_Dialogue-"
                        && dialogueDictionaryNames[i] != "Npc Gift_Tastes-"
                        && dialogueDictionaryNames[i] != "Internal_Dialogue-")
                    {
                        confirmedCharacterDialogue = false;
                        foreach (string value in characterDialogueDictionaries[i][key])
                        {
                          
                            Match characterNameMatch = Regex.Match(value, this.characterNameRegex, RegexOptions.IgnoreCase);
                            if ((characterNameMatch.Success && value.Contains("Abigail")) || !characterNameMatch.Success)
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