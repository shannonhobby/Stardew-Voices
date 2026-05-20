using StardewModdingAPI;
using StardewVoices.Testing.Models;
using StardewVoices.Testing.Scanners;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StardewValley;
using StardewVoices.Utils;

namespace StardewVoices.Testing.Validation
{
    public class ValidationManager
    {
        private DialogueScanner scanner;
        private List<DialogueTestCase> testCases = new List<DialogueTestCase>();
        private ManagerHub Managers;

        public ValidationReport ValidationReport { get; private set; }

        public ValidationManager(IModHelper helper, ManagerHub managers)
        {
            scanner = new DialogueScanner(helper);
            Managers = managers;
        }

        public void RunValidation()
        {
            GenerateTestCases();

            List<ValidationResult> Results = RunTestCases();

            ValidationReport = new ValidationReport(Results);
        }

        private void GenerateTestCases()
        {
            StardewValley.Dialogue[] lines = scanner.GetDialogueForCharacter(Game1.getCharacterFromName("Abigail"));
            if(lines.Count() == 0)
            {
                // LEFT OFF HERE !!! NO DIALOGUE LINES FOUND 
                ErrorLogger.Error("No dialogue lines found");
            }
            foreach (StardewValley.Dialogue line in lines)
            {
                testCases.Add(new DialogueTestCase("Abigail", line, Managers.DialogueManager));
            }
        }

        private List<ValidationResult> RunTestCases()
        {
            List<ValidationResult> results = new List<ValidationResult>();
            foreach (DialogueTestCase testCase in testCases)
            {
                results.Add(ValidateTestCase(testCase));
            }
            return results;
        }

        private ValidationResult ValidateTestCase(DialogueTestCase testCase)
        {
            return new ValidationResult(testCase, Managers.AudioManager, Managers.DialogueManager);
        }
    }
}
