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

            Success = RunTests(testCase);

            dialogueManager.EndEvent();
            dialogueManager.ResetFestival();
        }

        public bool RunTests(DialogueTestCase testCase)
        {
            return CueNameFound(testCase) && CueExists(testCase) && CueAudioPlays(testCase);
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

        // <summary>Whether the given Cue name maps to a Cue in the Sound Bank</summary>
        public bool CueExists(DialogueTestCase testCase)
        {
            try
            {
                bool CueExists = AudioManager.GetCueFromDialogueId(testCase.CueName, testCase.CharacterName) != null;
                if (!CueExists)
                {
                    ErrorMessages.Add($"2. [FAILURE] CueExists: Cue not found for ID {testCase.CueName}");
                }

                return CueExists;
            }
            catch (Exception e)
            {
                ErrorMessages.Add($"2. [FAILURE] CueExists: Cue not found for ID {testCase.CueName}.");
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
