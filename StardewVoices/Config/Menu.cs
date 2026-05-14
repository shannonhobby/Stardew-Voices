using StardewModdingAPI;
using System.Linq;
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

        public Menu(ModConfig config, IModHelper helper, IManifest manifest)
        {
            _config = config;
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
                 () => $"{LocalizationHelpers.GetLocalizedName("Abigail")}",
                 () => $"Volume for {LocalizationHelpers.GetLocalizedName("Abigail")} dialogue",
                 min: 0,
                 max: 100,
                 interval: 1
             );

            api.AddNumberOption(
                ModManifest,
                () => _config.AlexVolume,
                value => _config.AlexVolume = value,
                () => $"{LocalizationHelpers.GetLocalizedName("Alex")}",
                () => $"Volume for {LocalizationHelpers.GetLocalizedName("Alex")} dialogue",
                min: 0,
                max: 100,
                interval: 1
            );

            api.AddNumberOption(
                ModManifest,
                () => _config.ElliottVolume,
                value => _config.ElliottVolume = value,
                () => $"{LocalizationHelpers.GetLocalizedName("Elliott")}",
                () => $"Volume for {LocalizationHelpers.GetLocalizedName("Elliott")} dialogue",
                min: 0,
                max: 100,
                interval: 1
            );

            api.AddNumberOption(
                ModManifest,
                () => _config.EmilyVolume,
                value => _config.EmilyVolume = value,
                () => $"{LocalizationHelpers.GetLocalizedName("Emily")}",
                () => $"Volume for {LocalizationHelpers.GetLocalizedName("Emily")} dialogue",
                min: 0,
                max: 100,
                interval: 1
            );

            api.AddNumberOption(
                ModManifest,
                () => _config.HaleyVolume,
                value => _config.HaleyVolume = value,
                () => $"{LocalizationHelpers.GetLocalizedName("Haley")}",
                () => $"Volume for {LocalizationHelpers.GetLocalizedName("Haley")} dialogue",
                min: 0,
                max: 100,
                interval: 1
            );

            api.AddNumberOption(
                ModManifest,
                () => _config.HarveyVolume,
                value => _config.HarveyVolume = value,
                () => $"{LocalizationHelpers.GetLocalizedName("Harvey")}",
                () => $"Volume for {LocalizationHelpers.GetLocalizedName("Harvey")} dialogue",
                min: 0,
                max: 100,
                interval: 1
            );

            api.AddNumberOption(
                ModManifest,
                () => _config.LeahVolume,
                value => _config.LeahVolume = value,
                () => $"{LocalizationHelpers.GetLocalizedName("Leah")}",
                () => $"Volume for {LocalizationHelpers.GetLocalizedName("Leah")} dialogue",
                min: 0,
                max: 100,
                interval: 1
            );

            api.AddNumberOption(
                ModManifest,
                () => _config.MaruVolume,
                value => _config.MaruVolume = value,
                () => $"{LocalizationHelpers.GetLocalizedName("Maru")}",
                () => $"Volume for {LocalizationHelpers.GetLocalizedName("Maru")} dialogue",
                min: 0,
                max: 100,
                interval: 1
            );

            api.AddNumberOption(
                ModManifest,
                () => _config.PennyVolume,
                value => _config.PennyVolume = value,
                () => $"{LocalizationHelpers.GetLocalizedName("Penny")}",
                () => $"Volume for {LocalizationHelpers.GetLocalizedName("Penny")} dialogue",
                min: 0,
                max: 100,
                interval: 1
            );

            api.AddNumberOption(
                ModManifest,
                () => _config.SamVolume,
                value => _config.SamVolume = value,
                () => $"{LocalizationHelpers.GetLocalizedName("Sam")}",
                () => $"Volume for {LocalizationHelpers.GetLocalizedName("Sam")} dialogue",
                min: 0,
                max: 100,
                interval: 1
            );

            api.AddNumberOption(
                ModManifest,
                () => _config.SebastianVolume,
                value => _config.SebastianVolume = value,
                () => $"{LocalizationHelpers.GetLocalizedName("Sebastian")}",
                () => $"Volume for {LocalizationHelpers.GetLocalizedName("Sebastian")} dialogue",
                min: 0,
                max: 100,
                interval: 1
            );

            api.AddNumberOption(
                ModManifest,
                () => _config.ShaneVolume,
                value => _config.ShaneVolume = value,
                () => $"{LocalizationHelpers.GetLocalizedName("Shane")}",
                () => $"Volume for {LocalizationHelpers.GetLocalizedName("Shane")} dialogue",
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

    }
}

