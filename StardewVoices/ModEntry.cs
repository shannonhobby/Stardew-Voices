using StardewModdingAPI;
using StardewValley;
using StardewVoices.Testing;
using StardewVoices.FileGeneration;
using StardewVoices.Utils;
using System.IO;
using StardewVoices.Config;
using System;
using StardewValley.Menus;

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
                "sv_play_line",
                "Looks up and plays a voiced dialogue line for a character.\n" +
                "Usage: sv_play_line <characterName> <dialogueString...>\n" +
                "Example: sv_play_line Alex Hi there, farmer!",
                PlayDialogueLine
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

        private void PlayDialogueLine(string command, string[] args)
        {
            if (!Context.IsWorldReady)
            {
                ErrorLogger.Warn("[sv_play_line] A save must be loaded to use this command.");
                return;
            }

            if (args.Length < 2)
            {
                ErrorLogger.Info("[sv_play_line] Usage: sv_play_line <characterName> <dialogueString...>");
                return;
            }

            string characterName = args[0];
            string dialogueString = string.Join(" ", args, 1, args.Length - 1);

            string lineId = managerHub.DialogueManager.getIdFromDialogueString(dialogueString, characterName);
            ErrorLogger.Info($"[sv_play_line] {characterName}: {lineId}");

            NPC npc = Game1.getCharacterFromName(characterName);

            // Mirror Game1.drawDialogue but create the DialogueBox from a raw string
            // instead of peeking the NPC's CurrentDialogue stack.
            Game1.activeClickableMenu = new DialogueBox(dialogueString);
            Game1.dialogueUp = true;
            if (!Game1.eventUp)
            {
                Game1.player.Halt();
                Game1.player.CanMove = false;
            }
            if (npc != null)
                Game1.currentSpeaker = npc;

            // currentSpeaker must be set for UpdateCurrentDialogue to play audio.
            // If the NPC wasn't found, fall back to playing the cue directly.
            if (npc == null && !lineId.Contains("not found"))
            {
                ErrorLogger.Warn($"[sv_play_line] NPC '{characterName}' not found — playing audio directly.");
                managerHub.AudioManager.Play(lineId, characterName);
            }
        }

        private void GenerateFiles(string command, string[] args)
        {
            string filesToGenerate      = args.Length > 0 ? args[0].ToLower() : "both";
            string scriptFormat         = args.Length > 1 ? args[1].ToLower() : "both";
            string gossipArg            = args.Length > 2 ? args[2].ToLower() : "yes";

            bool generateJson           = filesToGenerate == "json"   || filesToGenerate == "both";
            bool generateScript         = filesToGenerate == "script" || filesToGenerate == "both";
            bool generateGossipDialogue = gossipArg != "no";

            fileGenerator.GenerateFiles(generateJson, generateScript, generateGossipDialogue, scriptFormat, helper.ReadConfig<ModConfig>());
        }

    }
}