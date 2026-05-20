using StardewVoices.Dialogue;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StardewVoices.Testing.Models
{
    public class DialogueTestCase
    {
        public string CharacterName { get; }
        public StardewValley.Dialogue DialogueLine { get; }
        public string CueName { get; set; }
        public string EventId { get; set; } // optional

        public DialogueTestCase(string characterName, StardewValley.Dialogue dialogueLine, DialogueManager dialogueManager)
        {
            CharacterName = characterName;
            DialogueLine = dialogueLine;
            CueName = dialogueManager.getIdFromDialogueString(characterName, dialogueLine.getCurrentDialogue());
        }
    }
}
