using StardewModdingAPI;
using StardewValley;
using StardewVoices.Testing;
using StardewVoices.FileGeneration;
using StardewVoices.Utils;
using System.IO;
using StardewVoices.Config;

namespace StardewVoices
{
    /// <summary>The mod entry point.</summary>
    public class ModEntry : Mod
    {
        public IModHelper helper = null;
        public ManagerHub managerHub = null;
        internal FileGenerator fileGenerator = null;



        /// <summary>The mod entry point, called after the mod is first loaded.</summary>
        /// <param name="helper">Provides simplified APIs for writing mods.</param>
        public override void Entry(IModHelper helper)
        {
            ErrorLogger.Init(this.Monitor);
            this.helper = helper;

            managerHub = new ManagerHub(helper, this.ModManifest);

            DialogueTestingManager.Init(helper, managerHub);
            fileGenerator = new FileGenerator(helper, managerHub);


            helper.ConsoleCommands.Add(
                "sv_validation",
                "Runs dialogue voice validation.",
                RunVoiceValidationCommand
            );

            helper.ConsoleCommands.Add(
              "sv_generation",
              "Generates Dialogue JSONs and Scripts." +
              "\n\nUsage: sv_generate_files [filesToGenerate] [scriptFormat]" +
              "\n- filesToGenerate: \"json\", \"script\", \"both\" (default: both)" +
              "\n- scriptFormat: \"txt\", \"md\", \"both\" (default: both)" +
              "\n- generateGossipDialogue: \"yes\", \"no\" (default: yes)",
              GenerateFiles
          );



            helper.Events.GameLoop.UpdateTicked += managerHub.GameEventManager.OnUpdateTicked;
            helper.Events.Input.ButtonPressed += managerHub.GameEventManager.OnButtonPressed;
            helper.Events.GameLoop.GameLaunched += managerHub.GameEventManager.OnGameLaunched;
            helper.Events.GameLoop.SaveLoaded += managerHub.GameEventManager.OnSaveLoaded;
        }

        private void RunVoiceValidationCommand(
            string command,
            string[] args
        )
        {
            DialogueTestingManager.RunValidation();

        }

        private void GenerateFiles(
            string command,
            string[] args
        )
        {
            fileGenerator.GenerateFiles(true, true, true, "both", helper.ReadConfig<ModConfig>());
        }

    }
}