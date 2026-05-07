using StardewModdingAPI.Events;
using StardewModdingAPI;
using StardewValley;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System;
using Newtonsoft.Json;
using System.IO;
using System.Text;

namespace StardewVoices.EventManager
{
    public class GameEvents {
        /// <summary>The in-game location detected on the last update tick.</summary>
        private GameLocation _lastLocation = null;
        private WorldDate _lastDate = null;
        private bool _currentlyTalking;

        private ManagerHub _managers { get; }
        private DialogueEventManager _dialogueEventManager { get; }

        public GameEvents(ManagerHub managers)
        {
            _managers = managers;
            _dialogueEventManager = new DialogueEventManager();
        }

        /// <summary>The method invoked when the game updates its state.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        public void OnUpdateTicked(object sender, EventArgs e)
        {
            this.createModTestingWorld();

            this.UpdateCurrentDialogue();

            this.UpdateLastLocationAndEvent();


            foreach (string character in Constants.LoveInterests)
            {
                // Update volume
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
        public void OnButtonPressed(object sender, ButtonPressedEventArgs e)
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
                if (currentEventSplitString.Length > 2)
                {
                    location = String.Join("_", currentEventSplitString.Skip(1).ToArray());
                }
                else
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
                    NPC inGameCharacter = characterDictionary[name].GetNPC();
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

        public void OnGameLaunched(object sender, GameLaunchedEventArgs e)
        {
            _managers.menu.SetupGMCM(Helper, ModManifest);
        }


        public void UpdateLastLocationAndEvent()
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
                        string jsonString = File.ReadAllText(filePath, Encoding.UTF8);
                        this.currentEventDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonString);
                        this.eventsToday.Add(this.currentEventDict);

                    }
                    catch (Exception ex)
                    {
                        this.Monitor.Log($"Event not found. {ex}", LogLevel.Debug);
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

                    string jsonString = File.ReadAllText(filePath, Encoding.UTF8);
                    this.currentEventDict = null;
                    this.currentFestivalDict = JsonConvert.DeserializeObject<Dictionary<string, string[]>>(jsonString);

                }
                catch (Exception ex)
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

    }
}