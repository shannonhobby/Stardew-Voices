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

namespace StardewVoices.StardewVoices.Testing
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
            // Results depend on which voice each character uses (built-in or a pack), so say up front.
            foreach (string character in Managers.CharacterManager.SupportedCharacters)
                ErrorLogger.Info($"Validating {character} against {Managers.AudioManager.Resolver.GetSource(character)?.Name ?? "no voices"}.");

            GenerateTestCases();

            Managers.AudioManager.Muted = true;

            // Each test line logs at Trace level, which would flood the log file here —
            // suppress it and show periodic progress instead (see RunTestCases).
            ErrorLogger.SuppressTrace = true;
            Dictionary<DialogueType, List<ValidationResult>> results;
            try
            {
                results = RunTestCases();
            }
            finally
            {
                ErrorLogger.SuppressTrace = false;
            }

            foreach (DialogueType dialogueType in results.Keys)
                ValidationReport.Add(new ValidationReport(dialogueType, results[dialogueType], Managers.CharacterManager.SupportedCharacters));

            // Stop the last test line first, or it would become audible when unmuted.
            Managers.AudioManager.StopAllLines();
            Managers.AudioManager.Muted = false;

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
                    string lineIds = string.Join("\n", report.failedTests[character]);
                    ErrorLogger.Error($"\n  {character}:\n {lineIds}");
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
            foreach (string characterName in Managers.CharacterManager.SupportedCharacters)
            {
                GenerateDialogueTestCases(characterName);
                GenerateEventDialogueTestCases(characterName);
                GenerateFestivalDialogueTestCases(characterName);
            }
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
                            if (ShouldSkipTestCase(dialogueBox.Text))
                                continue;

                            DialogueTestCase newTestCase = new DialogueTestCase(characterName, key, dialogueBox, Managers.DialogueManager, helper);
                            if (!newTestCase.ResolvedLineId.ToLower().Contains("event_"))
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
                if (ShouldSkipTestCase(text))
                {
                    continue;
                }
                else
                {
                    testCases.Add(new DialogueTestCase(characterName, DialogueType.Event, text, context, Managers.DialogueManager, helper));
                }
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
                if (ShouldSkipTestCase(text))
                {
                    continue;
                } else
                {
                    testCases.Add(new DialogueTestCase(characterName, DialogueType.Festival, text, context, Managers.DialogueManager, helper));
                }
        }

        private Dictionary<DialogueType, List<ValidationResult>> RunTestCases()
        {
            Dictionary<DialogueType, List<ValidationResult>> results = new Dictionary<DialogueType, List<ValidationResult>>();
            int total = testCases.Count;
            int lastReportedPercent = -1;

            for (int i = 0; i < total; i++)
            {
                DialogueTestCase testCase = testCases[i];

                if (!results.ContainsKey(testCase.DialogueType))
                    results.Add(testCase.DialogueType, new List<ValidationResult>() { ValidateTestCase(testCase) });
                else
                {
                    results.TryGetValue(testCase.DialogueType, out List<ValidationResult> currentResults);
                    currentResults.Add(ValidateTestCase(testCase));
                }

                // Replaces the suppressed per-line Trace logging with one line every 2%.
                int percent = (i + 1) * 100 / total;
                if (percent != lastReportedPercent && percent % 2 == 0)
                {
                    ErrorLogger.Info($"Validating... {i + 1}/{total} ({percent}%)");
                    lastReportedPercent = percent;
                }
            }
            return results;
        }

        private ValidationResult ValidateTestCase(DialogueTestCase testCase)
        {
            return new ValidationResult(testCase, Managers.AudioManager, Managers.DialogueManager, Managers.PathManager, helper);
        }

        private bool ShouldSkipTestCase(string text)
        {
            return text == null || text.StartsWith('%') || text.Equals("...");
        }
    }
}
