using Microsoft.Xna.Framework.Audio;
using StardewModdingAPI;
using StardewValley.Menus;
using StardewValley;
using System.Collections.Generic;

namespace StardewVoices.EventManager
{
    public class DialogueEventManager
    {
        public DialogueEventManager() { }



        /// <summary>The in-game dialogue line detected on the last update tick.</summary>
        private Stack<Dialogue> LastDialogue;
        /// <summary>The in-game dialogue line detected on the last update tick.</summary>
        private string LastDialogueBoxString = "";


        private string currentLineId;
        private string lastLineId;

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



                if (Game1.currentSpeaker?.getName() != null && StringContainsSupportedCharacter(Game1.currentSpeaker?.getName())
                    && (currentActiveClickableMenu != null && currentActiveClickableMenu.GetType() == typeof(DialogueBox)) && currentDialogueBoxString.Length > 0 && currentDialogueBoxString != "...")
                {
                    if (isReplacementDialogueBox)
                    {
                        isReplacementDialogueBox = false;
                        this.LastDialogueBoxString = currentDialogueBoxString;
                        return;
                    }

                    string currentSpeaker = GetName(Game1.currentSpeaker?.getName());


                    string currentLineId = getIdFromDialogueString(currentDialogueBoxString, currentSpeaker);


                    if (currentLineId.Contains("not found"))
                    {
                        this.Monitor.Log(currentLineId, LogLevel.Debug);
                        this.LastDialogueBoxString = currentDialogueBoxString;
                        return;
                    }

                    this.currentCue = this.getCueFromDialogueId(currentLineId, currentSpeaker);

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

    }
}