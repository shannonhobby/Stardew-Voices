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
        internal CharacterManager CharacterManager { get; }

        private Cue lastCue { get; set; }
        private Cue currentCue { get; set; }

        public AudioManager(CharacterManager characterManager, PathManager pathManager) {
            AudioPath = pathManager.GetPath("Audio");
            ErrorLogger.Trace($"Audio path located: {AudioPath}");
            AudioEngine = new AudioEngine(AudioPathHelper.GetGameRelativeAudioPath(AudioPath.ToString(), "Stardew Voices.xgs"));
            CharacterManager = characterManager;
            try
            {
                foreach (string character in characterManager.SupportedCharacters)
                {
                    CharacterAudio newCharacterAudio = new CharacterAudio(character, this);
                    characterManager.SetCharacterAudio(character, newCharacterAudio);
                }

            }
            catch (Exception e)
            {
                ErrorLogger.Error($"Error loading sound bank, audio cannot play:", e);
            }
        }

        public void Play(Cue cue)
        {
            StopLastCue();

            if (cue != null)
            {
                ErrorLogger.Trace($"Playing cue");
                currentCue = cue;
                cue.Play();
            } else
            {
                ErrorLogger.Warn($"Unable to play cue");
            }
        }


        public void Play(string lineId, string characterName)
        {
            currentCue = GetCueFromDialogueId(lineId, characterName);

            StopLastCue();

            if(currentCue != null)
            {
                ErrorLogger.Trace($"Playing cue: {characterName}: {lineId}");
                currentCue.Play();
            }
        }
  
        public void StopCue()
        {
            if (currentCue != null)
            {
                ErrorLogger.Trace("Current cue stopped");
                currentCue.Stop(AudioStopOptions.Immediate);
                currentCue = null;

            }

            StopLastCue();
        }

        public void StopLastCue()
        {

            if (lastCue != null && lastCue.IsPlaying)
            {
                ErrorLogger.Trace("Last cue stopped");
                lastCue.Stop(AudioStopOptions.Immediate);
            }
        }

        public Boolean LastCueIsStillPlaying() {
            return (this.lastCue == null || (this.lastCue != null && !this.lastCue.IsPlaying));
        }

        public void NextCue()
        {
            if (this.currentCue != null)
            {
                ErrorLogger.Trace("Last cue set to current cue");
                this.lastCue = currentCue;
            }
        }


        public void MuteAll()
        {
            foreach (string name in CharacterManager.SupportedCharacters)
                CharacterManager.GetCharacter(name)?.SetVolume(0);
        }

        public void UnmuteAll()
        {
            foreach (string name in CharacterManager.SupportedCharacters)
                CharacterManager.GetCharacter(name)?.SetVolume(1);
        }

        /// <param name="allowFallback">
        /// When true (the default, used during real playback) and no cue is recorded for
        /// <paramref name="lineId"/>, retries once against the generic cue with the
        /// trailing "-{suffix}" stripped (e.g. "NPC_cs_3985-Abigail" -> "NPC_cs_3985") —
        /// these dash-suffixed ids are name/genre-substituted variants of one base line,
        /// so the un-suffixed recording (voiced with the generic placeholder, e.g.
        /// "Someone") is a reasonable stand-in when the specific variant wasn't recorded.
        /// Pass false to check a single exact id, e.g. from validation, which needs to
        /// tell an exact hit apart from a fallback hit.
        /// </param>
        public Cue GetCueFromDialogueId(string lineId, string characterName, bool allowFallback = true)
        {
            try
            {
                Character character = CharacterManager.GetCharacter(characterName);
                SoundBank soundBank = character.GetSoundBank();
                return soundBank.GetCue(lineId);
            }
            catch (Exception error)
            {
                string fallbackLineId = allowFallback ? GetFallbackLineId(lineId) : null;
                if (fallbackLineId == null)
                {
                    ErrorLogger.WarnOnce($"Error getting cue for {characterName}: {lineId} Sound Bank: {error}");
                    return null;
                }

                Cue fallbackCue = GetCueFromDialogueId(fallbackLineId, characterName, allowFallback: false);
                if (fallbackCue == null)
                    ErrorLogger.WarnOnce($"Error getting cue for {characterName}: {lineId} Sound Bank: {error}");
                return fallbackCue;
            }
        }

        // "NPC_cs_3985-Abigail" -> "NPC_cs_3985". Null if lineId has no "-{suffix}" to strip.
        private static string GetFallbackLineId(string lineId)
        {
            int dashIndex = lineId?.LastIndexOf('-') ?? -1;
            return dashIndex > 0 ? lineId.Substring(0, dashIndex) : null;
        }
    }
}