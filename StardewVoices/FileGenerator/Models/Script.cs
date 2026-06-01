using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StardewVoices.FileGenerator.Models
{
    /// <summary>
    /// Builds formatted voiceover scripts in both plain text and Markdown
    /// for a single Stardew Valley character.
    ///
    /// Sample plain-text output:
    /// ┌──────────────────────────────────────────────────────────────────────────────┐
    ///
    ///               ˚✧₊⁺  STARDEW VALLEY VOICEOVER SCRIPT  ⁺₊✧˚
    ///
    ///                          CHARACTER:  ABIGAIL
    ///
    ///          ( 3 lines )   ·   prepared with ♡   ·   happy recording! (^‿^)
    ///
    ///                   ════════════════════════════════════════════════
    ///
    ///
    /// ✿── SPRING GREETINGS ───────────────────────────────────────────────────────✿
    ///
    ///                          [ introduction_greeting ]
    ///                     "Hello! You're new here, aren't you?"
    ///
    ///                               [ spring_rain ]
    ///                "Don't you love the sound of the rain? It makes
    ///                         everything feel so peaceful."
    ///
    ///                          ~ warm, a little dreamy ~
    ///
    /// └──────────────────────────────────────────────────────────────────────────────┘
    /// </summary>
    public class Script
    {
        // ── Layout constants ────────────────────────────────────────────────────────
        protected const int PageWidth = 80;   // Total plain-text column width
        protected const int DialogueWidth = 52;   // Max width of a single dialogue line
                                                  // (narrow enough to feel readable on stage)

        // ── Properties ──────────────────────────────────────────────────────────────
        public string CharacterName { get; }
        protected int LineCount = 0;
        protected string PlainTextScript { get; set; } = string.Empty;
        protected string MarkDownScript { get; set; } = string.Empty;
        private string PathString { get; }   // Reserved for future file I/O

        // ── Constructor ─────────────────────────────────────────────────────────────
        public Script(string characterName)
        {
            CharacterName = characterName;
            InitializeScript();
        }

        // ── Public API ───────────────────────────────────────────────────────────────

        public int GetLineCount() => LineCount;

        /// <summary>Returns the complete, ready-to-save plain-text script.</summary>
        public string GetPlainText() => BuildPlainTextHeader() + PlainTextScript;

        /// <summary>Returns the complete, ready-to-save Markdown script.</summary>
        public string GetMarkdown() => BuildMarkdownHeader() + MarkDownScript;

        /// <summary>
        /// Appends a labelled section with its dialogue lines.
        /// Lines are output in dictionary iteration order.
        /// </summary>
        public void AddScriptSection(string sectionTitle, Dictionary<string, string> lines)
        {
            AppendSectionHeader(sectionTitle);
            foreach (var kvp in lines)
                AddLine(kvp.Key, kvp.Value);
        }

        /// <summary>
        /// Appends a director/context note between or after lines.
        /// Rendered in a softer, indented style — clearly not dialogue.
        /// </summary>
        public void AddScriptNote(string text)
        {
            // Plain text: centered, quiet tilde decoration
            PlainTextScript += CenterLine($"~ {text} ~") + "\n\n";

            // Markdown: italic — subordinate to both ID and dialogue
            MarkDownScript += $"*~ {text} ~*\n\n";
        }

        // ── Private builders ─────────────────────────────────────────────────────────

        private void InitializeScript()
        {
            PlainTextScript = string.Empty;
            MarkDownScript = string.Empty;
        }

        /// <summary>
        /// Builds the plain-text header fresh each call so LineCount is always current.
        /// </summary>
        protected virtual string BuildPlainTextHeader()
        {
            string rule = new string('═', 60);

            return string.Join("\n",
                "",
                CenterLine("˚✧₊⁺  STARDEW VALLEY VOICEOVER SCRIPT  ⁺₊✧˚"),
                "",
                CenterLine($"CHARACTER:  {CharacterName.ToUpper()}"),
                "",
                CenterLine("prepared with ♡ by Shannon Hobby   ·   happy recording! (^‿^)"),
                CenterLine($"( {LineCount} lines )"),
                "",
                CenterLine(rule),
                "", ""
            );
        }

        /// <summary>
        /// Builds the Markdown header fresh each call so LineCount is always current.
        /// Uses an HTML center block — renders correctly on GitHub, Obsidian, VS Code preview, etc.
        /// </summary>
        protected virtual string BuildMarkdownHeader()
        {
            return string.Join("\n",
                "<div align=\"center\">",
                "",
                "# ✿ Stardew Valley Voiceover Script ✿",
                "",
                $"## {CharacterName}",
                "",
                $"*{LineCount} lines &nbsp;·&nbsp; prepared with ♡ &nbsp;·&nbsp; happy recording! (^‿^)*",
                "",
                "</div>",
                "",
                "---",
                "", ""
            );
        }

        protected void AppendSectionHeader(string title)
        {
            // Plain text full-width rule:
            // "✿── " (4) + TITLE + " " (1) + dashes + "✿" (1) = PageWidth
            // => dashCount = PageWidth - 6 - title.Length
            string upper = title.ToUpper();
            int dashCount = Math.Max(2, PageWidth - 6 - upper.Length);
            PlainTextScript += $"\n\n✿── {upper} {new string('─', dashCount)}✿\n\n";

            // Markdown: h3 keeps section below the character-name h2 in the hierarchy
            MarkDownScript += $"\n---\n\n### ✿ {title}\n\n";
        }

        private void AddLine(string lineId, string dialogue)
        {
            LineCount++;
            PlainTextScript += CenterLine($"[ {lineId} ]") + "\n";
            PlainTextScript += BuildQuotedBlock(dialogue) + "\n";
            MarkDownScript += $"**`{lineId}`**\n\n> {dialogue}\n\n";
        }

        // ── Helpers ──────────────────────────────────────────────────────────────────

        /// <summary>
        /// Word-wraps <paramref name="dialogue"/> and wraps it in opening/closing
        /// quotation marks per script convention: open quote on the first wrapped line,
        /// close quote on the last.
        /// </summary>
        protected string BuildQuotedBlock(string dialogue)
        {
            var wrapped = WordWrap(dialogue, DialogueWidth - 2);
            var result = string.Empty;

            if (wrapped.Count == 1)
            {
                result = CenterLine($"\"{wrapped[0]}\"") + "\n";
            }
            else
            {
                result = CenterLine($"\"{wrapped[0]}") + "\n";
                for (int i = 1; i < wrapped.Count - 1; i++)
                    result += CenterLine(wrapped[i]) + "\n";
                result += CenterLine($"{wrapped[^1]}\"") + "\n";
            }

            return result;
        }

        /// <summary>
        /// Returns <paramref name="text"/> left-padded so it appears visually centered
        /// within <see cref="PageWidth"/> columns.
        /// </summary>
        protected string CenterLine(string text)
        {
            if (text.Length >= PageWidth) return text;
            int leftPad = (PageWidth - text.Length) / 2;
            return new string(' ', leftPad) + text;
        }

        /// <summary>
        /// Greedy word-wrap: splits <paramref name="text"/> into lines of at most
        /// <paramref name="maxWidth"/> characters, breaking only on spaces.
        /// </summary>
        protected static List<string> WordWrap(string text, int maxWidth)
        {
            var lines = new List<string>();
            var current = new StringBuilder();

            foreach (var word in text.Split(' '))
            {
                if (current.Length == 0)
                {
                    current.Append(word);
                }
                else if (current.Length + 1 + word.Length <= maxWidth)
                {
                    current.Append(' ').Append(word);
                }
                else
                {
                    lines.Add(current.ToString());
                    current.Clear().Append(word);
                }
            }

            if (current.Length > 0)
                lines.Add(current.ToString());

            return lines.Count > 0 ? lines : new List<string> { string.Empty };
        }
    }
}
