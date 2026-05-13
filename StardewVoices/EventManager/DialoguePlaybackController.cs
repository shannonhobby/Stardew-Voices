using Microsoft.Xna.Framework.Audio;
using StardewModdingAPI;
using StardewValley.Menus;
using StardewValley;
using System.Collections.Generic;
using System;
using StardewVoices.Dialogue;
using StardewVoices.Localization;
using StardewVoices.Audio;

namespace StardewVoices.EventManager
{
    public class DialoguePlaybackController
    {
        private DialogueManager _dialogueManager { get; }
        private AudioManager _audioManager { get; }

        private LocalizationManager _localizationManager { get; }

        public bool currentlyTalking { get => _currentlyTalking; set => _currentlyTalking = value; }
        private bool _currentlyTalking;
        private bool isReplacementDialogueBox = false;


        public DialoguePlaybackController(
            DialogueManager dialogueManager, LocalizationManager localizationManager, AudioManager audioManager) 
        {
            _dialogueManager = dialogueManager;
            _localizationManager = localizationManager;
            _audioManager = audioManager;
        }



        /// <summary>The in-game dialogue line detected on the last update tick.</summary>
        private Stack<StardewValley.Dialogue> LastDialogue;
        /// <summary>The in-game dialogue line detected on the last update tick.</summary>
        private string LastDialogueBoxString = "";


        private string currentLineId;
        private string lastLineId;

        // Event Management

        public Dictionary<string, string> GetCurrentEventDict()
        {
            return _dialogueManager.GetCurrentEventDict();
        }

        public void SetCurrentEventDictToNull()
        {
            _dialogueManager.SetCurrentEventDictToNull();
        }

        public void EndEvent()
        {
            _dialogueManager.EndEvent();
        }

        public void StartEvent(string eventFilePath) 
        {
            _dialogueManager.StartEvent(eventFilePath);
        }

        public void ResetEventsToday()
        {
            _dialogueManager.ResetEventsToday();
        }

        // Festival Management


        public Boolean IsFestivalHappening()
        {
            return Game1.CurrentEvent != null && Game1.CurrentEvent.isFestival && GetLastFestival != null;
        }

        public string GetLastFestival()
        {
            return _dialogueManager.GetLastFestival();
        }

        public void StartFestival(string festivalName)
        {
            _dialogueManager.SetFestival(festivalName);
        }

        public void ResetFestival()
        {
            _dialogueManager.ResetFestival();
        }

        public void changeCurrentDialogueBoxString(string newDialogueString)
        {
            if (Game1.activeClickableMenu != null && Game1.activeClickableMenu.GetType() == typeof(DialogueBox))
            {
               Game1.activeClickableMenu = new DialogueBox(newDialogueString);
            }
        }

        // TODO REFACTOR
        internal void UpdateCurrentDialogue()
        {

            string currentDialogueBoxString = GetCurrentDialogueBoxString();

            _dialogueManager.CheckForTextAboveHead();

            // If the last dialogue box string stored doesn't equal the current dialogue box string that means there's been a change! (yay!)
            if (!this.LastDialogueBoxString.Equals(currentDialogueBoxString))
            {
                // If dialogue has gone from non-empty to empty stop the audio cues.
                if (this.LastDialogueBoxString.Length > 0 && !DialogueBoxIsOpen())
                {
                    this.LastDialogueBoxString = "";
                    currentlyTalking = false;
                }

                if (Game1.currentSpeaker?.getName() != null && DialogueHelpers.StringContainsSupportedCharacter(Game1.currentSpeaker?.getName())
                    && (DialogueBoxIsOpen() && currentDialogueBoxString != "..."))
                {
                    if (isReplacementDialogueBox)
                    {
                        isReplacementDialogueBox = false;
                        this.LastDialogueBoxString = currentDialogueBoxString;
                        return;
                    }

                    string currentSpeaker = _localizationManager.GetName(Game1.currentSpeaker?.getName());


                    string currentLineId = _dialogueManager.getIdFromDialogueString(currentDialogueBoxString, currentSpeaker);


                    if (currentLineId.Contains("not found"))
                    {
                        this.Monitor.Log(currentLineId, LogLevel.Debug);
                        this.LastDialogueBoxString = currentDialogueBoxString;
                        return;
                    }

                    Cue cue = _audioManager.GetCueFromDialogueId(currentLineId, currentSpeaker);

                    if (cue == null)
                    {
                        // this.Monitor.Log($"Cue could not be found for current line: {this.currentLineId}: {currentDialogueBoxString}", LogLevel.Debug);
                        this.LastDialogueBoxString = currentDialogueBoxString;
                        return;
                    }
                    else
                    {

                        _audioManager.StopLastCue();

                        // TODO - refactor ?? i think this is an attempt to fix the one after another dialogue
                        // If the last cue is still playing current cue probably isn't ready to play yet
                        if (_audioManager.LastCueIsStillPlaying() && !currentlyTalking)
                        {
                            currentlyTalking = true;
                            cue.Play();

                            // i THINK events are the only place the right after anotehr dialogue can happen 
                            if ((Game1.CurrentEvent != null && !Game1.CurrentEvent.isFestival)
                                || cue.Name.Contains("Gift")
                                || DialogueHelpers.DialogueHasContinuation(cue.Name)
                                || cue.Name.Contains("endDialogue")
                                || cue.Name.ToLower().Contains("event"))
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

                _audioManager.NextCue();
              

                this.LastDialogueBoxString = currentDialogueBoxString;

            }

        }

        private Boolean DialogueBoxIsOpen()
        {
            return Game1.activeClickableMenu != null 
                && Game1.activeClickableMenu.GetType() == typeof(DialogueBox)
                && ((DialogueBox)Game1.activeClickableMenu).getCurrentString().Length > 0;
        }

        private string GetCurrentDialogueBoxString()
        {
            IClickableMenu currentActiveClickableMenu = Game1.activeClickableMenu;

            if (
                currentActiveClickableMenu != null
                && currentActiveClickableMenu.GetType() == typeof(DialogueBox)
               )
            {
               return ((DialogueBox)currentActiveClickableMenu).getCurrentString();
            } else
            {
                return null;
            }
        }

    }
}