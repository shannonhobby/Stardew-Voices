using StardewValley;
using StardewVoices.Audio;
using StardewVoices.Dialogue;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Audio;

namespace StardewVoices.Characters
{ 
    public class Character
    {
        private string Name { get; }
        private NPC InGameNPC { get; set; }

        public CharacterAudio Audio { get; set; }
        private DialogueDictionary DialogueDictionary { get; set; }
        public Character(string name)
        {
            Name = name;
            InGameNPC = Game1.getCharacterFromName(name);
        }

        public string GetName() {  return Name; }
        public NPC GetNPC() { return InGameNPC; }

 
        // Dialogue

        public DialogueDictionary GetDialogueDictionary() { return DialogueDictionary; }

        public SoundBank GetSoundBank() { return Audio.SoundBank; }

        public void SetDialogueDictionary (DialogueDictionary dialogueDictionary )
        {
            DialogueDictionary = dialogueDictionary;
        }

        public void AddDialogueType (string type, Dictionary<string, string[]> dialogue)
        {
            DialogueDictionary.AddType(type, dialogue); 
        }

        // Audio

        internal void SetCharacterAudio(CharacterAudio audio)
        {
            Audio = audio;
        }

        public void SetVolume(int volume)
        {
            Audio.SetVolume(volume);
        }

        public void Play(string lineId)
        {
            Audio.Play(lineId);
        }

    }
}