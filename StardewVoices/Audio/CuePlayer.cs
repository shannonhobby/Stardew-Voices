
using Microsoft.Xna.Framework.Audio;
using StardewModdingAPI;
using System;

namespace StardewVoices.Audio
{
    public class CuePlayer
    {
        private Cue lastCue {  get; set; }
        private Cue currentCue { get; set; }

        public void Play(string lineId, string characterName, SoundBank soundBank)
        {

        }

        private static Cue getCueFromDialogueId(string lineId, SoundBank soundBank, string characterName)
        {
            try
            {
                // TODO: Check if i actually do need the dialogue context?
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