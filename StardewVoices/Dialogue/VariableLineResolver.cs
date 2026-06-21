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
    /// Handles dialogue lines whose cue names have a recorded-variant suffix
    /// (e.g. fall_Thu_chance_1-dragon). Loads variable_lines.json, then at
    /// runtime picks a random suffix and returns the resolved cue name.
    /// </summary>
    public class VariableLineResolver
    {
        // lineId (trimmed) → characterName → array of suffixes
        private readonly Dictionary<string, Dictionary<string, string[]>> _data =
            new(StringComparer.OrdinalIgnoreCase);

        private static readonly Random _rng = new();

        public VariableLineResolver(string jsonPath)
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
        /// chosen variant cue name (e.g. "fall_Thu_chance_1-dragon").
        /// Returns lineId unchanged if no variants are registered.
        /// An empty-string suffix means the base cue (no suffix) is one of the options.
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
        /// Returns every recorded variant cue name for this line and character.
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
            foreach (JProperty topProp in root.Properties())
            {
                var topObj = topProp.Value as JObject;
                if (topObj == null) continue;

                foreach (JProperty lineProp in topObj.Properties())
                {
                    string lineId = lineProp.Name.Trim();

                    if (!_data.ContainsKey(lineId))
                        _data[lineId] = new Dictionary<string, string[]>();

                    if (lineProp.Value.Type == JTokenType.Array)
                    {
                        // Direct character entry: top-level key is character name.
                        _data[lineId][topProp.Name] = lineProp.Value.ToObject<string[]>();
                    }
                    else if (lineProp.Value.Type == JTokenType.Object)
                    {
                        // Group entry (e.g. "LOVE INTERESTS"): inner object maps character → suffixes.
                        foreach (JProperty charProp in ((JObject)lineProp.Value).Properties())
                            _data[lineId][charProp.Name] = charProp.Value.ToObject<string[]>();
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
