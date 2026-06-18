using System.Collections.Generic;
using StardewVoices.Characters;

namespace StardewVoices.Dialogue
{
    public static class ContinuationDialogueHelper
    {
        private static List<string> _continuationDialogue = new List<string>();

        public static void Init(CharacterManager manager)
        {
            for (int i = 0; i < manager.SupportedCharacters.Count; i++)
            {
                Character character = manager.GetCharacter(manager.SupportedCharacters[i]);
                if (character == null) continue;
                DialogueDictionary dialogueDictionary = character.GetDialogueDictionary();
                Dictionary<string, string[]> continuationDialogue = dialogueDictionary.GetType("continuation_dialogue");
                string[] continuationLineIds = continuationDialogue["continuation_dialogue"];
                _continuationDialogue.AddRange(continuationLineIds);
            }
        }
 
        internal static bool DialogueHasContinuation(string dialogueId)
        {
            foreach (string id in _continuationDialogue)
            {
                if (dialogueId == id)
                {
                    return true;
                }
            }

            return false;
        }

    }
}