using System;
using StardewModdingAPI;
using StardewValley;
using StardewVoices.Dialogue;
using StardewVoices.Testing.Models;
using System.Collections.Generic;
using System.Linq;
using StardewVoices.Utils;
using StardewVoices.Utils.GameDialogueExtraction;
using StardewVoices.Utils.ParsingHelpers;

namespace StardewVoices.Testing.Validation
{
    public class ValidationManager
    {
        private CharacterDialogueExtractor dialogueExtractor;
        private EventDialogueExtractor eventExtractor;
        private FestivalDialogueExtractor festivalExtractor;
        private List<DialogueTestCase> testCases = new List<DialogueTestCase>();

        private ManagerHub Managers;
        private IModHelper helper;

        public List<ValidationReport> ValidationReport = new List<ValidationReport>();

        public ValidationManager(IModHelper helper, ManagerHub managers)
        {
            this.helper = helper;
            dialogueExtractor = new CharacterDialogueExtractor(helper);
            eventExtractor = new EventDialogueExtractor(helper);
            festivalExtractor = new FestivalDialogueExtractor(helper);
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
            GenerateDialogueTestCases("Abigail");

            /* foreach (string characterName in Constants.supportedCharacters)
             {
                 GenerateDialogueTestCases(characterName);
                 GenerateEventDialogueTestCases(characterName);
                 GenerateFestivalDialogueTestCases(characterName);
             }*/
        }

        private int GetCountOfDictionaryValues(Dictionary<DialogueType, List<LabelledDialogue>> dictionary)
        {
            int count = 0;
            foreach (KeyValuePair<DialogueType, List<LabelledDialogue>> pair in dictionary)
                count += pair.Value.Count();
            return count;
        }

        private void GenerateDialogueTestCases(string characterName)
        {
            Dictionary<DialogueType, List<LabelledDialogue>> lines = dialogueExtractor.GetAllDialogueForCharacter(Game1.getCharacterFromName(characterName));
            if (lines.Keys.Count == 0 || GetCountOfDictionaryValues(lines) == 0)
            {
                ErrorLogger.Error("No dialogue lines found");
            }

            foreach (DialogueType key in lines.Keys)
            {
                foreach (LabelledDialogue line in lines[key])
                {
                    if (line != null)
                    {
                        foreach (LabelledDialogueLine dialogueBox in line.Lines)
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
            List<(string Text, string Context)> lines = eventExtractor.GetEventDialogueForCharacter(characterName);
            if (lines.Count == 0)
            {
                ErrorLogger.Warn($"No event dialogue found for {characterName}.");
                return;
            }
            foreach (var (text, context) in lines)
                testCases.Add(new DialogueTestCase(characterName, DialogueType.Event, text, context, Managers.DialogueManager, helper));
        }

        private void GenerateFestivalDialogueTestCases(string characterName)
        {
            List<(string Text, string Context)> lines = festivalExtractor.GetFestivalDialogueForCharacter(characterName);
            if (lines.Count == 0)
            {
                ErrorLogger.Warn($"No festival dialogue found for {characterName}.");
                return;
            }
            foreach (var (text, context) in lines)
                testCases.Add(new DialogueTestCase(characterName, DialogueType.Festival, text, context, Managers.DialogueManager, helper));
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
