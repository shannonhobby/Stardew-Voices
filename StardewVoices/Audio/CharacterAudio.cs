using Microsoft.Xna.Framework.Audio;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Audio;
using StardewVoices.Utils;
using System.Threading;
using System;

namespace StardewVoices.Audio
{
    public class CharacterAudio
    {
        private SoundBank _soundBank { get; }
        private WaveBank _waveBank { get; }
        private AudioCategory _audioCategory { get; }

        public CharacterAudio(string name, AudioManager audioManager) {
            try
            {
                _soundBank = new SoundBank(audioManager.AudioEngine, AudioPathHelper.GetGameRelativeAudioPath(audioManager.AudioPath.ToString(), name + ".xsb"));
                _waveBank = new WaveBank(audioManager.AudioEngine, AudioPathHelper.GetGameRelativeAudioPath(audioManager.AudioPath.ToString(), name + ".xwb"));
                _audioCategory = audioManager.AudioEngine.GetCategory(name);
            } catch (Exception ex)
            {
                ErrorLogger.Warn($"Error setting up character audio for {name}\n{ex} :(");
            }
        
        }

        public void SetVolume(float volume)
        {
            this._audioCategory?.SetVolume(volume);
            // access the config from here?
            /*
            var prop = this.Config.GetType().GetProperty($"{character}Volume");
            if (prop == null)
            {
                this.characterDictionary[character].SetVolume(VolumeCalculator.CalculateVolume(100, this.Config.DialogueVolume));

            }
            else
            {
                this.characterDictionary[character].SetVolume(VolumeCalculator.CalculateVolume((int)prop.GetValue(this.Config), this.Config.DialogueVolume));

            }
            */
        }

        public SoundBank GetSoundBank()
        {
            return _soundBank;
        }

    }
}