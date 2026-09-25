using StardewVoices.Pathing;
using StardewVoices.Utils;
using StardewVoices.Utils.GameDialogueExtraction;
using System;
using System.Collections.Generic;
using System.Linq;


namespace StardewVoices.FileGeneration.Models
{

    /// <summary>
    /// A single attributed dialogue line in an event scene.
    /// </summary>
    /// <param name="LineId">The game dialogue key (e.g. "AbigailEvent1_greeting").</param>
    /// <param name="Character">The character speaking this line.</param>
    /// <param name="Dialogue">The spoken text.</param>
    public record EventLine(string LineId, string Character, string Dialogue);

    /// <summary>
    /// Builds a formatted voiceover script for a single character across one or more
    /// Stardew Valley events. Extends <see cref="Script"/>.
    ///
    /// Key differences from a standard Script:
    ///   • Each line is tagged with its speaker.
    ///   • The target character's lines are visually highlighted — actors can
    ///     scan the page and immediately spot what they need to record.
    ///   • Location changes can be inserted inline as scene-setting markers.
    ///   • Event changes insert a heavy full-width break — clearly more prominent
    ///     than a location change.
    ///   • LineCount tracks only the target character's lines across all events.
    ///
    /// Sample plain-text output:
    /// ┌────────────────────────────────────────────────────────────────────────────┐
    ///
    ///               ˚✧₊⁺  STARDEW VALLEY VOICEOVER SCRIPT  ⁺₊✧˚
    ///
    ///                              EVENT SCRIPTS
    ///
    ///                    SCRIPT FOR:  ABIGAIL   ( 4 lines to record )
    ///
    ///             prepared with ♡ by Shannon Hobby   ·   happy recording! (^‿^)
    ///
    ///                      ★ = your lines     ·  = other characters
    ///
    ///                  ════════════════════════════════════════════════
    ///
    ///
    /// ✿── OPENING SCENE ──────────────────────────────────────────────────────────✿
    ///
    /// ✦── at: Community Center  ·  morning ──────────────────────────────────────✦
    ///
    ///                              ★  ABIGAIL  ★
    ///                       [ ceremony_abigail_excited ]
    ///                  "I can't believe it's finally happening! This
    ///                           is SO exciting, right?!"
    ///
    ///
    /// ════════════════════════════════════════════════════════════════════════════════
    ///
    ///                        ✦✦  SPIRIT'S EVE FESTIVAL  ✦✦
    ///
    /// ════════════════════════════════════════════════════════════════════════════════
    ///
    ///
    /// ✿── HAUNTED MAZE ENCOUNTER ─────────────────────────────────────────────────✿
    ///
    /// └────────────────────────────────────────────────────────────────────────────┘
    /// </summary>
    public class EventScript : Script
    {
        // ── Properties ──────────────────────────────────────────────────────────────
        // CharacterName (inherited) = the target character for this script

        // ── Constructor ─────────────────────────────────────────────────────────────
        public EventScript(string targetCharacter, PathManager pathManager)
            : base(targetCharacter, pathManager)
        {
        }

        // ── Public API ───────────────────────────────────────────────────────────────

        /// <summary>
        /// Inserts a heavy visual break and begins a new event.
        /// More prominent than <see cref="AddLocationChange"/> — this is the big one.
        /// </summary>
        public void ChangeEvent(string newEventName)
        {
            string rule = new string('═', PageWidth);
            string label = $"✦✦  {newEventName.ToUpper()}  ✦✦";

            // Plain text: double rule with event name sandwiched between
            PlainTextScript += string.Join("\n",
                "",
                "",
                rule,
                "",
                CenterLine(label),
                "",
                rule,
                "", ""
            );

            // Markdown: horizontal rules around a centered h2
            MarkDownScript += string.Join("\n",
                "",
                "---",
                "",
                $"<div align=\"center\">",
                "",
                $"## ✦✦ {newEventName} ✦✦",
                "",
                "</div>",
                "",
                "---",
                "", ""
            );
        }

        /// <summary>
        /// Inserts a location context marker at the current position in the script.
        /// Call this before the first section or any time the scene location changes.
        /// </summary>
        /// <param name="location">The scene location (e.g. "Community Center").</param>
        /// <param name="timeOfDay">Optional time of day (e.g. "morning", "6pm"). Appended after a · separator.</param>
        public void AddLocationChange(string location, string? timeOfDay = null)
        {
            string label = timeOfDay is not null ? $"{location}  ·  {timeOfDay}" : location;
            int dashCount = Math.Max(2, PageWidth - 10 - label.Length);
            PlainTextScript += $"\n✦── at: {label} {new string('─', dashCount)}✦\n\n";
            MarkDownScript += $"\n> *✦ at: {label} ✦*\n\n";
        }

        /// <summary>
        /// Appends a labelled scene section with multi-character dialogue lines.
        /// The target character's lines are highlighted; all others are rendered quietly.
        /// Only the target character's lines increment <see cref="Script.LineCount"/>.
        /// </summary>
        public void AddScriptSection(string sectionTitle, List<EventLine> lines)
        {
            AppendSectionHeader(sectionTitle);
            AddEventLines(lines);
        }

        /// <summary>
        /// Adds a scene's lines in order, showing alternates side by side instead of one
        /// after the other: male/female farmer variants, prerequisite versions, and — when
        /// <paramref name="questionBranches"/> is given — each player question's replies,
        /// one column per answer.
        /// </summary>
        /// <param name="questionBranches">
        /// This event's inline question branches (EventDialogueExtractor.QuestionBranchesByEvent),
        /// whose line indexes refer to <paramref name="lines"/>. Null = no question grouping.
        /// </param>
        /// <param name="dialogueQuestions">
        /// This event's $q dialogue questions (EventDialogueExtractor.DialogueQuestionsByEvent).
        /// Their replies come from the speaker's dialogue file and are shown right after the
        /// question line, one column per reply. Null = none.
        /// </param>
        /// <param name="forks">
        /// This event's fork/switchEvent points (EventDialogueExtractor.ForkPointsByEvent), shown
        /// where they happen: a player-choice fork as a table of answers pointing to the scene
        /// each leads to, anything else as a one-line pointer to the scene. Null = none.
        /// </param>
        public void AddEventLines(
            IEnumerable<EventLine> lines,
            IReadOnlyList<EventQuestionBranches> questionBranches = null,
            IReadOnlyList<EventDialogueQuestion> dialogueQuestions = null,
            IReadOnlyList<EventFork> forks = null)
        {
            List<ScriptLine> scriptLines = lines.Select(line => new ScriptLine(line.LineId, line.Dialogue, line.Character)).ToList();

            foreach (ScriptEntry entry in VariantGroupBuilder.Build(scriptLines, questionBranches, dialogueQuestions, forks))
            {
                if (entry is VariantGroup group)
                    AddEventVariantGroup(group);
                else if (entry is SingleLineEntry single)
                    AddEventLine(new EventLine(single.Line.LineId, single.Line.Speaker, single.Line.Text));
                else if (entry is SceneJumpEntry jump)
                    AddSceneJump(jump.Text);
            }
        }

        // A quiet pointer to where the event jumps to another scene — not a line to record.
        private void AddSceneJump(string text)
        {
            PlainTextScript += CenterLine(text) + "\n\n";
            MarkDownScript += $"*{text}*\n\n";
        }

        public void AddEventLine(EventLine line)
        {
            bool isTarget = IsTargetCharacter(line.Character);

            if (isTarget) LineCount++;

            AddEventLinePlainText(line, isTarget);
            AddEventLineMarkdown(line, isTarget);
        }

        public override void WriteOut(string fileType="both")
        {
            if (fileType == "txt" || fileType == "both")
            {
                ParentFolder.CreateFile(CharacterName + " Events.txt", GetPlainText());
            }

            if (fileType == "md" || fileType == "both")
            {
                ParentFolder.CreateFile(CharacterName + " Events.md", GetMarkdown());
            }
        }

        // ── Overrides ────────────────────────────────────────────────────────────────

        protected override string BuildPlainTextHeader()
        {
            string rule = new string('═', 60);
            
            return string.Join("\n",
                "",
                CenterLine("˚✧₊⁺  STARDEW VALLEY VOICEOVER SCRIPT  ⁺₊✧˚"),
                "",
                CenterLine("EVENT SCRIPTS"),
                "",
                CenterLine($"SCRIPT FOR:  {CharacterName.ToUpper()}   ( {LineCount} lines to record )"),
                "",
                CenterLine("prepared with ♡ by Shannon Hobby   ·   happy recording! (^‿^)"),
                "",
                CenterLine("★ = your lines     ·  = other characters"),
                "",
                CenterLine(rule),
                "", ""
            );
        }

        protected override string BuildMarkdownHeader()
        {
            string eventLabel =  "Event Scripts";

            return string.Join("\n",
                "<div align=\"center\">",
                "",
                "# ✿ Stardew Valley Voiceover Script ✿",
                "",
                $"## {eventLabel}",
                "",
                $"### Script for: {CharacterName}",
                "",
                $"*{LineCount} lines to record &nbsp;·&nbsp; prepared with ♡ by Shannon Hobby &nbsp;·&nbsp; happy recording! (^‿^)*",
                "",
                "*★ = your lines &nbsp;&nbsp;·&nbsp;&nbsp; · = other characters*",
                "",
                "</div>",
                "",
                "---",
                "", ""
            );
        }

        // ── Private helpers ──────────────────────────────────────────────────────────

        private bool IsTargetCharacter(string character)
            => string.Equals(
                 LocalizationHelpers.GetLocalizedName(character), CharacterName, StringComparison.OrdinalIgnoreCase);

        // Character label: ★ NAME ★ for the target; · Name · for others
        private string PlainTextSpeakerLabel(string character, bool isTarget)
        {
            string localizedCharacterName = LocalizationHelpers.GetLocalizedName(character);
            return isTarget
                ? CenterLine($"★  {localizedCharacterName.ToUpper()}  ★") + "\n"
                : CenterLine($"· {localizedCharacterName} ·") + "\n";
        }

        /// <summary>
        /// Renders mutually exclusive alternates side by side under the usual speaker label.
        /// Only the target character's lines count toward <see cref="Script.LineCount"/>
        /// — in every option, since each one still has to be recorded.
        /// A question group can mix speakers (e.g. Sam replies, then Lewis); then there's no
        /// single speaker label and each line in a cell is labelled with its speaker instead.
        /// </summary>
        private void AddEventVariantGroup(VariantGroup group)
        {
            LineCount += group.Options.Sum(option => option.Lines.Count(line => IsTargetCharacter(line.Speaker)));

            string plainTextMarker = VariantGroupRenderer.PlainTextMarkerFor(group.Kind);
            string markdownMarker = VariantGroupRenderer.MarkdownMarkerFor(group.Kind);

            Func<ScriptLine, bool> emphasize = line => IsTargetCharacter(line.Speaker);
            Func<ScriptLine, string> plainTextLineLabel = null;
            Func<ScriptLine, string> markdownLineLabel = null;

            if (group.Speaker != null)
            {
                bool isTarget = IsTargetCharacter(group.Speaker);
                string localizedCharacterName = LocalizationHelpers.GetLocalizedName(group.Speaker);

                PlainTextScript += PlainTextSpeakerLabel(group.Speaker, isTarget);
                MarkDownScript += isTarget
                    ? $"**★ {localizedCharacterName.ToUpper()} ★**\n\n"
                    : $"· *{localizedCharacterName}* ·\n\n";
            }
            else
            {
                plainTextLineLabel = line => IsTargetCharacter(line.Speaker)
                    ? $"★ {LocalizationHelpers.GetLocalizedName(line.Speaker).ToUpper()}"
                    : $"· {LocalizationHelpers.GetLocalizedName(line.Speaker)}";
                markdownLineLabel = line => IsTargetCharacter(line.Speaker)
                    ? $"**★ {LocalizationHelpers.GetLocalizedName(line.Speaker).ToUpper()}**"
                    : $"*{LocalizationHelpers.GetLocalizedName(line.Speaker)}*";
            }

            PlainTextScript += CenterLine(plainTextMarker) + "\n";
            PlainTextScript += VariantGroupRenderer.RenderPlainText(group, PageWidth, plainTextLineLabel) + "\n";

            MarkDownScript += markdownMarker + "\n\n";
            MarkDownScript += VariantGroupRenderer.RenderMarkdownTable(group, emphasize, markdownLineLabel) + "\n";
        }

        private void AddEventLinePlainText(EventLine line, bool isTarget)
        {
            PlainTextScript += PlainTextSpeakerLabel(line.Character, isTarget);

            // Line ID
            PlainTextScript += CenterLine($"[ {line.LineId} ]") + "\n";

            // Quoted, word-wrapped dialogue (shared helper from base class)
            PlainTextScript += BuildQuotedBlock(line.Dialogue) + "\n";
        }

        private void AddEventLineMarkdown(EventLine line, bool isTarget)
        {
            string localizedCharacterName = LocalizationHelpers.GetLocalizedName(line.Character);

            if (isTarget)
            {
                // Bold character label + bold code ID so it jumps off the page
                MarkDownScript += $"**★ {localizedCharacterName.ToUpper()} ★** &nbsp; **`{line.LineId}`**\n\n";
                // Bold blockquote for the spoken line itself
                MarkDownScript += $"> **{line.Dialogue}**\n\n";
            }
            else
            {
                // Quiet: italic name, plain code ID, plain blockquote
                MarkDownScript += $"· *{localizedCharacterName}* · &nbsp; `{line.LineId}`\n\n";
                MarkDownScript += $"> {line.Dialogue}\n\n";
            }
        }
    }

}
