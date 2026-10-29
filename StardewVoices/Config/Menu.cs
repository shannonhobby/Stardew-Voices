using StardewModdingAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using StardewVoices.Audio;
using System.Threading;
using StardewVoices.Utils;

namespace StardewVoices.Config
{
    public class Menu
    {
        public IGenericModConfigMenuApi gmcm = null;
        private ModConfig _config { get; set; }
        private IModHelper Helper { get; }
        private IManifest ModManifest { get; }

        /// <summary>Raised whenever a config value changes through the GMCM menu (including reset).</summary>
        public Action OnConfigChanged;

        /// <summary>Every installed voice for each character (see AudioManager.VoiceOptions). Set before the menu is built.</summary>
        internal Dictionary<string, List<VoiceSourceOption>> VoiceOptions { get; set; } = new();

        public Menu(ModConfig config, IModHelper helper, IManifest manifest)
        {
            _config = config;
            Helper = helper;
            ModManifest = manifest;
        }

        // Copies default values onto the existing config instance instead of replacing it,
        // since ManagerHub and other managers hold onto this same ModConfig reference.
        private void ResetConfig()
        {
            ModConfig defaults = new ModConfig();
            foreach (var property in typeof(ModConfig).GetProperties())
            {
                if (property.CanWrite)
                    property.SetValue(_config, property.GetValue(defaults));
            }

            OnConfigChanged?.Invoke();
        }

        public void SetupGMCM()
        {
            var api = Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");

            if (api == null)
                return;

            api.Register(
                ModManifest,
                reset: () => ResetConfig(),
                save: () =>
                {
                    Helper.WriteConfig(_config);
                    // GMCM only calls the setValue callbacks on save, so apply the saved values now.
                    OnConfigChanged?.Invoke();
                }
            );

            api.AddTextOption(
            ModManifest,
            name: () => Helper.Translation.Get("settings.language"),
            getValue: () => _config.Language,
            setValue: value =>
            {
                _config.Language = value;
            },
            allowedValues: Constants.languageDisplayNames.Keys.ToArray(),
             formatAllowedValue: value =>
             Constants.languageDisplayNames.TryGetValue(value, out var name)
                 ? name
                 : value
             );

            api.AddSectionTitle(
                 ModManifest,
                 text: () => $"{Helper.Translation.Get("settings.volume")}========================"
                 );

            api.AddParagraph(
                ModManifest,
                () => Helper.Translation.Get("settings.volumeNote")
                );

            api.AddNumberOption(
                ModManifest,
                () => _config.DialogueVolume,
                value => _config.DialogueVolume = value,
                () => Helper.Translation.Get("settings.voices"),
                () => Helper.Translation.Get("settings.volumeTooltip"),
                min: 0,
                max: 100,
                interval: 1
            );

            AddVoiceChoices(api);

            api.AddSectionTitle(
                ModManifest,
                text: () => Helper.Translation.Get("settings.castList"),
                tooltip: () => Helper.Translation.Get("settings.castListToolTip")
                );

            void AddCastLine(string character, string actor)
            {
                api.AddSubHeader(
                    mod: ModManifest,
                    text: () => $"{character} - {actor}"
                );
            }

            AddCastLine(LocalizationHelpers.GetLocalizedName("Emily"), "Ariel Michelle");
            AddCastLine(LocalizationHelpers.GetLocalizedName("Penny"), "Heidi Tabing");
            AddCastLine(LocalizationHelpers.GetLocalizedName("Sebastian"), "David Meyers");
            AddCastLine(LocalizationHelpers.GetLocalizedName("Haley"), "Kemi Haydee Stanton");
            AddCastLine(LocalizationHelpers.GetLocalizedName("Leah"), "Aiden Dawn");
            AddCastLine(LocalizationHelpers.GetLocalizedName("Alex"), "Eamon Kinsman");
            AddCastLine(LocalizationHelpers.GetLocalizedName("Sam"), "Jacob Hopkins");
            AddCastLine(LocalizationHelpers.GetLocalizedName("Harvey"), "GM Hakim");
            AddCastLine(LocalizationHelpers.GetLocalizedName("Maru"), "Nia Celeste");
            AddCastLine(LocalizationHelpers.GetLocalizedName("Elliott"), "Emmanuel \"Menji\" Menjivar");
            AddCastLine(LocalizationHelpers.GetLocalizedName("Abigail"), "Diana Jurand");
            AddCastLine(LocalizationHelpers.GetLocalizedName("Shane"), "Jonathan David Bullock");


        }

        /// <summary>
        /// One dropdown per character that has more than one voice installed (a voice pack and
        /// the built-in voices, or several packs). Nothing is shown if there's no choice to make.
        /// </summary>
        private void AddVoiceChoices(IGenericModConfigMenuApi api)
        {
            List<string> charactersWithChoices = VoiceOptions
                .Where(entry => entry.Value.Count > 1)
                .Select(entry => entry.Key)
                .OrderBy(character => character)
                .ToList();
            if (charactersWithChoices.Count == 0)
                return;

            api.AddSectionTitle(ModManifest, text: () => Helper.Translation.Get("settings.voicePacks"));
            api.AddParagraph(ModManifest, () => Helper.Translation.Get("settings.voicePacksNote"));

            foreach (string character in charactersWithChoices)
            {
                List<VoiceSourceOption> options = VoiceOptions[character];
                api.AddTextOption(
                    ModManifest,
                    // Characters the player hasn't chosen for show their default, the first option.
                    getValue: () => _config.CharacterVoices.TryGetValue(character, out string choice) && options.Any(o => o.Id == choice)
                        ? choice
                        : options[0].Id,
                    setValue: value => _config.CharacterVoices[character] = value,
                    name: () => LocalizationHelpers.GetLocalizedName(character),
                    allowedValues: options.Select(o => o.Id).ToArray(),
                    formatAllowedValue: id => id == VoiceSourceSelector.BuiltInId
                        ? Helper.Translation.Get("settings.builtInVoices")
                        : options.First(o => o.Id == id).Source.Name
                );
            }
        }

    }
}

