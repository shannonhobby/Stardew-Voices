using StardewModdingAPI;
using StardewModdingAPI.Events;
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
    public class DialogueTestCase
    {
        public string CharacterName { get; }
        public StardewValley.DialogueLine DialogueLine { get; }
        public string DialogueText { get; }
        public string Context { get; }
        public string CueName { get; set; }
        public DialogueType DialogueType { get; set; }
        private IModHelper helper { get; }
        private DialogueManager dialogueManager { get; }

        public DialogueTestCase(string characterName, DialogueType dialogueType, StardewValley.DialogueLine dialogueLine, DialogueManager dialogueManager, IModHelper helper)
        {
            CharacterName = characterName;
            DialogueLine = dialogueLine;
            DialogueType = dialogueType;
            DialogueText = ParseDialogueString(dialogueLine.Text);
            this.helper = helper;
            this.dialogueManager = dialogueManager;
            CueName = dialogueManager.getIdFromDialogueString(DialogueText, characterName);
        }

        public DialogueTestCase(string characterName, DialogueType dialogueType, string dialogueText, string context, DialogueManager dialogueManager, IModHelper helper)
        {
            CharacterName = characterName;
            DialogueType = dialogueType;
            DialogueText = dialogueText;
            Context = context;
            this.helper = helper;
            this.dialogueManager = dialogueManager;
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
