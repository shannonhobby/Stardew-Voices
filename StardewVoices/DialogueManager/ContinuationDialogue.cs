using System.Collections.Generic;

namespace StardewVoices.DialogueManager
{
    public static class ContinuationDialogue
    {
        List<string> continuationDialogue = new List<string>();

        private void setupContinuationDialogue()
        {
            for (int i = 0; i < this.supportedCharacters.Count; i++)
            {
                Dictionary<string, Dictionary<string, string[]>> dialogueDictionary = this.characterDictionary[this.supportedCharacters[i]].DialogueDictionary;
                Dictionary<string, string[]> continuationDialogue = dialogueDictionary["continuation_dialogue"];
                string[] continuationLineIds = continuationDialogue["continuation_dialogue"];
                this.continuationDialogue.AddRange(continuationLineIds);
            }
        }

    }
}