
using System.Collections.Generic;
using System;

namespace StardewVoices.DialogueManager
{
    /// <summary>
    /// Type for dialogue dictionaries with multiple groupings / types of dialogue.
    /// I.i. Generic Dialogue: Schedule, Gift Tastes, Rainy (etc) which will map the corresponding
    /// to its line->line id dictionary.
    /// </summary>
    public class DialogueDictionary
    {
        private readonly Dictionary<string, Dictionary<string, List<string>>> _data = new();

        public void Add(string type, string lineId, string li)
        {
            // todo
        }

        public IReadOnlyList<string> Get(string type, string category)
        {
            return _data.TryGetValue(type, out var categories) &&
                   categories.TryGetValue(category, out var list)
                ? list
                : Array.Empty<string>();
        }
    }
}