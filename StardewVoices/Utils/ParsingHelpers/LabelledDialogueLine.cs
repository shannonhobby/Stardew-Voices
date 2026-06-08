namespace StardewVoices.Utils.ParsingHelpers
{
    /// <summary>
    /// A single audio-file-worth of dialogue: a unique line ID (matching the
    /// filename the voice actor will record) paired with the cleaned display text.
    /// One raw JSON entry may expand into several LabelledDialogueLines when it
    /// contains branching operators ($e, $b, $c, ^, ||, etc.).
    /// </summary>
    public class LabelledDialogueLine
    {
        /// <summary>
        /// The generated line ID, e.g. "Mon_sv1", "intro_male_farmer",
        /// "Sun1_chance_2". Corresponds directly to an audio file name.
        /// </summary>
        public string LineId { get; }

        /// <summary>
        /// The dialogue text with all Stardew formatting codes stripped out.
        /// </summary>
        public string Text { get; }

        /// <summary>
        /// The original JSON key this line was parsed from.
        /// </summary>
        public string OriginalKey { get; }

        /// <summary>
        /// Which dialogue category this line belongs to.
        /// </summary>
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
}