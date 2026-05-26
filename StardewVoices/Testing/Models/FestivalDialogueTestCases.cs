using StardewVoices.Dialogue;
using System.Collections.Generic;
using System.IO;

namespace StardewVoices.Testing.Models
{
    public class FestivalDialogueTestCase
    {
        public string CharacterName { get; }
        public string FestivalDate { get; }
        public string DialogueText { get; }
        public string CueName { get; }
        public DialogueType DialogueType => DialogueType.Festival;

        public FestivalDialogueTestCase(string characterName, string festivalDate, string dialogueText, DialogueManager dialogueManager)
        {
            CharacterName = characterName;
            FestivalDate = festivalDate;
            DialogueText = dialogueText;
            CueName = dialogueManager.getIdFromDialogueString(dialogueText, characterName);
        }
    }
}
