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
using StardewVoices.Utils;
using StardewVoices.Testing;
using StardewVoices.Config;

namespace StardewVoices.EventManager
{
    public class GameEventsService {
        /// <summary>The in-game location detected on the last update tick.</summary>
        private GameLocation lastLocation { get; set; }
        /// <summary>The in-game event detected on the last update tick.</summary>
        private Event lastEvent { get; set; }
        /// <summary>The last non-festival event whose dialogue json was loaded, and the location it started in.</summary>
        private Event startedEvent { get; set; }
        private string startedEventLocationName { get; set; }
        private WorldDate lastDate { get; set; }

        private ManagerHub _managers { get; }
        private ModConfig _config { get; }
        private DialoguePlaybackController _DialoguePlaybackController { get; }

        public GameEventsService(ManagerHub managers, ModConfig config)
        {
            _managers = managers;
            _config = config;
            _DialoguePlaybackController = new DialoguePlaybackController(managers.DialogueManager, this, managers.AudioManager);
        }

        public void changeCurrentDialogueBoxString(string dialogueBoxString)
        {
            _DialoguePlaybackController.changeCurrentDialogueBoxString(dialogueBoxString);
        }

        /// <summary>The method invoked when the game updates its state.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        public void OnUpdateTicked(object sender, EventArgs e)
        {
            _managers.TestingManager.createModTestingWorld();

            if (!Context.IsWorldReady)
                return;

            // Disposes voice lines that finished after being evicted from AudioManager's
            // current/last slots — otherwise they'd leak (see AudioManager.Release).
            _managers.AudioManager.SweepFinishedLines();

            _DialoguePlaybackController.UpdateCurrentDialogue();

            this.UpdateLastLocationAndEvent();

            // Setup testing if in testing world (only set in update tick because must be in specific farm name
            _managers.TestingManager.SetupTestDialogue();
           

            if (Game1.Date != null && Game1.Date != lastDate)
            {
                lastDate = Game1.Date;

                _DialoguePlaybackController.ResetEventsToday();

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
                ErrorLogger.Trace("K pressed");
                _managers.TestingManager.TriggerEvent();
            }

            if (isTestingWorldLoaded && e.Button == SButton.L)
            {
                ErrorLogger.Trace("L pressed");
                _managers.TestingManager.ResetCharacterDialogue();

            }

            if (isTestingWorldLoaded && e.Button == SButton.T)
            {
                ErrorLogger.Trace("T pressed");
                DialogueTestingManager.RunValidation();

            }

            if (isTestingWorldLoaded && e.Button == SButton.B)
            {
                ErrorLogger.Trace("B pressed");
                if (Game1.activeClickableMenu != null)
                {
                    Game1.activeClickableMenu = null;
                    _DialoguePlaybackController.currentlyTalking = false;
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

        public void OnSaveLoaded(object sender, SaveLoadedEventArgs e)
        {
           _managers.CharacterManager.SetupCharacterInGameNpcs();
           UpdateVoiceVolume();
        }


        /// <summary>Applies the configured voices volume slider.</summary>
        public void UpdateVoiceVolume()
        {
            _managers.AudioManager.Volume = VolumeCalculator.CalculateVolume(_config.DialogueVolume);
        }

        public void UpdateLastLocationAndEvent()
        {
            // Deal with events!

            this.lastLocation = Game1.currentLocation;

            if (Game1.CurrentEvent == null && this.lastEvent != null && this.lastLocation != null)
            {
                _DialoguePlaybackController.EndEvent();
            }

            if (Game1.CurrentEvent != null && !Game1.CurrentEvent.isFestival && this.lastEvent == null)
            {
                // A changeLocation command moves the event to a new location, and for a tick
                // in between Game1.CurrentEvent is null — so the same event shows up here
                // again. Its json lives under the location it started in, not the new one.
                if (!ReferenceEquals(Game1.CurrentEvent, this.startedEvent))
                {
                    this.startedEvent = Game1.CurrentEvent;
                    this.startedEventLocationName = this.lastLocation.Name;
                }

                string locationFilePath = Path.Combine(_managers.PathManager.GetPathString("Events"), this.startedEventLocationName);
                string lastEventId = Game1.CurrentEvent.id;

                if (lastEventId != null && lastEventId.Length > 0)
                {
                    string filePath = Path.Combine(locationFilePath, lastEventId + ".json");

                    try
                    {
                        _DialoguePlaybackController.StartEvent(filePath);

                    }
                    catch (Exception ex)
                    {
                        ErrorLogger.Debug($"Event not found. {ex}\nAttempted to read from filePath: {filePath}");

                    }

                }

            }

            // Festivals are just special events, so get the current festival from the current event.
            if (_DialoguePlaybackController.IsFestivalHappening())
            {
                _DialoguePlaybackController.StartFestival(Game1.CurrentEvent.FestivalName);
            }

            this.lastEvent = Game1.CurrentEvent;
            
            // TODO - is this logic correct regarding current event dict reset?
            // would the eventResponse dict also have to be reset? 
            // Have I flip flopped the logic? becasue eventResponseDict is set to null in EndEvent()

            // Unset Dialogue Dictionary when necessary
            if ((this.lastEvent == null || this.lastEvent.isFestival) && _DialoguePlaybackController.GetCurrentEventDict() != null)
            {
                _DialoguePlaybackController.SetCurrentEventDictToNull();
            }


            // Remember to unset the festival once the event changes!!
            if ((this.lastEvent == null || !this.lastEvent.isFestival) && _DialoguePlaybackController.GetLastFestival() != null)
            {
                _DialoguePlaybackController.ResetFestival();
            }

        }

    }
}