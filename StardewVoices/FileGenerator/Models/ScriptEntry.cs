using StardewVoices.Utils.GameDialogueExtraction;
using StardewVoices.Utils.ParsingHelpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;


namespace StardewVoices.FileGeneration.Models
{
    /// <summary>
    /// Why a set of lines are mutually exclusive alternates of each other.
    /// </summary>
    public enum VariantKind
    {
        Gender,             // _male_farmer / _female_farmer
        Prerequisite,       // _prereq_1 / _prereq_2 / ... — $p/$d condition branches
        QuestionResponse,   // $q/$r, event splitSpeak/quickQuestion — one option per player answer
        SceneBranch         // event question + fork — the answer picks which scene plays next
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
    /// A pointer to another event scene, where the event jumps there with no choice made
    /// in this scene (switchEvent, or a fork decided by an earlier choice/flag).
    /// </summary>
    public class SceneJumpEntry : ScriptEntry
    {
        public string Text { get; }
        public override string Speaker => null;

        public SceneJumpEntry(string text)
        {
            Text = text;
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

        /// <summary>
        /// Direction shown in the cell after any lines, e.g. "→ Go to the stayPut scene".
        /// Null = none.
        /// </summary>
        public string Note { get; set; }

        public VariantOption(string label, List<ScriptLine> lines, VariantSource source = null, string note = null)
        {
            Label = label;
            Lines = lines;
            Source = source;
            Note = note;
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
    /// Prerequisite branches are found the same way: a run of consecutive "_prereq_{n}" lines
    /// (same speaker, same base id, n increasing). Which condition picks each one isn't shown —
    /// only that they're alternates.
    ///
    /// Question responses can't be found from ids alone — the extractor records which lines
    /// each player answer leads to (EventDialogueExtractor.QuestionBranchesByEvent) and that
    /// is passed to <see cref="Build(IReadOnlyList{ScriptLine}, IReadOnlyList{EventQuestionBranches})"/>.
    /// Fork branches that jump to a separate event (e.g. stayPut / rejectSam) aren't grouped.
    /// </summary>
    public static class VariantGroupBuilder
    {
        public const string NoAnswerTextLabel = "Answer";   // "Answer 1", ... when the question lists no text

        /// <summary>
        /// Like <see cref="Build(IReadOnlyList{ScriptLine})"/>, but first folds each inline player
        /// question (splitSpeak / quickQuestion) into one QuestionResponse group — one option per
        /// answer, labelled with the answer text. Line indexes in <paramref name="questions"/>
        /// refer to <paramref name="lines"/>. Lines outside any question are grouped as usual.
        /// A question nested inside another's branch stays inside the outer group's cells.
        /// </summary>
        public static List<ScriptEntry> Build(IReadOnlyList<ScriptLine> lines, IReadOnlyList<EventQuestionBranches> questions)
            => Build(lines, questions, new List<(int position, ScriptEntry entry)>());

        // insertions: entries (fork tables, scene jumps) to drop in before lines[position] —
        // position == lines.Count means after the last line. One that falls inside a question's
        // lines is placed right after that question's table instead.
        private static List<ScriptEntry> Build(
            IReadOnlyList<ScriptLine> lines,
            IReadOnlyList<EventQuestionBranches> questions,
            List<(int position, ScriptEntry entry)> insertions)
        {
            if ((questions == null || questions.Count == 0) && insertions.Count == 0)
                return Build(lines);

            // Outermost first when two questions start on the same line.
            var spans = (questions ?? new List<EventQuestionBranches>())
                .Select(question => (question, start: QuestionStart(question), end: QuestionEnd(question)))
                .Where(span => span.question.Branches.Count >= 2 && span.end > span.start && span.end <= lines.Count)
                .OrderBy(span => span.start)
                .ThenByDescending(span => span.end)
                .ToList();

            var pending = insertions.OrderBy(insertion => insertion.position).ToList();
            int nextInsertion = 0;

            var entries = new List<ScriptEntry>();
            int next = 0;
            EventQuestionBranches previousQuestion = null;
            VariantGroup previousGroup = null;

            // Plain lines [from, to), with any pending insertions dropped in at their positions.
            void EmitPlain(int from, int to)
            {
                int segmentStart = from;
                for (; nextInsertion < pending.Count && pending[nextInsertion].position <= to; nextInsertion++)
                {
                    int at = Math.Max(pending[nextInsertion].position, segmentStart);
                    if (at > segmentStart)
                        entries.AddRange(Build(Slice(lines, segmentStart, at)));
                    entries.Add(pending[nextInsertion].entry);
                    segmentStart = at;
                    previousGroup = null;
                }

                if (to > segmentStart)
                {
                    entries.AddRange(Build(Slice(lines, segmentStart, to)));
                    previousGroup = null;
                }
            }

            foreach (var (question, start, end) in spans)
            {
                if (start < next) continue;   // nested inside a question already grouped

                EmitPlain(next, start);

                List<List<ScriptLine>> linesByAnswer = question.Branches
                    .Select(branch => Slice(lines, branch.FirstLineIndex, branch.FirstLineIndex + branch.LineCount))
                    .ToList();

                // Several splitSpeaks in a row answer the same question (e.g. Sam replies, then
                // Lewis replies) — fold them into one table so each column holds every reply
                // to that answer.
                if (previousGroup != null
                    && question.Source == "splitSpeak" && previousQuestion.Source == "splitSpeak"
                    && question.Answers.SequenceEqual(previousQuestion.Answers)
                    && question.Branches.Count == previousGroup.Options.Count)
                {
                    for (int answer = 0; answer < linesByAnswer.Count; answer++)
                        previousGroup.Options[answer].Lines.AddRange(linesByAnswer[answer]);

                    // The merged group may now have more than one speaker.
                    previousGroup = new VariantGroup(
                        VariantKind.QuestionResponse,
                        SharedSpeaker(previousGroup.Options.SelectMany(option => option.Lines)),
                        previousGroup.Options);
                    entries[^1] = previousGroup;
                }
                else
                {
                    var options = question.Branches
                        .Select((branch, answer) => new VariantOption(
                            string.IsNullOrWhiteSpace(branch.AnswerText) ? $"{NoAnswerTextLabel} {branch.AnswerIndex + 1}" : branch.AnswerText,
                            linesByAnswer[answer],
                            new VariantSource(AnswerId: branch.AnswerIndex.ToString()),
                            branch.Note))
                        .ToList();

                    previousGroup = new VariantGroup(VariantKind.QuestionResponse, SharedSpeaker(linesByAnswer.SelectMany(l => l)), options);
                    entries.Add(previousGroup);
                }

                previousQuestion = question;
                next = end;
            }

            EmitPlain(next, lines.Count);

            // Anything positioned past the end (shouldn't happen) still gets shown.
            for (; nextInsertion < pending.Count; nextInsertion++)
                entries.Add(pending[nextInsertion].entry);

            return entries;
        }

        /// <summary>
        /// Like <see cref="Build(IReadOnlyList{ScriptLine}, IReadOnlyList{EventQuestionBranches})"/>,
        /// but also shows the replies to $q dialogue questions asked in the event. Those replies
        /// live in the speaker's dialogue file rather than the event, so they're spliced in right
        /// after their question line — one branch per distinct reply, like
        /// <see cref="BuildDialogueQuestionReplies"/> — and <paramref name="questions"/>' line
        /// indexes are shifted to match.
        /// </summary>
        public static List<ScriptEntry> Build(
            IReadOnlyList<ScriptLine> lines,
            IReadOnlyList<EventQuestionBranches> questions,
            IReadOnlyList<EventDialogueQuestion> dialogueQuestions)
            => Build(lines, questions, dialogueQuestions, null);

        /// <summary>
        /// Like <see cref="Build(IReadOnlyList{ScriptLine}, IReadOnlyList{EventQuestionBranches}, IReadOnlyList{EventDialogueQuestion})"/>,
        /// but also marks where the event jumps to another scene (<paramref name="forks"/>, from
        /// EventDialogueExtractor.ForkPointsByEvent, LineIndex into <paramref name="lines"/>):
        /// a question-driven fork becomes a SceneBranch table — one column per player answer,
        /// the jumping answer pointing to its scene and the rest continuing below — and a
        /// switchEvent or flag-driven fork becomes a one-line pointer to the scene.
        /// </summary>
        public static List<ScriptEntry> Build(
            IReadOnlyList<ScriptLine> lines,
            IReadOnlyList<EventQuestionBranches> questions,
            IReadOnlyList<EventDialogueQuestion> dialogueQuestions,
            IReadOnlyList<EventFork> forks)
        {
            dialogueQuestions ??= new List<EventDialogueQuestion>();
            forks ??= new List<EventFork>();

            // A plain "fork {key}" with no "question forkN" before it jumps when a reply containing
            // %fork was shown — i.e. when the player picked one of those answers to the $q question
            // just before it. Point those replies' columns at the scene instead of adding a table.
            var forkAfterQuestion = new Dictionary<EventDialogueQuestion, EventFork>(ReferenceEqualityComparer.Instance);
            var forksShownInQuestions = new HashSet<EventFork>(ReferenceEqualityComparer.Instance);
            foreach (EventFork fork in forks.Where(fork => !fork.IsSwitchEvent && fork.RequiredId == null && fork.AnswerIndex == null && fork.LineIndex >= 0))
            {
                EventDialogueQuestion question = dialogueQuestions
                    .Where(q => q.QuestionLineIndex < fork.LineIndex
                        && q.ForkingReplyKeys != null && q.ForkingReplyKeys.Count > 0
                        && !forkAfterQuestion.ContainsKey(q))
                    .OrderByDescending(q => q.QuestionLineIndex)
                    .FirstOrDefault();

                if (question == null) continue;
                forkAfterQuestion[question] = fork;
                forksShownInQuestions.Add(fork);
            }

            var toSplice = dialogueQuestions
                .Where(question => question.QuestionLineIndex >= 0 && question.QuestionLineIndex < lines.Count)
                .OrderBy(question => question.QuestionLineIndex)
                .ToList();

            var combined = new List<ScriptLine>();
            var splices = new List<(int beforeOriginalIndex, int count)>();
            var replyQuestions = new List<EventQuestionBranches>();
            int nextSplice = 0;

            for (int i = 0; i < lines.Count; i++)
            {
                combined.Add(lines[i]);

                for (; nextSplice < toSplice.Count && toSplice[nextSplice].QuestionLineIndex == i; nextSplice++)
                {
                    EventDialogueQuestion question = toSplice[nextSplice];
                    int spliceStart = combined.Count;
                    var branches = new List<EventAnswerBranch>();

                    // Several answers often share one reply key — one branch per distinct reply.
                    foreach (var sameReply in question.Responses.Select((response, answer) => (response, answer)).GroupBy(item => item.response.ReplyKey))
                    {
                        IReadOnlyList<EventLine> replyLines = question.RepliesByKey.TryGetValue(sameReply.Key, out var found)
                            ? found
                            : new List<EventLine>();

                        string label = string.Join(" / ", sameReply.Select(item => string.IsNullOrWhiteSpace(item.response.AnswerText)
                            ? $"{NoAnswerTextLabel} {item.answer + 1}"
                            : item.response.AnswerText));

                        string note = forkAfterQuestion.TryGetValue(question, out EventFork fork)
                            ? (question.ForkingReplyKeys.Contains(sameReply.Key) ? $"→ Go to {SceneName(fork)}" : ContinuesBelowNote)
                            : null;

                        branches.Add(new EventAnswerBranch(sameReply.First().answer, label, combined.Count, replyLines.Count, note));
                        combined.AddRange(replyLines.Select(line => new ScriptLine(line.LineId, line.Dialogue, line.Character)));
                    }

                    splices.Add((i + 1, combined.Count - spliceStart));
                    replyQuestions.Add(new EventQuestionBranches(
                        "dialogueQuestion", null, question.Responses.Select(response => response.AnswerText).ToList(), branches));
                }
            }

            // Lines spliced in before an original index push it (and everything after) down.
            // A splice exactly at a branch's end belongs to that branch (the question was its
            // last line), so the end shifts by it too.
            int ShiftAt(int originalIndex) => splices.Where(splice => splice.beforeOriginalIndex <= originalIndex).Sum(splice => splice.count);

            IEnumerable<EventQuestionBranches> shiftedQuestions = (questions ?? new List<EventQuestionBranches>())
                .Select(question => question with
                {
                    Branches = question.Branches.Select(branch =>
                    {
                        int first = branch.FirstLineIndex + ShiftAt(branch.FirstLineIndex);
                        int originalEnd = branch.FirstLineIndex + branch.LineCount;
                        int end = originalEnd + ShiftAt(originalEnd);
                        return branch with { FirstLineIndex = first, LineCount = end - first };
                    }).ToList()
                });

            // A fork sits before lines[LineIndex]; replies spliced in before that point (the
            // question just before the fork) play first, so it shifts past them.
            var forkInsertions = forks
                .Where(fork => fork.LineIndex >= 0 && fork.LineIndex <= lines.Count && !forksShownInQuestions.Contains(fork))
                .Select(fork => (fork.LineIndex + ShiftAt(fork.LineIndex), BuildForkEntry(fork)))
                .ToList();

            return Build(combined, shiftedQuestions.Concat(replyQuestions).ToList(), forkInsertions);
        }

        public const string ContinuesBelowNote = "Continues below";

        // A question-driven fork as a SceneBranch table (answers as columns: the jumping one
        // points to its scene, the rest continue below); anything else as a one-line pointer.
        private static ScriptEntry BuildForkEntry(EventFork fork)
        {
            string scene = SceneName(fork);

            if (!fork.IsSwitchEvent
                && fork.AnswerIndex is int jumpAnswer
                && fork.Answers != null && fork.Answers.Count >= 2 && jumpAnswer < fork.Answers.Count)
            {
                var options = fork.Answers
                    .Select((answerText, answer) => new VariantOption(
                        string.IsNullOrWhiteSpace(answerText) ? $"{NoAnswerTextLabel} {answer + 1}" : answerText,
                        new List<ScriptLine>(),
                        new VariantSource(EventKey: answer == jumpAnswer ? fork.TargetEventKey : fork.ParentEventKey, AnswerId: answer.ToString()),
                        answer == jumpAnswer ? $"→ Go to {scene}" : ContinuesBelowNote))
                    .ToList();
                return new VariantGroup(VariantKind.SceneBranch, null, options);
            }

            // Otherwise the jump isn't a choice shown in this table: switchEvent always jumps, and
            // other forks hang on a mail flag, an earlier answer, a minigame result, etc.
            return new SceneJumpEntry(fork.IsSwitchEvent
                ? $"→ Continues in {scene}"
                : $"→ May go to {scene} here (depends on the player's choices)");
        }

        // "the stayPut scene (SamHouse)" — matches the event's heading in the event script.
        private static string SceneName(EventFork fork)
        {
            string[] target = fork.TargetEventKey.Split('/', 2);
            return target.Length == 2 ? $"the {target[1]} scene ({target[0]})" : $"the {fork.TargetEventKey} scene";
        }

        /// <summary>
        /// The replies to a $q dialogue question as one QuestionResponse group. Several answers
        /// often share one reply key (e.g. three answers all get "Hmm, interesting..."), so
        /// there's one option per distinct reply, headed by every answer that leads to it —
        /// in the order the replies first appear.
        /// </summary>
        /// <param name="replyLinesForKey">The reply's lines for a reply key (empty if none).</param>
        public static VariantGroup BuildDialogueQuestionReplies(
            IReadOnlyList<DialogueQuestionResponse> responses, Func<string, IEnumerable<ScriptLine>> replyLinesForKey)
        {
            var options = responses
                .Select((response, answer) => (response, answer))
                .GroupBy(item => item.response.ReplyKey)
                .Select(sameReply => new VariantOption(
                    string.Join(" / ", sameReply.Select(item => string.IsNullOrWhiteSpace(item.response.AnswerText)
                        ? $"{NoAnswerTextLabel} {item.answer + 1}"
                        : item.response.AnswerText)),
                    replyLinesForKey(sameReply.Key).ToList(),
                    new VariantSource(DialogueKey: sameReply.Key, AnswerId: string.Join(",", sameReply.Select(item => item.answer)))))
                .ToList();

            return new VariantGroup(VariantKind.QuestionResponse, null, options);
        }

        private static int QuestionStart(EventQuestionBranches question)
            => question.Branches.Min(branch => branch.FirstLineIndex);

        private static int QuestionEnd(EventQuestionBranches question)
            => question.Branches.Max(branch => branch.FirstLineIndex + branch.LineCount);

        private static List<ScriptLine> Slice(IReadOnlyList<ScriptLine> lines, int start, int end)
            => lines.Skip(start).Take(end - start).ToList();

        // The one speaker of every line, or null when several characters speak.
        private static string SharedSpeaker(IEnumerable<ScriptLine> lines)
        {
            var speakers = lines.Select(line => line.Speaker).Distinct().ToList();
            return speakers.Count == 1 ? speakers[0] : null;
        }

        private const string MaleSuffix = "_male_farmer";
        private const string FemaleSuffix = "_female_farmer";

        public const string MaleLabel = "Male Farmer";
        public const string FemaleLabel = "Female Farmer";
        public const string PrerequisiteLabel = "Version";   // "Version 1", "Version 2", ...

        // <base>_prereq_<n> — see CharacterDialogueParser.ParsePDialogueWithIds
        private static readonly Regex PrereqSuffixRegex = new Regex(@"^(.*)_prereq_(\d+)$");

        public static List<ScriptEntry> Build(IReadOnlyList<ScriptLine> lines)
        {
            var entries = new List<ScriptEntry>();

            for (int i = 0; i < lines.Count; i++)
            {
                List<ScriptLine> prereqRun = TakePrerequisiteRun(lines, i);
                if (prereqRun.Count >= 2)
                {
                    i += prereqRun.Count - 1;
                    entries.Add(BuildPrerequisiteEntry(prereqRun));
                    continue;
                }

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

        // The consecutive "_prereq_{n}" lines starting at i that belong to one branch set:
        // same speaker, same base id, n increasing (a branch with empty text is skipped by the
        // parser, so n can jump). Empty if lines[i] isn't a prereq line.
        private static List<ScriptLine> TakePrerequisiteRun(IReadOnlyList<ScriptLine> lines, int i)
        {
            var run = new List<ScriptLine>();

            Match first = PrereqSuffixRegex.Match(lines[i].LineId);
            if (!first.Success) return run;

            string baseId = first.Groups[1].Value;
            int previousNumber = int.Parse(first.Groups[2].Value);
            run.Add(lines[i]);

            for (int j = i + 1; j < lines.Count; j++)
            {
                Match next = PrereqSuffixRegex.Match(lines[j].LineId);
                if (!next.Success
                    || next.Groups[1].Value != baseId
                    || lines[j].Speaker != lines[i].Speaker
                    || int.Parse(next.Groups[2].Value) <= previousNumber)
                    break;

                previousNumber = int.Parse(next.Groups[2].Value);
                run.Add(lines[j]);
            }

            return run;
        }

        private static ScriptEntry BuildPrerequisiteEntry(List<ScriptLine> run)
        {
            // Every branch reads the same — nothing to choose between. Playback resolves
            // identical text to the first id in the json, so show that one.
            if (run.All(line => line.Text == run[0].Text))
                return new SingleLineEntry(run[0]);

            var options = run
                .Select((line, index) => new VariantOption($"{PrerequisiteLabel} {index + 1}", new List<ScriptLine> { line }))
                .ToList();
            return new VariantGroup(VariantKind.Prerequisite, run[0].Speaker, options);
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
