using StardewVoices.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StardewVoices.Testing.Models;
using StardewValley;

namespace StardewVoices.Testing.Validation
{
    public class ValidationReport
    {
        public int TotalTestCount { get; }
        public Dictionary<string, List<string>> failedTests = new Dictionary<string, List<string>>();
        public DialogueType DialogueType;

        public ValidationReport(DialogueType dialogueType, List<ValidationResult> results) 
        {
            TotalTestCount = results.Count;
            DialogueType = dialogueType;
            string lastCharacter = "";

            ErrorLogger.Trace($"==============================={DialogueType.ToString().ToUpper()} TESTS===============================");


            foreach (var result in results)
            {
                if (!result.TestCase.CharacterName.Equals(lastCharacter)) {
                    lastCharacter = result.TestCase.CharacterName;
                    PrintCharacterTestInfo(lastCharacter);
                }

                if(!result.Success)
                {
                    if(failedTests.TryGetValue(result.TestCase.CharacterName, out List<string> failedTestForCharacter))
                    {
                        failedTestForCharacter.Add(result.TestCase.CueName);
                    } else
                    {
                        failedTests.Add(result.TestCase.CharacterName, new List<string> () { result.TestCase.CueName });
                    }

                    PrintFailedTestCase(result);
                }
            }

            PrintReportStats();
        }

        private void PrintCharacterTestInfo(string characterName) 
        {
            ErrorLogger.Trace($"~~TESTING {characterName.ToUpper()}'S {DialogueType.ToString().ToUpper()} LINES~~" );
        }
        private void PrintFailedTestCase (ValidationResult result) {
            ErrorLogger.Error($"[FAILED D:]: {result.TestCase.CueName}: {result.TestCase.DialogueLine.Text}");
            foreach (string errorMessage in result.ErrorMessages)
            {
                ErrorLogger.Warn(errorMessage);
            }
        }


        private void PrintReportStats()
        {
            foreach(string characterName in Constants.supportedCharacters)
            {
                if(!failedTests.ContainsKey(characterName))
                {
                    PrintCharacterSuccess(characterName);
                }
                PrintFailedCuesForCharacter(characterName);
            }
            ErrorLogger.Trace($"{DialogueType} Testing complete!");
            ErrorLogger.Trace($"Total Tests: {TotalTestCount}");
            ErrorLogger.Error($"{GetCountOfFailedTests()} failed");
            ErrorLogger.Trace($"{Math.Floor((1.0 - (double)GetCountOfFailedTests() / TotalTestCount) * 100)}% Success");
        }

        private int GetCountOfFailedTests()
        {
            int count = 0;
            foreach(string key in failedTests.Keys)
            {
                count += failedTests[key].Count();
            }
            return count;
        }

        private void PrintCharacterSuccess(string characterName)
        {
            ErrorLogger.Trace($"GREAT SUCCESS FOR {characterName.ToUpper()} {DialogueType.ToString().ToUpper()} LINES!!");

        }

        private void PrintFailedCuesForCharacter(string characterName)
        {
            if (failedTests.TryGetValue(characterName, out List<string> failedTestsForCharacter))
            {
                ErrorLogger.Error($"\n\n\n\n\nFAILED TESTS FOR {characterName.ToUpper()}!!!!\n{String.Join("\n", failedTestsForCharacter)}");
            }
        }
    }
}
