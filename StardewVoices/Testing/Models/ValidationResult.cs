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
            return LineIdFound(testCase) && LineAudioExists(testCase) && LineAudioPlays(testCase);
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
            foreach (string lineId in allVariations)
            {
                ErrorLogger.Trace($"Testing variation: {lineId} for {testCase.CharacterName}");
                if (!LineAudioExists(lineId, testCase.CharacterName) || !LineAudioPlays(lineId, testCase.CharacterName))
                {
                    testPasses = false;
                }
            }

            return testPasses;
        }



        // TODO add a test for checking if the right audio plays? not just that audio plays
        public bool LineIdFound(DialogueTestCase testCase)
        {
            try
            {
                string lineId = DialogueManager.getIdFromDialogueString(testCase.DialogueText, testCase.CharacterName);
                bool lineIdFound = lineId != null && lineId.Count() > 0;
                if (lineIdFound) testCase.ResolvedLineId = lineId;
                if (!lineIdFound)
                    ErrorMessages.Add($"1. [FAILURE] LineIdFound: Line id not found for dialogue line {testCase.DialogueText}.");
                return lineIdFound;
            }
            catch (Exception e)
            {
                ErrorMessages.Add($"1. [FAILURE] LineIdFound: Line id not found for dialogue line {testCase.DialogueText}");
                return false;
            }
        }

        // <summary>
        // Whether audio exists for the given line id.
        //
        // Dash-suffixed line ids (e.g. "NPC_cs_3985-Abigail") are name/genre-substituted
        // variants of one base line — VoiceResolver.Resolve falls back to the un-suffixed line
        // for these at runtime if the specific variant wasn't recorded. That fallback keeps
        // the game silent about a missing recording, so validation checks the exact id
        // itself (Resolver.HasExactLine) and always fails when it's missing, distinguishing
        // two severities: the base line still exists (this character just doesn't have a
        // dedicated recording — a normal failure) vs. the base line is ALSO missing (this
        // line won't play at all in-game — a worse failure).
        // </summary>
        public bool LineAudioExists(DialogueTestCase testCase)
        {
            try
            {
                if (AudioManager.Resolver.HasExactLine(testCase.CharacterName, testCase.ResolvedLineId))
                    return true;

                string fallbackLineId = VoiceResolver.GetFallbackLineId(testCase.ResolvedLineId);
                if (fallbackLineId != null &&
                    AudioManager.Resolver.HasExactLine(testCase.CharacterName, fallbackLineId))
                {
                    ErrorMessages.Add(
                        $"2. [FAILURE] LineAudioExists: Audio not found for line {testCase.ResolvedLineId} — falls back to generic " +
                        $"line {fallbackLineId} at runtime. Record a dedicated line for this character.");
                }
                else
                {
                    ErrorMessages.Add(
                        $"2. [SEVERE FAILURE] LineAudioExists: Audio not found for line {testCase.ResolvedLineId}, and its fallback " +
                        $"line {fallbackLineId ?? "(none)"} is also missing — this line will not play at all.");
                }

                return false;
            }
            catch (Exception e)
            {
                ErrorMessages.Add($"2. [FAILURE] LineAudioExists: Audio not found for line {testCase.ResolvedLineId}.");
                return false;
            }

        }

        // <summary>Whether audio exists for the given line id</summary>
        public bool LineAudioExists(string lineId, string characterName)
        {
            try
            {
                bool LineAudioExists = AudioManager.GetVoiceLine(lineId, characterName) != null;
                if (!LineAudioExists)
                {
                    ErrorMessages.Add($"2. [FAILURE] LineAudioExists: Audio not found for line {lineId}");
                }

                return LineAudioExists;
            }
            catch (Exception e)
            {
                ErrorMessages.Add($"2. [FAILURE] LineAudioExists: Audio not found for line {lineId}.");
                return false;
            }

        }



        // ALSO CHECKS IF IT EXISTS?
        // <summary>Checks if an error is thrown when trying to play the given audio.</summary>
        public bool LineAudioPlays(DialogueTestCase testCase)
        {
            try
            {
                AudioManager.Play(testCase.ResolvedLineId, testCase.CharacterName);
                return true;
            }
            catch (Exception e)
            {
                ErrorMessages.Add($"3. [FAILURE] LineAudioPlays: {e}");
                return false;
            }
            finally
            {
                // Validation only cares whether Play() throws, not whether it's audible — stop
                // it immediately instead of leaving thousands of test lines playing at once.
                AudioManager.StopAllLines();
            }
        }

        public bool LineAudioPlays(string lineId, string characterName)
        {
            try
            {
                AudioManager.Play(lineId, characterName);
                return true;
            }
            catch (Exception e)
            {
                ErrorMessages.Add($"3. [FAILURE] LineAudioPlays: {e}");
                return false;
            }
            finally
            {
                AudioManager.StopAllLines();
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
