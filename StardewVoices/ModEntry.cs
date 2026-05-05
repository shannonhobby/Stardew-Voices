using StardewModdingAPI;
using StardewValley;
using StardewVoices.Localization;
using System.IO;

namespace StardewVoices
{
    /// <summary>The mod entry point.</summary>
    public class ModEntry : Mod
    {
        public IModHelper helper = null;

        /// <summary>The mod entry point, called after the mod is first loaded.</summary>
        /// <param name="helper">Provides simplified APIs for writing mods.</param>
        public override void Entry(IModHelper helper)
        {
            modPath = helper.DirectoryPath;
            audioPath = Path.Combine(modPath, "Audio");


            this.helper = helper;

            this.Config = helper.ReadConfig<ModConfig>();


            helper.Events.GameLoop.UpdateTicked += this.OnUpdateTicked;
            helper.Events.Input.ButtonPressed += this.OnButtonPressed;
            helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;

            inverseDialogueFolderPath = Path.Combine(modPath, "Dialogue Jsons");


            currentLanguage = this.Config.Language;


            if (currentLanguage != LocalizedContentManager.LanguageCode.en.ToString())
            {
                inverseDialogueFolderPath = Path.Combine(inverseDialogueFolderPath, currentLanguage);
            }

            this.SetupLocalizedToEnglishMap();


            eventDialogueFolderPath = Path.Combine(inverseDialogueFolderPath, "Events");

            characterDialogueFolderPath = Path.Combine(inverseDialogueFolderPath, "Characters");

            festivalDialogueFolderPath = Path.Combine(inverseDialogueFolderPath, "Festivals");


            festivalDatesJsonPath = Path.Combine(festivalDialogueFolderPath, $"FestivalDates.{LocalizationManager.GetLanguageJsonExtension(currentLanguage)}");



            genericDialogueFolderPath = Path.Combine(inverseDialogueFolderPath, "Generic");
            engagementDialogueJsonPath = Path.Combine(genericDialogueFolderPath, "EngagementDialogue.json");
            extraDialogueJsonPath = Path.Combine(genericDialogueFolderPath, "ExtraDialogue.json");
            genericMarriageDialogueJsonPath = Path.Combine(genericDialogueFolderPath, "Marriage Dialogue Generic.json");
            rainyDialogueJsonPath = Path.Combine(genericDialogueFolderPath, "rainy.json");


            this.supportedCharacters.Add("Shane");
            this.supportedCharacters.Add("Haley");
            this.supportedCharacters.Add("Alex");
            this.supportedCharacters.Add("Penny");
            this.supportedCharacters.Add("Sam");
            this.supportedCharacters.Add("Emily");
            this.supportedCharacters.Add("Maru");
            this.supportedCharacters.Add("Harvey");
            this.supportedCharacters.Add("Elliott");
            this.supportedCharacters.Add("Leah");
            this.supportedCharacters.Add("Sebastian");
            this.supportedCharacters.Add("Abigail");



            this.setupCharacterDictionary();

            this.setupCharacterSoundBanks();

            this.setupFestivalDictionary();

            this.setupDialogueDictionaries();

            this.setupContinuationDialogue();




            dialogueWithRandomNounOrAdj.Add("Last night I dreamt that my left hand had turned into a gigantic %noun. Does that mean anything?", "Dialogue-fall_Thu_chance_1");
            dialogueWithRandomNounOrAdj.Add("I did, but all I remember is that it had something to do with a %adj %noun. Weird.", "Generic Marriage Dialogue-Indoor_Day_3_sv1");

        }

    }
}