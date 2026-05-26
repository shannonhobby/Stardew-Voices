using StardewVoices.Audio;
using StardewVoices.Testing.Models;
using StardewVoices.Utils;
using System;
using System.Collections.Generic;
using System.Linq;

namespace StardewVoices.Testing.Validation
{
    public class FestivalValidationReport
    {
        public int TotalTestCount { get; }
        // LEFT OFF HERE!! Refactor festival validation report and event validation report to 
        // better match the original

        public FestivalValidationReport(List<FestivalDialogueTestCase> testCases, AudioManager audioManager)
        {
            TotalTestCount = testCases.Count;
            int failed = 0;
            string lastCharacter = "";

            ErrorLogger.Trace("=============================== FESTIVAL TESTS ===============================");

            foreach (FestivalDialogueTestCase tc in testCases)
            {
                if (!tc.CharacterName.Equals(lastCharacter))
                {
                    lastCharacter = tc.CharacterName;
                    ErrorLogger.Trace($"~~TESTING {lastCharacter.ToUpper()}'S FESTIVAL LINES~~");
                }

                if (tc.CueName == null)
                {
                    ErrorLogger.Error($"[FAILED D:] {tc.CharacterName} | {tc.FestivalDate}: Line ID not found for \"{tc.DialogueText}\"");
                    failed++;
                    continue;
                }

                bool cueExists = audioManager.GetCueFromDialogueId(tc.CueName, tc.CharacterName) != null;
                if (!cueExists)
                {
                    ErrorLogger.Error($"[FAILED D:] {tc.CharacterName} | {tc.FestivalDate}: Cue not found in sound bank: {tc.CueName}");
                    failed++;
                    continue;
                }

                try
                {
                    audioManager.Play(tc.CueName, tc.CharacterName);
                }
                catch (Exception e)
                {
                    ErrorLogger.Error($"[FAILED D:] {tc.CharacterName} | {tc.FestivalDate}: Cue failed to play: {tc.CueName} — {e.Message}");
                    failed++;
                }
            }

            int passed = TotalTestCount - failed;
            ErrorLogger.Trace("=============================== FESTIVAL RESULTS ===============================");
            foreach (string characterName in Constants.supportedCharacters)
            {
                bool anyFailed = testCases.Any(tc => tc.CharacterName == characterName && tc.CueName == null);
                if (!anyFailed)
                    ErrorLogger.Trace($"GREAT SUCCESS FOR {characterName.ToUpper()} FESTIVAL LINES!!");
            }
            ErrorLogger.Trace($"Festival testing complete! Total: {TotalTestCount}, Passed: {passed}, Failed: {failed}");
            ErrorLogger.Trace($"{Math.Floor((double)passed / TotalTestCount * 100)}% Success");
        }
    }
}
