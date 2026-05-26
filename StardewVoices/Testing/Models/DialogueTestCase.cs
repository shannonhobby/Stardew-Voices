using StardewValley;
using StardewValley.GameData.Characters;
using StardewVoices.Dialogue;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace StardewVoices.Testing.Models
{
    public enum DialogueType
    {
        Dialogue,
        Marriage,
        Generic,
        Engagement,
        Schedule,
        Rainy,
        Event,
        Festival
    }
    public class DialogueTestCase
    {
        public string CharacterName { get; }
        public StardewValley.DialogueLine DialogueLine { get; }
        public string CueName { get; set; }

        public DialogueType DialogueType { get; set; }

        public DialogueTestCase(string characterName, DialogueType dialogueType, StardewValley.DialogueLine dialogueLine, DialogueManager dialogueManager)
        {
            CharacterName = characterName;
            DialogueLine = dialogueLine;
            DialogueType = dialogueType;
            CueName = dialogueManager.getIdFromDialogueString(ParseDialogueString(dialogueLine.Text), characterName);
        }

        private string ParseDialogueString(string dialogue)
        {
            if (dialogue.Contains('}'))
            {
                dialogue = dialogue.Substring(dialogue.IndexOf("}") + 1);
            }
            dialogue = dialogue.Replace("{", "");

            if (dialogue.Contains("$"))
            {
                return Regex.Replace(dialogue, @"\$.", "");
            }

            return dialogue;
        }

    }
}
