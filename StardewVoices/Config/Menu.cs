using StardewModdingAPI;
using System.Linq;
using System.Threading;
using StardewVoices.Localization;

namespace StardewVoices.Config
{
    public class Menu
    {
        public IGenericModConfigMenuApi gmcm = null;
        private ModConfig _config { get; set; }
        private LocalizationManager _localizationManager { get; }
        private IModHelper Helper { get; }
        private IManifest ModManifest { get; }

        public Menu(ModConfig config, LocalizationManager localization, IModHelper helper, IManifest manifest)
        {
            _config = config;
            _localizationManager = localization;
            Helper = helper;
            ModManifest = manifest;
        }

        public void SetupGMCM()
        {
            var api = Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");

            if (api == null)
                return;

            api.Register(
                ModManifest,
                reset: () => _config = new ModConfig(),
                save: () => Helper.WriteConfig(_config)
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

            api.AddParagraph(
                ModManifest,
                () => Helper.Translation.Get("settings.languageWarning")
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
                () => Helper.Translation.Get("settings.allVoices"),
                () => Helper.Translation.Get("settings.volumeTooltip"),
                min: 0,
                max: 100,
                interval: 1
            );


            api.AddNumberOption(
                 ModManifest,
                 () => _config.AbigailVolume,
                 value => _config.AbigailVolume = value,
                 () => $"{_localizationManager.GetLocalizedName("Abigail")}",
                 () => $"Volume for {_localizationManager.GetLocalizedName("Abigail")} dialogue",
                 min: 0,
                 max: 100,
                 interval: 1
             );

            api.AddNumberOption(
                ModManifest,
                () => _config.AlexVolume,
                value => _config.AlexVolume = value,
                () => $"{_localizationManager.GetLocalizedName("Alex")}",
                () => $"Volume for {_localizationManager.GetLocalizedName("Alex")} dialogue",
                min: 0,
                max: 100,
                interval: 1
            );

            api.AddNumberOption(
                ModManifest,
                () => _config.ElliottVolume,
                value => _config.ElliottVolume = value,
                () => $"{_localizationManager.GetLocalizedName("Elliott")}",
                () => $"Volume for {_localizationManager.GetLocalizedName("Elliott")} dialogue",
                min: 0,
                max: 100,
                interval: 1
            );

            api.AddNumberOption(
                ModManifest,
                () => _config.EmilyVolume,
                value => _config.EmilyVolume = value,
                () => $"{_localizationManager.GetLocalizedName("Emily")}",
                () => $"Volume for {_localizationManager.GetLocalizedName("Emily")} dialogue",
                min: 0,
                max: 100,
                interval: 1
            );

            api.AddNumberOption(
                ModManifest,
                () => _config.HaleyVolume,
                value => _config.HaleyVolume = value,
                () => $"{_localizationManager.GetLocalizedName("Haley")}",
                () => $"Volume for {_localizationManager.GetLocalizedName("Haley")} dialogue",
                min: 0,
                max: 100,
                interval: 1
            );

            api.AddNumberOption(
                ModManifest,
                () => _config.HarveyVolume,
                value => _config.HarveyVolume = value,
                () => $"{_localizationManager.GetLocalizedName("Harvey")}",
                () => $"Volume for {_localizationManager.GetLocalizedName("Harvey")} dialogue",
                min: 0,
                max: 100,
                interval: 1
            );

            api.AddNumberOption(
                ModManifest,
                () => _config.LeahVolume,
                value => _config.LeahVolume = value,
                () => $"{_localizationManager.GetLocalizedName("Leah")}",
                () => $"Volume for {_localizationManager.GetLocalizedName("Leah")} dialogue",
                min: 0,
                max: 100,
                interval: 1
            );

            api.AddNumberOption(
                ModManifest,
                () => _config.MaruVolume,
                value => _config.MaruVolume = value,
                () => $"{_localizationManager.GetLocalizedName("Maru")}",
                () => $"Volume for {_localizationManager.GetLocalizedName("Maru")} dialogue",
                min: 0,
                max: 100,
                interval: 1
            );

            api.AddNumberOption(
                ModManifest,
                () => _config.PennyVolume,
                value => _config.PennyVolume = value,
                () => $"{_localizationManager.GetLocalizedName("Penny")}",
                () => $"Volume for {_localizationManager.GetLocalizedName("Penny")} dialogue",
                min: 0,
                max: 100,
                interval: 1
            );

            api.AddNumberOption(
                ModManifest,
                () => _config.SamVolume,
                value => _config.SamVolume = value,
                () => $"{_localizationManager.GetLocalizedName("Sam")}",
                () => $"Volume for {_localizationManager.GetLocalizedName("Sam")} dialogue",
                min: 0,
                max: 100,
                interval: 1
            );

            api.AddNumberOption(
                ModManifest,
                () => _config.SebastianVolume,
                value => _config.SebastianVolume = value,
                () => $"{_localizationManager.GetLocalizedName("Sebastian")}",
                () => $"Volume for {_localizationManager.GetLocalizedName("Sebastian")} dialogue",
                min: 0,
                max: 100,
                interval: 1
            );

            api.AddNumberOption(
                ModManifest,
                () => _config.ShaneVolume,
                value => _config.ShaneVolume = value,
                () => $"{_localizationManager.GetLocalizedName("Shane")}",
                () => $"Volume for {_localizationManager.GetLocalizedName("Shane")} dialogue",
                min: 0,
                max: 100,
                interval: 1
            );


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

            AddCastLine(_localizationManager.GetLocalizedName("Emily"), "Ariel Michelle");
            AddCastLine(_localizationManager.GetLocalizedName("Penny"), "Heidi Tabing");
            AddCastLine(_localizationManager.GetLocalizedName("Sebastian"), "David Meyers");
            AddCastLine(_localizationManager.GetLocalizedName("Haley"), "Kemi Haydee Stanton");
            AddCastLine(_localizationManager.GetLocalizedName("Leah"), "Aiden Dawn");
            AddCastLine(_localizationManager.GetLocalizedName("Alex"), "Eamon Kinsman");
            AddCastLine(_localizationManager.GetLocalizedName("Sam"), "Jacob Hopkins");
            AddCastLine(_localizationManager.GetLocalizedName("Harvey"), "GM Hakim");
            AddCastLine(_localizationManager.GetLocalizedName("Maru"), "Nia Celeste");
            AddCastLine(_localizationManager.GetLocalizedName("Elliott"), "Emmanuel \"Menji\" Menjivar");
            AddCastLine(_localizationManager.GetLocalizedName("Abigail"), "Diana Jurand");
            AddCastLine(_localizationManager.GetLocalizedName("Shane"), "Jonathan David Bullock");


        }

    }
}

