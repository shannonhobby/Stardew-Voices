using System.Collections.Generic;
using System.Linq;
using StardewModdingAPI;
using StardewVoices.Config;
using StardewVoices.Utils;
using StardewVoices.Pathing;
using StardewVoices.EventManager;
using StardewVoices.Characters;
using StardewVoices.Audio;
using StardewVoices.Dialogue;
using StardewVoices.Testing;

namespace StardewVoices
{
    /// <summary>Used to initialize and store global instances of managers.</summary>
    public class ManagerHub
    {
        public PathManager PathManager { get; }
        public Menu Menu { get; }
        public GameEventsService GameEventManager { get; }
        public CharacterManager CharacterManager { get; }
        public AudioManager AudioManager { get; }
        public List<VoicePack> VoicePacks { get; }
        public DialogueManager DialogueManager { get; }
        public TestingManager TestingManager { get; }
        public VariableLineResolver VariableLineResolver { get; }

        public ManagerHub(IModHelper helper, IManifest manifest) {
            ModConfig config = helper.ReadConfig<ModConfig>();
            LocalizationHelpers.Init(config.Language);
            Menu = new Menu(config, helper, manifest);
            PathManager = new PathManager(helper.DirectoryPath);
            VoicePacks = VoicePackLoader.LoadAll(helper.ContentPacks.GetOwned());
            AudioManager = new AudioManager(PathManager, VoicePacks, config.CharacterVoices);
            CharacterManager = new CharacterManager(GetVoicedCharacters());
            DialogueHelpers.Init(CharacterManager.SupportedCharacters, config.CharactersToGenerate);
            TestingManager = new TestingManager(CharacterManager, config);
            VariableLineResolver = new VariableLineResolver(PathManager.GetPathString("variable_lines.json"));
            DialogueManager = new DialogueManager(PathManager, CharacterManager, TestingManager, VariableLineResolver);

            GameEventManager = new GameEventsService(this, config);
            Menu.VoiceOptions = AudioManager.VoiceOptions;
            Menu.OnConfigChanged = () =>
            {
                GameEventManager.UpdateVoiceVolume();
                AudioManager.ApplyVoiceChoices(config.CharacterVoices);
            };

        }

        /// <summary>
        /// The characters the mod voices: every character with installed voices (built-in or a
        /// pack) that also has a dialogue file, since that's what matches on-screen text to a
        /// line id. Characters without one are logged and left out.
        /// </summary>
        private List<string> GetVoicedCharacters()
        {
            List<string> characters = new();
            foreach (string voiceFolderName in AudioManager.Resolver.Characters.OrderBy(name => name))
            {
                // A pack's folder might be "abigail"; the rest of the mod expects the game's "Abigail".
                string character = Constants.AllCharacterNames.FirstOrDefault(name => string.Equals(name, voiceFolderName, System.StringComparison.OrdinalIgnoreCase))
                    ?? voiceFolderName;

                if (CharacterDialogueHandler.FindDialogueFile(PathManager, character) != null)
                    characters.Add(character);
                else
                    ErrorLogger.Warn($"{character} has voices installed but no dialogue file in Dialogue Jsons/Characters, so their voices won't play.");
            }
            return characters;
        }
    }
}