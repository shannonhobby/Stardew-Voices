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
            => HasQuestionMarker(rawValue) || rawValue.Contains("$b");

        // True for the $q question command but NOT $query (which merely shares
        // the "$q" prefix and is a conditional gate, not a question).
        private static bool HasQuestionMarker(string s)
            => Regex.IsMatch(s, @"\$q(?!uery)");

        // Returns the id one past <lineId> to continue _sv numbering across pages.
        // Gender variants (..._sv1_male_farmer / _female_farmer) are one slot, not
        // two — only one ever plays. Strip that suffix so IncrementLineId sees the
        // trailing _sv<n> and advances the slot (..._sv2) instead of appending a
        // bogus counter (..._sv1_female_farmer_sv1).
        private static string AdvancePastLineId(string lineId)
        {
            string baseId = Regex.Replace(lineId, @"_(male|female)_farmer$", "");
            return IncrementLineId(baseId);
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
            string baseId = ParseLineId(baseLineId.Replace(".", "_"));

            // $query <game-state-query># — conditional gate (SDV 1.6). Must be
            // handled before the $q branch because "$query" also contains "$q".
            // The query only decides whether the line shows; the dialogue after
            // the first # is parsed normally (page breaks, | splits, gender, …).
            if (s.Contains("$query"))
            {
                Match queryMatch = Regex.Match(s, @"\$query[^#]*#");
                string remainder = queryMatch.Success
                    ? s.Substring(queryMatch.Index + queryMatch.Length)
                    : s;
                return ParseDialogueWithIds(remainder, baseId);
            }

            // $q question — NPC's question text, single line, same ID
            // (farmer responses are not voiced; no ID suffix needed)
            if (HasQuestionMarker(s))
            {
                string question = ParseQuestionDialogue(s);
                string cleaned = Clean(question);
                return new[] { (baseId, cleaned) };
            }

            // "/" separates whole dialogue variants in some StringsFromCsFiles
            // gift/relationship reactions (e.g. NPC.cs.3962, NPC.cs.4274-4281).
            // It is the OUTERMOST separator — each variant may itself contain
            // #/$e page breaks — so it must be split before the # branch below.
            // The variants are very often identical duplicates ("A/A") and
            // collapse to one; genuinely different halves become sequential lines.
            if (s.Contains("/"))
            {
                // Dedup on the cleaned text so emotion codes / page breaks don't
                // make otherwise-identical halves look different ("A$l/A$l").
                string[] variants = s.Split('/')
                    .Where(part => part.Trim().Length > 0)
                    .DistinctBy(Clean)
                    .ToArray();

                if (variants.Length > 0)
                {
                    // Recurse so each variant still gets full #/||/|/^ handling,
                    // with line IDs chaining across all of them.
                    var slashResults = new List<(string, string)>();
                    string currentId = baseId;
                    foreach (string variant in variants)
                        foreach (var pair in ParseDialogueWithIds(variant, currentId))
                        {
                            slashResults.Add(pair);
                            currentId = IncrementLineId(pair.LineId);
                        }
                    return slashResults.ToArray();
                }
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

            // $1 <id># only-once dialogue — like $query, a prefix command, not
            // an infix separator: "$1 <id>" tags the line so the game shows it
            // once, then the dialogue after the first # is parsed normally.
            // Strip "$1 <id>#" (incl. any leading page #) and recurse.
            if (s.Contains("#$1"))
            {
                string remainder = Regex.Replace(s, @"#?\$1[^#]*#", "");
                return ParseDialogueWithIds(remainder, baseId);
            }

            // # multi-page break — $b# and plain # both split into sequential lines
            if (s.Contains("#"))
            {
                string[] pages = s.Split('#');
                var results = new List<(string, string)>();
                string currentId = baseId;

                foreach (string page in pages)
                {
                    // Skip page-break markers / empty pages ($e, $b, …) before
                    // they consume a sequential id.
                    if (Clean(page).Length == 0)
                        continue;

                    // Recurse so each page still gets ^/${x^y}$ gender and |
                    // splitting (e.g. "scared at night.$s^lonely at night...$s").
                    var pageLines = ParseDialogueWithIds(page, currentId);
                    if (pageLines.Length == 0)
                        continue;

                    results.AddRange(pageLines);

                    // A page can itself expand into several lines (e.g. a |
                    // split), so continue numbering past the last id it emitted
                    // rather than blindly +1 — otherwise the next page collides.
                    currentId = AdvancePastLineId(pageLines[^1].LineId);
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

        private static string ParseLineId(string lineId)
        {
            return Regex.Replace(lineId, @"\(.*?\)", "");
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
                return ParseDialogueWithIds(rawValue, key)
                    .Select(pair => new LabelledDialogueLine(pair.LineId, pair.Text, key, type, isContinuation))
                    .ToList();
            }
    }
}
