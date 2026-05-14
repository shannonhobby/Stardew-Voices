
using System.Collections.Generic;
using System;
using Newtonsoft.Json;
using StardewVoices.Characters;
using StardewValley;
using StardewVoices.Testing;
using System.Linq;
using Microsoft.Xna.Framework.Input;

namespace StardewVoices.Dialogue
{
    /// <summary>
    /// Type for dialogue dictionaries with multiple groupings / types of dialogue.
    /// I.e. Generic Dialogue: Schedule, Gift Tastes, Rainy (etc) which will map the corresponding
    /// to its line->line id dictionary.
    /// </summary>
    public class DialogueDictionary
    {
        private Dictionary<string, Dictionary<string, string[]>> _data { get; }

        public DialogueDictionary(string jsonString)
        {
            _data = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, string[]>>>(jsonString);
        }

        public string CheckDictionaryForDialogueLine(string type, string dialogueLine, string currentSpeaker)
        {
            Dictionary<string, string[]> dialogueDict = GetType(type);
        
            if (dialogueDict.ContainsKey(dialogueLine))
            {
                if (dialogueDict[dialogueLine].Length > 1) {
                    return DialogueHelpers.getCorrectIdFromListOfLineIds(dialogueDict[dialogueLine], currentSpeaker, "Dialogue-");
                }

                return dialogueDict[dialogueLine][0];
            }
            else if (type == "misc" || type == "extra")
            {
                string foundLine = CheckDictionaryForVariableLines(dialogueDict, dialogueLine, currentSpeaker, type);
                if(foundLine != null)
                {
                    return foundLine;
                }
            }

            return null;
        }

        // TODO - WHAT DOES THIS DO??? IT JUST SKIPS LINES W/ VARIABLES? THIS IS SO COMPUTATIONALLY HEAVY
        private string CheckDictionaryForVariableLines(Dictionary<string, string[]> dict, string dialogueLine, string currentSpeaker, string type)
        {
            foreach (string key in dict.Keys)
            {
                if (!key.Contains("{") && !key.Contains("%"))
                {
                    continue;
                }

                string[] splitKey = key.Split(" ");
                string[] splitDialogueString = dialogueLine.Split(" ");


                // TODO what the fuck is this
                // Check to see if the amount of shared words is ALMOST equivalent to the length minus the number of substitutions possible (i've only seen up to 3)
                if (splitDialogueString.Intersect(splitKey).Count() > 0 && splitDialogueString.Length == splitKey.Length && splitDialogueString.Length - splitDialogueString.Intersect(splitKey).Count() <= 3)
                {
                    // Check for dad / mom / name stuff
                    if (CheckForParentString(splitDialogueString, splitKey))
                    {
                        continue;
                        // todo - is the first part of this if statement necessary?
                    } else if(!splitDialogueString.Contains("Dad") && !splitDialogueString.Contains("Mom") && CheckForPronounString(dialogueLine, key))
                    {
                        continue;
                    }

                    if (dict[key].Length > 1)
                    {
                        // TODO
                        /*
                        return DialogueHelpers.getCorrectIdFromListOfLineIds(dict[key], currentSpeaker, dialogueDictionaryNames[i]);
                        */
                    }

                    return dict[key][0];
                }
            }

            return null;
        }

        private Boolean CheckForParentString(string[] splitDialogueString, string[] splitKey)
        {
            return splitDialogueString.Contains("Dad") && !splitKey.Contains("Dad")
                        || splitDialogueString.Contains("Mom") && !splitKey.Contains("Mom")
                        || (!splitDialogueString.Contains("Dad") && !splitDialogueString.Contains("Mom") && splitKey.Contains("Dad") && splitKey.Contains("Mom"));
        }

        private Boolean CheckForPronounString(string dialogueLine, string key)
        {
            return dialogueLine.ToLower().Contains("she") && !key.ToLower().Contains("she")
                    || dialogueLine.ToLower().Contains(" he ") && !key.ToLower().Contains(" he ") // Spaces are because she and her also contain "he"
                    || dialogueLine.ToLower().Contains("him") && !key.ToLower().Contains("him")
                    || dialogueLine.ToLower().Contains("her") && !key.ToLower().Contains("her");
        }

        public string CheckAllDictionariesForDialogueLine(string  dialogueLine, string currentSpeaker, TestingManager testingManager)
        {
            string foundDialogueLine = null;
            foreach (string type in _data.Keys)
            {
                if (ShouldCheckDialogue(type, currentSpeaker) || testingManager.IsTestingWorldLoaded())
                {
                   foundDialogueLine = CheckDictionaryForDialogueLine(type, dialogueLine, currentSpeaker);
                    if (foundDialogueLine != null)
                    {
                        return foundDialogueLine;
                    }
                }
            }
            return foundDialogueLine;
        }

        private Boolean ShouldCheckDialogue(string type, string currentSpeaker)
        {
            return ShouldCheckEngagementDialogue(type) || ShouldCheckMarriageDialogue(type, currentSpeaker);
        }

        private Boolean ShouldCheckMarriageDialogue(string type, string currentSpeaker)
        {
            return type == "marriage_dialogue" || type == "generic_marriage" &&
                    Game1.player.getSpouse()?.getName() == currentSpeaker;
        }

        private Boolean ShouldCheckEngagementDialogue(string type)
        {
            return type == "engagement" && Game1.player.isEngaged();
        }

        public void AddType(string type, Dictionary<string, string[]> dialogue)
        {
            _data[type] = dialogue;
        }

        public Dictionary<string, string[]> GetType(string type) 
        {
            return _data[type];
        }

        public Dictionary<string, Dictionary<string, string[]>> Data()
        {
            return this._data;
        }

        public IReadOnlyList<string> GetLineId(string type, string line)
        {
            return _data.TryGetValue(type, out var categories) &&
                   categories.TryGetValue(line, out var list)
                ? list
                : Array.Empty<string>();
        }
    }
}