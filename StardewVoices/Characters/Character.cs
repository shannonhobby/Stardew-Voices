using StardewValley;
using StardewVoices.Audio;
using StardewVoices.DialogueManager;

namespace StardewVoices
{ 
    public class Character
    {
        private string Name { get; }
        private NPC InGameNPC { get; }

        private CharacterAudio Audio { get; }
        private DialogueDictionary DialogueDictionary { get; }
        public Character(string name)
        {
            Name = name;
            Audio = new CharacterAudio();
            InGameNPC = Game1.getCharacterFromName(name);
        }

        public string GetName() {  return Name; }
        public NPC GetNPC() { return InGameNPC; }

        public void SetVolume(int volume )
        {
            Audio.SetVolume(volume);
        }

    }
}