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
            return ParseDialogueWithIds(s, "test")
                .Select(pair => pair.Text)
                .ToArray();
        }

        private static string StripEmotionCodes(string text)
        {
            text = Regex.Replace(text, @"\$\w+", "");
            text = Regex.Replace(text, @"\[.*?\]", "");
            text = Regex.Replace(text, @"%noturn", "");
            text = Regex.Replace(text, @"%fork", "");
            return text.Trim();
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
        public static string AdvancePastLineId(string lineId)
        {
            string baseId = Regex.Replace(lineId, @"_(male|female)_farmer$", "");
            return IncrementLineId(baseId);
        }

        // ── Line ID generation ───────────────────────────────────────────────

        private static readonly Regex SvSuffixRegex = new Regex(@"_sv(\d+)$");

        /// <summary>
        /// Like <see cref="ParseDialogue"/> but returns (LineId, Text) pairs
        /// using the same naming rules as the Python pipeline:
        ///   _sv{n}            — | splits and #$b#/#$e# page breaks within one segment
        ///   _mw{n}            — || multiple-week segments (week 1 = no suffix, week 2 = _mw1, ...)
        ///   _prereq_{n}       — $p prerequisite variants
        ///   _male_farmer      — ^ / ${x^y}$ male variant
        ///   _female_farmer    — ^ / ${x^y}$ female variant
        /// </summary>
        public static (string LineId, string Text)[] ParseDialogueWithIds(string s, string baseLineId)
        {
            string baseId = ParseLineId(baseLineId.Replace(".", "_"));

            // $query — before $q since "$query" contains "$q". True/false → _query_1/_query_2.
            // Text before the $query marker (if any) is parsed normally first.
            if (s.Contains("$query"))
                return ParseQueryPrefixedWithIds(s, baseId);

            // $q question — text before $q (if any) is parsed normally, then the
            // question continues the _sv chain. $r response choices aren't voiced.
            if (HasQuestionMarker(s))
                return ParseQDialogueWithIds(s, baseId);

            // $y festival question — NPC's question with voiced farmer response choices.
            // Format: $y 'Question_OptionLabel1_ResponseText1_OptionLabel2_ResponseText2...'
            // Text before/after the $y block (if any) is parsed normally too.
            if (s.Contains("$y"))
                return ParseYDialogueWithIds(s, baseId);

            // Event script format — some regular dialogue entries embed event commands
            // (speak, emote, pause, …) separated by "/". Must be checked before the
            // generic "/" branch so those commands aren't treated as dialogue variants.
            if (s.Contains("/speak ") || s.Contains("/emote "))
                return ParseEventScriptDialogueWithIds(s, baseId);

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

            // || multiple-week dialogue — each segment is an independent line shown
            // in a different week, not a continuation of the previous segment. Week 1
            // keeps the original id; week 2+ get their own _mw{n} slot off the
            // ORIGINAL id (never chained from the previous segment's last _sv id), so
            // the _mw counter can't merge with the _sv continuation counter.
            if (s.Contains("||"))
            {
                string[] segments = s.Split(new[] { "||" }, StringSplitOptions.None)
                    .Where(x => x.Trim().Length > 0)
                    .ToArray();
                var results = new List<(string, string)>();

                for (int i = 0; i < segments.Length; i++)
                {
                    // Recurse so each segment's own #$b#/#$e# page breaks still split
                    // into sequential _sv{n} lines, rooted fresh at this segment's id
                    // (the sv counter resets per segment — it never carries over).
                    string segBaseId = i == 0 ? baseId : $"{baseId}_mw{i}";
                    results.AddRange(ParseDialogueWithIds(segments[i], segBaseId));
                }
                return results.ToArray();
            }

            // $p/$d prerequisite — two (or more) mutually exclusive variants
            if (s.Contains("$p") || s.Contains("$d"))
                return ParsePDialogueWithIds(s, baseId);

            // $c chance dialogue — _chance_1 / _chance_2
            if (s.Contains("$c"))
                return ParseCDialogueWithIds(s, baseId);

            // $1 <id># one-time-only dialogue — _{id}_1 / _{id}_2. Regex (not Contains)
            // so stray "$1" text with no "#" after it doesn't match and recurse forever.
            if (Regex.IsMatch(s, @"\$1\s+[^#]*#"))
                return ParseOneTimeDialogueWithIds(s, baseId);

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

                // # is always a structural separator, never literal text — if every
                // page cleaned to empty, there's genuinely nothing here. Return (even
                // empty) rather than falling through to the plain-text fallback below,
                // which would otherwise treat a bare "#" as real dialogue content.
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
            lineId = Regex.Replace(lineId, @"\(.*?\)", "");

            // Some raw keys use '*' as a wildcard (e.g. Strings/MovieReactions'
            // "Penny_*_BeforeMovie" for "any movie"). '*' isn't safe in a line id
            // / file name, so normalize it to "1".
            return lineId.Replace("*", "1");
        }

        /// <summary>
        /// $p/$d prerequisite: strips the condition token and returns one
        /// (LineId, Text) pair per variant.
        /// Format: $p {condition}#{line_if_met}|{line_if_not_met}
        ///         $d {condition}#{line_if_met}|{line_if_not_met}
        /// </summary>
        private static (string LineId, string Text)[] ParsePDialogueWithIds(string s, string baseId)
        {
            Match pMatch = Regex.Match(s, @"\$[pd][^#]*#");
            string before = pMatch.Success ? s.Substring(0, pMatch.Index) : "";
            string remainder = pMatch.Success ? s.Substring(pMatch.Index + pMatch.Length) : s;

            var results = new List<(string, string)>();
            string currentId = baseId;

            if (Clean(before).Length > 0)
            {
                var beforeLines = ParseDialogueWithIds(before, baseId);
                results.AddRange(beforeLines);
                if (beforeLines.Length > 0)
                    currentId = AdvancePastLineId(beforeLines[^1].LineId);
            }

            string[] parts = remainder.Split('|');
            for (int i = 0; i < parts.Length; i++)
            {
                string text = Clean(parts[i].TrimStart('#'));
                if (text.Length > 0)
                    results.Add(($"{currentId}_prereq_{i + 1}", text));
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

        // $1 {id}#{text_first_time} #$e# {text_every_time} → _{id}_1 / _{id}_2
        private static (string LineId, string Text)[] ParseOneTimeDialogueWithIds(string s, string baseId)
        {
            Match idMatch = Regex.Match(s, @"\$1\s+([^#]*)#");
            string idToken = idMatch.Success
                ? Regex.Replace(idMatch.Groups[1].Value.Trim(), @"[^A-Za-z0-9_]", "_")
                : "once";

            // Split on the $1 marker to separate any text before it from the two branches
            string[] outerSplit = Regex.Split(s, @"#?\$1[^#]*#");

            var results = new List<(string, string)>();
            string currentId = baseId;

            if (outerSplit[0].Trim().Length > 0)
            {
                var beforeLines = ParseDialogueWithIds(outerSplit[0], baseId);
                results.AddRange(beforeLines);
                if (beforeLines.Length > 0)
                    currentId = AdvancePastLineId(beforeLines[^1].LineId);
            }

            string remainder = string.Join("", outerSplit.Skip(1));
            string[] branches = remainder.Split(new[] { "#$e#" }, StringSplitOptions.None);

            for (int i = 0; i < branches.Length; i++)
            {
                string branch = branches[i].Trim();
                if (branch.Length == 0) continue;

                string branchId = i < 2 ? $"{currentId}_{idToken}_{i + 1}" : IncrementLineId(currentId);
                results.AddRange(ParseDialogueWithIds(branch, branchId));
            }
            return results.ToArray();
        }

        // $query — text before the marker (if any) is parsed normally, then the
        // true/false branches continue the chain via ParseQueryDialogueWithIds.
        private static (string LineId, string Text)[] ParseQueryPrefixedWithIds(string s, string baseId)
        {
            Match queryMatch = Regex.Match(s, @"\$query[^#]*#");
            string before = queryMatch.Success ? s.Substring(0, queryMatch.Index) : "";
            string remainder = queryMatch.Success ? s.Substring(queryMatch.Index + queryMatch.Length) : s;

            var results = new List<(string, string)>();
            string currentId = baseId;

            if (Clean(before).Length > 0)
            {
                var beforeLines = ParseDialogueWithIds(before, baseId);
                results.AddRange(beforeLines);
                if (beforeLines.Length > 0)
                    currentId = AdvancePastLineId(beforeLines[^1].LineId);
            }

            results.AddRange(ParseQueryDialogueWithIds(remainder, currentId));
            return results.ToArray();
        }

        // $query {condition}#{if_true}|{if_false} → _query_1 / _query_2, each with its own _sv chain
        private static (string LineId, string Text)[] ParseQueryDialogueWithIds(string remainder, string baseId)
        {
            string[] branches = remainder.Split('|');
            var results = new List<(string, string)>();

            for (int i = 0; i < branches.Length; i++)
            {
                string branch = branches[i].Trim();
                if (branch.Length == 0) continue;

                string branchId = i < 2 ? $"{baseId}_query_{i + 1}" : IncrementLineId(baseId);
                results.AddRange(ParseDialogueWithIds(branch, branchId));
            }
            return results.ToArray();
        }

        // $q — text before the marker (if any) is parsed normally, then the question
        // continues the _sv chain. $r response choices aren't voiced, so they're dropped.
        private static (string LineId, string Text)[] ParseQDialogueWithIds(string s, string baseId)
        {
            Match qMatch = Regex.Match(s, @"\$q(?:[^#]*)#(.*?)#");
            if (!qMatch.Success)
                return new[] { (baseId, Clean(s)) };

            string before = s.Substring(0, qMatch.Index);
            string question = qMatch.Groups[1].Value;

            var results = new List<(string, string)>();
            string currentId = baseId;

            if (Clean(before).Length > 0)
            {
                var beforeLines = ParseDialogueWithIds(before, baseId);
                results.AddRange(beforeLines);
                if (beforeLines.Length > 0)
                    currentId = AdvancePastLineId(beforeLines[^1].LineId);
            }

            results.AddRange(ParseDialogueWithIds(question, currentId));
            return results.ToArray();
        }

        /// <summary>
        /// $y festival question: NPC asks a question with voiced farmer response choices.
        /// Format: $y 'Question_Option1_Response1_Option2_Response2...'
        /// Produces the question and each response as {id}__{sanitized_option}__response.
        /// Text before/after the $y block (if any) is parsed normally and chained in.
        /// </summary>
        private static (string LineId, string Text)[] ParseYDialogueWithIds(string s, string baseId)
        {
            // Greedy .* ensures internal apostrophes (e.g. "I'm") don't close the match early.
            Match outerMatch = Regex.Match(s, @"\$y\s+'(.*)'");
            string before, inner, trailing;

            if (outerMatch.Success)
            {
                before = s.Substring(0, outerMatch.Index);
                inner = outerMatch.Groups[1].Value;
                trailing = s.Substring(outerMatch.Index + outerMatch.Length);
            }
            else
            {
                int yIndex = s.IndexOf("$y");
                before = s.Substring(0, yIndex);
                inner = s.Substring(yIndex + 2).Trim();
                trailing = "";
            }

            var results = new List<(string, string)>();
            string currentId = baseId;

            if (Clean(before).Length > 0)
            {
                var beforeLines = ParseDialogueWithIds(before, baseId);
                results.AddRange(beforeLines);
                if (beforeLines.Length > 0)
                    currentId = AdvancePastLineId(beforeLines[^1].LineId);
            }

            string[] parts = inner.Split('_');

            // Index 0 is the NPC's question; indices 1,3,5,... are option labels; 2,4,6,... are responses.
            results.Add((currentId, Clean(parts[0])));

            for (int i = 1; i + 1 < parts.Length; i += 2)
            {
                string sanitizedOption = SanitizeForLineId(parts[i]);
                string responseText = Clean(parts[i + 1]);
                if (responseText.Length > 0)
                    results.Add(($"{currentId}__{sanitizedOption}__response", responseText));
            }

            if (Clean(trailing).Length > 0)
                results.AddRange(ParseDialogueWithIds(trailing, AdvancePastLineId(currentId)));

            return results.ToArray();
        }

        // Lowercases and replaces every character that is not a-z, 0-9, or _ with an underscore,
        // producing a string safe to use as a file name component in a line ID.
        private static string SanitizeForLineId(string s)
            => Regex.Replace(s.ToLowerInvariant(), @"[^a-z0-9_]", "_");

        /// <summary>
        /// Handles regular dialogue entries that embed an event script:
        /// an initial text block followed by slash-separated event commands.
        /// Extracts spoken text (initial block + speak commands), discards
        /// action commands (emote, pause, …), and chains line IDs across blocks.
        /// </summary>
        private static (string LineId, string Text)[] ParseEventScriptDialogueWithIds(string s, string baseId)
        {
            var textBlocks = new List<string>();
            string[] commands = s.Split('/');

            // First token: initial dialogue text not wrapped in a speak command.
            // Strip any trailing " that closes an implicit outer quote in the game data.
            string firstToken = commands[0].TrimEnd('"').Trim();
            if (Clean(firstToken).Length > 0)
                textBlocks.Add(firstToken);

            // Extract spoken text from speak commands; skip emote/pause/etc.
            foreach (string command in commands.Skip(1))
            {
                Match m = Regex.Match(command.Trim(), @"^speak \S+ ""(.+)""$");
                if (m.Success)
                    textBlocks.Add(m.Groups[1].Value);
            }

            var results = new List<(string, string)>();
            string currentId = baseId;
            foreach (string block in textBlocks)
            {
                var blockLines = ParseDialogueWithIds(block, currentId);
                if (blockLines.Length == 0) continue;
                results.AddRange(blockLines);
                currentId = AdvancePastLineId(blockLines[^1].LineId);
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
        internal static string Clean(string s)
            => DialogueHelpers.Normalize(StripEmotionCodes(s));

        /// <summary>
        /// Runs the parser on a raw value and wraps each result as a
        /// LabelledDialogueLine. Used by BuildLabelledDialogue and by any
        /// caller that already has the StardewValley.Dialogue separately
        /// (e.g. rainy dialogue).
        /// </summary>
        /// <param name="englishRawValue">
        /// When generating non-English files, pass the English raw value here so that
        /// IDs derived from option label text (e.g. $y responses) stay identical across
        /// languages. Text is always taken from <paramref name="rawValue"/>.
        /// </param>
        public static List<LabelledDialogueLine> ParseRawValueToLines(
            string rawValue, string key, DialogueType type, string englishRawValue = null)
        {
            bool isContinuation = IsContinuationDialogue(rawValue);

            if (englishRawValue == null)
            {
                return ParseDialogueWithIds(rawValue, key)
                    .Select(pair => new LabelledDialogueLine(pair.LineId, pair.Text, key, type, isContinuation))
                    .ToList();
            }

            // IDs from the English parse, text from the localized parse.
            // If the counts differ (malformed data), fall back to the localized ID for the extra lines.
            var idPairs   = ParseDialogueWithIds(englishRawValue, key);
            var textPairs = ParseDialogueWithIds(rawValue, key);

            var results = new List<LabelledDialogueLine>();
            for (int i = 0; i < textPairs.Length; i++)
            {
                string lineId = i < idPairs.Length ? idPairs[i].LineId : textPairs[i].LineId;
                results.Add(new LabelledDialogueLine(lineId, textPairs[i].Text, key, type, isContinuation));
            }
            return results;
        }
    }
}
