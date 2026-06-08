using StardewVoices.Dialogue;
using System.Text.RegularExpressions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace StardewVoices.Utils.ParsingHelpers
{
    public static class CharacterDialogueParser
    {
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
        // $d 38#First dialogue line.|Second dialogue line
        private static string ParsePDialogue(string s)
        {
            if (s.Contains("$p") || s.Contains("$d"))
                return s.Split("#")[1];
            return s;
        }

        private static string StripEmotionCodes(string text)
        {
            return Regex.Replace(text, @"\$\w+", "").Trim();
        }

        public static bool IsContinuationDialogue(string rawValue)
            => rawValue.Contains("$q") || rawValue.Contains("$b");

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

            // $p/$d prerequisite — two (or more) mutually exclusive variants
            if (s.Contains("$p") || s.Contains("$d"))
                return ParsePDialogueWithIds(s, baseId);

            // $c chance dialogue — _chance_1 / _chance_2
            if (s.Contains("$c"))
                return ParseCDialogueWithIds(s, baseId);

            // $1 only-once dialogue
            if (s.Contains("$1") && s.Contains("#$1"))
                return ParseOnlyOnceDialogueWithIds(s, baseId);

            // # multi-page break — $b# and plain # both split into sequential lines
            if (s.Contains("#"))
            {
                string[] pages = s.Split('#');
                var results = new List<(string, string)>();
                string currentId = baseId;

                foreach (string page in pages)
                {
                    string cleaned = Clean(page);
                    if (cleaned.Length > 0)
                    {
                        results.Add((currentId, cleaned));
                        currentId = IncrementLineId(currentId);
                    }
                }

                if (results.Count > 0)
                    return results.ToArray();
            }

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
        /// $p/$d prerequisite: strips the condition token and returns one
        /// (LineId, Text) pair per variant.
        /// Format: $p {condition}#{line_if_met}|{line_if_not_met}
        ///         $d {condition}#{line_if_met}|{line_if_not_met}
        /// </summary>
        private static (string LineId, string Text)[] ParsePDialogueWithIds(string s, string baseId)
        {
            // Strip "$p {condition}#" or "$d {condition}#" to leave just the dialogue parts
            string stripped = Regex.Replace(s, @"\$[pd][^#]*#", "");
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
        /// $c chance dialogue: may or may not play → _chance_1 / _chance_2.
        /// Format: $c {probability}#{text_if_yes}#{text_if_no}
        ///      or {text_before}#$c {probability}#{text_if_yes}#{text_if_no}
        /// </summary>
        private static (string LineId, string Text)[] ParseCDialogueWithIds(string s, string baseId)
        {
            var results = new List<(string, string)>();
            string currentId = baseId;

            // Split on the $c marker to separate any text before it from the chance options
            string[] outerSplit = Regex.Split(s, @"#?\$c[^#]*#");

            if (outerSplit[0].Trim().Length > 0)
            {
                results.Add((currentId, Clean(outerSplit[0])));
                currentId = IncrementLineId(currentId);
            }

            for (int i = 1; i < outerSplit.Length; i++)
            {
                string chunk = outerSplit[i];
                string[] parts = chunk.Contains("#$e#")
                    ? chunk.Split(new[] { "#$e#" }, StringSplitOptions.None)
                    : chunk.Split('#');

                for (int j = 0; j < parts.Length; j++)
                {
                    string cleaned = Clean(parts[j]);
                    if (cleaned.Length == 0) continue;
                    string id = j < 2 ? $"{currentId}_chance_{j + 1}" : IncrementLineId(currentId);
                    results.Add((id, cleaned));
                }
            }
            return results.ToArray();
        }


        /// <summary>
        /// $1 only-once dialogue: shown once then replaced → _chance_1 / _chance_2.
        /// Format: {shown_once_text}#$1#{text_after}
        /// </summary>
        private static (string LineId, string Text)[] ParseOnlyOnceDialogueWithIds(string s, string baseId)
        {
            string stripped = Regex.Replace(s, @"#?\$1#?", "");
            string[] parts = stripped.Contains("#$e#")
                ? stripped.Split(new[] { "#$e#" }, StringSplitOptions.None)
                : stripped.Split('#');

            var results = new List<(string, string)>();
            for (int i = 0; i < parts.Length; i++)
            {
                string cleaned = Clean(parts[i]);
                if (cleaned.Length == 0) continue;
                string id = i >= 2 ? IncrementLineId(baseId) : $"{baseId}_chance_{i + 1}";
                results.Add((id, cleaned));
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

        /// <summary>
        /// Runs the parser on a raw value and wraps each result as a
        /// LabelledDialogueLine. Used by BuildLabelledDialogue and by any
        /// caller that already has the StardewValley.Dialogue separately
        /// (e.g. rainy dialogue).
        /// </summary>
        public static List<LabelledDialogueLine> ParseRawValueToLines(
          string rawValue, string key, DialogueType type)
            {
                bool isContinuation = IsContinuationDialogue(rawValue);
                return ParseDialogueWithIds(rawValue, key)
                    .Select(pair => new LabelledDialogueLine(pair.LineId, pair.Text, key, type, isContinuation))
                    .ToList();
            }
    }
}
