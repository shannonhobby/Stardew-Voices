using System.Collections.Generic;
using StardewVoices.Characters;

namespace StardewVoices.Dialogue
{
    public class ContinuationDialogue
    {
        List<string> continuationDialogue = new List<string>();
        
        public ContinuationDialogue(CharacterManager characterManager)
        {
            for (int i = 0; i < Constants.supportedCharacters.Count; i++)
            {
                DialogueDictionary dialogueDictionary = characterManager.GetCharacter(Constants.supportedCharacters[i]).GetDialogueDictionary();
                Dictionary<string, string[]> continuationDialogue = dialogueDictionary.GetType("continuation_dialogue");
                string[] continuationLineIds = continuationDialogue["continuation_dialogue"];
                this.continuationDialogue.AddRange(continuationLineIds);
            }
        }

    }
}