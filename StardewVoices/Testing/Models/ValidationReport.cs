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
        public Dictionary<string, int> testCountByCharacter = new Dictionary<string, int>();
        public DialogueType DialogueType;
        private List<string> _supportedCharacters;

        public ValidationReport(DialogueType dialogueType, List<ValidationResult> results, List<string> supportedCharacters)
        {
            // Each variant of a variable line is its own pass/fail, matching how failures are counted.
            TotalTestCount = results.Sum(result => result.TestCount);
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

                testCountByCharacter.TryGetValue(lastCharacter, out int characterTestCount);
                testCountByCharacter[lastCharacter] = characterTestCount + result.TestCount;

                // Failures are only listed in the per-character recap, not one by one, to keep the log short.
                if (!result.Success && result.FailedVariantLineIds.Count > 0)
                {
                    foreach (string lineId in result.FailedVariantLineIds)
                        AddFailedTest(result.TestCase.CharacterName, lineId);
                }
                else if (!result.Success)
                {
                    AddFailedTest(result.TestCase.CharacterName,
                        result.TestCase.ResolvedLineId ?? $"(no line id found) \"{result.TestCase.DialogueText}\"");
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
            ErrorLogger.Trace($"{FormatSuccessRate(TotalTestCount - GetCountOfFailedTests(), TotalTestCount)} Success");
        }

        /// <summary>
        /// Passed/total as a percentage to one decimal — whole-number flooring showed e.g.
        /// 3 passes out of 464 as "0%", which reads as nothing passing.
        /// </summary>
        public static string FormatSuccessRate(int passed, int total)
        {
            if (total == 0)
                return "n/a";
            return $"{Math.Floor((double)passed / total * 1000) / 10:0.0}%";
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
