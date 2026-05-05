using StardewModdingAPI;
using System.Linq;
using System.Threading;

namespace StardewVoices
{
    public class Menu
    {
        public IGenericModConfigMenuApi gmcm = null;
        private ModConfig Config { get; set; }

        public static void SetupGMCM(IModHelper Helper, IManifest ModManifest)
        {
            var api = Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
            ModConfig Config = null;

            if (api == null)
                return;

            api.Register(
                ModManifest,
                reset: () => Config = new ModConfig(),
                save: () => Helper.WriteConfig(Config)
            );

            api.AddTextOption(
            ModManifest,
            name: () => Helper.Translation.Get("settings.language"),
            getValue: () => Config.Language,
            setValue: value =>
            {
                Config.Language = value;
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
                () => this.Config.DialogueVolume,
                value => this.Config.DialogueVolume = value,
                () => Helper.Translation.Get("settings.allVoices"),
                () => Helper.Translation.Get("settings.volumeTooltip"),
                min: 0,
                max: 100,
                interval: 1
            );


            api.AddNumberOption(
                 ModManifest,
                 () => this.Config.AbigailVolume,
                 value => this.Config.AbigailVolume = value,
                 () => $"{GetLocalizedName("Abigail")}",
                 () => $"Volume for {GetLocalizedName("Abigail")} dialogue",
                 min: 0,
                 max: 100,
                 interval: 1
             );

            api.AddNumberOption(
                ModManifest,
                () => this.Config.AlexVolume,
                value => this.Config.AlexVolume = value,
                () => $"{GetLocalizedName("Alex")}",
                () => $"Volume for {GetLocalizedName("Alex")} dialogue",
                min: 0,
                max: 100,
                interval: 1
            );

            api.AddNumberOption(
                ModManifest,
                () => this.Config.ElliottVolume,
                value => this.Config.ElliottVolume = value,
                () => $"{GetLocalizedName("Elliott")}",
                () => $"Volume for {GetLocalizedName("Elliott")} dialogue",
                min: 0,
                max: 100,
                interval: 1
            );

            api.AddNumberOption(
                ModManifest,
                () => this.Config.EmilyVolume,
                value => this.Config.EmilyVolume = value,
                () => $"{GetLocalizedName("Emily")}",
                () => $"Volume for {GetLocalizedName("Emily")} dialogue",
                min: 0,
                max: 100,
                interval: 1
            );

            api.AddNumberOption(
                ModManifest,
                () => this.Config.HaleyVolume,
                value => this.Config.HaleyVolume = value,
                () => $"{GetLocalizedName("Haley")}",
                () => $"Volume for {GetLocalizedName("Haley")} dialogue",
                min: 0,
                max: 100,
                interval: 1
            );

            api.AddNumberOption(
                ModManifest,
                () => this.Config.HarveyVolume,
                value => this.Config.HarveyVolume = value,
                () => $"{GetLocalizedName("Harvey")}",
                () => $"Volume for {GetLocalizedName("Harvey")} dialogue",
                min: 0,
                max: 100,
                interval: 1
            );

            api.AddNumberOption(
                ModManifest,
                () => this.Config.LeahVolume,
                value => this.Config.LeahVolume = value,
                () => $"{GetLocalizedName("Leah")}",
                () => $"Volume for {GetLocalizedName("Leah")} dialogue",
                min: 0,
                max: 100,
                interval: 1
            );

            api.AddNumberOption(
                ModManifest,
                () => this.Config.MaruVolume,
                value => this.Config.MaruVolume = value,
                () => $"{GetLocalizedName("Maru")}",
                () => $"Volume for {GetLocalizedName("Maru")} dialogue",
                min: 0,
                max: 100,
                interval: 1
            );

            api.AddNumberOption(
                ModManifest,
                () => this.Config.PennyVolume,
                value => this.Config.PennyVolume = value,
                () => $"{GetLocalizedName("Penny")}",
                () => $"Volume for {GetLocalizedName("Penny")} dialogue",
                min: 0,
                max: 100,
                interval: 1
            );

            api.AddNumberOption(
                ModManifest,
                () => this.Config.SamVolume,
                value => this.Config.SamVolume = value,
                () => $"{GetLocalizedName("Sam")}",
                () => $"Volume for {GetLocalizedName("Sam")} dialogue",
                min: 0,
                max: 100,
                interval: 1
            );

            api.AddNumberOption(
                ModManifest,
                () => this.Config.SebastianVolume,
                value => this.Config.SebastianVolume = value,
                () => $"{GetLocalizedName("Sebastian")}",
                () => $"Volume for {GetLocalizedName("Sebastian")} dialogue",
                min: 0,
                max: 100,
                interval: 1
            );

            api.AddNumberOption(
                ModManifest,
                () => this.Config.ShaneVolume,
                value => this.Config.ShaneVolume = value,
                () => $"{GetLocalizedName("Shane")}",
                () => $"Volume for {GetLocalizedName("Shane")} dialogue",
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

            AddCastLine(GetLocalizedName("Emily"), "Ariel Michelle");
            AddCastLine(GetLocalizedName("Penny"), "Heidi Tabing");
            AddCastLine(GetLocalizedName("Sebastian"), "David Meyers");
            AddCastLine(GetLocalizedName("Haley"), "Kemi Haydee Stanton");
            AddCastLine(GetLocalizedName("Leah"), "Aiden Dawn");
            AddCastLine(GetLocalizedName("Alex"), "Eamon Kinsman");
            AddCastLine(GetLocalizedName("Sam"), "Jacob Hopkins");
            AddCastLine(GetLocalizedName("Harvey"), "GM Hakim");
            AddCastLine(GetLocalizedName("Maru"), "Nia Celeste");
            AddCastLine(GetLocalizedName("Elliott"), "Emmanuel \"Menji\" Menjivar");
            AddCastLine(GetLocalizedName("Abigail"), "Diana Jurand");
            AddCastLine(GetLocalizedName("Shane"), "Jonathan David Bullock");


        }

    }
}

