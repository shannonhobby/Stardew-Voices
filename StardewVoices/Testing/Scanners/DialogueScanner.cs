using StardewModdingAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StardewValley;

namespace StardewVoices.Testing.Scanners
{
    /// <summary>Enumerate NPC dialogue assets</summary>
    internal class DialogueScanner
    {
        private readonly IModHelper helper;

        public DialogueScanner(IModHelper helper)
        {
            this.helper = helper;
        }

        public void ScanAllDialogue()
        {

        }

        public StardewValley.Dialogue[] GetDialogueForCharacter(NPC character)
        {
            return character.CurrentDialogue?.ToArray();
        }
    }
}
