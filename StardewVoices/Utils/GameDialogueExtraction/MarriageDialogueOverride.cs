namespace StardewVoices.Utils.GameDialogueExtraction
{
    /// <summary>
    /// A marriage dialogue ID that exists in both the generic MarriageDialogue.json
    /// and a character-specific MarriageDialogue{Name}.json file, meaning the
    /// character's own line fully shadows the generic one — the generic line is
    /// never shown to that character in-game and should not be scripted/voiced
    /// under their name.
    /// </summary>
    public class MarriageDialogueOverride
    {
        public string DialogueId { get; }
        public string GenericText { get; }
        public string CharacterText { get; }

        public MarriageDialogueOverride(string dialogueId, string genericText, string characterText)
        {
            DialogueId = dialogueId;
            GenericText = genericText;
            CharacterText = characterText;
        }
    }
}
