using System;
using StardewModdingAPI;
using StardewValley;
using StardewVoices.Audio;
using StardewVoices.Dialogue;
using StardewVoices.Pathing;
using StardewVoices.Testing.Models;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using StardewVoices.Utils;
using StardewVoices.Utils.GameDialogueExtraction;
using StardewVoices.Utils.ParsingHelpers;

namespace StardewVoices.StardewVoices.Testing
{
    public class ValidationManager
    {
        private CharacterDialogueExtractor dialogueExtractor;
        private EventDialogueExtractor eventExtractor;
        private FestivalDialogueExtractor festivalExtractor;
        private List<DialogueTestCase> testCases = new List<DialogueTestCase>();

        // Audio files that don't match any known line id, per character — the reverse of what
        // the test cases above check (missing audio). Populated by CheckForUnusedAudioFiles,
        // reported as its own section since it's a different kind of problem (a stray/typo'd
        // file) than a failed resolve-and-play test.
        private Dictionary<string, List<string>> unusedAudioFilesByCharacter = new();

        private ManagerHub Managers;
        private IModHelper helper;

        public List<ValidationReport> ValidationReport = new List<ValidationReport>();

        private readonly bool listUnusedAudio;

        // The characters to validate — every supported character unless a subset was asked for.
        private readonly List<string> characters;

        public ValidationManager(IModHelper helper, ManagerHub managers, bool listUnusedAudio = false, List<string> characters = null)
        {
            this.helper = helper;
            this.listUnusedAudio = listUnusedAudio;
            this.characters = characters ?? managers.CharacterManager.SupportedCharacters;
            dialogueExtractor = new CharacterDialogueExtractor(helper);
            eventExtractor = new EventDialogueExtractor(helper);
            festivalExtractor = new FestivalDialogueExtractor(helper);
            Managers = managers;
        }

        public void RunValidation()
        {
            // Results depend on which voice each character uses (built-in or a pack), so say up front.
            foreach (string character in characters)
                ErrorLogger.Info($"Validating {character} against {Managers.AudioManager.Resolver.GetSource(character)?.Name ?? "no voices"}.");

            GenerateTestCases();

            Managers.AudioManager.Muted = true;

            // Each test line logs at Trace level, which would flood the log file here —
            // suppress it and show periodic progress instead (see RunTestCases).
            ErrorLogger.SuppressTrace = true;
            Dictionary<DialogueType, List<ValidationResult>> results;
            try
            {
                results = RunTestCases();
            }
            finally
            {
                ErrorLogger.SuppressTrace = false;
            }

            foreach (DialogueType dialogueType in results.Keys)
                ValidationReport.Add(new ValidationReport(dialogueType, results[dialogueType], characters));

            // Stop the last test line first, or it would become audible when unmuted.
            Managers.AudioManager.StopAllLines();
            Managers.AudioManager.Muted = false;

            CheckForUnusedAudioFiles();

            PrintFinalRecap();

            Managers.TestingManager.ValidationIsRunning = false;
        }

        private void PrintFinalRecap()
        {
            int grandTotal = 0;
            int grandFailed = 0;

            foreach (ValidationReport report in ValidationReport)
            {
                grandTotal += report.TotalTestCount;
                grandFailed += report.FailedTestCount;
            }

            ErrorLogger.Trace("===================================FINAL VALIDATION RECAP===================================");

            foreach (ValidationReport report in ValidationReport)
            {
                if (report.failedTests.Count == 0)
                    continue;

                ErrorLogger.Trace($"--- FAILED {report.DialogueType.ToString().ToUpper()} LINES ---");
                foreach (string character in report.failedTests.Keys)
                {
                    string lineIds = string.Join("\n", report.failedTests[character]);
                    ErrorLogger.Error($"\n  {character}:\n {lineIds}");
                }
            }

            if (unusedAudioFilesByCharacter.Count > 0 && !listUnusedAudio)
            {
                int unusedCount = unusedAudioFilesByCharacter.Values.Sum(ids => ids.Count);
                ErrorLogger.Warn($"{unusedCount} unused audio file(s) (no matching line id). Run 'sv_validation --unused' to list them.");
            }
            else if (unusedAudioFilesByCharacter.Count > 0)
            {
                ErrorLogger.Trace("--- UNUSED AUDIO FILES (no matching line id) ---");
                foreach (string character in unusedAudioFilesByCharacter.Keys)
                {
                    string lineIds = string.Join("\n", unusedAudioFilesByCharacter[character]);
                    ErrorLogger.Error($"\n  {character}:\n {lineIds}");
                }
            }

            ErrorLogger.Trace("--------------------------------------------------------------------------------------------");
            ErrorLogger.Trace($"Grand Total: {grandTotal} tests");
            ErrorLogger.Error($"Failed: {grandFailed}");
            ErrorLogger.Trace($"Passed: {grandTotal - grandFailed}");
            ErrorLogger.Trace($"Success Rate: {Math.Floor((1.0 - (double)grandFailed / grandTotal) * 100)}%");
            ErrorLogger.Trace("============================================================================================");
        }

        private void GenerateTestCases()
        {
            foreach (string characterName in characters)
            {
                GenerateDialogueTestCases(characterName);
                GenerateEventDialogueTestCases(characterName);
                GenerateFestivalDialogueTestCases(characterName);
                GenerateModdedDialogueTestCases(characterName);
            }
        }

        /// <summary>
        /// A voice pack's own added lines, merged into the character's dialogue dictionary at
        /// startup (see CharacterDialogueHandler) under DialogueDictionary.ModdedDialogueType —
        /// unlike the other Generate*TestCases methods, these aren't re-extracted from vanilla
        /// game data, so this is the only source of test coverage for a pack's new lines. Most
        /// characters have none, which is the normal case, not a failure worth logging.
        /// </summary>
        private void GenerateModdedDialogueTestCases(string characterName)
        {
            DialogueDictionary dictionary = Managers.CharacterManager.GetCharacter(characterName)?.GetDialogueDictionary();
            if (dictionary == null)
                return;

            foreach (KeyValuePair<string, string[]> entry in dictionary.GetType(DialogueDictionary.ModdedDialogueType))
            {
                if (ShouldSkipTestCase(entry.Key))
                    continue;

                foreach (string lineId in entry.Value)
                {
                    var dialogueLine = new LabelledDialogueLine(lineId, entry.Key, lineId, DialogueType.Modded);
                    testCases.Add(new DialogueTestCase(characterName, DialogueType.Modded, dialogueLine, Managers.DialogueManager, helper));
                }
            }
        }

        private int GetCountOfDictionaryValues(Dictionary<DialogueType, List<LabelledDialogue>> dictionary)
        {
            int count = 0;
            foreach (KeyValuePair<DialogueType, List<LabelledDialogue>> pair in dictionary)
                count += pair.Value.Count();
            return count;
        }

        private void GenerateDialogueTestCases(string characterName)
        {
            Dictionary<DialogueType, List<LabelledDialogue>> lines = dialogueExtractor.GetAllDialogueForCharacter(Game1.getCharacterFromName(characterName));
            if (lines.Keys.Count == 0 || GetCountOfDictionaryValues(lines) == 0)
            {
                ErrorLogger.Error("No dialogue lines found");
            }

            foreach (DialogueType key in lines.Keys)
            {
                foreach (LabelledDialogue line in lines[key])
                {
                    if (line != null)
                    {
                        foreach (LabelledDialogueLine dialogueBox in line.Lines)
                        {
                            if (ShouldSkipTestCase(dialogueBox.Text))
                                continue;

                            DialogueTestCase newTestCase = new DialogueTestCase(characterName, key, dialogueBox, Managers.DialogueManager, helper);
                            if (!newTestCase.ResolvedLineId.ToLower().Contains("event_"))
                                testCases.Add(newTestCase);
                        }
                    }
                }
            }

            if (testCases.Count() == 0)
                ErrorLogger.Error($"No test cases created for {characterName}.");
        }

        private void GenerateEventDialogueTestCases(string characterName)
        {
            List<(string Text, string Context)> lines = eventExtractor.GetEventDialogueForCharacter(characterName);
            if (lines.Count == 0)
            {
                ErrorLogger.Warn($"No event dialogue found for {characterName}.");
                return;
            }
            foreach (var (text, context) in lines)
                if (ShouldSkipTestCase(text))
                {
                    continue;
                }
                else
                {
                    testCases.Add(new DialogueTestCase(characterName, DialogueType.Event, text, context, Managers.DialogueManager, helper));
                }
           }

        private void GenerateFestivalDialogueTestCases(string characterName)
        {
            List<(string Text, string Context)> lines = festivalExtractor.GetFestivalDialogueForCharacter(characterName);
            if (lines.Count == 0)
            {
                ErrorLogger.Warn($"No festival dialogue found for {characterName}.");
                return;
            }
            foreach (var (text, context) in lines)
                if (ShouldSkipTestCase(text))
                {
                    continue;
                } else
                {
                    testCases.Add(new DialogueTestCase(characterName, DialogueType.Festival, text, context, Managers.DialogueManager, helper));
                }
        }

        private Dictionary<DialogueType, List<ValidationResult>> RunTestCases()
        {
            Dictionary<DialogueType, List<ValidationResult>> results = new Dictionary<DialogueType, List<ValidationResult>>();
            int total = testCases.Count;
            int lastReportedPercent = -1;

            for (int i = 0; i < total; i++)
            {
                DialogueTestCase testCase = testCases[i];

                if (!results.ContainsKey(testCase.DialogueType))
                    results.Add(testCase.DialogueType, new List<ValidationResult>() { ValidateTestCase(testCase) });
                else
                {
                    results.TryGetValue(testCase.DialogueType, out List<ValidationResult> currentResults);
                    currentResults.Add(ValidateTestCase(testCase));
                }

                // Replaces the suppressed per-line Trace logging with one line every 2%.
                int percent = (i + 1) * 100 / total;
                if (percent != lastReportedPercent && percent % 2 == 0)
                {
                    ErrorLogger.Info($"Validating... {i + 1}/{total} ({percent}%)");
                    lastReportedPercent = percent;
                }
            }
            return results;
        }

        private ValidationResult ValidateTestCase(DialogueTestCase testCase)
        {
            return new ValidationResult(testCase, Managers.AudioManager, Managers.DialogueManager, Managers.PathManager, helper);
        }

        private bool ShouldSkipTestCase(string text)
        {
            return text == null || text.StartsWith('%') || text.Equals("...");
        }

        /// <summary>
        /// For whichever source (built-in or a pack) is active per character, flags audio files
        /// that don't match any known line id — the reverse of what the test cases check. Formerly
        /// sv_pack_report's job, scoped to one named pack; this instead checks whatever's actually
        /// active right now, built-in voices included.
        /// </summary>
        private void CheckForUnusedAudioFiles()
        {
            HashSet<string> knownVanillaLineIds = LoadKnownVanillaLineIds(Managers.PathManager);

            foreach (string character in characters)
            {
                FolderVoiceSource source = Managers.AudioManager.Resolver.GetSource(character);
                if (source == null)
                    continue;

                // A fully modded NPC (no vanilla Dialogue Jsons/Characters file) only speaks its
                // pack's own lines — a file named after some vanilla id (e.g. "4") is still unused.
                bool hasVanillaDialogue = CharacterDialogueHandler.FindDialogueFile(Managers.PathManager, character) != null;
                HashSet<string> knownIds = hasVanillaDialogue
                    ? new(knownVanillaLineIds, StringComparer.OrdinalIgnoreCase)
                    : new(StringComparer.OrdinalIgnoreCase);
                DialogueDictionary dictionary = Managers.CharacterManager.GetCharacter(character)?.GetDialogueDictionary();
                if (dictionary != null)
                {
                    foreach (string id in dictionary.GetType(DialogueDictionary.ModdedDialogueType).Values.SelectMany(ids => ids))
                        knownIds.Add(id);
                }

                List<string> unused = source.GetLineIds(character)
                    .Where(lineId => !IsKnownLineId(lineId, knownIds))
                    .OrderBy(lineId => lineId)
                    .ToList();

                if (unused.Count > 0)
                    unusedAudioFilesByCharacter[character] = unused;
            }
        }

        /// <summary>Whether a file's line id is used anywhere, directly or as the base of a "-suffix" variant.</summary>
        private static bool IsKnownLineId(string lineId, HashSet<string> knownLineIds)
        {
            return knownLineIds.Contains(lineId)
                || (VoiceResolver.GetFallbackLineId(lineId) is string baseId && knownLineIds.Contains(baseId));
        }

        /// <summary>
        /// Every line id the dialogue JSONs for the current language map text to, plus every
        /// recorded variant from variable_lines.json. Doesn't include pack-added (modded_dialogue)
        /// ids — those are character-specific and added separately in CheckForUnusedAudioFiles.
        /// </summary>
        private static HashSet<string> LoadKnownVanillaLineIds(PathManager pathManager)
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
    }
}
