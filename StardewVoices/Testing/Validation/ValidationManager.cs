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
using StardewValley.Menus;

namespace StardewVoices.Testing.Validation
{
    public class ValidationManager
    {
        private DialogueScanner scanner;
        private List<DialogueTestCase> testCases = new List<DialogueTestCase>();
        private ManagerHub Managers;

        public List<ValidationReport> ValidationReport = new List<ValidationReport>();

        public ValidationManager(IModHelper helper, ManagerHub managers)
        {
            scanner = new DialogueScanner(helper);
            Managers = managers;
        }

        public void RunValidation()
        {
            GenerateTestCases();

            Dictionary<DialogueType, List<ValidationResult>> Results = RunTestCases();


            foreach(DialogueType dialogueType in Results.Keys)
            {
                ValidationReport.Add(new ValidationReport(dialogueType, Results[dialogueType]));
            }
        }

        private void GenerateTestCases()
        {
            foreach (string characterName in Constants.supportedCharacters)
            {
                GenerateDialogueTestCases(characterName);
                // Generate event dialogue test cases
                // Generate festival dialogue test cases
                // generate schedule test cases
                // Generate text above head test cases?
               
            }
        }

        private int GetCountOfDictionaryValues(Dictionary<DialogueType, List<StardewValley.Dialogue>> dictionary)
        {
            int count = 0;

            foreach(KeyValuePair<DialogueType, List<StardewValley.Dialogue>> pair in dictionary)
            {
                count += pair.Value.Count();
            }
            return count;
        }

        private void GenerateDialogueTestCases(string characterName)
        {
            Dictionary<DialogueType, List<StardewValley.Dialogue>>lines = scanner.GetAllDialogueForCharacter(Game1.getCharacterFromName(characterName));
            if (lines.Keys.Count == 0 || GetCountOfDictionaryValues(lines) == 0)
            {
                ErrorLogger.Error("No dialogue lines found");
            }
            
            foreach(DialogueType key in lines.Keys)
            {
                foreach (StardewValley.Dialogue line in lines[key])
                {
                    if (line != null)
                    {
                        foreach (DialogueLine dialogueBox in line.dialogues)
                        {
                            // These are actions that don't have dialogue (ex: %Abigail is lost in her music)
                            if (dialogueBox.Text.StartsWith('%') || dialogueBox.Text.Equals("..."))
                            {
                                continue;
                            }

                            DialogueTestCase newTestCase = new DialogueTestCase(characterName, key, dialogueBox, Managers.DialogueManager);
                            // Since this is the dialogue test case we don't need to test the event strings contained in the dialogue files.
                            if (!newTestCase.CueName.ToLower().Contains("event_"))
                            {
                                testCases.Add(newTestCase);

                            }
                        }
                    }
                }
            }
                
            if (testCases.Count() == 0)
            {
                ErrorLogger.Error($"No test cases created for {characterName}.");
            }
        }

        private Dictionary<DialogueType, List<ValidationResult>>  RunTestCases()
        {
            Dictionary<DialogueType, List<ValidationResult>> results = new Dictionary<DialogueType, List<ValidationResult>>();
            foreach (DialogueTestCase testCase in testCases)
            {
                if(!results.ContainsKey(testCase.DialogueType))
                {
                    results.Add(testCase.DialogueType, new List<ValidationResult>() { ValidateTestCase(testCase) });
                }
                else
                {
                    results.TryGetValue(testCase.DialogueType, out List<ValidationResult> currentResults);
                    currentResults.Add(ValidateTestCase(testCase));
                }
            }
            return results;
        }

        private ValidationResult ValidateTestCase(DialogueTestCase testCase)
        {
            return new ValidationResult(testCase, Managers.AudioManager, Managers.DialogueManager);
        }
        
        private void PrintFinalValidationReportStats()
        {

        }
    }
}
