using Microsoft.Xna.Framework.Audio;
using StardewModdingAPI;
using StardewValley.Audio;
using StardewVoices.Utils;
using System.Threading;
using System;
using StardewVoices.Characters;
using StardewVoices.Pathing;
using System.Collections.Generic;

namespace StardewVoices.Audio
{
    public class AudioManager
    {
        internal AudioEngine AudioEngine { get; }
        internal PathNode AudioPath { get; }
        public List<CharacterAudio> CharacterAudios { get; }
        internal CharacterManager CharacterManager { get; }

        private Cue lastCue { get; set; }
        private Cue currentCue { get; set; }

        public AudioManager(CharacterManager characterManager, PathManager pathManager) {
            AudioPath = pathManager.GetPath("Audio");
;           AudioEngine = new AudioEngine(AudioPathHelper.GetGameRelativeAudioPath(AudioPath.ToString(), "Stardew Voices.xgs"));
            CharacterManager = characterManager;
            try
            {
                foreach (string character in Constants.supportedCharacters)
                {
                    CharacterAudio newCharacterAudio = new CharacterAudio(character, this);
                    CharacterAudios.Add(newCharacterAudio);
                    characterManager.SetCharacterAudio(character, newCharacterAudio);
                }

            }
            catch (Exception e)
            {
                ErrorLogger.Error($"Error loading sound bank, audio cannot play:", e);
            }
        }


        public void Play(string lineId, string characterName)
        {
            SoundBank soundBank = CharacterManager.GetCharacter(characterName)?.GetSoundBank();
            currentCue = getCueFromDialogueId(lineId, soundBank, characterName);


            if (lastCue != null && lastCue.IsPlaying)
            {
                lastCue.Stop(AudioStopOptions.Immediate);
            }

            currentCue.Play();
        }

        public Cue GetCueFromDialogueId(string lineId, string characterName)
        {
            Character character = CharacterManager.GetCharacter(characterName);
            return character.Audio.GetCue(lineId);

        }


        public void StopCue()
        {
            if (currentCue != null)
            {
                currentCue.Stop(AudioStopOptions.Immediate);
                currentCue = null;

            }

            StopLastCue();
        }

        public void StopLastCue()
        {

            if (lastCue != null && lastCue.IsPlaying)
            {
                lastCue.Stop(AudioStopOptions.Immediate);
            }
        }

        public Boolean LastCueIsStillPlaying() {
            return (this.lastCue == null || (this.lastCue != null && !this.lastCue.IsPlaying);
        }

        public void NextCue()
        {
            if (this.currentCue != null)
            {
                this.lastCue = currentCue;
            }
        }

        public Cue getCueFromDialogueId(string lineId, SoundBank soundBank, string characterName)
        {
            try
            {
                return soundBank.GetCue(lineId);
            }
            catch (Exception error)
            {
                ErrorLogger.WarnOnce($"Error getting cue for {characterName}: {lineId} Sound Bank: {error}");
                return null;
            }
        }
    }
}