using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using StardewVoices.Audio;
using StardewVoices.Dialogue;
using StardewVoices.Testing.Models;

namespace StardewVoices.Testing.Validation
{
    public class ValidationResult
    {
        public bool Success { get; set; }

        public List<string> ErrorMessages { get; set; }

        public DialogueTestCase TestCase { get; set; }

        private AudioManager AudioManager;
        private DialogueManager DialogueManager;

        public ValidationResult (DialogueTestCase testCase, AudioManager audioManager, DialogueManager dialogueManager) 
        {
            AudioManager = audioManager;
            DialogueManager = dialogueManager;
            Success = RunTests(testCase);
        }

        public bool RunTests(DialogueTestCase testCase)
        {
            return CueNameFound(testCase) && CueExists(testCase) && CueAudioPlays(testCase);
        }

        public bool CueNameFound(DialogueTestCase testCase)
        {
            try
            {
                bool cueNameFound = DialogueManager.getIdFromDialogueString(testCase.DialogueLine.getCurrentDialogue(), testCase.CharacterName) != null;
                if (!cueNameFound)
                {
                    ErrorMessages.Add($"[FAILURE] CueExists: Cue name not found for dialogue line {testCase.DialogueLine.getCurrentDialogue()}.");
                }
                return cueNameFound;
            } catch (Exception e)
            {
                ErrorMessages.Add($"[FAILURE] CueExists: Cue name not found for dialogue line {testCase.DialogueLine.getCurrentDialogue()}. {e}");
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
                    ErrorMessages.Add($"[FAILURE] CueExists: Cue not found for ID {testCase.CueName}");
                }

                return CueExists;
            } catch (Exception e)
            {
                ErrorMessages.Add($"[FAILURE] CueExists: Cue not found for ID {testCase.CueName}. {e}");
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
            } catch(Exception e)
            {
                ErrorMessages.Add($"[FAILURE] CueAudioPlays: {e}");
                return false;
            }
        }



    }
}
