using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StardewVoices.FileGenerator.Models
{
    internal class Script
    {
        public string CharacterName { get; }
        private int LineCount = 0;
        private string MarkDownScript { get; set; }
        private string PlainTextScript { get; set; }
        private string PathString { get; }
        
        public Script(string characterName) 
        { 
            CharacterName = characterName;
            InitializeScript();
        }

        private int GetLineCount() { return LineCount;}

        private void InitializeScript()
        {
            // Title, credits, line count (eventually) etc.
        }

        public void AddScriptSection(string sectionTitle, Dictionary<string, string> lines)
        {
        }

        private void AddLine(string lineId, string dialogue)
        {

        }

        private void AddScriptNote(string text)
        {

        }
    }
}
