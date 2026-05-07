using Newtonsoft.Json;
using StardewVoices.Pathing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StardewVoices.Characters;
using StardewValley;

namespace StardewVoices.Dialogue
{
    internal class CharacterDialogueHandler
    {

        private CharacterManager _characterManageer { get; }
        public CharacterDialogueHandler(PathManager pathManager, CharacterManager characterManager) 
        {
            _characterManageer = characterManager;
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

        public string CheckAllCharacterDictionariesForDialogue(string currentSpeaker, string dialogue)
        {

        }

        public string CheckEverydayCharacterDictionariesForDialogue(string currentSpeaker, string dialogue)
        {
            DialogueDictionary currentSpeakerDialogueDict = _characterManageer.GetCharacter(currentSpeaker).GetDialogueDictionary();

            string dialogueCheck = currentSpeakerDialogueDict.CheckDictionaryForDialogueLine("dialogue", dialogue, currentSpeaker);
            string miscDialogueCheck = currentSpeakerDialogueDict.CheckDictionaryForDialogueLine("misc", dialogue, currentSpeaker);
            if (dialogueCheck != null)
            {
                return dialogueCheck;
            }
            else if (miscDialogueCheck != null)
            {
                return miscDialogueCheck;
            }
            else
            {
                return null;
            }
            /**else
            {
                closeEnoughKey = DialogueContainsCloseEnoughLine(dialogueDict, dialogue);
                 if (!closeEnoughKey.Contains("Not found"))
                {
                    if (dialogueDict[closeEnoughKey].Length > 1)
                    {
                        return this.getCorrectIdFromListOfLineIds(dialogueDict[closeEnoughKey], currentSpeaker, "Dialogue-");
                    }else
                    {
                        return dialogueDict[closeEnoughKey][0];

                    }
            }
            } */
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
 