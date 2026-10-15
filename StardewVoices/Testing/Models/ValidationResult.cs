using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewVoices.Audio;
using StardewVoices.Dialogue;
using StardewVoices.Pathing;
using StardewVoices.Testing.Models;
using System.IO;
using StardewVoices.Utils;
using Microsoft.Xna.Framework.Input;


namespace StardewVoices.Testing.Models
{
    public class ValidationResult
    {
        public bool Success { get; set; }

        public List<string> ErrorMessages = new List<string>();

        public DialogueTestCase TestCase { get; }

        private AudioManager AudioManager;
        private DialogueManager DialogueManager;
        private PathManager PathManager;

        private IModHelper Helper;

        public ValidationResult(DialogueTestCase testCase, AudioManager audioManager, DialogueManager dialogueManager, PathManager pathManager, IModHelper helper)
        {
            AudioManager = audioManager;
            DialogueManager = dialogueManager;
            PathManager = pathManager;
            Helper = helper;
            TestCase = testCase;
            if (TestCase.DialogueType == DialogueType.Event)
            {
                SetFakeEvent();
            }

            if (TestCase.DialogueType == DialogueType.Festival)
            {
                SetFakeFestival();
            }

            if (TestCase.HasVariations == true)
            {
                Success = RunVariationsTest(testCase);
            } else
            {
                Success = RunTests(testCase);

            }

            dialogueManager.EndEvent();
            dialogueManager.ResetFestival();
        }

        public bool RunTests(DialogueTestCase testCase)
        {
            return CueNameFound(testCase) && CueExists(testCase) && CueAudioPlays(testCase);
        }

        public bool RunVariationsTest(DialogueTestCase testCase)
        {
            string[] allVariations = DialogueManager.VariableLineResolver.GetAllVariants(testCase.DialogueLine.LineId, testCase.CharacterName);

            if(allVariations.Length == 0)
            {
                ErrorMessages.Add($"[FAILURE] Variations Test: No variations found for: {testCase.DialogueLine.LineId}.");
                return false;
            }
            bool testPasses = true;
            foreach (string cueName in allVariations)
            {
                ErrorLogger.Trace($"Testing variation: {cueName} for {testCase.CharacterName}");
                if (!CueExists(cueName, testCase.CharacterName) || !CueAudioPlays(cueName, testCase.CharacterName))
                {
                    testPasses = false;
                }
            }

            return testPasses;
        }



        // TODO add a test for checking if the right audio plays? not just that audio plays
        public bool CueNameFound(DialogueTestCase testCase)
        {
            try
            {
                string cueName = DialogueManager.getIdFromDialogueString(testCase.DialogueText, testCase.CharacterName);
                bool cueNameFound = cueName != null && cueName.Count() > 0;
                if (cueNameFound) testCase.CueName = cueName;
                if (!cueNameFound)
                    ErrorMessages.Add($"1. [FAILURE] CueNameFound: Cue name not found for dialogue line {testCase.DialogueText}.");
                return cueNameFound;
            }
            catch (Exception e)
            {
                ErrorMessages.Add($"1. [FAILURE] CueNameFound: Cue name not found for dialogue line {testCase.DialogueText}");
                return false;
            }
        }

        // <summary>
        // Whether the given Cue name maps to a Cue in the Sound Bank.
        //
        // Dash-suffixed cue names (e.g. "NPC_cs_3985-Abigail") are name/genre-substituted
        // variants of one base line — AudioManager.Play falls back to the un-suffixed cue
        // for these at runtime if the specific variant wasn't recorded. That fallback keeps
        // the game silent about a missing recording, so validation checks the exact id
        // itself (allowFallback: false) and always fails when it's missing, distinguishing
        // two severities: the base cue still exists (this character just doesn't have a
        // dedicated recording — a normal failure) vs. the base cue is ALSO missing (this
        // line won't play at all in-game — a worse failure).
        // </summary>
        public bool CueExists(DialogueTestCase testCase)
        {
            try
            {
                if (AudioManager.GetCueFromDialogueId(testCase.CueName, testCase.CharacterName, allowFallback: false) != null)
                    return true;

                string fallbackCueName = GetFallbackCueName(testCase.CueName);
                if (fallbackCueName != null &&
                    AudioManager.GetCueFromDialogueId(fallbackCueName, testCase.CharacterName, allowFallback: false) != null)
                {
                    ErrorMessages.Add(
                        $"2. [FAILURE] CueExists: Cue not found for ID {testCase.CueName} — falls back to generic " +
                        $"cue {fallbackCueName} at runtime. Record a dedicated line for this character.");
                }
                else
                {
                    ErrorMessages.Add(
                        $"2. [SEVERE FAILURE] CueExists: Cue not found for ID {testCase.CueName}, and its fallback " +
                        $"cue {fallbackCueName ?? "(none)"} is also missing — this line will not play at all.");
                }

                return false;
            }
            catch (Exception e)
            {
                ErrorMessages.Add($"2. [FAILURE] CueExists: Cue not found for ID {testCase.CueName}.");
                return false;
            }

        }

        // "NPC_cs_3985-Abigail" -> "NPC_cs_3985". Null if cueName has no "-{suffix}" to strip.
        private static string GetFallbackCueName(string cueName)
        {
            int dashIndex = cueName?.LastIndexOf('-') ?? -1;
            return dashIndex > 0 ? cueName.Substring(0, dashIndex) : null;
        }

        // <summary>Whether the given Cue name maps to a Cue in the Sound Bank</summary>
        public bool CueExists(string cueName, string characterName)
        {
            try
            {
                bool CueExists = AudioManager.GetCueFromDialogueId(cueName, characterName) != null;
                if (!CueExists)
                {
                    ErrorMessages.Add($"2. [FAILURE] CueExists: Cue not found for ID {cueName}");
                }

                return CueExists;
            }
            catch (Exception e)
            {
                ErrorMessages.Add($"2. [FAILURE] CueExists: Cue not found for ID {cueName}.");
                return false;
            }

        }



        // ALSO CHECKS IF IT EXISTS? 
        // <summary>Checks if an error is thrown when trying to play the given audio.</summary>
        public bool CueAudioPlays(DialogueTestCase testCase)
        {
            try
            {
                AudioManager.Play(testCase.CueName, testCase.CharacterName);
                return true;
            }
            catch (Exception e)
            {
                ErrorMessages.Add($"3. [FAILURE] CueAudioPlays: {e}");
                return false;
            }
        }

        public bool CueAudioPlays(string cueName, string characterName)
        {
            try
            {
                AudioManager.Play(cueName, characterName);
                return true;
            }
            catch (Exception e)
            {
                ErrorMessages.Add($"3. [FAILURE] CueAudioPlays: {e}");
                return false;
            }
        }


        private void SetFakeEvent()
        {
            string locationName = TestCase.Context.Split("/")[0];
            string eventId = TestCase.Context.Split("/")[1];

            string locationFilePath = Path.Combine(PathManager.GetPathString("Events"), locationName);
            string eventFilePath = Path.Combine(locationFilePath, eventId + ".json");

            DialogueManager.StartEvent(eventFilePath);
        }

        private void SetFakeFestival()
        {
            DialogueManager.SetFestival(GetFestivalNameFromDate(TestCase.Context));
        }

        private string GetFestivalNameFromDate(string festivalDate)
        {
            Dictionary<string, string> FestivalDateDictionary = Helper.GameContent.Load<Dictionary<string, string>>(
              "Data/Festivals/FestivalDates"
            );
            FestivalDateDictionary.TryGetValue(festivalDate, out string festivalName);
            return festivalName;
        }
    }
}
