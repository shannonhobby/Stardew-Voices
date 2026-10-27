using StardewModdingAPI;
using StardewVoices.Utils;
using System.Threading;
using System;
using StardewValley;
using System.Collections.Generic;
using System.Linq;
using StardewVoices.Audio;
using StardewVoices.Pathing;
using StardewVoices.Dialogue;
using StardewValley.GameData.Characters;

namespace StardewVoices.Characters
{
    public class CharacterManager
    {
        private string characterNameRegex { get; set; }
        private Dictionary<string, Character> characterDictionary = new Dictionary<string, Character>();
        public List<string> SupportedCharacters { get; }

        public CharacterManager(VoicePackManager voicePackManager, PathManager pathManager)
        {
            SupportedCharacters = GetVoicedCharacters(voicePackManager, pathManager);
            SetupCharacterDictionary();
        }

        /// <summary>
        /// The characters the mod voices: every character with installed voices (built-in or a
        /// pack) that also has a dialogue file, since that's what matches on-screen text to a
        /// line id — either the vanilla one in Dialogue Jsons/Characters, or (for a modded NPC,
        /// or extra lines on a vanilla one) a "dialogue/&lt;Name&gt;.json" supplied by whichever
        /// pack is active for that character. Characters with neither are logged and left out.
        /// </summary>
        private static List<string> GetVoicedCharacters(VoicePackManager voicePackManager, PathManager pathManager)
        {
            List<string> characters = new();
            foreach (string voiceFolderName in voicePackManager.Characters.OrderBy(name => name))
            {
                // A pack's folder might be "abigail"; the rest of the mod expects the game's "Abigail".
                string character = Constants.AllCharacterNames.FirstOrDefault(name => string.Equals(name, voiceFolderName, StringComparison.OrdinalIgnoreCase))
                    ?? voiceFolderName;

                bool hasVanillaDialogue = CharacterDialogueHandler.FindDialogueFile(pathManager, character) != null;

                // VoicePackManager's selection is keyed by the same raw folder name as
                // Characters (both built from FolderVoiceSource.Characters), not the
                // canonicalized "character" — but lookups are case-insensitive either way.
                VoicePack activePack = voicePackManager.GetActivePack(voiceFolderName);
                bool hasPackDialogue = activePack?.DialogueFolderPath != null
                    && FileManipulationHelpers.FindFile(activePack.DialogueFolderPath, character + ".json") != null;

                if (hasVanillaDialogue || hasPackDialogue)
                    characters.Add(character);
                else
                    ErrorLogger.Warn($"{character} has voices installed but no dialogue file (in Dialogue Jsons/Characters, or in the active pack's 'dialogue' folder), so their voices won't play.");
            }
            return characters;
        }

        public Character GetCharacter(string characterName)
        {
            if (!characterDictionary.TryGetValue(characterName, out Character character))
            {
                ErrorLogger.Warn($"GetCharacter called for unknown character: {characterName}");
                return null;
            }
            return character;
        }

        public void SetupCharacterInGameNpcs()
        {
            if (Game1.characterData == null)
            {
                ErrorLogger.Warn("Cannot match voiced characters to NPCs: game character data not loaded yet.");
                return;
            }

            foreach (string characterName in SupportedCharacters)
            {
                if (!Game1.characterData.ContainsKey(characterName) && ResolveFormerName(characterName) == null)
                {
                    ErrorLogger.Warn($"'{characterName}' has voices installed but doesn't match any Stardew Valley NPC, so it's skipped.");
                    continue;
                }
                characterDictionary[characterName].SetupNpc();
            }
        }

        /// <summary>
        /// The NPC's current name, if <paramref name="possiblyFormerName"/> is listed as one of
        /// its FormerCharacterNames (Data/Characters, 1.6) rather than being its current key —
        /// e.g. a pack author named their dialogue/voices after an NPC's old name. Null if it
        /// isn't a former name of any NPC, or if character data isn't loaded yet.
        /// </summary>
        internal static string ResolveFormerName(string possiblyFormerName)
        {
            if (Game1.characterData == null)
                return null;

            foreach (KeyValuePair<string, CharacterData> entry in Game1.characterData)
            {
                if (entry.Value.FormerCharacterNames != null && entry.Value.FormerCharacterNames.Contains(possiblyFormerName))
                    return entry.Key;
            }
            return null;
        }

        private void SetupCharacterDictionary()
        {
            for (int i = 0; i < SupportedCharacters.Count; i++)
            {
                this.characterDictionary.Add(SupportedCharacters[i], new Character(SupportedCharacters[i]));
            }
        }

        internal string GetCharacterNameRegex()
        {
            return characterNameRegex;
        }



        public void PopulateCharacterNamesList()
        {
            if (Game1.characterData == null)
            {
                ErrorLogger.Trace($"Error populating character names list: no game character data found?");
                return;
            }

            try
            {
                List<string> characterNames = new List<string>();
                foreach (string character in Game1.characterData.Keys)
                {
                    if (character != "???")
                    {
                        characterNames.Add(character);
                    }
                }


                characterNameRegex = String.Join("|", characterNames);
            }
            catch (Exception ex)
            {
                ErrorLogger.Trace($"Error populating character names list: {ex}");
                return;
            }

        }

    }
}