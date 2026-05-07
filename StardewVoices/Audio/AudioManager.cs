using Microsoft.Xna.Framework.Audio;
using StardewModdingAPI;
using StardewValley.Audio;
using StardewVoices.Utils;
using System.Threading;
using System;
using StardewVoices.Characters;
using StardewVoices.Pathing;
using System.Collections.Generic;

namespace StardewVoices.Audio
{
    public class AudioManager
    {
        internal AudioEngine AudioEngine {  get; }
        internal CuePlayer CuePlayer { get; }
        internal PathNode AudioPath { get; }
        public List<CharacterAudio> CharacterAudios { get; }

        public AudioManager(CharacterManager characterManager, PathManager pathManager) {
            AudioPath = pathManager.GetPath("Audio");
;           AudioEngine = new AudioEngine(AudioPathHelper.GetGameRelativeAudioPath(AudioPath.ToString(), "Stardew Voices.xgs"));

            try
            {
                foreach (string character in Constants.supportedCharacters)
                {
                    CharacterAudio newCharacterAudio = new CharacterAudio(character, this);
                    CharacterAudios.Add(newCharacterAudio);
                    characterManager.SetCharacterAudio(character, newCharacterAudio);
                }

            }
            catch (Exception e)
            {
                Monitor.Log($"Error loading sound bank, audio cannot play: {e}", LogLevel.Error);
            }
        }
    }
}