using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace StardewVoices.FileGeneration.Models
{
    /// <summary>
    /// Plain-text layout measured in display columns rather than characters, so scripts in
    /// Japanese, Chinese and Korean — whose characters are two columns wide in a monospaced
    /// font — wrap, center and line up the same as English ones.
    /// </summary>
    internal static class TextLayout
    {
        /// <summary>
        /// Greedy word-wrap on spaces. Words wider than <paramref name="maxWidth"/> (long
        /// line ids, text with no spaces) are broken so they can't push past a box border or
        /// the page edge — preferring to break just after an underscore.
        /// Widths are display columns (see <see cref="DisplayWidth"/>), not characters,
        /// so Japanese/Chinese/Korean text — which may have no spaces at all — still fits.
        /// </summary>
        internal static List<string> Wrap(string text, int maxWidth)
        {
            var lines = new List<string>();
            var current = new StringBuilder();
            int currentWidth = 0;

            foreach (string rawWord in text.Split(' '))
            {
                string word = rawWord;

                while (DisplayWidth(word) > maxWidth)
                {
                    if (current.Length > 0)
                    {
                        lines.Add(current.ToString());
                        current.Clear();
                        currentWidth = 0;
                    }

                    int cut = FindCut(word, maxWidth);
                    lines.Add(word[..cut]);
                    word = word[cut..];
                }

                int wordWidth = DisplayWidth(word);

                if (current.Length == 0)
                {
                    current.Append(word);
                    currentWidth = wordWidth;
                }
                else if (currentWidth + 1 + wordWidth <= maxWidth)
                {
                    current.Append(' ').Append(word);
                    currentWidth += 1 + wordWidth;
                }
                else
                {
                    lines.Add(current.ToString());
                    current.Clear().Append(word);
                    currentWidth = wordWidth;
                }
            }

            if (current.Length > 0)
                lines.Add(current.ToString());

            return lines.Count > 0 ? lines : new List<string> { string.Empty };
        }

        /// <summary>
        /// Index to break an over-wide <paramref name="word"/> at: the longest prefix that
        /// fits in <paramref name="maxWidth"/> columns, cut just after its last underscore if
        /// it has one. Only cuts between whole characters (never inside a surrogate pair or
        /// before a combining mark), and always takes at least one.
        /// </summary>
        private static int FindCut(string word, int maxWidth)
        {
            int width = 0;
            int cut = 0;
            int afterUnderscore = 0;
            TextElementEnumerator elements = StringInfo.GetTextElementEnumerator(word);

            while (elements.MoveNext())
            {
                string element = elements.GetTextElement();
                width += DisplayWidth(element);
                if (width > maxWidth && cut > 0) break;

                cut = elements.ElementIndex + element.Length;
                if (element == "_") afterUnderscore = cut;
            }

            return afterUnderscore > 0 ? afterUnderscore : cut;
        }

        /// <summary>
        /// Columns <paramref name="text"/> takes up in a monospaced font: East Asian wide
        /// and full-width characters (kana, CJK ideographs, Hangul, full-width punctuation)
        /// take two, combining marks none, everything else one.
        /// </summary>
        internal static int DisplayWidth(string text)
        {
            int width = 0;
            foreach (Rune rune in text.EnumerateRunes())
            {
                UnicodeCategory category = Rune.GetUnicodeCategory(rune);
                if (category is UnicodeCategory.NonSpacingMark or UnicodeCategory.EnclosingMark or UnicodeCategory.Format)
                    continue;

                width += IsWide(rune.Value) ? 2 : 1;
            }
            return width;
        }

        private static bool IsWide(int codePoint) =>
            (codePoint >= 0x1100 && codePoint <= 0x115F)       // Hangul Jamo initials
            || (codePoint >= 0x2E80 && codePoint <= 0x303E)    // CJK radicals, symbols & punctuation (「」。、)
            || (codePoint >= 0x3041 && codePoint <= 0x33FF)    // Hiragana, Katakana, CJK compatibility
            || (codePoint >= 0x3400 && codePoint <= 0x4DBF)    // CJK Extension A
            || (codePoint >= 0x4E00 && codePoint <= 0x9FFF)    // CJK Unified Ideographs
            || (codePoint >= 0xA000 && codePoint <= 0xA4CF)    // Yi
            || (codePoint >= 0xAC00 && codePoint <= 0xD7A3)    // Hangul syllables
            || (codePoint >= 0xF900 && codePoint <= 0xFAFF)    // CJK compatibility ideographs
            || (codePoint >= 0xFE30 && codePoint <= 0xFE4F)    // CJK compatibility forms
            || (codePoint >= 0xFF00 && codePoint <= 0xFF60)    // Full-width forms (！？～)
            || (codePoint >= 0xFFE0 && codePoint <= 0xFFE6)    // Full-width signs
            || (codePoint >= 0x20000 && codePoint <= 0x3FFFD); // CJK Extensions B+
    }
}
