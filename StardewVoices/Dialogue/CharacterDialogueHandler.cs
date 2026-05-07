using Newtonsoft.Json;
using StardewVoices.Pathing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StardewVoices.Characters;

namespace StardewVoices.Dialogue
{
    internal class CharacterDialogueHandler
    {
        public CharacterDialogueHandler(PathManager pathManager, CharacterManager characterManager) 
        {
            foreach(string characterName in Constants.supportedCharacters)
            {
                string characterDialoguePath = Path.Combine(
                    pathManager.GetPathString("Characters"), 
                    characterName.ToLower() + ".json");
                Character character = characterManager.GetCharacter(characterName);
                character.SetDialogueDictionary(SetupDialogueDictionaryFromJsonPath(characterDialoguePath));
                
                foreach(string dialogueType in Constants.DialogueTypes)
                {
                    character.AddDialogueType(
                        dialogueType,
                        GetDictionaryFromPath(pathManager.GetPathString(Constants.DialogueTypeToFileName[dialogueType])));
                }
            }
        }

        private DialogueDictionary SetupDialogueDictionaryFromJsonPath(string path) 
        {
            try
            {
                string jsonString = File.ReadAllText(path, Encoding.UTF8);
                return new DialogueDictionary(jsonString);
             
            }catch(Exception ex)
            {
                // error handling
                return null;
            }
           
        }

        private Dictionary<string, string[]> GetDictionaryFromPath(string path)
        {
            try
            {
                string jsonString = File.ReadAllText(path, Encoding.UTF8);
                return JsonConvert.DeserializeObject<Dictionary<string, string[]>>(jsonString).ToDictionary(
                                kvp => DialogueHelpers.Normalize(kvp.Key),
                                kvp => kvp.Value
                            );
            } catch
            {
                // error handling
                return null;
            }
        
        }


    }
}
 