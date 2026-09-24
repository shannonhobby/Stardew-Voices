using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace StardewVoices.FileGeneration.Models
{
    /// <summary>
    /// Renders a <see cref="VariantGroup"/> as side-by-side columns — one per option —
    /// so an actor records every version but can see only one plays per scene.
    /// Never assumes a fixed number of options.
    ///
    /// Sample plain-text output (2 options, 80 wide):
    /// +--------------------------------------+--------------------------------------+
    /// | Male Farmer                          | Female Farmer                        |
    /// +--------------------------------------+--------------------------------------+
    /// | [Maru_event2_male_farmer]            | [Maru_event2_female_farmer]          |
    /// | "Huh? What did you say to him, Dad?" | "Huh? What did you say to her, Dad?" |
    /// +--------------------------------------+--------------------------------------+
    ///
    /// When there are too many options for columns to stay readable, falls back to a
    /// stacked list instead.
    /// </summary>
    public static class VariantGroupRenderer
    {
        public const string PlainTextMarker = "[ ALTERNATES — record all, only one plays per scene ]";
        public const string MarkdownMarker = "*⇄ Alternate takes — record every column, only one plays per scene*";

        // Below this, columns wrap too aggressively to read — use the stacked layout.
        public const int MinColumnWidth = 20;

        // ── Markdown ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// One table column per option. Each cell holds the line id and its text;
        /// multi-line options are separated with &lt;br&gt;.
        /// </summary>
        public static string RenderMarkdownTable(VariantGroup group, bool emphasizeText = false)
        {
            var sb = new StringBuilder();

            sb.Append("| ").Append(string.Join(" | ", group.Options.Select(option => EscapeCell(option.Label)))).Append(" |\n");
            sb.Append('|').Append(string.Concat(group.Options.Select(_ => "---|"))).Append('\n');
            sb.Append("| ").Append(string.Join(" | ", group.Options.Select(option => BuildMarkdownCell(option, emphasizeText)))).Append(" |\n");

            return sb.ToString();
        }

        private static string BuildMarkdownCell(VariantOption option, bool emphasizeText)
        {
            return string.Join("<br><br>", option.Lines.Select(line =>
            {
                string text = EscapeCell(line.Text);
                if (emphasizeText && text.Length > 0) text = $"**{text}**";
                return $"`{line.LineId}`<br>{text}";
            }));
        }

        private static string EscapeCell(string text) => text.Replace("|", "\\|");

        // ── Plain text ───────────────────────────────────────────────────────────────

        /// <summary>
        /// Boxed side-by-side columns dividing <paramref name="totalWidth"/> evenly across
        /// the options, or a stacked list if that would make columns narrower than
        /// <see cref="MinColumnWidth"/>.
        /// </summary>
        public static string RenderPlainText(VariantGroup group, int totalWidth)
        {
            int optionCount = group.Options.Count;

            // Each column is "| " + content + " ", plus one closing "|" for the row.
            int columnWidth = (totalWidth - 1) / optionCount - 3;

            return columnWidth >= MinColumnWidth
                ? RenderColumns(group, columnWidth)
                : RenderStacked(group, totalWidth);
        }

        private static string RenderColumns(VariantGroup group, int columnWidth)
        {
            string rule = "+" + string.Concat(Enumerable.Repeat(new string('-', columnWidth + 2) + "+", group.Options.Count));
            var sb = new StringBuilder();

            sb.Append(rule).Append('\n');
            AppendRow(sb, group.Options.Select(option => Wrap(option.Label, columnWidth)).ToList(), columnWidth);
            sb.Append(rule).Append('\n');
            AppendRow(sb, group.Options.Select(option => BuildPlainTextCell(option, columnWidth)).ToList(), columnWidth);
            sb.Append(rule).Append('\n');

            return sb.ToString();
        }

        private static List<string> BuildPlainTextCell(VariantOption option, int columnWidth)
        {
            var cell = new List<string>();

            foreach (ScriptLine line in option.Lines)
            {
                if (cell.Count > 0) cell.Add(string.Empty);
                cell.AddRange(Wrap($"[{line.LineId}]", columnWidth));
                cell.AddRange(Wrap($"\"{line.Text}\"", columnWidth));
            }

            return cell;
        }

        // Writes the cells side by side, padding shorter cells with blank rows.
        private static void AppendRow(StringBuilder sb, List<List<string>> cells, int columnWidth)
        {
            int height = cells.Max(cell => cell.Count);

            for (int row = 0; row < height; row++)
            {
                sb.Append('|');
                foreach (List<string> cell in cells)
                {
                    string text = row < cell.Count ? cell[row] : string.Empty;
                    sb.Append(' ').Append(text.PadRight(columnWidth)).Append(" |");
                }
                sb.Append('\n');
            }
        }

        private static string RenderStacked(VariantGroup group, int totalWidth)
        {
            const string bulletIndent = "    ";
            const string textIndent = "      ";
            int textWidth = totalWidth - textIndent.Length;
            var sb = new StringBuilder();

            foreach (VariantOption option in group.Options)
            {
                List<string> label = Wrap(option.Label, textWidth);
                sb.Append(bulletIndent).Append("▸ ").Append(label[0]).Append('\n');
                foreach (string labelLine in label.Skip(1))
                    sb.Append(textIndent).Append(labelLine).Append('\n');

                foreach (ScriptLine line in option.Lines)
                {
                    foreach (string idLine in Wrap($"[{line.LineId}]", textWidth))
                        sb.Append(textIndent).Append(idLine).Append('\n');
                    foreach (string textLine in Wrap($"\"{line.Text}\"", textWidth))
                        sb.Append(textIndent).Append(textLine).Append('\n');
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Greedy word-wrap like <see cref="Script"/>'s, except words wider than the
        /// column (long line ids, text with no spaces) are broken so they can't push
        /// past the box border — preferring to break just after an underscore.
        /// </summary>
        private static List<string> Wrap(string text, int maxWidth)
        {
            var lines = new List<string>();
            var current = new StringBuilder();

            foreach (string rawWord in text.Split(' '))
            {
                string word = rawWord;

                while (word.Length > maxWidth)
                {
                    if (current.Length > 0)
                    {
                        lines.Add(current.ToString());
                        current.Clear();
                    }

                    int underscore = word.LastIndexOf('_', maxWidth - 1);
                    int cut = underscore > 0 ? underscore + 1 : maxWidth;
                    lines.Add(word[..cut]);
                    word = word[cut..];
                }

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
