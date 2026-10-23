using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using StardewVoices.Audio;
using StardewVoices.Pathing;
using StardewVoices.Utils;

namespace StardewVoices.Testing
{
    /// <summary>
    /// The sv_pack_report command, for voice pack authors: which characters a pack provides,
    /// whether each one is in use, and which of its files don't match any line id (usually a typo).
    /// Missing lines are what sv_validation reports, once the pack is chosen for a character.
    /// </summary>
    internal static class VoicePackReport
    {
        // Show this many unknown files in the console; the full list goes to a file.
        private const int MaxUnknownShown = 20;

        public static void Run(ManagerHub managers, string packQuery)
        {
            VoicePack pack = FindPack(managers.VoicePacks, packQuery);
            if (pack == null)
                return;

            HashSet<string> knownLineIds = LoadKnownLineIds(managers.PathManager);
            List<string> reportLines = new() { $"Voice pack report: {pack.Source.Name} ({pack.Manifest.UniqueID})" };

            foreach (string character in pack.Source.Characters.OrderBy(name => name))
            {
                bool inUse = managers.AudioManager.Resolver.GetSource(character) == pack.Source;
                string usedSource = managers.AudioManager.Resolver.GetSource(character)?.Name ?? "nothing";
                List<string> unknown = pack.Source.GetLineIds(character)
                    .Where(lineId => !IsKnown(lineId, knownLineIds))
                    .OrderBy(lineId => lineId)
                    .ToList();

                ErrorLogger.Info($"{character}: {pack.Source.CountLines(character)} lines, " +
                    (inUse ? "in use." : $"not in use ({usedSource} is chosen)."));
                reportLines.Add($"{character}: {pack.Source.CountLines(character)} lines, {(inUse ? "in use" : "not in use")}");

                if (unknown.Count > 0)
                {
                    ErrorLogger.Warn($"  {unknown.Count} file(s) don't match any line id:");
                    foreach (string lineId in unknown.Take(MaxUnknownShown))
                        ErrorLogger.Warn($"    {lineId}");
                    if (unknown.Count > MaxUnknownShown)
                        ErrorLogger.Warn($"    ...and {unknown.Count - MaxUnknownShown} more (see the report file).");

                    reportLines.Add($"  Files that don't match any line id ({unknown.Count}):");
                    reportLines.AddRange(unknown.Select(lineId => $"    {lineId}"));
                }
            }

            string reportPath = WriteReport(managers.PathManager, pack, reportLines);
            ErrorLogger.Info($"Full report: {reportPath}");
            ErrorLogger.Info("To see which lines a character is missing, choose this pack for them in the config menu and run sv_validation.");
        }

        /// <summary>The installed pack whose UniqueID or name matches, or null (with the installed packs listed).</summary>
        private static VoicePack FindPack(List<VoicePack> packs, string packQuery)
        {
            VoicePack pack = packs.FirstOrDefault(p =>
                string.Equals(p.Manifest.UniqueID, packQuery, StringComparison.OrdinalIgnoreCase)
                || string.Equals(p.Manifest.Name, packQuery, StringComparison.OrdinalIgnoreCase));
            if (pack != null)
                return pack;

            ErrorLogger.Warn(packs.Count == 0
                ? "No voice packs are installed."
                : $"No installed voice pack matches '{packQuery}'. Installed: {string.Join(", ", packs.Select(p => $"{p.Manifest.Name} ({p.Manifest.UniqueID})"))}");
            return null;
        }

        /// <summary>Whether a file's line id is used anywhere, directly or as the base of a "-suffix" variant.</summary>
        private static bool IsKnown(string lineId, HashSet<string> knownLineIds)
        {
            return knownLineIds.Contains(lineId)
                || (VoiceResolver.GetFallbackLineId(lineId) is string baseId && knownLineIds.Contains(baseId));
        }

        /// <summary>
        /// Every line id the dialogue JSONs for the current language map text to, plus every
        /// recorded variant from variable_lines.json.
        /// </summary>
        private static HashSet<string> LoadKnownLineIds(PathManager pathManager)
        {
            HashSet<string> lineIds = new(StringComparer.OrdinalIgnoreCase);
            string languageFolder = Path.GetDirectoryName(pathManager.GetPathString("Characters"));

            foreach (string subfolder in new[] { "Characters", "Events", "Festivals", "Generic" })
            {
                string folder = Path.Combine(languageFolder, subfolder);
                if (!Directory.Exists(folder))
                    continue;
                foreach (string file in Directory.GetFiles(folder, "*.json", SearchOption.AllDirectories))
                    CollectLineIds(JToken.Parse(File.ReadAllText(file)), lineIds);
            }

            string variableLinesPath = Path.Combine(languageFolder, "variable_lines.json");
            if (File.Exists(variableLinesPath))
                CollectVariantIds(JObject.Parse(File.ReadAllText(variableLinesPath)), lineIds);

            return lineIds;
        }

        // The dialogue JSONs map text to line ids at varying depths ({text: id}, {text: [ids]},
        // {type: {text: [ids]}}), so every string value is a line id.
        private static void CollectLineIds(JToken token, HashSet<string> lineIds)
        {
            switch (token)
            {
                case JValue value when value.Type == JTokenType.String:
                    lineIds.Add((string)value);
                    break;
                case JArray array:
                    foreach (JToken item in array)
                        CollectLineIds(item, lineIds);
                    break;
                case JObject obj:
                    foreach (JProperty property in obj.Properties())
                        CollectLineIds(property.Value, lineIds);
                    break;
            }
        }

        // variable_lines.json: {group or character: {lineId: [suffixes] or {character: [suffixes]}}}.
        // An empty suffix means the base id itself.
        private static void CollectVariantIds(JObject variableLines, HashSet<string> lineIds)
        {
            foreach (JProperty group in variableLines.Properties())
            {
                if (group.Value is not JObject lines)
                    continue;
                foreach (JProperty line in lines.Properties())
                {
                    string lineId = line.Name.Trim();
                    IEnumerable<JToken> suffixLists = line.Value is JObject byCharacter ? byCharacter.Properties().Select(p => p.Value) : new[] { line.Value };
                    foreach (string suffix in suffixLists.OfType<JArray>().SelectMany(list => list.Values<string>()))
                        lineIds.Add(suffix.Length == 0 ? lineId : $"{lineId}-{suffix}");
                }
            }
        }

        private static string WriteReport(PathManager pathManager, VoicePack pack, List<string> reportLines)
        {
            string folder = pathManager.GeneratedFilesRoot.FullPath;
            FileManipulationHelpers.CreateNewFolder(folder);
            string path = Path.Combine(folder, $"pack-report-{pack.Manifest.UniqueID}.txt");
            File.WriteAllLines(path, reportLines);
            return path;
        }
    }
}
