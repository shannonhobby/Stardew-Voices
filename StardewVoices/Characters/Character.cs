using StardewValley;
using StardewVoices.Dialogue;
using System.Collections.Generic;
using StardewVoices.Utils;

namespace StardewVoices.Characters
{ 
    public class Character
    {
        private string Name { get; }
        private NPC InGameNPC { get; set; }

        private DialogueDictionary DialogueDictionary { get; set; }
        public Character(string name)
        {
            Name = name;
            ErrorLogger.Trace($"{Name} initialized");
        }

        public string GetName() {  return Name; }
        public NPC GetNPC() { return InGameNPC; }

        public void SetupNpc() { 
            InGameNPC = Game1.getCharacterFromName(Name);
            ErrorLogger.Trace($"{Name}'s in game NPC instance initialized");
        }

        // Dialogue

        public DialogueDictionary GetDialogueDictionary() { return DialogueDictionary; }

        public void SetDialogueDictionary (DialogueDictionary dialogueDictionary )
        {
            DialogueDictionary = dialogueDictionary;
            ErrorLogger.Trace($"Dialogue dictionary set for ${Name}");
        }

        public void AddDialogueType (string type, Dictionary<string, string[]> dialogue)
        {
            DialogueDictionary.AddType(type, dialogue);
        }
    }
}