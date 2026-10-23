using System;
using System.Collections.Generic;
using System.Linq;
using StardewVoices.Utils;

namespace StardewVoices.Audio
{
    /// <summary>One source a character's voice can come from: the built-in voices or a voice pack.</summary>
    public record VoiceSourceOption(string Id, FolderVoiceSource Source);

    /// <summary>
    /// Picks the one source that voices each character, so a character's lines never mix actors.
    /// </summary>
    internal static class VoiceSourceSelector
    {
        /// <summary>The option id for the voices that ship with Stardew Voices. Pack options use the pack's UniqueID.</summary>
        public const string BuiltInId = "Built-in";

        /// <summary>
        /// Every source that has audio for each character: packs first (alphabetical by name),
        /// then the built-in voices. The first option is the default.
        /// </summary>
        public static Dictionary<string, List<VoiceSourceOption>> GetOptions(FolderVoiceSource builtIn, IEnumerable<VoicePack> packs)
        {
            Dictionary<string, List<VoiceSourceOption>> options = new(StringComparer.OrdinalIgnoreCase);

            IEnumerable<VoicePack> packsInOrder = packs
                .OrderBy(pack => pack.Manifest.Name, StringComparer.OrdinalIgnoreCase)
                .ThenBy(pack => pack.Manifest.UniqueID, StringComparer.OrdinalIgnoreCase);
            foreach (VoicePack pack in packsInOrder)
                AddOption(options, new VoiceSourceOption(pack.Manifest.UniqueID, pack.Source));

            AddOption(options, new VoiceSourceOption(BuiltInId, builtIn));
            return options;
        }

        /// <summary>
        /// The option voicing each character: the player's choice in <paramref name="choices"/>
        /// (character → option id) if it's installed, otherwise the default option.
        /// </summary>
        public static Dictionary<string, VoiceSourceOption> Choose(Dictionary<string, List<VoiceSourceOption>> options, IDictionary<string, string> choices)
        {
            Dictionary<string, VoiceSourceOption> chosen = new(StringComparer.OrdinalIgnoreCase);
            foreach ((string character, List<VoiceSourceOption> characterOptions) in options)
            {
                string choiceId = GetChoiceId(choices, character);
                chosen[character] = characterOptions.FirstOrDefault(option => string.Equals(option.Id, choiceId, StringComparison.OrdinalIgnoreCase))
                    ?? characterOptions[0];
            }
            return chosen;
        }

        /// <summary>
        /// Logs which source voices each character, and anything the player should know: a
        /// character in several packs, or a chosen pack that isn't installed. Run once at startup.
        /// </summary>
        public static void LogChoices(Dictionary<string, List<VoiceSourceOption>> options, Dictionary<string, VoiceSourceOption> chosen, IDictionary<string, string> choices)
        {
            foreach ((string character, VoiceSourceOption option) in chosen)
            {
                List<VoiceSourceOption> characterOptions = options[character];
                string choiceId = GetChoiceId(choices, character);

                if (choiceId != null && !string.Equals(option.Id, choiceId, StringComparison.OrdinalIgnoreCase))
                {
                    ErrorLogger.Warn($"{character}'s chosen voice '{choiceId}' isn't installed. Using {option.Source.Name} instead.");
                }
                else if (choiceId == null && characterOptions.Count(o => o.Id != BuiltInId) > 1)
                {
                    string packNames = string.Join(", ", characterOptions.Where(o => o.Id != BuiltInId).Select(o => o.Source.Name));
                    ErrorLogger.Info($"{character} is in more than one voice pack ({packNames}). Using {option.Source.Name}. You can pick one in the config menu.");
                }

                if (option.Id == BuiltInId)
                    ErrorLogger.Trace($"{character} → {option.Source.Name}");
                else
                    ErrorLogger.Info($"{character} → {option.Source.Name}");
            }
        }

        /// <summary>The player's chosen option id for a character, or null if they haven't chosen.</summary>
        private static string GetChoiceId(IDictionary<string, string> choices, string character)
        {
            if (choices == null)
                return null;

            foreach ((string choiceCharacter, string choiceId) in choices)
            {
                if (string.Equals(choiceCharacter, character, StringComparison.OrdinalIgnoreCase))
                    return choiceId;
            }
            return null;
        }

        private static void AddOption(Dictionary<string, List<VoiceSourceOption>> options, VoiceSourceOption option)
        {
            foreach (string character in option.Source.Characters)
            {
                if (!options.TryGetValue(character, out var characterOptions))
                    options[character] = characterOptions = new List<VoiceSourceOption>();
                characterOptions.Add(option);
            }
        }
    }
}
