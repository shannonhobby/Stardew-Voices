using System;

namespace StardewVoices.Utils
{
    internal class VolumeCalculator
    {
        private static float CalculateCharacterVolume(int volume)
        {
            float t = volume / 100f;
            return 0.25f + MathF.Pow(t, 1.8f) * 4.5f;

        }

        private static float CalculateMasterVolume(int volume)
        {
            float t = volume / 100f;

            // softer curve than characters so it behaves like a real "fader"
            return MathF.Pow(t, 2.0f);
        }

        public static float CalculateVolume(int characterVolume, int masterVolume)
        {
            return CalculateCharacterVolume(characterVolume)
                 * CalculateMasterVolume(masterVolume);
        }
    }
}
