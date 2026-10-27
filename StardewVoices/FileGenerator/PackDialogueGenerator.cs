using System.Collections.Generic;
using System.IO;
using StardewVoices.Dialogue;
using StardewVoices.Pathing;
using StardewVoices.Utils;
using StardewVoices.Utils.ParsingHelpers;

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

            // Shape matches what DialogueDictionary.MergeFrom expects and produces at runtime —
            // one flat bucket for every line, plus the usual continuation_dialogue list.
            Dictionary<string, List<string>> moddedDialogue = new();
            List<string> continuationIds = new();

            foreach (PackDialogueEntry entry in entries)
            {
                List<LabelledDialogueLine> lines = CharacterDialogueParser.ParseRawValueToLines(entry.RawText, entry.LineId, DialogueType.Dialogue);
                foreach (LabelledDialogueLine line in lines)
                {
                    if (!moddedDialogue.TryGetValue(line.Text, out List<string> ids))
                        moddedDialogue[line.Text] = ids = new List<string>();
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

            PathNode packsFolder = pathManager.GeneratedFilesRoot;
            packsFolder.CreateFolder(OutputFolderName);
            packsFolder = packsFolder.GetChild(OutputFolderName);

            string outputFileName = characterName + ".json";
            string outputPath = Path.Combine(packsFolder.FullPath, outputFileName);

            // Authors re-run this command as they edit their input file — unlike vanilla
            // generation's one-time output, this one should always reflect the latest parse.
            if (File.Exists(outputPath))
                File.Delete(outputPath);
            packsFolder.CreateJsonFile(outputFileName, output);

            ErrorLogger.Info($"[sv_parse_pack] Parsed {entries.Count} line(s) for {characterName} " +
                $"({continuationIds.Count} continuation). Wrote {outputPath} — copy it into your " +
                $"pack's 'dialogue' folder as {outputFileName}.");
        }
    }
}
