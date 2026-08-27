using StardewModdingAPI;
using StardewValley;
using StardewValley.GameData.Characters;
using StardewVoices.Dialogue;
using StardewVoices.Utils.ParsingHelpers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace StardewVoices.Utils.GameDialogueExtraction
{
    public class CharacterDialogueExtractor
    {
        private readonly IModHelper helper;
        private readonly bool isEnglish =
            LocalizationHelpers.GetCurrentLanguage() == LocalizedContentManager.LanguageCode.en.ToString();

        private readonly Dictionary<string, List<string>> stringsFromCsFileRules = new()
        {
            { "Abigail",  new() { "AbigailGame.cs.11896-11917", "ItemDeliveryQuest.cs.13597-13602" } },
            { "Maru",     new() { "ItemDeliveryQuest.cs.13585-13587" } },
            { "Sam",      new() { "ItemDeliveryQuest.cs.13577" } },
            { "Haley",    new() { "ItemDeliveryQuest.cs.13566" } },
            { "Elliott",  new() { "Event.cs.1257-1260" } },
            { "Grandpa",  new() { "Event.cs.1306-1318", "GrandpaStory.cs.12026-12055" } },
            { "Marnie",   new() { "Event.cs.1798", "PurchaseAnimalsMenu.cs.11311-11314", "ShopMenu.cs.11507-11508" } },
            { "Gus",      new() { "ShopMenu.cs.11512-11514" } },
            { "Marlon",   new() { "ShopMenu.cs.11517-11525", "SlayMonsterQuest.cs.13730-13744",
                                  "ItemRecovery_Description", "ItemRecovery_Engaged", "ItemRecovery_Engaged_Stack" } },
            { "Robin",    new() { "NPC.cs.3926-3927", "ShopMenu.cs.11460-11464", "ResourceCollectionQuest.cs.13677-13683" } },
            { "Clint",    new() { "ShopMenu.cs.11469-11474", "ResourceCollectionQuest.cs.13655-13673", "Tool.cs.14317" } },
            { "Willy",    new() { "ShopMenu.cs.11477-11478", "FishingQuest.cs.13256-13268" } },
            { "Pierre",   new() { "ShopMenu.cs.11481-11489" } },
            { "Wizard",   new() { "ItemDeliveryQuest.cs.13555", "SlayMonsterQuest.cs.13760" } },
            { "Krobus",   new() { "NPC.cs.3990", "Krobus_Engaged", "Krobus_AfterWedding" } },
            { "Lewis",    new() { "Utility.cs.5367-5379" } },
            { "EVERYONE", new() {
                "ItemDeliveryQuest.cs.13526-13528",
                "ItemDeliveryQuest.cs.13530-13544",
                "NPC.cs.4274-4281",
                "NPC.cs.4293-4294" } },
            { "WIFE ONLY", new() {
                // I'm pregnant
                "NPC.cs.4442",
                "NPC.cs.4443",
                "NPC.cs.4445"
            } },
            { "HUSBAND ONLY", new() { 
                // You're pregnant
                "NPC.cs.4448"
            } },
            { "LOVE INTERESTS", new() {
                "Event.cs.1632-1635",
                "Game1.cs.2782",
                // Rejection / accept proposals
                "NPC.cs.3956-3980",
                "NPC.cs.3985",
                "NPC.cs.4001",
                "NPC.cs.4406-4441",
                "NPC.cs.4444",
                "NPC.cs.4446-4447",
                "NPC.cs.4449-4500",
                "MultiplePetBowls_watered" } },
            { "CHILDREN", new() { "Event.cs.1497-1498" } },
        };

        private readonly BookPlaceholderResolver bookPlaceholderResolver;

        public CharacterDialogueExtractor(IModHelper helper)
        {
            this.helper = helper;
            this.bookPlaceholderResolver = new BookPlaceholderResolver(helper);
        }


        // ── Public API ───────────────────────────────────────────────────────

        /// <summary>
        /// Returns all dialogue for a character grouped by type. Each
        /// LabelledDialogue holds the original StardewValley.Dialogue (for
        /// game-engine consumers) and a parsed list of LabelledDialogueLines
        /// (for file generation / audio lookup).
        /// </summary>
        public Dictionary<DialogueType, List<LabelledDialogue>> GetAllDialogueForCharacter(NPC character)
        {
            if (character == null) { return new Dictionary<DialogueType, List<LabelledDialogue>>(); }

            if (character.GetData().CanBeRomanced)
            {
                return new Dictionary<DialogueType, List<LabelledDialogue>>
                {
                    { DialogueType.Dialogue,        GetCharacterDialogue(character) },
                    { DialogueType.Engagement,      GetEngagementDialogue(character) },
                    { DialogueType.Rainy,           GetRainyDialogue(character) },
                    { DialogueType.Marriage,        GetMarriageDialogue(character) },
                    { DialogueType.Extra,           GetExtraDialogue(character) },
                    { DialogueType.GenericMarriage, GetGenericMarriageDialogue(character) },
                    { DialogueType.Generic,         GetGenericDialogue(character) },
                    { DialogueType.GiftTastes,      GetGiftTasteDialogue(character) },
                    { DialogueType.Schedule,        GetScheduleDialogue(character) },
                    { DialogueType.SpeechBubble,    GetSpeechBubbleDialogue(character) },
                };
            } else if (character.Name == "Krobus") {
                return new Dictionary<DialogueType, List<LabelledDialogue>>
                {
                    { DialogueType.Dialogue,        GetCharacterDialogue(character) },
                    { DialogueType.Rainy,           GetRainyDialogue(character) },
                    { DialogueType.Extra,           GetExtraDialogue(character) },
                    { DialogueType.Generic,         GetGenericDialogue(character) },
                    { DialogueType.Marriage,        GetMarriageDialogue(character) },
                    { DialogueType.SpeechBubble,    GetSpeechBubbleDialogue(character) },
                    { DialogueType.GiftTastes,      GetGiftTasteDialogue(character) },

                };
            }
            else
            {
                return new Dictionary<DialogueType, List<LabelledDialogue>>
                {
                    { DialogueType.Dialogue,        GetCharacterDialogue(character) },
                    { DialogueType.Rainy,           GetRainyDialogue(character) },
                    { DialogueType.Extra,           GetExtraDialogue(character) },
                    { DialogueType.Generic,         GetGenericDialogue(character) },
                    { DialogueType.SpeechBubble,    GetSpeechBubbleDialogue(character) },
                    { DialogueType.GiftTastes,      GetGiftTasteDialogue(character) },

                };
            }

      
        }


        // ── Dialogue type extractors ─────────────────────────────────────────

        private List<LabelledDialogue> GetCharacterDialogue(NPC character)
        {
            var lines = GetLabelledDialogueFromGameContent(
                character, $"Characters/Dialogue/{LocalizationHelpers.GetName(character?.getName())}", DialogueType.Dialogue);

            if (lines.Count == 0)
                ErrorLogger.Warn($"No character dialogue found for {character?.Name}.");

            return lines;
        }

        private List<LabelledDialogue> GetEngagementDialogue(NPC character)
        {
            try
            {
                Dictionary<string, string> data =
                    helper.GameContent.Load<Dictionary<string, string>>("Data/EngagementDialogue");

                var lines = new List<LabelledDialogue>();
                foreach (string key in new[] { $"{character.Name}0", $"{character.Name}1" })
                {
                    if (data.TryGetValue(key, out string rawValue))
                        lines.Add(BuildLabelledDialogue(character, key, rawValue, DialogueType.Engagement));
                }
                return lines;
            }
            catch (Exception e)
            {
                ErrorLogger.Error($"Error getting engagement dialogue for {character.Name}. {e}");
                return new List<LabelledDialogue>();
            }
        }

        private List<LabelledDialogue> GetRainyDialogue(NPC character)
        {
            try
            {
                Dictionary<string, string> data =
                    helper.GameContent.Load<Dictionary<string, string>>("Characters\\Dialogue\\rainy");

                string sheetName = character.GetDialogueSheetName();
                if (!data.TryGetValue(sheetName, out string rawValue))
                    return new List<LabelledDialogue>();

                // Rainy uses a static TryGetDialogue overload with a colon-separated path
                var sdvDialogue = StardewValley.Dialogue.TryGetDialogue(
                    character, "Characters\\Dialogue\\rainy:" + sheetName);
                var labelledLines = CharacterDialogueParser.ParseRawValueToLines(rawValue, $"rainy_{sheetName}", DialogueType.Rainy);

                return new List<LabelledDialogue>
                {
                    new LabelledDialogue(sdvDialogue, labelledLines, sheetName, DialogueType.Rainy)
                };
            }
            catch (Exception e)
            {
                ErrorLogger.Error($"Error getting rainy dialogue for {character.Name}. {e}");
                return new List<LabelledDialogue>();
            }
        }

        private List<LabelledDialogue> GetMarriageDialogue(NPC character)
        {
            try
            {
                var lines = GetLabelledDialogueFromGameContent(
                    character, $"Characters/Dialogue/MarriageDialogue{character.Name}", DialogueType.Marriage);

                if (lines.Count == 0)
                    ErrorLogger.Warn($"No marriage dialogue found for {character.Name}.");

                return lines;
            }
            catch (Exception e)
            {
                ErrorLogger.Error($"Error getting marriage dialogue for {character.Name}. {e}");
                return new List<LabelledDialogue>();
            }
        }

        private List<LabelledDialogue> GetGenericMarriageDialogue(NPC character)
        {
            try
            {
                var lines = GetLabelledLinesWithoutCharacterNameInId(
                    character, "Characters/Dialogue/MarriageDialogue", DialogueType.GenericMarriage);

                // A character-specific file fully shadows the generic line for the same
                // id — the game will never show it, so it must not be scripted/voiced as
                // this character's generic dialogue either.
                var overriddenIds = new HashSet<string>(
                    GetMarriageDialogueOverrides(character.Name).Select(o => o.DialogueId));

                return lines.Where(line => !overriddenIds.Contains(line.OriginalKey)).ToList();
            }
            catch (Exception e)
            {
                ErrorLogger.Error($"Error getting generic marriage dialogue for {character.Name}. {e}");
                return new List<LabelledDialogue>();
            }
        }

        /// <summary>
        /// Finds marriage dialogue IDs that exist in both the generic MarriageDialogue.json
        /// and <paramref name="characterName"/>'s own MarriageDialogue{characterName}.json —
        /// an exact (non-prefix, non-substring) key match. The character-specific entry
        /// always shadows the generic one for that id, so the generic line is never
        /// shown/heard for this character.
        /// </summary>
        public List<MarriageDialogueOverride> GetMarriageDialogueOverrides(string characterName)
        {
            Dictionary<string, string> generic = TryLoadDialogueDict("Characters/Dialogue/MarriageDialogue");
            Dictionary<string, string> characterFile = TryLoadDialogueDict($"Characters/Dialogue/MarriageDialogue{characterName}");

            if (generic == null || generic.Count == 0 || characterFile == null)
                return new List<MarriageDialogueOverride>();

            var overrides = new List<MarriageDialogueOverride>();
            foreach (var (id, characterText) in characterFile)
            {
                if (generic.TryGetValue(id, out string genericText))
                    overrides.Add(new MarriageDialogueOverride(id, genericText, characterText));
            }

            return overrides;
        }

        private Dictionary<string, string> TryLoadDialogueDict(string path)
        {
            try
            {
                return helper.GameContent.Load<Dictionary<string, string>>(path);
            }
            catch
            {
                return null;
            }
        }

        private List<LabelledDialogue> GetGenericDialogue(NPC character)
        {
            try
            {
                var lines = GetMovieDialogue(character);
                if (lines.Count == 0)
                    ErrorLogger.Warn($"No movie dialogue found for {character.Name}.");

                lines.AddRange(Get16Strings(character));
                lines.AddRange(GetStringsFromCharactersFiles(character));
                lines.AddRange(GetLinesFromStringsFromCsFile(character));

                return lines;
            }
            catch (Exception e)
            {
                ErrorLogger.Error($"Error getting generic dialogue for {character.Name}. {e}");
                return new List<LabelledDialogue>();
            }
        }

        private List<LabelledDialogue> GetMovieDialogue(NPC character)
        {
            var lines = GetLabelledLinesWithCharacterNameInId(
                character, "Strings/MovieReactions", DialogueType.Generic);

            if (lines.Count == 0)
                ErrorLogger.Warn($"No movie reaction dialogue found for {character.Name}.");

            return lines;
        }

        private List<LabelledDialogue> GetExtraDialogue(NPC character)
        {
            try
            {
                Dictionary<string, string> data =
                    helper.GameContent.Load<Dictionary<string, string>>("Data/ExtraDialogue");

                var lines = new List<LabelledDialogue>();
                foreach (var (key, value) in data)
                {
                    if (StringContainsCharacter(key, character.getName())
                        || StringContainsSpouse(key)
                        || key.Contains("NewChild")
                        || key.Contains("LostItemQuest")
                        || key.Equals("Town_DumpsterDiveComment_Teen") && character.GetData().Age == NpcAge.Teen
                        || key.Equals("Town_DumpsterDiveComment_Adult") && character.GetData().Age == NpcAge.Adult
                        || key.Equals("Town_DumpsterDiveComment_Child") && character.GetData().Age == NpcAge.Child)
                    {
                        lines.Add(BuildLabelledDialogue(character, key, value, DialogueType.Generic));
                    }
                }
                return lines;
            }
            catch (Exception e)
            {
                ErrorLogger.Warn($"Could not load ExtraDialogue for {character.Name}. {e}");
                return new List<LabelledDialogue>();
            }
        }

  
        private List<LabelledDialogue> Get16Strings(NPC character)
        {
            var lines = GetLabelledLinesWithCharacterNameInId(
                character, "Strings/1_6_Strings", DialogueType.Generic);

            if (lines.Count == 0)
                ErrorLogger.Warn($"No 1.6 dialogue found for {character.Name}.");

            return lines;
        }

        private List<LabelledDialogue> GetStringsFromCharactersFiles(NPC character)
        {
            try
            {
                Dictionary<string, string> data =
                    helper.GameContent.Load<Dictionary<string, string>>("Strings/Characters");

                var lines = new List<LabelledDialogue>();
                foreach (var (key, value) in data)
                {
                    if (ShouldAddLineFromCharactersFiles(key, character))
                        lines.Add(BuildLabelledDialogue(character, key, value, DialogueType.Generic));
                }
                return lines;
            }
            catch (Exception e)
            {
                ErrorLogger.Warn($"Could not load 'Strings/Characters' for {character.Name}. {e}");
                return new List<LabelledDialogue>();
            }
        }

        private List<LabelledDialogue> GetLinesFromStringsFromCsFile(NPC character)
        {
            try
            {
                Dictionary<string, string> data =
                    helper.GameContent.Load<Dictionary<string, string>>("Strings/StringsFromCsFiles");

                var lines = new List<LabelledDialogue>();

                AddStringsFromCsFilesGroup(character.Name, data, character, lines);
                AddStringsFromCsFilesGroup("EVERYONE", data, character, lines);

                if (character.datable.Value)
                {
                    AddStringsFromCsFilesGroup("LOVE INTERESTS", data, character, lines);
                    AddStringsFromCsFilesGroup(
                        character.GetData().Gender == Gender.Female ? "WIFE ONLY" : "HUSBAND ONLY" ,
                        data,
                        character,
                        lines);
                }
                    
                if (character.GetData().Age == NpcAge.Child)
                    AddStringsFromCsFilesGroup("CHILDREN", data, character, lines);


                return lines;
            }
            catch (Exception e)
            {
                ErrorLogger.Warn($"Could not load 'Strings/StringsFromCsFiles' for {character.Name}. {e}");
                return new List<LabelledDialogue>();
            }
        }

        private List<LabelledDialogue> GetSpeechBubbleDialogue(NPC character)
        {
            var lines = GetLabelledLinesWithCharacterNameInId(
                character, "Strings/SpeechBubbles", DialogueType.Generic);

            if (lines.Count == 0)
                ErrorLogger.Warn($"No speech bubble dialogue found for {character.Name}.");

            return lines;
        }

        // Fixed category order of Data/NPCGiftTastes: dialogue line, item-ID list
        // (discarded), repeated once per category. IDs are the category's 1-based
        // position in this order, not a count of non-empty lines seen so far.
        private static readonly string[] GiftTasteCategories = { "Love", "Like", "Dislike", "Hate", "Neutral" };

        private List<LabelledDialogue> GetGiftTasteDialogue(NPC character)
        {
            try
            {
                Dictionary<string, string> data =
                    helper.GameContent.Load<Dictionary<string, string>>("Data/NPCGiftTastes");

                if (!data.TryGetValue(character.Name, out string rawValue))
                {
                    ErrorLogger.Warn($"No gift taste dialogue found for {character.Name}.");
                    return new List<LabelledDialogue>();
                }

                string[] sections = rawValue.Split('/');
                var lines = new List<LabelledDialogue>();

                for (int i = 0; i < GiftTasteCategories.Length; i++)
                {
                    int textIndex = i * 2;
                    if (textIndex >= sections.Length) break;

                    string text = sections[textIndex].Trim();
                    if (string.IsNullOrEmpty(text)) continue;

                    string lineId = $"{character.Name}GiftTaste_{i + 1}";
                    var sdvDialogue = new StardewValley.Dialogue(character, null, text);
                    var labelledLine = new LabelledDialogueLine(lineId, text, lineId, DialogueType.GiftTastes);
                    lines.Add(new LabelledDialogue(sdvDialogue, new List<LabelledDialogueLine> { labelledLine }, lineId, DialogueType.GiftTastes));
                }

                if (lines.Count == 0)
                    ErrorLogger.Warn($"No gift taste dialogue found for {character.Name}.");

                return lines;
            }
            catch (Exception e)
            {
                ErrorLogger.Error($"Error getting gift taste dialogue for {character.Name}. {e}");
                return new List<LabelledDialogue>();
            }
        }

        private List<LabelledDialogue> GetScheduleDialogue(NPC character)
        {
            var lines = GetLabelledDialogueFromGameContent(
                character, $"Strings/schedules/{LocalizationHelpers.GetName(character.getName())}", DialogueType.Schedule);

            if (lines.Count == 0)
                ErrorLogger.Warn($"No schedule dialogue found for {character.Name}.");

            return lines;
        }


        // ── stringsFromCsFile helpers ────────────────────────────────────────

        /// <summary>
        /// Generates name-specific variants of the "I heard you secretly gave {0} a gift"
        /// line (NPC.cs.3985). One entry per other love interest, with the display name
        /// already substituted, so lookups at runtime can match directly instead of going
        /// through the fuzzy variable-matching path.
        /// </summary>
        private List<LabelledDialogue> GetGiftDialogueVariants(NPC character, Dictionary<string, string> data)
        {
            if (!data.TryGetValue("NPC.cs.3985", out string template))
                return new List<LabelledDialogue>();

            var lines = new List<LabelledDialogue>();

            foreach (string loveName in Constants.LoveInterests)
            {
                if (loveName == character.Name) continue;

                NPC loveNPC = Game1.getCharacterFromName(loveName);
                string localizedName = loveNPC?.displayName ?? NPC.GetDisplayName(loveName);

                string substitutedText = template.Replace("{0}", localizedName);
                string key = $"NPC_cs_3985-{loveName}";

                // Only the first page varies by name; _sv1/_sv2 are shared across all characters
                // and are already covered by the generic NPC.cs.3985 entry.
                var parsedLines = CharacterDialogueParser.ParseRawValueToLines(substitutedText, key, DialogueType.Generic);
                if (parsedLines.Count == 0) continue;

                var sdvDialogue = character.TryGetDialogue(key);
                lines.Add(new LabelledDialogue(sdvDialogue, new List<LabelledDialogueLine> { parsedLines[0] }, key, DialogueType.Generic));
            }

            return lines;
        }

        private void AddStringsFromCsFilesGroup(
            string group,
            Dictionary<string, string> data,
            NPC character,
            List<LabelledDialogue> lines)
        {
            if (!stringsFromCsFileRules.TryGetValue(group, out List<string> rules))
                return;

            foreach (string rule in rules)
            {
                if (rule.Contains("-"))
                    lines.AddRange(GetDialogueRange(data, rule, character));
                else if (data.TryGetValue(rule, out string value))
                {
                    lines.Add(BuildLabelledDialogue(character, rule, value, DialogueType.Generic));
                    if (rule == "NPC.cs.3985")
                        lines.AddRange(GetGiftDialogueVariants(character, data));
                }
            }
        }

        /// <summary>
        /// Fetches all keys in the range "File.cs.1234-1333" from <paramref name="data"/>.
        /// </summary>
        private List<LabelledDialogue> GetDialogueRange(
            Dictionary<string, string> data, string range, NPC character)
        {
            var lines = new List<LabelledDialogue>();

            // range format: "File.cs.1234-1333"
            string[] rangeSections = range.Split('.');
            string[] bounds = rangeSections[2].Split('-');
            string prefix = rangeSections[0] + "." + rangeSections[1] + ".";

            if (!int.TryParse(bounds[0], out int current) ||
                !int.TryParse(bounds[1], out int end))
                return lines;

            while (current <= end)
            {
                string key = prefix + current;
                if (data.TryGetValue(key, out string value))
                    lines.Add(BuildLabelledDialogue(character, key, value, DialogueType.Generic));
                current++;
            }

            return lines;
        }


        // ── Filter helpers ───────────────────────────────────────────────────

        private bool ShouldAddLineFromCharactersFiles(string lineId, NPC character)
        {
            return StringContainsCharacter(lineId, character.Name)
                || ((lineId.Contains("WipedMemory") || lineId.Contains("Divorced")) && character.datable.Value)
                || (lineId.Contains("Saloon") && (character.Name == "Kent" || character.Name == "Alex" || character.Name == "George"))
                || ShouldAddMovieInviteLineForCharacter(lineId, character);
        }

        private bool ShouldAddMovieInviteLineForCharacter(string lineId, NPC character)
        {
            if (!lineId.Contains("MovieInvite")
                || (lineId.Contains("Child") && character.GetData().Age != NpcAge.Child)
                || (lineId.Contains("Rude") && character.GetData().Manner != NpcManner.Rude)
                || (DialogueHelpers.StringContainAnyCharacter(lineId) && !lineId.Contains(character.Name)))
            {
                return false;
            }
            return true;
        }

        private bool StringContainsCharacter(string str, string characterName)
        {
            return DialogueHelpers.StringContainsSupportedCharacter(str) && str.Contains(characterName);
        }

        private bool StringContainsSpouse(string str)
        {
            return !DialogueHelpers.StringContainsSupportedCharacter(str) && str.Contains("Spouse");
        }


        // ── Core loading helpers ─────────────────────────────────────────────

        private List<LabelledDialogue> GetLabelledDialogueFromGameContent(
            NPC character, string path, DialogueType type)
        {
            try
            {
                Dictionary<string, string> data =
                    helper.GameContent.Load<Dictionary<string, string>>(path);
                Dictionary<string, string> englishData = isEnglish ? null : LoadEnglishContent(path);

                var lines = new List<LabelledDialogue>();
                foreach (var (key, value) in data)
                {
                    string englishRawValue = (englishData != null && englishData.TryGetValue(key, out string ev)) ? ev : null;
                    lines.Add(BuildLabelledDialogue(character, key, value, type, englishRawValue));
                }
                return lines;
            }
            catch (Exception e)
            {
                ErrorLogger.Warn($"Could not load '{path}' for {character?.Name}. {e}");
                return new List<LabelledDialogue>();
            }
        }

        private List<LabelledDialogue> GetLabelledLinesWithCharacterNameInId(
            NPC character, string path, DialogueType type)
        {
            try
            {
                Dictionary<string, string> data =
                    helper.GameContent.Load<Dictionary<string, string>>(path);
                Dictionary<string, string> englishData = isEnglish ? null : LoadEnglishContent(path);

                var lines = new List<LabelledDialogue>();
                foreach (var (key, value) in data)
                {
                    if (!StringContainsCharacter(key, character.getName())) continue;
                    string englishRawValue = (englishData != null && englishData.TryGetValue(key, out string ev)) ? ev : null;
                    lines.Add(BuildLabelledDialogue(character, key, value, type, englishRawValue));
                }
                return lines;
            }
            catch (Exception e)
            {
                ErrorLogger.Warn($"Could not load '{path}' for {character.Name}. {e}");
                return new List<LabelledDialogue>();
            }
        }

        private List<LabelledDialogue> GetLabelledLinesWithoutCharacterNameInId(
            NPC character, string path, DialogueType type)
        {
            try
            {
                Dictionary<string, string> data =
                    helper.GameContent.Load<Dictionary<string, string>>(path);
                Dictionary<string, string> englishData = isEnglish ? null : LoadEnglishContent(path);

                var lines = new List<LabelledDialogue>();
                foreach (var (key, value) in data)
                {
                    if (DialogueHelpers.StringContainAnyCharacter(key)) continue;
                    string englishRawValue = (englishData != null && englishData.TryGetValue(key, out string ev)) ? ev : null;
                    lines.Add(BuildLabelledDialogue(character, key, value, type, englishRawValue));
                }
                return lines;
            }
            catch (Exception e)
            {
                ErrorLogger.Warn($"Could not load '{path}' for {character.Name}. {e}");
                return new List<LabelledDialogue>();
            }
        }


        // ── Build helpers ────────────────────────────────────────────────────

        /// <summary>
        /// Creates a LabelledDialogue from a raw (key, value) pair: gets the
        /// StardewValley.Dialogue via TryGetDialogue and parses the raw string
        /// for line IDs in one step.
        /// </summary>
        private LabelledDialogue BuildLabelledDialogue(
            NPC character, string key, string rawValue, DialogueType type, string englishRawValue = null)
        {
            var sdvDialogue = character.TryGetDialogue(key);
            var labelledLines = CharacterDialogueParser.ParseRawValueToLines(rawValue, key, type, englishRawValue);
            labelledLines = ResolveBookPlaceholders(labelledLines, type);
            return new LabelledDialogue(sdvDialogue, labelledLines, key, type);
        }

        // Resolves Elliott's "%book" placeholder (see BookPlaceholderResolver) in each
        // parsed line, expanding any generic "your book" line into one entry per genre.
        // A no-op for lines that don't contain the placeholder.
        private List<LabelledDialogueLine> ResolveBookPlaceholders(List<LabelledDialogueLine> lines, DialogueType type)
        {
            List<LabelledDialogueLine> resolved = null;
            for (int i = 0; i < lines.Count; i++)
            {
                LabelledDialogueLine line = lines[i];
                if (!line.Text.Contains("%book"))
                {
                    resolved?.Add(line);
                    continue;
                }

                resolved ??= new List<LabelledDialogueLine>(lines.Take(i));
                foreach (var (resolvedId, resolvedText) in bookPlaceholderResolver.Resolve(line.LineId, line.Text))
                    resolved.Add(new LabelledDialogueLine(resolvedId, resolvedText, line.OriginalKey, type, line.IsContinuation));
            }
            return resolved ?? lines;
        }

        // Loads the English version of a game content asset regardless of current language.
        private Dictionary<string, string> LoadEnglishContent(string path)
        {
            try
            {
                return Game1.content.Load<Dictionary<string, string>>(
                    path, LocalizedContentManager.LanguageCode.en);
            }
            catch { return null; }
        }

   
    }
}