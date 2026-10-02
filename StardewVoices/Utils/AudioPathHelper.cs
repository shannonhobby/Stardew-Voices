using Microsoft.Xna.Framework;
using System;
using System.Reflection;

namespace StardewVoices.Utils
{
    public static class AudioPathHelper
    {
        /// <summary>
        /// Runs <paramref name="load"/> with MonoGame's TitleContainer temporarily rooted at
        /// <paramref name="directory"/>, so XACT files (AudioEngine/SoundBank/WaveBank) can be
        /// opened by bare file name from any folder.
        /// </summary>
        /// <remarks>
        /// XACT classes only open files through TitleContainer.OpenStream, which rejects absolute
        /// paths and strips leading "../" segments, so files outside the game folder (e.g. mods
        /// installed by Stardrop or via --mods-path) can't be reached with a relative path.
        /// TitleContainer resolves names against its internal static Location, so we swap that
        /// instead. Only non-streaming wave banks are safe to load this way, since streaming ones
        /// reopen their file after the original Location is restored.
        /// </remarks>
        public static T LoadFromDirectory<T>(string directory, Func<T> load)
        {
            if (string.IsNullOrWhiteSpace(directory))
                throw new ArgumentException("Directory cannot be null or empty.", nameof(directory));

            PropertyInfo locationProperty = typeof(TitleContainer).GetProperty("Location", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("Couldn't find MonoGame's TitleContainer.Location, so voice audio can't be loaded. A game update may have changed it.");

            object originalLocation = locationProperty.GetValue(null);
            locationProperty.SetValue(null, directory);
            try
            {
                return load();
            }
            finally
            {
                locationProperty.SetValue(null, originalLocation);
            }
        }
    }
}
