using StardewModdingAPI;
using StardewValley;
using System;
using System.IO;

namespace StardewVoices.Utils
{
    public static class AudioPathHelper
    {
        /// <summary>
        /// Converts a mod-local audio file path into a MonoGame-compatible
        /// relative path from the game root directory.
        /// </summary>
        public static string GetGameRelativeAudioPath(string modDirectory, string relativeFilePath)
        {
            if (string.IsNullOrWhiteSpace(modDirectory))
                throw new ArgumentException("Mod directory cannot be null or empty.", nameof(modDirectory));

            if (string.IsNullOrWhiteSpace(relativeFilePath))
                throw new ArgumentException("File path cannot be null or empty.", nameof(relativeFilePath));

            // 1. Build absolute path to file inside mod
            string absolutePath = Path.Combine(modDirectory, relativeFilePath);

            // 2. Game root (what TitleContainer expects as base)
            string gameRoot = AppDomain.CurrentDomain.BaseDirectory;

            // 3. Convert to relative path from game root
            string relativeToGame = Path.GetRelativePath(gameRoot, absolutePath);

            // 4. Normalize separators for MonoGame/XNA
            relativeToGame = relativeToGame
                .Replace(Path.DirectorySeparatorChar, '/')
                .Replace(Path.AltDirectorySeparatorChar, '/');

            return relativeToGame;
        }
    }
}