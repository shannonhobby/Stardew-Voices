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
        private GameLocation lastLocation { get; set; }
        /// <summary>The in-game event detected on the last update tick.</summary>
        private Event lastEvent { get; set; }
        private WorldDate lastDate { get; set; }

        private ManagerHub _managers { get; }
        private DialogueEventManager _dialogueEventManager { get; }

        public GameEvents(ManagerHub managers)
        {
            _managers = managers;
            _dialogueEventManager = new DialogueEventManager(managers.DialogueManager);
        }

        public void changeCurrentDialogueBoxString(string dialogueBoxString)
        {
            _dialogueEventManager.changeCurrentDialogueBoxString(dialogueBoxString);
        }

        /// <summary>The method invoked when the game updates its state.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        public void OnUpdateTicked(object sender, EventArgs e)
        {
            _managers.TestingManager.createModTestingWorld();

            _dialogueEventManager.UpdateCurrentDialogue();

            this.UpdateLastLocationAndEvent();


            foreach (string character in Constants.LoveInterests)
            {
                // Update volume
            }

            // Setup testing if in testing world (only set in update tick because must be in specific farm name
            _managers.TestingManager.SetupTestDialogue();
           

            if (Game1.Date != null && Game1.Date != lastDate)
            {
                lastDate = Game1.Date;

                _dialogueEventManager.ResetEventsToday();

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

            Boolean isTestingWorldLoaded = _managers.TestingManager.IsTestingWorldLoaded();

            if (isTestingWorldLoaded && e.Button == SButton.K)
            {
                _managers.TestingManager.TriggerEvent();
            }

            if (isTestingWorldLoaded && e.Button == SButton.L)
            {
                _managers.TestingManager.ResetCharacterDialogue();

            }

            if (isTestingWorldLoaded && e.Button == SButton.B)
            {
                this.Monitor.Log($"B Pressed", LogLevel.Debug);
                if (Game1.activeClickableMenu != null)
                {
                    Game1.activeClickableMenu = null;
                    _dialogueEventManager.currentlyTalking = false;
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
            _managers.Menu.SetupGMCM();
        }


        public void UpdateLastLocationAndEvent()
        {
            // Deal with events!

            this.lastLocation = Game1.currentLocation;

            if (Game1.CurrentEvent == null && this.lastEvent != null && this.lastLocation != null)
            {
                _dialogueEventManager.EndEvent();
            }

            if (Game1.CurrentEvent != null && !Game1.CurrentEvent.isFestival && this.lastEvent == null)
            {
                string locationFilePath = Path.Combine(_managers.PathManager.GetPathString("Events"), this.lastLocation.Name);
                string lastEventId = Game1.CurrentEvent.id;

                if (lastEventId != null && lastEventId.Length > 0)
                {
                    string filePath = Path.Combine(locationFilePath, lastEventId + ".json");

                    try
                    {
                        _dialogueEventManager.StartEvent(filePath);

                    }
                    catch (Exception ex)
                    {
                        this.Monitor.Log($"Event not found. {ex}", LogLevel.Debug);
                        this.Monitor.Log($"Attempted to read from filePath: {filePath}", LogLevel.Debug);

                    }

                }

            }

            // Festivals are just special events, so get the current festival from the current event.
            if (_dialogueEventManager.IsFestivalHappening())
            {
                _dialogueEventManager.StartFestival(Game1.CurrentEvent.FestivalName);
            }

            this.lastEvent = Game1.CurrentEvent;
            
            // TODO - is this logic correct regarding current event dict reset?
            // would the eventResponse dict also have to be reset? 
            // Have I flip flopped the logic? becasue eventResponseDict is set to null in EndEvent()

            // Unset Dialogue Dictionary when necessary
            if ((this.lastEvent == null || this.lastEvent.isFestival) && _dialogueEventManager.GetCurrentEventDict() != null)
            {
                _dialogueEventManager.SetCurrentEventDictToNull();
            }


            // Remember to unset the festival once the event changes!!
            if ((this.lastEvent == null || !this.lastEvent.isFestival) && _dialogueEventManager.GetLastFestival() != null)
            {
                _dialogueEventManager.ResetFestival();
            }

        }

    }
}