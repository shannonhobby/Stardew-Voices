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

        public CharacterManager() 
        {
            SetupCharacterDictionary();
        }

        public Character GetCharacter(string characterName)
        {
            return characterDictionary[characterName];
        }

        public void SetupCharacterInGameNpcs()
        {
            foreach (string characterName in Constants.supportedCharacters)
            {
                characterDictionary[characterName].SetupNpc();
            }
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