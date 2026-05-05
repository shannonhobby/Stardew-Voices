using Microsoft.Xna.Framework.Audio;
using StardewModdingAPI;
using StardewVoices.Utils;
using System.Threading;
using System;
using StardewValley;
using System.Collections.Generic;

namespace StardewVoices.CharacterManager
{
    public class CharacterManager
    {
        string characterNameRegex = "";

        public CharacterManager() { }
        private void setupCharacterSoundBanks()
        {
            try
            {
                this.audioEngine = new AudioEngine(AudioPathHelper.GetGameRelativeAudioPath(audioPath, "Stardew Voices.xgs"));

                foreach (string character in this.supportedCharacters)
                {
                    this.characterDictionary[character].Audio.SetupAudioBanks(character, audioEngine, audioPath);
                }

            }
            catch (Exception e)
            {
                Monitor.Log($"Error loading sound bank, audio cannot play: {e}", LogLevel.Error);
            }
        }

        private void setupCharacterDictionary()
        {
            for (int i = 0; i < this.supportedCharacters.Count; i++)
            {
                this.characterDictionary.Add(supportedCharacters[i], new Character(supportedCharacters[i]));
            }
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
            catch (Exception ex)
            {
                Monitor.Log($"Error populating character names list: {ex}", LogLevel.Debug);
                return;
            }

        }

    }
}