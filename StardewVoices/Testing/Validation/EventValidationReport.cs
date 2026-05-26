using StardewVoices.Audio;
using StardewVoices.Testing.Models;
using StardewVoices.Utils;
using System;
using System.Collections.Generic;
using System.Linq;

namespace StardewVoices.Testing.Validation
{
    public class EventValidationReport
    {
        public int TotalTestCount { get; }

        public EventValidationReport(List<EventDialogueTestCase> testCases, AudioManager audioManager)
        {
            TotalTestCount = testCases.Count;
            int failed = 0;
            string lastCharacter = "";

            ErrorLogger.Trace("=============================== EVENT TESTS ===============================");

            foreach (EventDialogueTestCase tc in testCases)
            {
                if (!tc.CharacterName.Equals(lastCharacter))
                {
                    lastCharacter = tc.CharacterName;
                    ErrorLogger.Trace($"~~TESTING {lastCharacter.ToUpper()}'S EVENT LINES~~");
                }

                if (tc.CueName == null)
                {
                    ErrorLogger.Error($"[FAILED D:] {tc.CharacterName} | {tc.LocationName}/{tc.EventId}: Line ID not found for \"{tc.DialogueText}\"");
                    failed++;
                    continue;
                }

                bool cueExists = audioManager.GetCueFromDialogueId(tc.CueName, tc.CharacterName) != null;
                if (!cueExists)
                {
                    ErrorLogger.Error($"[FAILED D:] {tc.CharacterName} | {tc.LocationName}/{tc.EventId}: Cue not found in sound bank: {tc.CueName}");
                    failed++;
                    continue;
                }

                try
                {
                    audioManager.Play(tc.CueName, tc.CharacterName);
                }
                catch (Exception e)
                {
                    ErrorLogger.Error($"[FAILED D:] {tc.CharacterName} | {tc.LocationName}/{tc.EventId}: Cue failed to play: {tc.CueName} — {e.Message}");
                    failed++;
                }
            }

            int passed = TotalTestCount - failed;
            ErrorLogger.Trace("=============================== EVENT RESULTS ===============================");
            foreach (string characterName in Constants.supportedCharacters)
            {
                bool anyFailed = testCases.Any(tc => tc.CharacterName == characterName && tc.CueName == null);
                if (!anyFailed)
                    ErrorLogger.Trace($"GREAT SUCCESS FOR {characterName.ToUpper()} EVENT LINES!!");
            }
            ErrorLogger.Trace($"Event testing complete! Total: {TotalTestCount}, Passed: {passed}, Failed: {failed}");
            ErrorLogger.Trace($"{Math.Floor((double)passed / TotalTestCount * 100)}% Success");
        }
    }
}
