using System;
using System.IO;
using StardewModdingAPI;
using StardewVoices.Utils;

namespace StardewVoices.Pathing
{
    internal static class FileManipulationHelpers
    {
        private static IModHelper _helper;

        public static void Init(IModHelper helper)
        {
            _helper = helper;
        }

        /// <summary>
        /// The path of <paramref name="parent"/>'s subfolder called <paramref name="name"/>, ignoring
        /// case ("Voices" matches "voices"), so it behaves the same on Linux as on Windows and macOS.
        /// Null if there's no such folder.
        /// </summary>
        public static string FindFolder(string parent, string name)
        {
            if (!Directory.Exists(parent))
                return null;

            string exactMatch = Path.Combine(parent, name);
            if (Directory.Exists(exactMatch))
                return exactMatch;

            foreach (string folder in Directory.GetDirectories(parent))
            {
                if (string.Equals(Path.GetFileName(folder), name, StringComparison.OrdinalIgnoreCase))
                    return folder;
            }
            return null;
        }

        /// <summary>
        /// The path of the file called <paramref name="name"/> in <paramref name="folder"/>, ignoring
        /// case. Null if there's no such file.
        /// </summary>
        public static string FindFile(string folder, string name)
        {
            if (!Directory.Exists(folder))
                return null;

            string exactMatch = Path.Combine(folder, name);
            if (File.Exists(exactMatch))
                return exactMatch;

            foreach (string file in Directory.GetFiles(folder))
            {
                if (string.Equals(Path.GetFileName(file), name, StringComparison.OrdinalIgnoreCase))
                    return file;
            }
            return null;
        }

        public static void CreateNewFolder(string path)
        {
            if (Directory.Exists(path)) return;
            try
            {
                Directory.CreateDirectory(path);
            }
            catch (Exception ex)
            {
                ErrorLogger.Error($"Failed to create folder at '{path}'", ex);
            }
        }

        public static void CreateNewFile(string path, string text)
        {
            if (File.Exists(path)) return;
            try
            {
                File.WriteAllText(path, text);
            }
            catch (Exception ex)
            {
                ErrorLogger.Error($"Failed to create file at '{path}'", ex);
            }
        }

        public static void CreateNewJsonFile<T>(string path, T model) where T : class
        {
            if (File.Exists(path)) return;
            if (_helper is null)
            {
                ErrorLogger.Error("FileManipulationHelpers not initialized — call Init() before generating JSON files.");
                return;
            }
            try
            {
                _helper.Data.WriteJsonFile(Path.GetRelativePath(_helper.DirectoryPath, path), model);
                ErrorLogger.Trace($"Successfully created json file at {path}");

            }
            catch (Exception ex)
            {
                ErrorLogger.Error($"Failed to create JSON file at '{path}'", ex);
            }
        }
    }
}
