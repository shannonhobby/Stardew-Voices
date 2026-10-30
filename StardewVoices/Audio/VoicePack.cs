using StardewModdingAPI;

namespace StardewVoices.Audio
{
    /// <summary>An installed content pack that provides voices for one or more characters.</summary>
    public class VoicePack
    {
        public IManifest Manifest { get; }

        /// <summary>The pack's voice files. Its characters are the pack's character folders.</summary>
        public FolderVoiceSource Source { get; }

        /// <summary>
        /// The pack's "dialogue" folder path, if it has one, e.g. for voicing a modded NPC or
        /// adding extra lines for a vanilla one. Null if the pack only provides audio.
        /// </summary>
        public string DialogueFolderPath { get; }

        /// <summary>
        /// The pack's own "variable_lines.json", if it has one, for recording its own suffixed
        /// variants (e.g. a different dub's line count for a chance dialogue) instead of using
        /// the base mod's. Null if the pack doesn't define any.
        /// </summary>
        public string VariableLinesPath { get; }

        public VoicePack(IManifest manifest, FolderVoiceSource source, string dialogueFolderPath = null, string variableLinesPath = null)
        {
            Manifest = manifest;
            Source = source;
            DialogueFolderPath = dialogueFolderPath;
            VariableLinesPath = variableLinesPath;
        }
    }
}
