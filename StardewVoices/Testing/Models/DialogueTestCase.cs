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
using StardewVoices.Utils.ParsingHelpers;

namespace StardewVoices.Testing.Models
{
    public class DialogueTestCase
    {
        public string CharacterName { get; }
        public LabelledDialogueLine DialogueLine { get; }
        public string DialogueText { get; }
        public string Context { get; }
        public string ResolvedLineId { get; set; }
        public bool HasVariations = false;
        public DialogueType DialogueType { get; set; }
        private IModHelper helper { get; }
        private DialogueManager dialogueManager { get; }

        public DialogueTestCase(string characterName, DialogueType dialogueType, LabelledDialogueLine dialogueLine, DialogueManager dialogueManager, IModHelper helper)
        {
            CharacterName = characterName;
            DialogueLine = dialogueLine;
            DialogueText = dialogueLine.Text;
            DialogueType = dialogueType;
            this.helper = helper;
            this.dialogueManager = dialogueManager;
            ResolvedLineId = dialogueManager.getIdFromDialogueString(DialogueText, characterName);
            // A dash in the line id isn't a reliable signal on its own anymore — we now
            // generate dash-suffixed ids ourselves for name-substituted (NPC_cs_3985-Alex)
            // and %book-genre-expanded (Event_cs_1257_sv1-Romance) lines, which are each a
            // single fixed audio file, not a set of interchangeable recorded takes. Only
            // treat this as a variations test when the line is actually registered in
            // variable_lines.json.
            HasVariations = dialogueManager.VariableLineResolver.IsVariableLine(dialogueLine.LineId, characterName);
        }

        public DialogueTestCase(string characterName, DialogueType dialogueType, string dialogueText, string context, DialogueManager dialogueManager, IModHelper helper)
        {
            CharacterName = characterName;
            DialogueType = dialogueType;
            DialogueText = ParseDialogueString(dialogueText);
            Context = context;
            this.helper = helper;
            this.dialogueManager = dialogueManager;
        }


        private string ParseDialogueString(string dialogue)
        {
            // Handle {male_text^female_text} gender variants — take the first variant
            dialogue = Regex.Replace(dialogue, @"\{([^}^]+)\^([^}^]+)\}", m => m.Groups[1].Value);

            // Strip {n} runtime variable placeholders (e.g. {0}, {2}) without consuming surrounding text
            dialogue = Regex.Replace(dialogue, @"\{\d+\}", "VARIABLE");

            // Remove any stray { } brackets left over
            dialogue = dialogue.Replace("{", "").Replace("}", "");

            if (dialogue.Contains("$"))
                return Regex.Replace(dialogue, @"\$.", "");

            return dialogue;
        }

    }
}
