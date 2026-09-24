using StardewVoices.Utils.ParsingHelpers;
using System.Collections.Generic;
using System.Linq;


namespace StardewVoices.FileGeneration.Models
{
    /// <summary>
    /// Why a set of lines are mutually exclusive alternates of each other.
    /// </summary>
    public enum VariantKind
    {
        Gender,             // _male_farmer / _female_farmer
        QuestionResponse    // Phase 2: $q/$r, event question/fork — one option per player answer
    }

    /// <summary>
    /// One recordable line as the script sees it: id, spoken text, and (for events) the speaker.
    /// </summary>
    public record ScriptLine(string LineId, string Text, string Speaker = null);

    /// <summary>
    /// A single entry in a script: either a normal line or a group of alternate lines.
    /// </summary>
    public abstract class ScriptEntry
    {
        public abstract string Speaker { get; }
    }

    public class SingleLineEntry : ScriptEntry
    {
        public ScriptLine Line { get; }
        public override string Speaker => Line.Speaker;

        public SingleLineEntry(ScriptLine line)
        {
            Line = line;
        }
    }

    /// <summary>
    /// Where an option came from, so a later pass can look up extra context for it.
    /// Unused for gender groups. In Phase 2 this is where the answer-text lookup plugs in:
    /// resolve the player's answer from the original dialogue/event data using these
    /// fields and write it to <see cref="VariantOption.Label"/>.
    /// </summary>
    public record VariantSource(string DialogueKey = null, string EventKey = null, string AnswerId = null);

    /// <summary>
    /// One alternate within a <see cref="VariantGroup"/>. May span several lines/pages.
    /// </summary>
    public class VariantOption
    {
        /// <summary>Column header, e.g. "Male Farmer" — or, in Phase 2, the player's answer text.</summary>
        public string Label { get; set; }
        public List<ScriptLine> Lines { get; }
        public VariantSource Source { get; set; }

        public VariantOption(string label, List<ScriptLine> lines, VariantSource source = null)
        {
            Label = label;
            Lines = lines;
            Source = source;
        }
    }

    /// <summary>
    /// A set of mutually exclusive alternates: every option gets recorded, but only
    /// one ever plays in a given scene. Supports any number of options.
    /// </summary>
    public class VariantGroup : ScriptEntry
    {
        public VariantKind Kind { get; }
        public override string Speaker { get; }
        public List<VariantOption> Options { get; }

        /// <summary>Total lines to record across every option.</summary>
        public int LineCount => Options.Sum(option => option.Lines.Count);

        public VariantGroup(VariantKind kind, string speaker, List<VariantOption> options)
        {
            Kind = kind;
            Speaker = speaker;
            Options = options;
        }
    }

    /// <summary>
    /// Turns a flat, in-order list of lines into script entries, folding alternates
    /// into <see cref="VariantGroup"/>s.
    ///
    /// Gender pairs are found by line id: a "_male_farmer" line immediately followed by
    /// its "_female_farmer" partner (same speaker, same base id). Pairs whose ids don't
    /// match — e.g. one half was renamed by EventDialogueReconciler's duplicate collapsing —
    /// are left as normal lines, one after the other. Ids are never changed here.
    ///
    /// TODO Phase 2: QuestionResponse groups can't be found from ids alone — the extractor
    /// will need to pass branch/answer info through to here.
    /// </summary>
    public static class VariantGroupBuilder
    {
        private const string MaleSuffix = "_male_farmer";
        private const string FemaleSuffix = "_female_farmer";

        public const string MaleLabel = "Male Farmer";
        public const string FemaleLabel = "Female Farmer";

        public static List<ScriptEntry> Build(IReadOnlyList<ScriptLine> lines)
        {
            var entries = new List<ScriptEntry>();

            for (int i = 0; i < lines.Count; i++)
            {
                if (!IsGenderPair(lines, i))
                {
                    entries.Add(new SingleLineEntry(lines[i]));
                    continue;
                }

                ScriptLine male = lines[i];
                ScriptLine female = lines[i + 1];
                i++;

                // Both halves read the same (emotion codes are already stripped by the
                // parser) — nothing to choose between. Playback resolves identical text to
                // the first id in the json, which is the male one, so show that.
                if (male.Text == female.Text)
                {
                    entries.Add(new SingleLineEntry(male));
                    continue;
                }

                // A multi-page gendered dialogue produces one pair per page, chained
                // _sv ids apart. Fold the next page into the same group so each option
                // reads as one multi-line take instead of several separate tables.
                if (entries.Count > 0
                    && entries[^1] is VariantGroup previous
                    && previous.Kind == VariantKind.Gender
                    && previous.Speaker == male.Speaker
                    && CharacterDialogueParser.AdvancePastLineId(previous.Options[0].Lines[^1].LineId) == BaseId(male.LineId))
                {
                    previous.Options[0].Lines.Add(male);
                    previous.Options[1].Lines.Add(female);
                    continue;
                }

                entries.Add(new VariantGroup(VariantKind.Gender, male.Speaker, new List<VariantOption>
                {
                    new VariantOption(MaleLabel,   new List<ScriptLine> { male }),
                    new VariantOption(FemaleLabel, new List<ScriptLine> { female }),
                }));
            }

            return entries;
        }

        private static bool IsGenderPair(IReadOnlyList<ScriptLine> lines, int i)
        {
            if (i + 1 >= lines.Count) return false;

            ScriptLine first = lines[i];
            ScriptLine second = lines[i + 1];

            return first.LineId.EndsWith(MaleSuffix)
                && second.LineId.EndsWith(FemaleSuffix)
                && first.Speaker == second.Speaker
                && BaseId(first.LineId) == BaseId(second.LineId);
        }

        // Id with its gender suffix removed — only used for matching, never written out.
        private static string BaseId(string lineId)
        {
            if (lineId.EndsWith(MaleSuffix)) return lineId[..^MaleSuffix.Length];
            if (lineId.EndsWith(FemaleSuffix)) return lineId[..^FemaleSuffix.Length];
            return lineId;
        }
    }
}
