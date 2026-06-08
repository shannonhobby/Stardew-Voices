using StardewVoices.Dialogue;
using System.Collections.Generic;

namespace StardewVoices.Utils.ParsingHelpers
{
    /// <summary>
    /// A single audio-file-worth of dialogue: a generated line ID paired with
    /// its cleaned display text. One StardewValley.Dialogue may produce several
    /// of these when it contains branching operators ($e, $b, ||, ^, etc.).
    /// </summary>
    public class LabelledDialogueLine
    {
        public string LineId { get; }
        public string Text { get; }
        public string OriginalKey { get; }
        public DialogueType Type { get; }

        public LabelledDialogueLine(string lineId, string text, string originalKey, DialogueType type)
        {
            LineId = lineId;
            Text = text;
            OriginalKey = originalKey;
            Type = type;
        }

        public override string ToString() => $"[{Type}] {LineId}: {Text}";
    }

    /// <summary>
    /// Wraps a StardewValley.Dialogue together with its parsed line IDs.
    /// 
    /// Consumers that need game-engine context (testing manager, anything that
    /// drives portrait / expression logic) use <see cref="Dialogue"/>.
    /// Consumers that need audio file IDs (file generator, lookup tables) use
    /// <see cref="Lines"/>.
    /// </summary>
    public class LabelledDialogue
    {
        /// <summary>
        /// The original game dialogue object, preserving multi-page context and
        /// any engine metadata (portraits, expressions, etc.).
        /// </summary>
        public StardewValley.Dialogue Dialogue { get; }

        /// <summary>
        /// One entry per audio file: the parsed (LineId, Text) pairs derived
        /// from this dialogue's raw string.
        /// </summary>
        public List<LabelledDialogueLine> Lines { get; }

        public string OriginalKey { get; }
        public DialogueType Type { get; }

        public LabelledDialogue(
            StardewValley.Dialogue dialogue,
            List<LabelledDialogueLine> lines,
            string originalKey,
            DialogueType type)
        {
            Dialogue = dialogue;
            Lines = lines;
            OriginalKey = originalKey;
            Type = type;
        }
    }
}