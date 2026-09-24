using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StardewValley;
using StardewVoices.Pathing;
using StardewVoices.Utils.ParsingHelpers;
using StardewVoices.Utils;


namespace StardewVoices.FileGeneration.Models
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
    ///            prepared with ♡ by Shannon Hobby  ·   happy recording! (^‿^)
    ///                                 
    ///                                 ( 3 lines )
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
        protected PathNode ParentFolder { get; }   // Reserved for future file I/O

        // ── Constructor ─────────────────────────────────────────────────────────────
        public Script(string characterName, PathManager pathManager)
        {
            CharacterName = characterName;
            InitializeScript();

            PathNode root = pathManager.GeneratedFilesRoot;

            // Always (re)create the folder on disk in case a previous run in this
            // session deleted it — PathNode caches children in memory, so a cached
            // "Scripts" node doesn't mean the folder still exists on disk.
            root.CreateFolder("Scripts");

            root.GetChild("Scripts").CreateChild(CharacterName);
            ParentFolder = root.GetChild("Scripts");
        }

        // ── Public API ───────────────────────────────────────────────────────────────

        public int GetLineCount() => LineCount;

        /// <summary>Returns the complete, ready-to-save plain-text script.</summary>
        public string GetPlainText() => BuildPlainTextHeader() + PlainTextScript;

        /// <summary>Returns the complete, ready-to-save Markdown script.</summary>
        public string GetMarkdown() => BuildMarkdownHeader() + MarkDownScript;

        /// <summary>Adds formatted section header to plain-text and Markdown script.</summary>
        public void AppendSectionHeader(string title)
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

        /// <summary>
        /// Adds a line to plain-text and Markdown script.
        /// Set <paramref name="grouped"/> to <c>true</c> when this line immediately follows
        /// the previous in the same context (e.g. consecutive lines of one dialogue sequence).
        /// Grouped lines appear tight with no gap; ungrouped lines are separated by two blank lines.
        /// </summary>
        public void AddLine(LabelledDialogueLine dialogue, bool grouped = false)
            => AddLine(dialogue.LineId, dialogue.Text, grouped);

        /// <summary>
        /// Adds the lines of one dialogue sequence in order (the first ungrouped, the rest
        /// grouped), showing male/female farmer alternates side by side instead of one
        /// after the other.
        /// </summary>
        public void AddLines(IEnumerable<LabelledDialogueLine> lines)
        {
            bool firstInGroup = true;
            foreach (ScriptEntry entry in VariantGroupBuilder.Build(lines.Select(line => new ScriptLine(line.LineId, line.Text)).ToList()))
            {
                AddEntry(entry, grouped: !firstInGroup);
                firstInGroup = false;
            }
        }

        /// <summary>Adds a normal line or a group of alternates. See <see cref="AddLine(LabelledDialogueLine, bool)"/> for <paramref name="grouped"/>.</summary>
        public void AddEntry(ScriptEntry entry, bool grouped = false)
        {
            if (entry is VariantGroup group)
                AddVariantGroup(group, grouped);
            else if (entry is SingleLineEntry single)
                AddLine(single.Line.LineId, single.Line.Text, grouped);
        }

        private void AddLine(string lineId, string text, bool grouped)
        {
            LineCount++;
            if (LineCount > 1)
            {
                PlainTextScript += grouped ? "" : "\n\n";
                MarkDownScript  += grouped ? "" : "\n";
            }
            PlainTextScript += CenterLine($"[ {lineId} ]") + "\n";
            PlainTextScript += BuildQuotedBlock(text);
            MarkDownScript  += $"**`{lineId}`**\n\n> {text}\n\n";
        }

        /// <summary>
        /// Renders mutually exclusive alternates side by side. Every option's lines count
        /// toward <see cref="LineCount"/> since each one still has to be recorded.
        /// </summary>
        private void AddVariantGroup(VariantGroup group, bool grouped)
        {
            if (LineCount > 0)
            {
                PlainTextScript += grouped ? "" : "\n\n";
                MarkDownScript  += grouped ? "" : "\n";
            }
            LineCount += group.LineCount;

            PlainTextScript += CenterLine(VariantGroupRenderer.PlainTextMarker) + "\n";
            PlainTextScript += VariantGroupRenderer.RenderPlainText(group, PageWidth);

            MarkDownScript += VariantGroupRenderer.MarkdownMarker + "\n\n";
            MarkDownScript += VariantGroupRenderer.RenderMarkdownTable(group) + "\n";
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

        /// <summary>
        /// Writes out the file to appropriate GeneratedFiles folder
        /// fileType: "txt", "md", "both"
        /// </summary>
        public virtual void WriteOut(string fileType="both")
        {
            if (fileType == "txt" || fileType == "both")
            {
                ParentFolder.CreateFile(CharacterName + ".txt", GetPlainText());
            }

            if (fileType == "md" || fileType == "both")
            {
                ParentFolder.CreateFile(CharacterName + ".md", GetMarkdown());
            }
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
                $"*{LineCount} lines &nbsp;·&nbsp; prepared with ♡ by Shannon Hobby &nbsp;·&nbsp; happy recording! (^‿^)*",
                "",
                "</div>",
                "",
                "---",
                "", ""
            );
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
