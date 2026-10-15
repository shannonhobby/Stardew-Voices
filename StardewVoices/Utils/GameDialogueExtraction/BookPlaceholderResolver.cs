using StardewModdingAPI;
using System.Collections.Generic;

namespace StardewVoices.Utils.GameDialogueExtraction
{
    /// <summary>
    /// Resolves the literal "%book" placeholder that appears in Elliott's dialogue,
    /// which the game engine substitutes at runtime with the title of whichever genre
    /// book he's holding. Since our extracted text is static, we resolve it ourselves
    /// using the same three titles the game pulls from Strings/Events.
    ///
    /// These lines aren't confined to events — the same source strings (e.g. the
    /// Event.cs.1257-1260 range) are also pulled in as generic character dialogue by
    /// <see cref="CharacterDialogueExtractor"/>, so this resolution logic is shared
    /// rather than living only in <see cref="EventDialogueExtractor"/>.
    ///
    /// Lines that always reference the same genre (regardless of the player's book
    /// choice) are mapped by their generated line id below. Lines that speak about
    /// "your book" generically (i.e. work for any of the three genres) are expanded
    /// into three separate entries, one per genre, suffixed "-Romance"/"-Mystery"/"-SciFi".
    /// Any other %book occurrence is unmapped — rather than guess, it's logged and
    /// left with the literal placeholder intact.
    /// </summary>
    public class BookPlaceholderResolver
    {
        private static readonly Dictionary<string, string> FixedBookLineGenre = new()
        {
            { "Event_cs_1258_sv1", "romance" },
            { "Event_cs_1257_sv1", "mystery" },
            { "Event_cs_1260_sv1", "mystery" },
            { "Event_cs_1259_sv1", "default" },
        };

        private static readonly HashSet<string> GenericBookLineIds = new()
        {
            "Farm_3912125_8_Elliott_event2",
        };

        private static readonly (string Suffix, string Genre)[] GenericBookSuffixes =
        {
            ("romance", "romance"),
            ("mystery", "mystery"),
            ("scifi", "default"),
        };

        private readonly IModHelper helper;
        private Dictionary<string, string> _elliottBookTitles;

        public BookPlaceholderResolver(IModHelper helper)
        {
            this.helper = helper;
        }

        // Looks up the localized title for a book genre ("mystery"/"romance"/"default")
        // from Strings/Events (keys "ElliottBook_mystery" etc.), respecting whatever
        // locale the game is currently loaded with.
        private string GetElliottBookTitle(string genre)
        {
            _elliottBookTitles ??= helper.GameContent.Load<Dictionary<string, string>>("Strings/Events");

            string key = $"ElliottBook_{genre}";
            if (_elliottBookTitles.TryGetValue(key, out string title))
                return title;

            ErrorLogger.Warn($"Could not find '{key}' in Strings/Events while resolving %book placeholder.");
            return "%book";
        }

        /// <summary>
        /// Resolves any "%book" placeholder in a single dialogue line, returning one
        /// entry for a fixed-genre or unresolved line, or three (one per genre) for a
        /// generic line. Line ids for generic expansions are suffixed
        /// "-Romance"/"-Mystery"/"-SciFi".
        /// </summary>
        public List<(string LineId, string Dialogue)> Resolve(string lineId, string dialogue)
        {
            if (!dialogue.Contains("%book"))
                return new List<(string, string)> { (lineId, dialogue) };

            if (FixedBookLineGenre.TryGetValue(lineId, out string fixedGenre))
            {
                string title = GetElliottBookTitle(fixedGenre);
                return new List<(string, string)> { (lineId, dialogue.Replace("%book", title)) };
            }

            if (GenericBookLineIds.Contains(lineId))
            {
                var expanded = new List<(string, string)>();
                foreach (var (suffix, genre) in GenericBookSuffixes)
                {
                    string title = GetElliottBookTitle(genre);
                    expanded.Add(($"{lineId}-{suffix}", dialogue.Replace("%book", title)));
                }
                return expanded;
            }

            ErrorLogger.Warn(
                $"Unresolved %book placeholder in line '{lineId}' (\"{dialogue}\") — no known genre mapping for " +
                "this line id. Leaving the literal %book placeholder in place; add it to FixedBookLineGenre or " +
                "GenericBookLineIds in BookPlaceholderResolver if this is expected to resolve.");
            return new List<(string, string)> { (lineId, dialogue) };
        }
    }
}
