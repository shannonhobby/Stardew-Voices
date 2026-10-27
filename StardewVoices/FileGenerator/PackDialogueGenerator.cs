using StardewVoices.Dialogue;
using StardewVoices.Pathing;
using StardewVoices.Utils;
using StardewVoices.Utils.ParsingHelpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace StardewVoices.FileGeneration
{
    /// <summary>
    /// The sv_parse_pack command's logic: parses a pack author's pasted-dialogue input file
    /// (dropped into Stardew Voices' own "Input" folder) for one character, through the same
    /// parser vanilla generation uses, and writes the result into the mod's own Generated Files
    /// folder — same precedent as sv_generation — for the author to copy into their pack's own
    /// "dialogue" folder.
    /// </summary>
    internal static class PackDialogueGenerator
    {
        private const string OutputFolderName = "Custom Mod Jsons";

        public static void Run(PathManager pathManager, string characterName)
        {
            string inputFile = FileManipulationHelpers.FindFile(pathManager.InputRoot.FullPath, characterName + ".txt");
            if (inputFile == null)
            {
                ErrorLogger.Warn($"[sv_parse_pack] No '{characterName}.txt' found in '{pathManager.InputRoot.FullPath}'.");
                return;
            }

            List<PackDialogueEntry> entries = PackDialogueInputParser.Parse(inputFile, out List<string> parseErrors);
            foreach (string error in parseErrors)
                ErrorLogger.Warn($"[sv_parse_pack] {error}");

            // Keyed by line id so a translation's entries can be parsed with the same ids as the
            // English ones (see CharacterDialogueParser.ParseRawValueToLines's englishRawValue param),
            // so the same recorded audio matches every language.
            Dictionary<string, string> englishRawByLineId = new();
            foreach (PackDialogueEntry entry in entries)
                englishRawByLineId[entry.LineId] = entry.RawText;

            DialogueDictionary vanillaDialogue = CheckForVanillaDialogue(pathManager, characterName);

            GenerateOutput(pathManager, characterName, entries, vanillaDialogue, englishRawByLineId: null, languageSubfolder: null);

            // Translations: <Character>.<lang>.txt for any language this mod recognizes.
            foreach (string language in Constants.languageDisplayNames.Keys)
            {
                if (language == "en") continue;

                string localizedInputFile = FileManipulationHelpers.FindFile(pathManager.InputRoot.FullPath, $"{characterName}.{language}.txt");
                if (localizedInputFile == null) continue;

                List<PackDialogueEntry> localizedEntries = PackDialogueInputParser.Parse(localizedInputFile, out List<string> localizedErrors);
                foreach (string error in localizedErrors)
                    ErrorLogger.Warn($"[sv_parse_pack] ({language}) {error}");

                GenerateOutput(pathManager, characterName, localizedEntries, vanillaDialogue, englishRawByLineId, language);
            }
        }

        private static DialogueDictionary CheckForVanillaDialogue(PathManager pathManager, string characterName)
        {
            string vanillaDialogueJsonPath = CharacterDialogueHandler.FindDialogueFile(pathManager, characterName);

            if (vanillaDialogueJsonPath != null)
            {
                try
                {
                    string jsonString = File.ReadAllText(vanillaDialogueJsonPath, Encoding.UTF8);
                    return new DialogueDictionary(jsonString);

                }
                catch (Exception ex)
                {
                    ErrorLogger.Debug($"Error setting up dialogue dictionary from path {vanillaDialogueJsonPath}.\n{ex} :(");
                    return null;
                }
            }

            return null;
            
        }


        /// <summary>
        /// Parses <paramref name="entries"/> and writes the result to Generated Files/Custom Mod
        /// Jsons[/&lt;languageSubfolder&gt;]/&lt;characterName&gt;.json. When
        /// <paramref name="englishRawByLineId"/> is given (a translation pass), each entry's line id
        /// is anchored to the English raw text for the same id, if there is one, so ids stay
        /// identical across languages.
        /// </summary>
        private static void GenerateOutput(
            PathManager pathManager,
            string characterName,
            List<PackDialogueEntry> entries,
            DialogueDictionary vanillaDialogue,
            Dictionary<string, string> englishRawByLineId,
            string languageSubfolder)
        {
            // Shape matches what DialogueDictionary.MergeFrom expects and produces at runtime —
            // one flat bucket for every line, plus the usual continuation_dialogue list.
            Dictionary<string, List<string>> moddedDialogue = new();
            List<string> continuationIds = new();

            // Tracks which input entry (by its OriginalKey, i.e. the author's "line_id") first
            // claimed each distinct text, so a second *different* entry producing the same text
            // can be flagged as a duplicate (C4) rather than treated as an intentional multi-id
            // text (e.g. a gender-variant pair from the same entry sharing identical text).
            Dictionary<string, string> firstOriginalKeyByText = new();

            foreach (PackDialogueEntry entry in entries)
            {
                string englishRawValue = null;
                englishRawByLineId?.TryGetValue(entry.LineId, out englishRawValue);
                List<LabelledDialogueLine> lines = CharacterDialogueParser.ParseRawValueToLines(entry.RawText, entry.LineId, DialogueType.Dialogue, englishRawValue);
                foreach (LabelledDialogueLine line in lines)
                {
                    // Check for line id collissions with vanilla dialogue
                    if (vanillaDialogue != null)
                    {
                       string existingLine = vanillaDialogue.FindTextForLineId(line.LineId);
                       if (existingLine != null)
                        {
                            ErrorLogger.Warn($"{characterName}: the pack's line id \"{line.LineId}\" is already " +
                               $"used for a different line (\"{existingLine}\"). Both would try to play the same audio file — rename one of them.");
                        }
                    }

                    if (!moddedDialogue.TryGetValue(line.Text, out List<string> ids))
                    {
                        moddedDialogue[line.Text] = ids = new List<string>();
                        firstOriginalKeyByText[line.Text] = line.OriginalKey;
                    }
                    else if (firstOriginalKeyByText[line.Text] != line.OriginalKey && !ids.Contains(line.LineId))
                    {
                        ErrorLogger.Warn($"{characterName}: \"{entry.LineId}\" produces the same text as " +
                            $"\"{firstOriginalKeyByText[line.Text]}\" (\"{line.Text}\"). Keeping the first id " +
                            $"({string.Join(", ", ids)}); \"{line.LineId}\" will not be written.");
                        continue;
                    }

                    if (!ids.Contains(line.LineId))
                        ids.Add(line.LineId);

                    if (line.IsContinuation)
                        continuationIds.Add(line.LineId);
                }
            }

            Dictionary<string, object> output = new()
            {
                [DialogueDictionary.ModdedDialogueType] = moddedDialogue,
                ["continuation_dialogue"] = new Dictionary<string, List<string>> { ["continuation_dialogue"] = continuationIds },
            };

            PathNode outputFolder = pathManager.GeneratedFilesRoot;
            outputFolder.CreateFolder(OutputFolderName);
            outputFolder = outputFolder.GetChild(OutputFolderName);
            if (languageSubfolder != null)
            {
                outputFolder.CreateFolder(languageSubfolder);
                outputFolder = outputFolder.GetChild(languageSubfolder);
            }

            string outputFileName = characterName + ".json";
            string outputPath = Path.Combine(outputFolder.FullPath, outputFileName);

            // Authors re-run this command as they edit their input file — unlike vanilla
            // generation's one-time output, this one should always reflect the latest parse.
            if (File.Exists(outputPath))
                File.Delete(outputPath);
            outputFolder.CreateJsonFile(outputFileName, output);

            string dialogueSubpath = languageSubfolder != null ? $"dialogue/{languageSubfolder}" : "dialogue";
            ErrorLogger.Info($"[sv_parse_pack] Parsed {entries.Count} line(s) for {characterName}" +
                (languageSubfolder != null ? $" ({languageSubfolder})" : "") +
                $" ({continuationIds.Count} continuation). Wrote {outputPath} — copy it into your " +
                $"pack's '{dialogueSubpath}' folder as {outputFileName}.");
        }
    }
}
