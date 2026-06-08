using StardewVoices.Dialogue;
using System.Text.RegularExpressions;

namespace StardewVoices.Utils.ParsingHelpers
{
    public static class DialogueParsingHelper
    {
        public static string[] ParseDialogue(string s)
        {
            string parsed = ParseQuestionDialogue(s);
            parsed = ParsePDialogue(parsed);
            parsed = StripEmotionCodes(parsed);
            parsed = DialogueHelpers.Normalize(parsed);

            if (HasGenderVariants(parsed))
                return GenerateGenderVariants(parsed);

            if (parsed.Contains("|"))
                return parsed.Split("|");

            if (parsed.Contains("^"))
                return parsed.Split("^");

            return new string[] { parsed };
        }

        private static string ParseQuestionDialogue(string s)
        {
            if (s.Contains("$q"))
            {
                Match match = Regex.Match(s, @"\$q(?:[^#]*)#(.*?)#");
                if (match.Success)
                    return match.Groups[1].Value;
            }
            return s;
        }

        // $p 38#First dialogue line.|Second dialogue line
        private static string ParsePDialogue(string s)
        {
            if (s.Contains("$p"))
                return s.Split("#")[1];
            return s;
        }

        private static string StripEmotionCodes(string text)
        {
        {
            return Regex.Replace(text, @"\$\w+", "").Trim();
        }

        private static string[] GenerateGenderVariants(string input)
        {
            var pattern = @"\$\{([^}^]+)\^([^}^]+)\}\$";
            string first = Regex.Replace(input, pattern, m => m.Groups[1].Value);
            string second = Regex.Replace(input, pattern, m => m.Groups[2].Value);
            return new string[] { first, second };
        }

        private static bool HasGenderVariants(string input)
        {
            return Regex.IsMatch(input, @"\$\{([^}^]+)\^([^}^]+)\}\$");
        }

        // ── Line ID generation ───────────────────────────────────────────────

        private static readonly Regex SvSuffixRegex = new Regex(@"_sv(\d+)$");

        /// <summary>
        /// Like <see cref="ParseDialogue"/> but returns (LineId, Text) pairs
        /// using the same naming rules as the Python pipeline:
        ///   _sv{n}            — | splits and || multiple-week segments
        ///   _mw               — || multiple-week prefix
        ///   _prereq_{n}       — $p prerequisite variants
        ///   _male_farmer      — ^ / ${x^y}$ male variant
        ///   _female_farmer    — ^ / ${x^y}$ female variant
        /// </summary>
        public static (string LineId, string Text)[] ParseDialogueWithIds(string s, string baseLineId)
        {
            string baseId = baseLineId.Replace(".", "_");

            // $q question — NPC's question text, single line, same ID
            // (farmer responses are not voiced; no ID suffix needed)
            if (s.Contains("$q"))
            {
                string question = ParseQuestionDialogue(s);
                string cleaned = Clean(question);
                return new[] { (baseId, cleaned) };
            }

            // || multiple-week dialogue — each segment gets _mw + _sv{n}
            if (s.Contains("||"))
            {
                string[] segments = s.Split(new[] { "||" }, StringSplitOptions.None);
                var results = new List<(string, string)>();
                string mwId = baseId + "_mw";

                foreach (string seg in segments.Where(x => x.Trim().Length > 0))
                {
                    results.Add((mwId, Clean(seg)));
                    mwId = IncrementLineId(mwId);
                }
                return results.ToArray();
            }

            // $p prerequisite — two (or more) mutually exclusive variants
            if (s.Contains("$p"))
                return ParsePDialogueWithIds(s, baseId);

            string parsed = Clean(s);

            // ${x^y}$ gender variants
            if (HasGenderVariants(parsed))
            {
                string[] variants = GenerateGenderVariants(parsed);
                return new[]
                {
                    (baseId + "_male_farmer",   variants[0]),
                    (baseId + "_female_farmer", variants[1]),
                };
            }

            // | multi-line split — each part gets _sv{n}
            if (parsed.Contains("|"))
            {
                string[] parts = parsed.Split('|');
                string currentId = baseId;
                var results = new List<(string, string)>();

                foreach (string part in parts.Where(x => x.Trim().Length > 0))
                {
                    results.Add((currentId, part.Trim()));
                    currentId = IncrementLineId(currentId);
                }
                return results.ToArray();
            }

            // ^ plain gender split (no ${...}$ wrapper)
            if (parsed.Contains("^"))
            {
                string[] parts = parsed.Split('^');
                return new[]
                {
                    (baseId + "_male_farmer",   parts[0].Trim()),
                    (baseId + "_female_farmer", parts[1].Trim()),
                };
            }

            // Simple single line
            return new[] { (baseId, parsed) };
        }

        /// <summary>
        /// $p prerequisite: strips the condition token and returns one
        /// (LineId, Text) pair per variant.
        /// Format: $p {condition}#{line_if_met}|{line_if_not_met}
        /// </summary>
        private static (string LineId, string Text)[] ParsePDialogueWithIds(string s, string baseId)
        {
            // Strip "$p {condition}#" to leave just the dialogue parts
            string stripped = Regex.Replace(s, @"\$p[^#]*#", "");
            string[] parts = stripped.Split('|');

            var results = new List<(string, string)>();
            for (int i = 0; i < parts.Length; i++)
            {
                string text = Clean(parts[i]);
                if (text.Length > 0)
                    results.Add(($"{baseId}_prereq_{i + 1}", text));
            }
            return results.ToArray();
        }

        /// <summary>
        /// Appends or increments the _sv{n} sequential suffix used when a single
        /// JSON entry expands into multiple audio lines.
        ///   "Mon"      → "Mon_sv1"
        ///   "Mon_sv1"  → "Mon_sv2"
        ///   "Mon_sv9"  → "Mon_sv10"
        /// </summary>
        public static string IncrementLineId(string lineId)
        {
            Match m = SvSuffixRegex.Match(lineId);
            if (!m.Success)
                return lineId + "_sv1";

            int n = int.Parse(m.Groups[1].Value);
            return lineId[..m.Index] + "_sv" + (n + 1);
        }

        // Shared clean-up used by both the existing and ID-aware paths
        private static string Clean(string s)
            => DialogueHelpers.Normalize(StripEmotionCodes(s));
    }
}
}
