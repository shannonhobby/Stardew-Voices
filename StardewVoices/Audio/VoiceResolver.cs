using System;
using System.Collections.Generic;

namespace StardewVoices.Audio
{
    /// <summary>
    /// Finds the audio for a (character, line id) pair. Each character is served by exactly
    /// one source, so a character's lines never mix voice actors.
    /// </summary>
    public class VoiceResolver
    {
        private readonly Dictionary<string, FolderVoiceSource> _sourceByCharacter = new(StringComparer.OrdinalIgnoreCase);

        public IReadOnlyCollection<string> Characters => _sourceByCharacter.Keys;

        /// <summary>Makes <paramref name="source"/> the only source for <paramref name="character"/>, replacing any earlier one.</summary>
        public void SetSource(string character, FolderVoiceSource source)
        {
            _sourceByCharacter[character] = source;
        }

        /// <summary>Null if no source has audio for <paramref name="character"/>.</summary>
        public FolderVoiceSource GetSource(string character)
        {
            return character != null && _sourceByCharacter.TryGetValue(character, out FolderVoiceSource source)
                ? source
                : null;
        }

        /// <summary>
        /// Whether the character's source has audio for exactly <paramref name="lineId"/>,
        /// without the fallback. Validation uses this to tell an exact hit apart from a
        /// fallback hit.
        /// </summary>
        public bool HasExactLine(string character, string lineId)
        {
            return lineId != null && (GetSource(character)?.HasLine(character, lineId) ?? false);
        }

        /// <summary>
        /// Returns the audio for <paramref name="lineId"/>. If there's none, retries once with
        /// the trailing "-{suffix}" stripped (e.g. "NPC_cs_3985-Abigail" -> "NPC_cs_3985") —
        /// these dash-suffixed ids are name/genre-substituted variants of one base line, so the
        /// un-suffixed recording (voiced with the generic placeholder, e.g. "Someone") is a
        /// reasonable stand-in when the specific variant wasn't recorded. The retry only looks
        /// in the same source. Null if neither id has audio.
        /// </summary>
        public FileVoiceLine Resolve(string character, string lineId)
        {
            FolderVoiceSource source = GetSource(character);
            if (source == null || lineId == null)
                return null;

            if (source.HasLine(character, lineId))
                return source.CreateLine(character, lineId);

            string fallbackLineId = GetFallbackLineId(lineId);
            if (fallbackLineId != null && source.HasLine(character, fallbackLineId))
                return source.CreateLine(character, fallbackLineId);

            return null;
        }

        // "NPC_cs_3985-Abigail" -> "NPC_cs_3985". Null if lineId has no "-{suffix}" to strip.
        public static string GetFallbackLineId(string lineId)
        {
            int dashIndex = lineId?.LastIndexOf('-') ?? -1;
            return dashIndex > 0 ? lineId.Substring(0, dashIndex) : null;
        }
    }
}
