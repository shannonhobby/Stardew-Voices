
using Microsoft.Xna.Framework.Audio;
using StardewModdingAPI;
using StardewValley;
using System;

namespace StardewVoices.Audio
{
    public class CuePlayer
    {
        private Cue lastCue {  get; set; }
        private Cue currentCue { get; set; }

        public void Play(string lineId, string characterName, SoundBank soundBank)
        {
            currentCue = getCueFromDialogueId(lineId, soundBank, characterName);

           
            if (this.lastCue != null && this.lastCue.IsPlaying)
            {
                this.lastCue.Stop(AudioStopOptions.Immediate);
            }

            this.currentCue.Play();

        }

        public void Stop()
        {
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

        public void NextCue()
        {
            if (this.currentCue != null)
            {
                this.lastCue = currentCue;
            }
        }

        private static Cue getCueFromDialogueId(string lineId, SoundBank soundBank, string characterName)
        {
            try
            {
                // TODO: Check if i actually do need the dialogue context?2
                return soundBank.GetCue(lineId);

            }
            catch (Exception error)
            {
                //TODO ERROR LOGGER
                //this.Monitor.Log($"Error getting cue for {characterName}: {lineId} Sound Bank: {error}", LogLevel.Debug);
                return null;
            }

        }
    }
}