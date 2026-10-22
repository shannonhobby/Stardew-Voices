using System;

namespace StardewVoices.Utils
{
    internal class VolumeCalculator
    {
        /// <summary>
        /// Turns the 0–100 voices slider into a 0–1 playback volume. Squared so it behaves like
        /// a real fader (loudness isn't linear). 100 = 1.0, the voice file's own level: playback
        /// can't go louder than the file, so how loud voices are is set when the files are made.
        /// </summary>
        public static float CalculateVolume(int sliderVolume)
        {
            float t = Math.Clamp(sliderVolume, 0, 100) / 100f;
            return MathF.Pow(t, 2.0f);
        }
    }
}
