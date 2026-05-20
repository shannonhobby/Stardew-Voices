using StardewVoices.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StardewVoices.Testing.Validation
{
    public class ValidationReport
    {
        public int TotalTestCount { get; }
        public List<string> failedCues = new List<string>();


        public ValidationReport(List<ValidationResult> results) 
        {
            TotalTestCount = results.Count;
            foreach (var result in results)
            {
                PrintTestCaseInfo(result);

                if(result.Success)
                {
                    PrintPassedTestCase(result);
                }
                else
                {
                    PrintFailedTestCase(result);
                }
            }

            PrintReportStats();
        }

        private void PrintTestCaseInfo(ValidationResult result) 
        {
            ErrorLogger.Trace($"==========TESTS CASES FOR {result.TestCase.CharacterName.ToUpper()}==========");
        }
        private void PrintFailedTestCase (ValidationResult result) {
            ErrorLogger.Error($"[FAILED D:]: {result.TestCase.CueName}: {result.TestCase.DialogueLine.getCurrentDialogue}");
            failedCues.Add(result.TestCase.CueName);
            foreach (string errorMessage in result.ErrorMessages)
            {
                ErrorLogger.Error(errorMessage);
            }
        }
        private void PrintPassedTestCase(ValidationResult result) {
            ErrorLogger.Trace($"[SUCCESS :D]");
        }

        private void PrintReportStats()
        {
            ErrorLogger.Trace($"Testing complete!");
            ErrorLogger.Trace($"{(failedCues.Count / TotalTestCount) * 100}% Success");
            ErrorLogger.Error($"List of failed cues: {String.Join("\n", failedCues)}");

        }
    }
}
