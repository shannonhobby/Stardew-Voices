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

namespace StardewVoices
{
    /// <summary>The mod entry point.</summary>
    public class StardewVoices : Mod
    {
        private static string modPath = GetModPath();

        private static string audioPath = Path.Combine(modPath, "Audio");
        private static string inverseDialogueFolderPath = Path.Combine(modPath, "Dialogue Jsons");

        private static string inverseDialogueJsonPath = Path.Combine(inverseDialogueFolderPath, "Dialogue.json");
        private static string inverseExtraDialogueJsonPath = Path.Combine(inverseDialogueFolderPath, "ExtraDialogue.json");
        private static string inverseGenericMarriageDialogueJsonPath = Path.Combine(inverseDialogueFolderPath, "Generic Marriage Dialogue.json");
        private static string inverseMarriageDialogueJsonPath = Path.Combine(inverseDialogueFolderPath, "Marriage Dialogue.json");
        private static string inverseRainyDialogueJsonPath = Path.Combine(inverseDialogueFolderPath, "rainy.json");
        private static string inverseEngagementDialogueJsonPath = Path.Combine(inverseDialogueFolderPath, "EngagementDialogue.json");
        private static string inverseInternalDialogueJsonPath = Path.Combine(inverseDialogueFolderPath, "Internal Dialogue.json");
        private static string inverseNpcGiftTastesDialogueJsonPath = Path.Combine(inverseDialogueFolderPath, "NpcGiftTastes.json");


        private static string eventsJsonPath = Path.Combine(inverseDialogueFolderPath, "Events");
        private static string festivalsJsonPath = Path.Combine(inverseDialogueFolderPath, "Festivals");

        private static string festivalDatesJsonPath = Path.Combine(festivalsJsonPath, "FestivalDates.json");

        private WaveBank eventsWaveBank;
        private SoundBank eventsSoundBank;

        private WaveBank festivalsWaveBank;
        private SoundBank festivalsSoundBank;

        private WaveBank dialogueWaveBank;
        private SoundBank dialogueSoundBank;

        private WaveBank engagementDialogueWaveBank;
        private SoundBank engagementDialogueSoundBank;

        private WaveBank marriageDialogueWaveBank;
        private SoundBank marriageDialogueSoundBank;

        private WaveBank genericMarriageDialogueWaveBank;
        private SoundBank genericMarriageDialogueSoundBank;

        private WaveBank extraDialogueWaveBank;
        private SoundBank extraDialogueSoundBank;

        private WaveBank rainyWaveBank;
        private SoundBank rainySoundBank;

        private WaveBank internalDialogueWaveBank;
        private SoundBank internalDialogueSoundBank;

        private WaveBank npcGiftTastesWaveBank;
        private SoundBank npcGiftTastesSoundBank;

        private AudioEngine audioEngine;
        private AudioCategory dialogueCategory;

        private Cue currentCue;
        private Cue lastCue;
        private string currentLineId;
        private string lastLineId;

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

        private Dictionary<string, string[]> dialogueDict = null;
        private Dictionary<string, string[]> extraDialogueDict = null;
        private Dictionary<string, string[]> rainyDialogueDict = null;
        private Dictionary<string, string[]> marriageDialogueDict = null;
        private Dictionary<string, string[]> genericMarriageDialogueDict = null;
        private Dictionary<string, string[]> engagementDialogueDict = null;
        private Dictionary<string, string[]> internalDialogueDict = null;
        private Dictionary<string, string[]> npcGiftTastesDict = null;

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

        private bool currentlyTalking;

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


        /*********
        ** Public methods
        *********/
        /// <summary>The mod entry point, called after the mod is first loaded.</summary>
        /// <param name="helper">Provides simplified APIs for writing mods.</param>
        public override void Entry(IModHelper helper)
        {
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

                this.eventsWaveBank = new WaveBank(audioEngine, Path.Combine(audioPath, "Abigail Events.xwb"));
                this.eventsSoundBank = new SoundBank(audioEngine, Path.Combine(audioPath, "Abigail Events.xsb"));

                this.festivalsWaveBank = new WaveBank(audioEngine, Path.Combine(audioPath, "Abigail Festival Dialogue.xwb"));
                this.festivalsSoundBank = new SoundBank(audioEngine, Path.Combine(audioPath, "Abigail Festival Dialogue.xsb"));

                this.dialogueWaveBank = new WaveBank(audioEngine, Path.Combine(audioPath, "Abigail Dialogue.xwb"));
                this.dialogueSoundBank = new SoundBank(audioEngine, Path.Combine(audioPath, "Abigail Dialogue.xsb"));

                this.engagementDialogueWaveBank = new WaveBank(audioEngine, Path.Combine(audioPath, "Abigail Engagement Dialogue.xwb"));
                this.engagementDialogueSoundBank = new SoundBank(audioEngine, Path.Combine(audioPath, "Abigail Engagement Dialogue.xsb"));

                this.marriageDialogueWaveBank = new WaveBank(audioEngine, Path.Combine(audioPath, "Abigail Marriage Dialogue.xwb"));
                this.marriageDialogueSoundBank = new SoundBank(audioEngine, Path.Combine(audioPath, "Abigail Marriage Dialogue.xsb"));

                this.genericMarriageDialogueWaveBank = new WaveBank(audioEngine, Path.Combine(audioPath, "Abigail Generic Marriage Dialogue.xwb"));
                this.genericMarriageDialogueSoundBank = new SoundBank(audioEngine, Path.Combine(audioPath, "Abigail Generic Marriage Dialogue.xsb"));

                this.extraDialogueWaveBank = new WaveBank(audioEngine, Path.Combine(audioPath, "Abigail Extra Dialogue.xwb"));
                this.extraDialogueSoundBank = new SoundBank(audioEngine, Path.Combine(audioPath, "Abigail Extra Dialogue.xsb"));

                this.rainyWaveBank = new WaveBank(audioEngine, Path.Combine(audioPath, "Abigail Rainy.xwb"));
                this.rainySoundBank = new SoundBank(audioEngine, Path.Combine(audioPath, "Abigail Rainy.xsb"));

                this.internalDialogueWaveBank = new WaveBank(audioEngine, Path.Combine(audioPath, "Abigail Internal Dialogue.xwb"));
                this.internalDialogueSoundBank = new SoundBank(audioEngine, Path.Combine(audioPath, "Abigail Internal Dialogue.xsb"));

                this.npcGiftTastesWaveBank = new WaveBank(audioEngine, Path.Combine(audioPath, "NpcGiftTastes.xwb"));
                this.npcGiftTastesSoundBank = new SoundBank(audioEngine, Path.Combine(audioPath, "NpcGiftTastes.xsb"));
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
                string locationFilePath = Path.Combine(eventsJsonPath, this.lastLocation.Name);
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
                    if (!Directory.Exists(festivalsJsonPath))
                        throw new Exception("Festival dialogue folder could not be found");

                    string filePath = Path.Combine(festivalsJsonPath, (string)festivalNameToDateDict[this.lastFestival] + ".json");

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

        private Cue getCueFromDialogueId(string lineId, string dialogueContext)
        {
            switch (dialogueContext)
            {
                case "Dialogue":
                    try
                    {
                        return this.dialogueSoundBank.GetCue(lineId);
                    } catch(Exception error)
                    {
                        this.Monitor.Log($"Error getting cue for: {lineId} in Dialogue Sound Bank: {error}", LogLevel.Debug);
                        return null;
                    }

                case "Event Dialogue":
                    try
                    {
                        return this.eventsSoundBank.GetCue(lineId);
                    }
                    catch (Exception error)
                    {
                        this.Monitor.Log($"Error getting cue for: {lineId} in Events Sound Bank: {error}", LogLevel.Debug);
                        return null;
                    }

                case "Festival Dialogue":
                    try
                    {
                        return this.festivalsSoundBank.GetCue(lineId);
                    }
                    catch (Exception error)
                    {
                        this.Monitor.Log($"Error getting cue for: {lineId} in Festivals Sound Bank: {error}", LogLevel.Debug);
                        return null;
                    }

                case "Engagement Dialogue":
                    try
                    {
                        return this.engagementDialogueSoundBank.GetCue(lineId);
                    }
                    catch (Exception error)
                    {
                        this.Monitor.Log($"Error getting cue for: {lineId} in Engagement Sound Bank: {error}", LogLevel.Debug);
                        return null;
                    }

                case "Extra Dialogue":
                    try
                    {
                        return this.extraDialogueSoundBank.GetCue(lineId);
                    }
                    catch (Exception error)
                    {
                        this.Monitor.Log($"Error getting cue for: {lineId} in Extra Dialogue Sound Bank: {error}", LogLevel.Debug);
                        return null;
                    }

                case "Marriage Dialogue":
                    try
                    {
                        return this.marriageDialogueSoundBank.GetCue(lineId);
                    }
                    catch (Exception error)
                    {
                        this.Monitor.Log($"Error getting cue for: {lineId} in Marriage Dialogue Sound Bank: {error}", LogLevel.Debug);
                        return null;
                    }

                case "Generic Marriage Dialogue":
                    try
                    {
                        return this.genericMarriageDialogueSoundBank.GetCue(lineId);
                    }
                    catch (Exception error)
                    {
                        this.Monitor.Log($"Error getting cue for: {lineId} in Generic Marriage Dialogue Sound Bank: {error}", LogLevel.Debug);
                        return null;
                    }

                case "Rainy Dialogue":
                    try
                    {
                        return this.rainySoundBank.GetCue(lineId);
                    }
                    catch (Exception error)
                    {
                        this.Monitor.Log($"Error getting cue for: {lineId} in Rainy Dialogue Sound Bank: {error}", LogLevel.Debug);
                        return null;
                    }

                case "Npc Gift Tastes":
                    try
                    {
                        return this.npcGiftTastesSoundBank.GetCue(lineId);
                    }
                    catch (Exception error)
                    {
                        this.Monitor.Log($"Error getting cue for: {lineId} in Npc Gift Tastes Sound Bank: {error}", LogLevel.Debug);
                        return null;
                    }

                case "Internal Dialogue":
                    try
                    {
                        return this.internalDialogueSoundBank.GetCue(lineId);
                    }
                    catch (Exception error)
                    {
                        this.Monitor.Log($"Error getting cue for: {lineId} in Internal Dialogue Sound Bank: {error}", LogLevel.Debug);
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

                        if (currentCommand.Contains("textAboveHead") && (currentCommand.Contains("Abigail") || currentCommand.Contains("ClothesTherapyCharacters")))
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

                // hard code for Abigail
                if (currentCommand.Contains("textAboveHead") && (currentCommand.Contains("Abigail") || currentCommand.Contains("ClothesTherapyCharacters")))
                {
                    Regex rx = new Regex(@"""(.*?)""");
                    string commandDialogue;
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
         

                    if (currentCommand.Contains("ClothesTherapyCharacters") && commandDialogue != "It's sweaty in here...")
                    {
                        this.lastEventCommand = Game1.CurrentEvent.currentCommand;
                        return;
                    }

                    if(this.currentEventDict != null)
                    {
                        string textAboveHeadLineId = getIdFromDialogueString(this.formatDialogueString(commandDialogue), "Abigail");

                        if (textAboveHeadLineId.Contains("not found"))
                        {
                            this.Monitor.Log(textAboveHeadLineId, LogLevel.Debug);
                            this.lastEventCommand = Game1.CurrentEvent.currentCommand;
                            return;
                        }

                        this.currentCue = this.getCueFromDialogueId(textAboveHeadLineId.Split("-")[1], textAboveHeadLineId.Split("-")[0]);

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
                // Also only check if the speakers name is Abigail for performance purposes.
                if (
                    Game1.currentSpeaker?.getName() == "Abigail"
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

                    this.currentCue = this.getCueFromDialogueId(currentLine.Split("-")[1], currentLine.Split("-")[0]);
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

                string jsonString = File.ReadAllText(inverseDialogueJsonPath);
                this.dialogueDict = JsonConvert.DeserializeObject<Dictionary<string, string[]>>(jsonString);

                jsonString = File.ReadAllText(inverseExtraDialogueJsonPath);
                this.extraDialogueDict = JsonConvert.DeserializeObject<Dictionary<string, string[]>>(jsonString);

                jsonString = File.ReadAllText(inverseEngagementDialogueJsonPath);
                this.engagementDialogueDict = JsonConvert.DeserializeObject<Dictionary<string, string[]>>(jsonString);

                jsonString = File.ReadAllText(inverseGenericMarriageDialogueJsonPath);
                this.genericMarriageDialogueDict = JsonConvert.DeserializeObject<Dictionary<string, string[]>>(jsonString);

                jsonString = File.ReadAllText(inverseMarriageDialogueJsonPath);
                this.marriageDialogueDict = JsonConvert.DeserializeObject<Dictionary<string, string[]>>(jsonString);

                jsonString = File.ReadAllText(inverseRainyDialogueJsonPath);
                this.rainyDialogueDict = JsonConvert.DeserializeObject<Dictionary<string, string[]>>(jsonString);

                jsonString = File.ReadAllText(inverseInternalDialogueJsonPath);
                this.internalDialogueDict = JsonConvert.DeserializeObject<Dictionary<string, string[]>>(jsonString);

                jsonString = File.ReadAllText(inverseNpcGiftTastesDialogueJsonPath);
                this.npcGiftTastesDict = JsonConvert.DeserializeObject<Dictionary<string, string[]>>(jsonString);
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
                    string eventPath = Path.Combine(eventsJsonPath, Game1.currentLocation.Name);
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
                        if (this.dialogueDict.ContainsKey(formattedDialogueString))
                        {
                            if (this.dialogueDict[formattedDialogueString].Length > 1)
                            {
                                return this.getCorrectIdFromListOfLineIds(this.dialogueDict[formattedDialogueString], currentSpeaker, "Dialogue-");
                            }

                            return "Dialogue-" + this.dialogueDict[formattedDialogueString][0];
                        }

                        if (this.internalDialogueDict.ContainsKey(formattedDialogueString))
                        {
                            if (this.internalDialogueDict[formattedDialogueString].Length > 1)
                            {
                                return this.getCorrectIdFromListOfLineIds(this.internalDialogueDict[formattedDialogueString], currentSpeaker, "Internal Dialogue-");
                            }

                            return "Internal Dialogue-" + this.internalDialogueDict[formattedDialogueString][0];
                        }

                        return $"Festival dialogue not found for {formattedDialogueString}";
                    }
                }

                // If not currently in event or festival check all dialogue dictionaries
                else
                {
                    List<Dictionary<string, string[]>> characterDialogueDictionaries = new List<Dictionary<string, string[]>>();
                    List<string> dialogueDictionaryNames = new List<string>();

                    if (Game1.player.isEngaged() || testingWorldLoaded)
                    {
                        characterDialogueDictionaries.Add(this.engagementDialogueDict);
                        dialogueDictionaryNames.Add("Engagement Dialogue-");
                    }

                    if (Game1.player.getSpouse()?.getName() == currentSpeaker || testingWorldLoaded)
                    {
                        characterDialogueDictionaries.Add(this.marriageDialogueDict);
                        dialogueDictionaryNames.Add("Marriage Dialogue-");

                        characterDialogueDictionaries.Add(this.genericMarriageDialogueDict);
                        dialogueDictionaryNames.Add("Generic Marriage Dialogue-");
                    }

                    characterDialogueDictionaries.Add(this.dialogueDict);
                    dialogueDictionaryNames.Add("Dialogue-");

                    characterDialogueDictionaries.Add(this.extraDialogueDict);
                    dialogueDictionaryNames.Add("Extra Dialogue-");

                    characterDialogueDictionaries.Add(this.rainyDialogueDict);
                    dialogueDictionaryNames.Add("Rainy Dialogue-");

                    characterDialogueDictionaries.Add(this.npcGiftTastesDict);
                    dialogueDictionaryNames.Add("Npc Gift Tastes-");

                    characterDialogueDictionaries.Add(this.internalDialogueDict);
                    dialogueDictionaryNames.Add("Internal Dialogue-");

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

                        if (dialogueDictionaryNames[i] == "Extra Dialogue-" || dialogueDictionaryNames[i] == "Internal Dialogue-")
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

        private void PopulateTestDialogueList()
        {
            List<Dictionary<string, string[]>> characterDialogueDictionaries = new List<Dictionary<string, string[]>>();
            List<string> dialogueDictionaryNames = new List<string>();

            //characterDialogueDictionaries.Add(this.npcGiftTastesDict);
            //dialogueDictionaryNames.Add("Npc Gift Tastes-");

            //characterDialogueDictionaries.Add(this.internalDialogueDict);
            //dialogueDictionaryNames.Add("Internal Dialogue-");

            characterDialogueDictionaries.Add(this.extraDialogueDict);
            dialogueDictionaryNames.Add("Extra Dialogue-");

            characterDialogueDictionaries.Add(this.engagementDialogueDict);
            dialogueDictionaryNames.Add("Engagement Dialogue-");

            characterDialogueDictionaries.Add(this.marriageDialogueDict);
            dialogueDictionaryNames.Add("Marriage Dialogue-");

            characterDialogueDictionaries.Add(this.genericMarriageDialogueDict);
            dialogueDictionaryNames.Add("Generic Marriage Dialogue-");

            characterDialogueDictionaries.Add(this.dialogueDict);
            dialogueDictionaryNames.Add("Dialogue-");

            characterDialogueDictionaries.Add(this.rainyDialogueDict);
            dialogueDictionaryNames.Add("Rainy Dialogue-");


            for (int i = 0; i < characterDialogueDictionaries.Count; i++)
            {
                foreach (string key in characterDialogueDictionaries[i].Keys)
                {
                    bool confirmedCharacterDialogue = true;

                    // Make sure not to add non abigail dialogue
                    if (dialogueDictionaryNames[i] != "Dialogue-"
                        && dialogueDictionaryNames[i] != "Marriage Dialogue-"
                        && dialogueDictionaryNames[i] != "Npc Gift Tastes-"
                        && dialogueDictionaryNames[i] != "Internal Dialogue-")
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