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

        public VoicePack(IManifest manifest, FolderVoiceSource source, string dialogueFolderPath = null)
        {
            Manifest = manifest;
            Source = source;
            DialogueFolderPath = dialogueFolderPath;
        }
    }
}
