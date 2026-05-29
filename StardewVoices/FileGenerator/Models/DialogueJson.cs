using StardewVoices.Pathing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StardewVoices.FileGenerator.Models
{
    internal class DialogueJson
    {
        public string CharacterName { get; }
        public string PathString { get; }

        public DialogueJson(string characterName, PathManager pathManager)
        {
            pathManager.GetPath("root").CreateFolder("Generated Files");
        }
    }
}
