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
        string characterNameRegex = "";
        private Dictionary<string, Character> characterDictionary = new Dictionary<string, Character>();

        public CharacterManager() 
        {
            SetupCharacterDictionary();
            PopulateCharacterNamesList();
        }

        public Character GetCharacter(string characterName)
        {
            return characterDictionary[characterName];
        }
      

        private void SetupCharacterDictionary()
        {
            for (int i = 0; i < Constants.supportedCharacters.Count; i++)
            {
                this.characterDictionary.Add(Constants.supportedCharacters[i], new Character(Constants.supportedCharacters[i]));
            }
        }

        internal void SetCharacterAudio(string characterName, CharacterAudio audio)
        {
            try
            {
                characterDictionary[characterName].SetCharacterAudio(audio);
            } catch (Exception e)
            {
                // LOG ERROR
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