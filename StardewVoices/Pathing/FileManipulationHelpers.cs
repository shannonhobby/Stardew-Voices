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
