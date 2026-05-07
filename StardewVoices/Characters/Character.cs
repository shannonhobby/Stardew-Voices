using StardewValley;
using StardewVoices.Audio;
using StardewVoices.Dialogue;
using System.Collections.Generic;

namespace StardewVoices.Characters
{ 
    public class Character
    {
        private string Name { get; }
        private NPC InGameNPC { get; }

        private CharacterAudio Audio { get; set; }
        private DialogueDictionary DialogueDictionary { get; set; }
        public Character(string name)
        {
            Name = name;
            InGameNPC = Game1.getCharacterFromName(name);
        }

        public string GetName() {  return Name; }
        public NPC GetNPC() { return InGameNPC; }

        public void SetVolume( int volume )
        {
            Audio.SetVolume(volume);
        }

        public DialogueDictionary GetDialogueDictionary() { return DialogueDictionary; }

        public void SetDialogueDictionary (DialogueDictionary dialogueDictionary )
        {
            DialogueDictionary = dialogueDictionary;
        }

        public void AddDialogueType (string type, Dictionary<string, string[]> dialogue)
        {
            DialogueDictionary.AddType(type, dialogue); 
        }

        internal void SetCharacterAudio(CharacterAudio audio)
        {
            Audio = audio;
        }
    }
}