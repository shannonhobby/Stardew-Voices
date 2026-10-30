using Newtonsoft.Json.Linq;
using StardewVoices.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace StardewVoices.Dialogue
{
    /// <summary>
    /// Handles dialogue lines whose line ids have a recorded-variant suffix
    /// (e.g. fall_Thu_chance_1-dragon). Loads variable_lines.json, then at
    /// runtime picks a random suffix and returns the resolved line id.
    /// </summary>
    public class VariableLineResolver
    {
        // lineId (trimmed) → characterName → array of suffixes
        private readonly Dictionary<string, Dictionary<string, string[]>> _data =
            new(StringComparer.OrdinalIgnoreCase);

        private static readonly Random _rng = new();

        private readonly string _basePath;

        public VariableLineResolver(string jsonPath)
        {
            _basePath = jsonPath;
            LoadFromPath(jsonPath);
        }

        /// <summary>
        /// Reloads the base mod's variable_lines.json, then for each character with an active
        /// pack that defines its own variable_lines.json, pulls <i>only that character's</i>
        /// entries out of that pack's file and merges them on top of the base. Scoping the pull
        /// per character (rather than merging whole pack files) means two different packs active
        /// for two different characters can't clobber each other's data even if both files happen
        /// to mention both characters (e.g. a pack author copied the base file as a starting point).
        /// </summary>
        public void RefreshPackOverrides(IReadOnlyDictionary<string, string> characterOverridePaths)
        {
            _data.Clear();
            LoadFromPath(_basePath);

            foreach (KeyValuePair<string, string> entry in characterOverridePaths)
            {
                string characterName = entry.Key;
                string jsonPath = entry.Value;
                try
                {
                    string json = File.ReadAllText(jsonPath, Encoding.UTF8);
                    LoadCharacterEntries(JObject.Parse(json), characterName);
                }
                catch (Exception e)
                {
                    ErrorLogger.Error($"VariableLineResolver: could not load {jsonPath} for {characterName}. {e}");
                }
            }
        }

        private void LoadFromPath(string jsonPath)
        {
            try
            {
                string json = File.ReadAllText(jsonPath, Encoding.UTF8);
                Load(JObject.Parse(json));
            }
            catch (Exception e)
            {
                ErrorLogger.Error($"VariableLineResolver: could not load {jsonPath}. {e}");
            }
        }

        /// <summary>
        /// If lineId has recorded variants for characterName, returns a randomly
        /// chosen variant line id (e.g. "fall_Thu_chance_1-dragon").
        /// Returns lineId unchanged if no variants are registered.
        /// An empty-string suffix means the base line (no suffix) is one of the options.
        /// </summary>
        public string Resolve(string lineId, string characterName)
        {
            if (!TryGetSuffixes(lineId, characterName, out string[] suffixes))
                return lineId;

            string suffix = suffixes[_rng.Next(suffixes.Length)];
            return suffix.Length == 0 ? lineId : lineId + "-" + suffix;
        }

        public bool IsVariableLine(string lineId, string characterName)
            => TryGetSuffixes(lineId, characterName, out _);

        /// <summary>
        /// Returns every recorded variant line id for this line and character.
        /// Used by validation to test all recorded variants instead of the raw template line.
        /// </summary>
        public string[] GetAllVariants(string lineId, string characterName)
        {
            if (!TryGetSuffixes(lineId, characterName, out string[] suffixes))
                return Array.Empty<string>();

            return suffixes
                .Select(s => s.Length == 0 ? lineId : lineId + "-" + s)
                .ToArray();
        }

        // ── Loading ──────────────────────────────────────────────────────────

        private void Load(JObject root)
        {
            ForEachEntry(root, (lineId, charName, suffixes) => SetEntry(lineId, charName, suffixes));
        }

        /// <summary>
        /// Like <see cref="Load"/>, but only keeps entries for <paramref name="targetCharacter"/> —
        /// any other character mentioned in <paramref name="root"/> (e.g. a pack's file that still
        /// has every vanilla character left over from copying the base file) is ignored.
        /// </summary>
        private void LoadCharacterEntries(JObject root, string targetCharacter)
        {
            ForEachEntry(root, (lineId, charName, suffixes) =>
            {
                if (charName == targetCharacter)
                    SetEntry(lineId, charName, suffixes);
            });
        }

        private void SetEntry(string lineId, string charName, string[] suffixes)
        {
            if (!_data.ContainsKey(lineId))
                _data[lineId] = new Dictionary<string, string[]>();
            _data[lineId][charName] = suffixes;
        }

        /// <summary>
        /// Walks variable_lines.json's shape — a line's value is either a straight suffix array
        /// (its enclosing top-level key is the character name) or an object keyed by character
        /// name (e.g. the "LOVE INTERESTS" grouping, whose own top-level key is just a label and
        /// is never read) — invoking <paramref name="visit"/> once per (lineId, character, suffixes).
        /// </summary>
        private static void ForEachEntry(JObject root, Action<string, string, string[]> visit)
        {
            foreach (JProperty topProp in root.Properties())
            {
                var topObj = topProp.Value as JObject;
                if (topObj == null) continue;

                foreach (JProperty lineProp in topObj.Properties())
                {
                    string lineId = lineProp.Name.Trim();

                    if (lineProp.Value.Type == JTokenType.Array)
                    {
                        // Direct character entry: top-level key is character name.
                        string charName = LocalizationHelpers.GetName(topProp.Name) ?? topProp.Name;
                        visit(lineId, charName, lineProp.Value.ToObject<string[]>());
                    }
                    else if (lineProp.Value.Type == JTokenType.Object)
                    {
                        // Group entry (e.g. "LOVE INTERESTS"): inner object maps character → suffixes.
                        foreach (JProperty charProp in ((JObject)lineProp.Value).Properties())
                        {
                            string charName = LocalizationHelpers.GetName(charProp.Name) ?? charProp.Name;
                            visit(lineId, charName, charProp.Value.ToObject<string[]>());
                        }
                    }
                }
            }
        }

        private bool TryGetSuffixes(string lineId, string characterName, out string[] suffixes)
        {
            suffixes = null;
            return lineId != null
                && characterName != null
                && _data.TryGetValue(lineId, out var charMap)
                && charMap.TryGetValue(characterName, out suffixes)
                && suffixes.Length > 0;
        }
    }
}
