
using System.Collections.Generic;
using System;
using Newtonsoft.Json;

namespace StardewVoices.Dialogue
{
    /// <summary>
    /// Type for dialogue dictionaries with multiple groupings / types of dialogue.
    /// I.i. Generic Dialogue: Schedule, Gift Tastes, Rainy (etc) which will map the corresponding
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

            return null;
        }

        public string CheckAllDictionariesForDialogueLine(string  dialogueLine)
        {

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