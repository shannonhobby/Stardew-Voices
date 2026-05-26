using StardewVoices.Dialogue;

namespace StardewVoices.Testing.Models
{
    public class EventDialogueTestCase
    {
        public string CharacterName { get; }
        public string EventId { get; }
        public string LocationName { get; }
        public string DialogueText { get; }
        public string CueName { get; }
        public DialogueType DialogueType => DialogueType.Event;

        public EventDialogueTestCase(string characterName, string eventId, string locationName, string dialogueText, DialogueManager dialogueManager)
        {
            CharacterName = characterName;
            EventId = eventId;
            LocationName = locationName;
            DialogueText = dialogueText;
            CueName = dialogueManager.getIdFromDialogueString(dialogueText, characterName);
        }
    }
}
