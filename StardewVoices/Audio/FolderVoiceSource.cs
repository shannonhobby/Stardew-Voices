using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using StardewVoices.Utils;

namespace StardewVoices.Audio
{
    /// <summary>
    /// Voice audio stored as loose files: <c>&lt;root&gt;/&lt;Character&gt;/&lt;lineId&gt;.ogg</c>
    /// (or <c>.wav</c>). The file name without its extension is the line id, exactly.
    /// Only file paths are indexed up front; audio is loaded when a line plays.
    /// </summary>
    public class FolderVoiceSource
    {
        private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase) { ".ogg", ".wav" };

        // character -> (line id -> file path). Case-insensitive at both levels, so lookups
        // behave the same on Windows, macOS and Linux.
        private readonly Dictionary<string, Dictionary<string, string>> _pathsByCharacter = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>Shown in logs, e.g. "Built-in voices" or a pack's name and version.</summary>
        public string Name { get; }
        public string RootPath { get; }
        public int LineCount => _pathsByCharacter.Values.Sum(paths => paths.Count);
        public IReadOnlyCollection<string> Characters => _pathsByCharacter.Keys;

        public FolderVoiceSource(string name, string rootPath)
        {
            Name = name;
            RootPath = rootPath;

            if (!Directory.Exists(rootPath))
            {
                ErrorLogger.Trace($"{Name}: no voices folder at {rootPath}");
                return;
            }

            // Each subfolder is one character's lines.
            string[] characterFolders = Directory.GetDirectories(rootPath);
            Array.Sort(characterFolders, StringComparer.Ordinal);
            foreach (string characterFolder in characterFolders)
                AddCharacter(characterFolder);
        }

        /// <summary>Every line id this source has a file for, for one character.</summary>
        public IEnumerable<string> GetLineIds(string character)
        {
            return character != null && _pathsByCharacter.TryGetValue(character, out var paths) ? paths.Keys : Enumerable.Empty<string>();
        }

        public int CountLines(string character)
        {
            return character != null && _pathsByCharacter.TryGetValue(character, out var paths) ? paths.Count : 0;
        }

        public bool HasLine(string character, string lineId)
        {
            return GetPath(character, lineId) != null;
        }

        /// <summary>Null if this source has no audio for the line.</summary>
        public FileVoiceLine CreateLine(string character, string lineId)
        {
            string path = GetPath(character, lineId);
            return path != null ? new FileVoiceLine(path, character, lineId) : null;
        }

        internal static bool IsOgg(string file)
        {
            return Path.GetExtension(file).Equals(".ogg", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Null if this source has no file for the line.</summary>
        private string GetPath(string character, string lineId)
        {
            if (character == null || lineId == null)
                return null;

            return _pathsByCharacter.TryGetValue(character, out var paths) && paths.TryGetValue(lineId, out string path)
                ? path
                : null;
        }

        /// <summary>
        /// Registers one character folder as a character this source can voice.
        /// The folder name is the character name.
        /// </summary>
        private void AddCharacter(string characterFolder)
        {
            string character = Path.GetFileName(characterFolder);

            // Folders that only differ by case ("Abigail" + "abigail", possible on Linux)
            // would be the same character. Keep the first.
            if (_pathsByCharacter.ContainsKey(character))
            {
                ErrorLogger.Warn($"{Name}: more than one folder for {character}. Skipping {characterFolder}.");
                return;
            }

            Dictionary<string, string> paths = FindAudioFiles(character, characterFolder);

            // An empty folder doesn't claim the character, so it can't silence another source.
            if (paths.Count > 0)
                _pathsByCharacter[character] = paths;
        }

        /// <summary>
        /// Maps each line id in <paramref name="characterFolder"/> to its audio file.
        /// Files that aren't .ogg or .wav are ignored.
        /// </summary>
        private Dictionary<string, string> FindAudioFiles(string character, string characterFolder)
        {
            Dictionary<string, string> paths = new(StringComparer.OrdinalIgnoreCase);

            // Sorted so that which file wins a name clash doesn't depend on the file system.
            string[] files = Directory.GetFiles(characterFolder);
            Array.Sort(files, StringComparer.Ordinal);

            foreach (string file in files)
            {
                if (!SupportedExtensions.Contains(Path.GetExtension(file)))
                    continue;

                string lineId = Path.GetFileNameWithoutExtension(file);
                paths[lineId] = paths.TryGetValue(lineId, out string existingFile)
                    ? ChooseFile(character, lineId, existingFile, file)
                    : file;
            }

            return paths;
        }

        /// <summary>
        /// Picks which of two files for the same line to use, and warns about it. Happens with
        /// "Intro.ogg" + "Intro.wav", or with names that only differ by case (possible on Linux).
        /// An .ogg beats a .wav; otherwise the file found first is kept.
        /// </summary>
        private string ChooseFile(string character, string lineId, string existingFile, string newFile)
        {
            string chosen = IsOgg(newFile) && !IsOgg(existingFile) ? newFile : existingFile;
            ErrorLogger.Warn($"{Name}: {character} has more than one file for line '{lineId}' " +
                $"({Path.GetFileName(existingFile)}, {Path.GetFileName(newFile)}). Using {Path.GetFileName(chosen)}.");
            return chosen;
        }
    }
}
