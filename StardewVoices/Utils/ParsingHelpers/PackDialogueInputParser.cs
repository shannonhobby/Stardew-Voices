using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace StardewVoices.Utils.ParsingHelpers
{
    /// <summary>One raw entry pasted by a pack author: a line id and its unparsed dialogue text.</summary>
    public record PackDialogueEntry(string LineId, string RawText, int LineNumber);

    /// <summary>
    /// Reads a pack author's input file — one "line id": "raw dialogue text" pair per line,
    /// usually pasted straight from a mod's i18n/default.json — into an ordered list of entries.
    /// Tolerant of things a strict JSON parser would reject: a "//" comment line, a trailing
    /// comma, curly "smart" quotes, blank lines, and the file with or without a wrapping "{ }".
    /// Anything else that doesn't parse is reported by line number rather than failing the whole
    /// file, so one bad line doesn't block every other line in it.
    /// </summary>
    public static class PackDialogueInputParser
    {
        // Key/value quotes may be ' or " or their smart-quote forms (not required to match each
        // other, since smart-quote autocorrect can turn one side of a pasted pair but not the
        // other). The value is greedy up to the last quote on the line, so apostrophes ("Isn't")
        // and escaped quotes (\") inside the text don't end the match early.
        private static readonly Regex EntryPattern = new Regex(
            @"^\{?\s*[""'“”‘’]([^""'“”‘’]+)[""'“”‘’]\s*:\s*[""'“”‘’](.*)[""'“”‘’]\s*,?\s*\}?\s*$",
            RegexOptions.Compiled);

        // Content Patcher tokens (e.g. "{{randomize: ...}}") resolve differently every time the
        // pack loads, so the plain text we'd match against at runtime never stays stable.
        private static readonly Regex UnresolvedTokenPattern = new Regex(@"\{\{.*?\}\}", RegexOptions.Compiled);

        public static List<PackDialogueEntry> Parse(string filePath, out List<string> errors)
        {
            return ParseLines(File.ReadAllLines(filePath), out errors);
        }

        public static List<PackDialogueEntry> ParseLines(string[] lines, out List<string> errors)
        {
            errors = new List<string>();
            List<PackDialogueEntry> entries = new();

            for (int i = 0; i < lines.Length; i++)
            {
                int lineNumber = i + 1;
                // Not smart-quote normalized: curly apostrophes inside the text ("It’s") must stay
                // as-is to match what the game displays at runtime.
                string line = lines[i].Trim();

                if (line.Length == 0 || line.StartsWith("//") || line == "{" || line == "}")
                    continue;

                Match match = EntryPattern.Match(line);
                if (!match.Success)
                {
                    errors.Add($"Line {lineNumber}: couldn't parse \"{line}\".");
                    continue;
                }

                string lineId = match.Groups[1].Value.Trim();
                string rawText = DecodeJsonStringContent(match.Groups[2].Value);

                if (UnresolvedTokenPattern.IsMatch(rawText))
                    errors.Add($"Line {lineNumber}: \"{lineId}\" contains a Content Patcher token (\"{{{{...}}}}\") " +
                        "that can't be reliably text-matched at runtime.");

                entries.Add(new PackDialogueEntry(lineId, rawText, lineNumber));
            }

            return entries;
        }

        // The captured value is already de-escaped except for standard JSON string escapes
        // (\", \\, \n, \t, \uXXXX, ...) — exactly what a real JSON parser would do with the same
        // text — so reuse Newtonsoft's string decoder instead of reimplementing it by hand.
        private static string DecodeJsonStringContent(string capturedValue)
        {
            try
            {
                return JsonConvert.DeserializeObject<string>("\"" + capturedValue + "\"") ?? capturedValue;
            }
            catch
            {
                // Not valid JSON escaping (e.g. a stray backslash from pasted prose) — use it
                // as-is rather than failing the whole line over it.
                return capturedValue;
            }
        }
    }
}
