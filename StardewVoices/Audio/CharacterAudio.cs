using Microsoft.Xna.Framework.Audio;
using StardewValley;
using StardewVoices.Utils;

namespace StardewVoices.Audio
{
    public class CharacterAudio
    {
        public SoundBank SoundBank { get; set; }
        private AudioCategory AudioCategory { get; set; }

        public void SetVolume(float volume)
        {
            this.AudioCategory?.SetVolume(volume);
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

        public void SetupAudioBanks(string name, AudioEngine audioEngine, string audioPath)
        {
            SoundBank = new SoundBank(audioEngine, AudioPathHelper.GetGameRelativeAudioPath(audioPath, name + ".xsb"));
            AudioCategory = audioEngine.GetCategory(name);
        }

        public void Play(string cueName)
        {
            /// TODO
        }
    }
}