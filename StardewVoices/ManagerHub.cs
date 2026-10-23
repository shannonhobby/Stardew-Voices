using System.Collections.Generic;
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
            CharacterManager = new CharacterManager(AudioManager, PathManager);
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
    }
}