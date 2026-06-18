using Microsoft.Xna.Framework.Audio;
using StardewModdingAPI;
using StardewVoices.Utils;
using System.Threading;
using System;
using StardewValley;
using System.Collections.Generic;
using StardewVoices.Audio;

namespace StardewVoices.Characters
{
    public class CharacterManager
    {
        private string characterNameRegex { get; set; }
        private Dictionary<string, Character> characterDictionary = new Dictionary<string, Character>();
        public List<string> SupportedCharacters { get; }

        public CharacterManager(List<string> supportedCharacters)
        {
            SupportedCharacters = supportedCharacters;
            SetupCharacterDictionary();
        }

        public Character GetCharacter(string characterName)
        {
            if (!characterDictionary.TryGetValue(characterName, out Character character))
            {
                ErrorLogger.Warn($"GetCharacter called for unknown character: {characterName}");
                return null;
            }
            return character;
        }

        public void SetupCharacterInGameNpcs()
        {
            if (Game1.characterData == null)
            {
                ErrorLogger.Warn("Cannot validate SupportedCharacters: game character data not loaded yet.");
                return;
            }

            foreach (string characterName in SupportedCharacters)
            {
                if (!Game1.characterData.ContainsKey(characterName))
                {
                    ErrorLogger.Warn($"'{characterName}' in SupportedCharacters does not match any Stardew Valley NPC and will be skipped.");
                    continue;
                }
                characterDictionary[characterName].SetupNpc();
            }
        }

        private void SetupCharacterDictionary()
        {
            for (int i = 0; i < SupportedCharacters.Count; i++)
            {
                this.characterDictionary.Add(SupportedCharacters[i], new Character(SupportedCharacters[i]));
            }
        }

        internal void SetCharacterAudio(string characterName, CharacterAudio audio)
        {
            try
            {
                characterDictionary[characterName].SetCharacterAudio(audio);
            } catch (Exception e)
            {
                ErrorLogger.Warn($"Error setting character audio for {characterName}. {e}");
            }
        }

        internal string GetCharacterNameRegex()
        {
            return characterNameRegex;
        }



        public void PopulateCharacterNamesList()
        {
            if (Game1.characterData == null)
            {
                ErrorLogger.Trace($"Error populating character names list: no game character data found?");
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
                ErrorLogger.Trace($"Error populating character names list: {ex}");
                return;
            }

        }

    }
}