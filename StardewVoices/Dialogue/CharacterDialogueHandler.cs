using Newtonsoft.Json;
using StardewVoices.Pathing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StardewVoices.Characters;
using StardewVoices.Testing;
using StardewVoices.Utils;

namespace StardewVoices.Dialogue
{
    public class CharacterDialogueHandler
    {

        private CharacterManager _characterManager { get; }
        private TestingManager _testingManager { get; }

        public CharacterDialogueHandler(PathManager pathManager, CharacterManager characterManager, TestingManager testingManager) 
        {
            _characterManager = characterManager;
            foreach(string characterName in Constants.supportedCharacters)
            {
                string characterDialoguePath = Path.Combine(
                    pathManager.GetPathString("Characters"), 
                    characterName.ToLower() + ".json");
                Character character = characterManager.GetCharacter(characterName);
                character.SetDialogueDictionary(SetupDialogueDictionaryFromJsonPath(characterDialoguePath));
                
                // We only need the generic dialogue types because the dialogue and misc dialogue are included in
                // [characterName].json
                foreach(string dialogueType in Constants.GenericDialogueTypes)
                {
                    character.AddDialogueType(
                        dialogueType,
                        DialogueHelpers.GetDictionaryFromPath(pathManager.GetPathString(Constants.GenericDialogueTypeToFileName[dialogueType])));
                }
            }
        }

        private DialogueDictionary SetupDialogueDictionaryFromJsonPath(string path)
        {
            try
            {
                string jsonString = File.ReadAllText(path, Encoding.UTF8);
                return new DialogueDictionary(jsonString);

            }
            catch (Exception ex)
            {
                ErrorLogger.Debug($"Error setting up dialogue dictionary from path {path}.\n{ex} :(");
                return null;
            }
        }


        public string CheckForCharacterDialogueLineId(string currentSpeaker, string dialogue)
        {
            DialogueDictionary dialogueDictionary = _characterManager.GetCharacter(currentSpeaker).GetDialogueDictionary();
            return dialogueDictionary.CheckAllDictionariesForDialogueLine(dialogue, currentSpeaker, _testingManager);
        }

        public string CheckEverydayCharacterDictionariesForDialogue(string currentSpeaker, string dialogue)
        {
            DialogueDictionary currentSpeakerDialogueDict = _characterManager.GetCharacter(currentSpeaker).GetDialogueDictionary();

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

   

    }
}
 