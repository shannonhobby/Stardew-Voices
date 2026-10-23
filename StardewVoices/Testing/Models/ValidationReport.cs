using StardewVoices.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using StardewVoices.Dialogue;
using StardewValley;

namespace StardewVoices.Testing.Models
{
    public class ValidationReport
    {
        public int TotalTestCount { get; }
        public int FailedTestCount => GetCountOfFailedTests();
        public Dictionary<string, List<string>> failedTests = new Dictionary<string, List<string>>();
        public DialogueType DialogueType;
        private List<string> _supportedCharacters;

        public ValidationReport(DialogueType dialogueType, List<ValidationResult> results, List<string> supportedCharacters)
        {
            TotalTestCount = results.Count;
            DialogueType = dialogueType;
            _supportedCharacters = supportedCharacters;
            string lastCharacter = "";

            ErrorLogger.Trace($"==============================={DialogueType.ToString().ToUpper()} TESTS===============================");


            foreach (var result in results)
            {
                if (!result.TestCase.CharacterName.Equals(lastCharacter))
                {
                    lastCharacter = result.TestCase.CharacterName;
                    PrintCharacterTestInfo(lastCharacter);
                }

                if (!result.Success)
                {
                    AddFailedTest(result.TestCase.CharacterName, result.TestCase.ResolvedLineId);
                    PrintFailedTestCase(result);
                }
            }

            PrintReportStats();
        }

        private void AddFailedTest(string characterName, string lineId)
        {
            if (failedTests.TryGetValue(characterName, out List<string> existing))
                existing.Add(lineId);
            else
                failedTests.Add(characterName, new List<string>() { lineId });
        }

        private void PrintCharacterTestInfo(string characterName)
        {
            ErrorLogger.Trace($"~~TESTING {characterName.ToUpper()}'S {DialogueType.ToString().ToUpper()} LINES~~");
        }
        private void PrintFailedTestCase(ValidationResult result)
        {
            ErrorLogger.Error($"[FAILED D:]: {result.TestCase.ResolvedLineId ?? "EMPTY LINE ID"}: {result.TestCase.DialogueText}");
            foreach (string errorMessage in result.ErrorMessages)
                ErrorLogger.Warn(errorMessage);
        }


        private void PrintReportStats()
        {
            foreach (string characterName in _supportedCharacters)
            {
                if (!failedTests.ContainsKey(characterName))
                {
                    PrintCharacterSuccess(characterName);
                }
                PrintFailedLinesForCharacter(characterName);
            }
            ErrorLogger.Trace($"{DialogueType} Testing complete!");
            ErrorLogger.Trace($"Total Tests: {TotalTestCount}");
            ErrorLogger.Error($"{GetCountOfFailedTests()} failed");
            ErrorLogger.Trace($"{Math.Floor((1.0 - (double)GetCountOfFailedTests() / TotalTestCount) * 100)}% Success");
        }

        private int GetCountOfFailedTests()
        {
            int count = 0;
            foreach (string key in failedTests.Keys)
            {
                count += failedTests[key].Count();
            }
            return count;
        }

        private void PrintCharacterSuccess(string characterName)
        {
            ErrorLogger.Trace($"GREAT SUCCESS FOR {characterName.ToUpper()} {DialogueType.ToString().ToUpper()} LINES!!");

        }

        private void PrintFailedLinesForCharacter(string characterName)
        {
            if (failedTests.TryGetValue(characterName, out List<string> failedTestsForCharacter))
            {
                ErrorLogger.Error($"\n\n\n\n\nFAILED TESTS FOR {characterName.ToUpper()}!!!!\n{string.Join("\n", failedTestsForCharacter)}");
            }
        }
    }
}
