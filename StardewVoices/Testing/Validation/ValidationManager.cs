using System;
using StardewModdingAPI;
using StardewVoices.Testing.Models;
using StardewVoices.Testing.Scanners;
using System.Collections.Generic;
using System.Linq;
using StardewValley;
using StardewVoices.Utils;

namespace StardewVoices.Testing.Validation
{
    public class ValidationManager
    {
        private DialogueScanner dialogueScanner;
        private EventScanner eventScanner;
        private FestivalScanner festivalScanner;
        private List<DialogueTestCase> testCases = new List<DialogueTestCase>();

        private ManagerHub Managers;
        private IModHelper helper;

        public List<ValidationReport> ValidationReport = new List<ValidationReport>();


        public ValidationManager(IModHelper helper, ManagerHub managers)
        {
            this.helper = helper;
            dialogueScanner = new DialogueScanner(helper);
            eventScanner = new EventScanner(helper, managers.DialogueManager);
            festivalScanner = new FestivalScanner(helper, managers.DialogueManager);
            Managers = managers;
        }

        public void RunValidation()
        {
            GenerateTestCases();

            Managers.AudioManager.MuteAll();

            Dictionary<DialogueType, List<ValidationResult>> results = RunTestCases();
            foreach (DialogueType dialogueType in results.Keys)
                ValidationReport.Add(new ValidationReport(dialogueType, results[dialogueType]));
            // TODO unmute the dialogue!

            PrintFinalRecap();

            Managers.TestingManager.ValidationIsRunning = false;
        }

        private void PrintFinalRecap()
        {
            int grandTotal = 0;
            int grandFailed = 0;

            foreach (ValidationReport report in ValidationReport)
            {
                grandTotal += report.TotalTestCount;
                grandFailed += report.FailedTestCount;
            }

            ErrorLogger.Trace("===================================FINAL VALIDATION RECAP===================================");

            foreach (ValidationReport report in ValidationReport)
            {
                if (report.failedTests.Count == 0)
                    continue;

                ErrorLogger.Trace($"--- FAILED {report.DialogueType.ToString().ToUpper()} LINES ---");
                foreach (string character in report.failedTests.Keys)
                {
                    string cues = string.Join("\n", report.failedTests[character]);
                    ErrorLogger.Error($"\n  {character}:\n {cues}");
                }
            }

            ErrorLogger.Trace("--------------------------------------------------------------------------------------------");
            ErrorLogger.Trace($"Grand Total: {grandTotal} tests");
            ErrorLogger.Error($"Failed: {grandFailed}");
            ErrorLogger.Trace($"Passed: {grandTotal - grandFailed}");
            ErrorLogger.Trace($"Success Rate: {Math.Floor((1.0 - (double)grandFailed / grandTotal) * 100)}%");
            ErrorLogger.Trace("============================================================================================");
        }

        private void GenerateTestCases()
        {
            foreach (string characterName in Constants.supportedCharacters)
            {
                GenerateDialogueTestCases(characterName);
               // GenerateEventDialogueTestCases(characterName);
                //GenerateFestivalDialogueTestCases(characterName);
            }
        }

        private int GetCountOfDictionaryValues(Dictionary<DialogueType, List<StardewValley.Dialogue>> dictionary)
        {
            int count = 0;
            foreach (KeyValuePair<DialogueType, List<StardewValley.Dialogue>> pair in dictionary)
                count += pair.Value.Count();
            return count;
        }

        private void GenerateDialogueTestCases(string characterName)
        {
            Dictionary<DialogueType, List<StardewValley.Dialogue>> lines = dialogueScanner.GetAllDialogueForCharacter(Game1.getCharacterFromName(characterName));
            if (lines.Keys.Count == 0 || GetCountOfDictionaryValues(lines) == 0)
            {
                ErrorLogger.Error("No dialogue lines found");
            }

            foreach (DialogueType key in lines.Keys)
            {
                foreach (StardewValley.Dialogue line in lines[key])
                {
                    if (line != null)
                    {
                        foreach (DialogueLine dialogueBox in line.dialogues)
                        {
                            if (dialogueBox.Text.StartsWith('%') || dialogueBox.Text.Equals("..."))
                                continue;

                            DialogueTestCase newTestCase = new DialogueTestCase(characterName, key, dialogueBox, Managers.DialogueManager, helper);
                            if (!newTestCase.CueName.ToLower().Contains("event_"))
                                testCases.Add(newTestCase);
                        }
                    }
                }
            }

            if (testCases.Count() == 0)
                ErrorLogger.Error($"No test cases created for {characterName}.");
        }

        private void GenerateEventDialogueTestCases(string characterName)
        {
            List<DialogueTestCase> cases = eventScanner.GetEventTestCasesForCharacter(characterName);
            if (cases.Count == 0)
                ErrorLogger.Warn($"No event test cases found for {characterName}.");
            else
                testCases.AddRange(cases);
        }

        private void GenerateFestivalDialogueTestCases(string characterName)
        {
            List<DialogueTestCase> cases = festivalScanner.GetFestivalTestCasesForCharacter(characterName);
            if (cases.Count == 0)
                ErrorLogger.Warn($"No festival test cases found for {characterName}.");
            else
                testCases.AddRange(cases);
        }

        private Dictionary<DialogueType, List<ValidationResult>> RunTestCases()
        {
            Dictionary<DialogueType, List<ValidationResult>> results = new Dictionary<DialogueType, List<ValidationResult>>();
            foreach (DialogueTestCase testCase in testCases)
            {
                if (!results.ContainsKey(testCase.DialogueType))
                    results.Add(testCase.DialogueType, new List<ValidationResult>() { ValidateTestCase(testCase) });
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
            return new ValidationResult(testCase, Managers.AudioManager, Managers.DialogueManager, Managers.PathManager, helper);
        }
    }
}
