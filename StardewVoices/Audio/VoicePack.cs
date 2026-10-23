using StardewModdingAPI;

namespace StardewVoices.Audio
{
    /// <summary>An installed content pack that provides voices for one or more characters.</summary>
    public class VoicePack
    {
        public IManifest Manifest { get; }

        /// <summary>The pack's voice files. Its characters are the pack's character folders.</summary>
        public FolderVoiceSource Source { get; }

        public VoicePack(IManifest manifest, FolderVoiceSource source)
        {
            Manifest = manifest;
            Source = source;
        }
    }
}
