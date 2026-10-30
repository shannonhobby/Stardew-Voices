using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using StardewModdingAPI;
using StardewVoices.Pathing;

namespace StardewVoices.Audio
{
    /// <summary>
    /// Owns every installed voice pack and decides which single source — a pack, or the
    /// built-in voices — is active for each character. This is the one place that answers
    /// "whose pack (if any) is voicing character X", for both AudioManager (picking audio to
    /// play) and CharacterDialogueHandler (picking up a pack's own added dialogue for the same
    /// character) — so that policy lives in one place instead of being duplicated or reached
    /// into across unrelated managers.
    /// </summary>
    public class VoicePackManager
    {
        /// <summary>Every installed voice pack that has voice files.</summary>
        public List<VoicePack> Packs { get; }

        /// <summary>The voices that ship with Stardew Voices itself.</summary>
        internal FolderVoiceSource BuiltInVoices { get; }

        /// <summary>Every installed source for each character, default first. For the config menu.</summary>
        internal Dictionary<string, List<VoiceSourceOption>> VoiceOptions { get; }

        /// <summary>Every character with at least one installed voice source (a pack or the built-in voices).</summary>
        public IReadOnlyCollection<string> Characters => VoiceOptions.Keys;

        private Dictionary<string, VoiceSourceOption> _chosen;

        // Looked up by VoiceSourceOption.Id (a pack's UniqueID) when resolving the active VoicePack.
        private readonly Dictionary<string, VoicePack> _packsById;

        /// <param name="contentPacks">Installed SMAPI content packs for this mod.</param>
        /// <param name="characterVoices">The player's choice of voice per character (see ModConfig.CharacterVoices).</param>
        public VoicePackManager(PathManager pathManager, IEnumerable<IContentPack> contentPacks, IDictionary<string, string> characterVoices)
        {
            Packs = VoicePackLoader.LoadAll(contentPacks);
            _packsById = Packs.ToDictionary(pack => pack.Manifest.UniqueID, StringComparer.OrdinalIgnoreCase);

            string modPath = pathManager.ModPath.ToString();
            string voicesFolder = FileManipulationHelpers.FindFolder(modPath, VoicePackLoader.VoicesFolderName)
                ?? Path.Combine(modPath, VoicePackLoader.VoicesFolderName);
            BuiltInVoices = new FolderVoiceSource("Built-in voices", voicesFolder);

            // Each character gets exactly one source: a pack or the built-in voices.
            VoiceOptions = VoiceSourceSelector.GetOptions(BuiltInVoices, Packs);
            ApplyChoices(characterVoices);
        }

        /// <summary>
        /// Re-applies the player's choice of voice per character, e.g. after they change one in
        /// the config menu. Callers that act on the previous choice (AudioManager's resolver)
        /// need to re-apply it themselves afterward.
        /// </summary>
        public void ApplyChoices(IDictionary<string, string> characterVoices)
        {
            _chosen = VoiceSourceSelector.Choose(VoiceOptions, characterVoices);
            VoiceSourceSelector.LogChoices(VoiceOptions, _chosen, characterVoices);
        }

        /// <summary>The audio source active for a character, or null if it has none installed.</summary>
        public FolderVoiceSource GetActiveSource(string character)
        {
            return _chosen.TryGetValue(character, out VoiceSourceOption option) ? option.Source : null;
        }

        /// <summary>The pack active for a character, or null if it's using the built-in voices (or has no source at all).</summary>
        public VoicePack GetActivePack(string character)
        {
            return _chosen.TryGetValue(character, out VoiceSourceOption option) && _packsById.TryGetValue(option.Id, out VoicePack pack)
                ? pack
                : null;
        }
    }
}
