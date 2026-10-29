using Newtonsoft.Json;
using StardewValley;
using StardewVoices.Pathing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StardewVoices.Audio;
using StardewVoices.Characters;
using StardewVoices.Testing;
using StardewVoices.Utils;

namespace StardewVoices.Dialogue
{
    public class CharacterDialogueHandler
    {

        private CharacterManager _characterManager { get; }
        private TestingManager _testingManager { get; }

        public CharacterDialogueHandler(PathManager pathManager, CharacterManager characterManager, VoicePackManager voicePackManager, TestingManager testingManager)
        {
            _characterManager = characterManager;
            _testingManager = testingManager;
            foreach(string characterName in characterManager.SupportedCharacters)
            {
                Characters.Character character = characterManager.GetCharacter(characterName);
                if (character == null) continue;

                LoadDialogueDictionary(pathManager, voicePackManager, characterName, character);

                // Generic dialogue (engagement/marriage/extra/rainy templates) is vanilla content
                // written with vanilla NPCs in mind. A fully modded NPC (no vanilla Dialogue
                // Jsons/Characters file at all) shouldn't be matched against it — their own
                // dialogue text could coincidentally match a generic line and resolve to an id
                // the pack never recorded audio for.
                if (FindDialogueFile(pathManager, characterName) != null)
                {
                    foreach (string dialogueType in Constants.GenericDialogueTypes)
                    {
                        character.AddDialogueType(
                            dialogueType,
                            DialogueHelpers.GetDictionaryFromPath(pathManager.GetPathString(Constants.GenericDialogueTypeToFileName[dialogueType])));
                    }
                }
            }
        }

        /// <summary>
        /// Re-merges vanilla + active-pack dialogue for every supported character. The constructor
        /// only does this once, at mod startup — call this after the player changes a character's
        /// voice in the config menu, or a pack's dialogue additions stay silently stuck on whichever
        /// pack was active at launch even though the audio source itself updates correctly.
        /// </summary>
        public void RefreshPackDialogue(PathManager pathManager, VoicePackManager voicePackManager)
        {
            foreach (string characterName in _characterManager.SupportedCharacters)
            {
                Characters.Character character = _characterManager.GetCharacter(characterName);
                if (character == null) continue;

                LoadDialogueDictionary(pathManager, voicePackManager, characterName, character);
            }
        }

        private void LoadDialogueDictionary(PathManager pathManager, VoicePackManager voicePackManager, string characterName, Characters.Character character)
        {
            string characterDialoguePath = FindDialogueFile(pathManager, characterName);
            DialogueDictionary dialogueDictionary = characterDialoguePath != null
                ? SetupDialogueDictionaryFromJsonPath(characterDialoguePath) ?? new DialogueDictionary()
                : new DialogueDictionary();

            // Merge in dialogue from active content pack if it exists.
            VoicePack activePack = voicePackManager.GetActivePack(characterName);
            if (activePack?.DialogueFolderPath != null)
            {
                string packDialoguePath = FindPackDialogueFile(activePack.DialogueFolderPath, characterName);
                if (packDialoguePath != null)
                {
                    DialogueDictionary packDialogue = SetupDialogueDictionaryFromJsonPath(packDialoguePath);
                    if (packDialogue != null)
                        dialogueDictionary.MergeFrom(packDialogue, characterName);
                }
            }

            character.SetDialogueDictionary(dialogueDictionary);
        }

        /// <summary>
        /// The character's dialogue file in Dialogue Jsons/Characters, or null if there isn't one.
        /// File names vary in case ("abigail.json", "Caroline.json"), so this ignores case.
        /// </summary>
        internal static string FindDialogueFile(PathManager pathManager, string characterName)
        {
            return FileManipulationHelpers.FindFile(pathManager.GetPathString("Characters"), characterName + ".json");
        }

        /// <summary>
        /// The character's dialogue file inside a pack's "dialogue" folder: a "&lt;lang&gt;/" subfolder
        /// version if the game isn't running in English and the pack shipped one (mirroring this
        /// mod's own Dialogue Jsons/&lt;lang&gt;/Characters convention, see sv_parse_pack's translation
        /// support), otherwise the pack's plain "&lt;Character&gt;.json". Null if neither exists.
        /// </summary>
        private static string FindPackDialogueFile(string dialogueFolderPath, string characterName)
        {
            string language = LocalizationHelpers.GetCurrentLanguage();
            if (language != LocalizedContentManager.LanguageCode.en.ToString())
            {
                string languageFolder = FileManipulationHelpers.FindFolder(dialogueFolderPath, language);
                string localizedPath = languageFolder != null ? FileManipulationHelpers.FindFile(languageFolder, characterName + ".json") : null;
                if (localizedPath != null)
                    return localizedPath;
            }

            return FileManipulationHelpers.FindFile(dialogueFolderPath, characterName + ".json");
        }

        private DialogueDictionary SetupDialogueDictionaryFromJsonPath(string path)
        {
            try
            {
                string jsonString = File.ReadAllText(path, Encoding.UTF8);
                return new DialogueDictionary(jsonString);

            }
            catch (Exception ex)
            {
                ErrorLogger.Debug($"Error setting up dialogue dictionary from path {path}.\n{ex} :(");
                return null;
            }
        }


        public string CheckForCharacterDialogueLineId(string currentSpeaker, string dialogue)
        {
            DialogueDictionary dialogueDictionary = _characterManager.GetCharacter(currentSpeaker).GetDialogueDictionary();
            return dialogueDictionary.CheckAllDictionariesForDialogueLine(dialogue, currentSpeaker, _testingManager);
        }

        public string CheckEverydayCharacterDictionariesForDialogue(string currentSpeaker, string dialogue)
        {
            DialogueDictionary currentSpeakerDialogueDict = _characterManager.GetCharacter(currentSpeaker).GetDialogueDictionary();

            string dialogueCheck = currentSpeakerDialogueDict.CheckDictionaryForDialogueLine("dialogue", dialogue, currentSpeaker);
            string miscDialogueCheck = currentSpeakerDialogueDict.CheckDictionaryForDialogueLine("misc", dialogue, currentSpeaker);
            if (dialogueCheck != null)
            {
                return dialogueCheck;
            }
            else if (miscDialogueCheck != null)
            {
                return miscDialogueCheck;
            }
            else
            {
                return null;
            }
            /**else
            {
                closeEnoughKey = DialogueContainsCloseEnoughLine(dialogueDict, dialogue);
                 if (!closeEnoughKey.Contains("Not found"))
                {
                    if (dialogueDict[closeEnoughKey].Length > 1)
                    {
                        return this.getCorrectIdFromListOfLineIds(dialogueDict[closeEnoughKey], currentSpeaker, "Dialogue-");
                    }else
                    {
                        return dialogueDict[closeEnoughKey][0];

                    }
            }
            } */
        }

   

    }
}
 