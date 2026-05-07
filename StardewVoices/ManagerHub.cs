using StardewModdingAPI;
using StardewVoices.Config;
using StardewVoices.Localization;
using StardewVoices.Pathing;
using StardewVoices.EventManager;
using StardewVoices.Characters;
using StardewVoices.Audio;
using StardewVoices.Dialogue;

namespace StardewVoices
{
    /// <summary>Used to initialize and store global instances of managers.</summary>
    public class ManagerHub
    {
        public PathManager PathManager { get; }
        public LocalizationManager LocalizationManager { get; }
        public Menu Menu { get; }
        public GameEvents GameEventManager { get; }
        public CharacterManager CharacterManager { get; }
        public AudioManager AudioManager { get; }
        public DialogueManager DialogueManager { get; }

        public ManagerHub(IModHelper helper) {
            ModConfig config = helper.ReadConfig<ModConfig>();
            LocalizationManager = new LocalizationManager(config.Language);
            Menu = new Menu(config, LocalizationManager);
            PathManager = new PathManager(helper.DirectoryPath, LocalizationManager);
            GameEventManager = new GameEvents(this);
            CharacterManager = new CharacterManager();
            AudioManager = new AudioManager(CharacterManager, PathManager);
            DialogueManager = new DialogueManager(PathManager, CharacterManager, GameEventManager);
        }
    }
}