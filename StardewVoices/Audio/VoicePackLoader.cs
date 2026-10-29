using System.Collections.Generic;
using System.Linq;
using StardewModdingAPI;
using StardewVoices.Pathing;
using StardewVoices.Utils;

namespace StardewVoices.Audio
{
    /// <summary>
    /// Finds installed voice packs: SMAPI content packs for Stardew Voices, laid out like the mod's
    /// own voices, i.e. <c>voices/&lt;Character&gt;/&lt;lineId&gt;.ogg</c>.
    /// A pack that needs a newer Stardew Voices says so with <c>ContentPackFor.MinimumVersion</c>
    /// in its manifest, and SMAPI won't load it.
    /// </summary>
    internal static class VoicePackLoader
    {
        public const string VoicesFolderName = "voices";

        /// <summary>A pack's optional folder of dialogue JSON, alongside its "voices" folder.</summary>
        public const string DialogueFolderName = "dialogue";

        /// <summary>A pack's optional override of the base mod's variable_lines.json, inside its dialogue folder.</summary>
        public const string VariableLinesFileName = "variable_lines.json";

        /// <summary>Every installed voice pack that has voice files. Packs without any are logged and left out.</summary>
        public static List<VoicePack> LoadAll(IEnumerable<IContentPack> contentPacks)
        {
            List<VoicePack> packs = new();
            foreach (IContentPack contentPack in contentPacks)
            {
                VoicePack pack = Load(contentPack);
                if (pack != null)
                    packs.Add(pack);
            }

            if (packs.Count == 0)
                ErrorLogger.Trace("No voice packs installed.");
            return packs;
        }

        private static VoicePack Load(IContentPack contentPack)
        {
            string packName = $"{contentPack.Manifest.Name} {contentPack.Manifest.Version}";

            string voicesFolder = FileManipulationHelpers.FindFolder(contentPack.DirectoryPath, VoicesFolderName);
            FolderVoiceSource source = voicesFolder != null ? new FolderVoiceSource(packName, voicesFolder) : null;
            if (source == null || source.LineCount == 0)
            {
                ErrorLogger.Warn($"{packName} has no voice files in a '{VoicesFolderName}' folder, so it doesn't add any voices.");
                return null;
            }

            string characters = string.Join(", ", source.Characters.OrderBy(name => name).Select(name => $"{name} ({source.CountLines(name)} lines)"));
            ErrorLogger.Info($"Voice pack {packName}: {characters}.");

            string dialogueFolder = FileManipulationHelpers.FindFolder(contentPack.DirectoryPath, DialogueFolderName);
            if (dialogueFolder != null)
                ErrorLogger.Info($"{packName} also has a '{DialogueFolderName}' folder, so it can voice modded NPCs or add extra lines for vanilla ones.");

            string variableLinesPath = dialogueFolder != null ? FileManipulationHelpers.FindFile(dialogueFolder, VariableLinesFileName) : null;
            if (variableLinesPath != null)
                ErrorLogger.Info($"{packName} also has its own '{VariableLinesFileName}', so its own variable-line options override the base mod's for the character(s) it voices.");

            return new VoicePack(contentPack.Manifest, source, dialogueFolder, variableLinesPath);
        }
    }
}
