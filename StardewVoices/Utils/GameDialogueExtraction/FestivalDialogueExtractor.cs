using StardewModdingAPI;
using StardewVoices.Utils.ParsingHelpers;
using StardewValley;
using System;
using System.Collections.Generic;
using System.Linq;

namespace StardewVoices.Utils.GameDialogueExtraction
{
    public class FestivalDialogueExtractor
    {
        private readonly IModHelper helper;

        private string[] FestivalDates { get; }

        private static readonly string[] PageBreakTokens = { "#$b#", "#$e#" };

        // Leading words of known top-level event-script commands (see Data/Events /
        // Data/Festivals command grammar). Used by IsEventScript to tell an event-script
        // value apart from plain per-character dialogue by structure, not by key name, so
        // fork targets (e.g. "AbbyWin") are detected the same way as top-level keys (e.g.
        // "mainEvent"). Case-sensitive on purpose — event commands are always lowercase
        // camelCase, while dialogue text starts with a capital letter, so this avoids
        // false-positiving on dialogue that happens to start with a word like "End" or "Move".
        private static readonly HashSet<string> EventCommandVerbs = new(StringComparer.Ordinal)
        {
            "speak", "textAboveHead", "splitSpeak", "message", "question", "end", "pause",
            "playMusic", "stopMusic", "playSound", "emote", "jump", "shake", "showFrame",
            "specificTemporarySprite", "temporarySprite", "globalFade", "fade", "viewport",
            "warp", "warpFarmers", "faceDirection", "move", "advancedMove", "positionOffset",
            "proceedPosition", "waitForOtherPlayers", "fork", "switchEvent", "quickQuestion",
            "skippable", "removeQuest", "addQuest", "changeLocation", "animate", "halt",
            "changeToTemporaryMap", "cutscene", "addItem", "changePortrait",
            "addConversationTopic", "mail", "setRunning", "itemAboveHead",
            "netSpeak", "netTextAboveHead", "opacity", "glow", "bloom", "playerControl",
        };

        /// <summary>
        /// Detects festival values that are event scripts (a "/"-delimited command stream)
        /// rather than plain per-character dialogue, by checking whether any command's
        /// leading word is a known event-script verb.
        /// </summary>
        public static bool IsEventScript(string value)
        {
            if (string.IsNullOrEmpty(value) || !value.Contains('/'))
                return false;

            foreach (string command in value.Split('/'))
            {
                string trimmed = command.TrimStart();
                int spaceIndex = trimmed.IndexOf(' ');
                string verb = spaceIndex >= 0 ? trimmed.Substring(0, spaceIndex) : trimmed;
                if (EventCommandVerbs.Contains(verb))
                    return true;
            }
            return false;
        }

        public FestivalDialogueExtractor(IModHelper helper)
        {
            this.helper = helper;
            FestivalDates = GetAllFestivalDates();
        }

        // Date → festival name, in the current game language.
        private Dictionary<string, string> LoadFestivalDates()
        {
            return helper.GameContent.Load<Dictionary<string, string>>($"Data/Festivals/FestivalDates");
        }

        public string[] GetAllFestivalDates()
        {
            return LoadFestivalDates().Keys.ToArray();
        }

        // Festival name (matching Event.FestivalName) → date, the reverse of the game's data.
        public Dictionary<string, string> GetFestivalNameToDateDict()
        {
            return LoadFestivalDates().ToDictionary(kvp => kvp.Value, kvp => kvp.Key);
        }

        // Returns (Text, Context) pairs where Context is the festival date (e.g. "spring13").
        public List<(string Text, string Context)> GetFestivalDialogueForCharacter(string characterName)
        {
            var results = new List<(string Text, string Context)>();

            foreach (string festivalDate in FestivalDates)
            {
                Dictionary<string, string> festivalDict = LoadGameFestivalData(festivalDate);
                if (festivalDict == null)
                {
                    ErrorLogger.Warn($"Could not load festival data: {festivalDate}");
                    continue;
                }

                foreach (var kvp in festivalDict)
                {
                    if (!kvp.Key.Contains(LocalizationHelpers.GetName(characterName))) continue;

                    string[] rawLines = kvp.Value.Split(PageBreakTokens, StringSplitOptions.RemoveEmptyEntries);
                    foreach (string rawLine in rawLines)
                    {
                        string[] parsedLines = CharacterDialogueParser.ParseDialogue(rawLine);
                        if (parsedLines != null && parsedLines.Length > 0)
                        {
                            foreach (string line in parsedLines)
                            {
                                if (line.Contains('_'))
                                {
                                    IEnumerable<string> splitLines = splitResponseFestivalLine(line);
                                    foreach (string splitLine in splitLines)
                                    {
                                        results.Add((splitLine, festivalDate));
                                    }
                                }
                                else
                                {
                                    results.Add((line, festivalDate));
                                }
                            }
                        }
                        else
                        {
                            ErrorLogger.Warn($"Error parsing festival dialogue: {rawLine}");
                        }
                    }
                }
            }

            return results;
        }

        public IEnumerable<string> splitResponseFestivalLine(string s)
        {
            // Split on _ then keep even indices (0, 2, 4...) � the NPC lines
            string[] parts = s.Split('_');
            return parts.Where((_, index) => index % 2 == 0);
        }

        public Dictionary<string, string> LoadGameFestivalData(string festivalDate)
        {
            try
            {
                return helper.GameContent.Load<Dictionary<string, string>>($"Data/Festivals/{festivalDate}");
            }
            catch
            {
                ErrorLogger.Warn($"Could not load festival data for date: {festivalDate}");
                return null;
            }
        }

        // Loads the English version of festival data regardless of current game language.
        // Used to derive canonical line IDs so they stay identical across all languages.
        public Dictionary<string, string> LoadEnglishFestivalData(string festivalDate)
        {
            try
            {
                return Game1.content.Load<Dictionary<string, string>>(
                    $"Data/Festivals/{festivalDate}",
                    LocalizedContentManager.LanguageCode.en);
            }
            catch
            {
                ErrorLogger.Warn($"Could not load English festival data for date: {festivalDate}");
                return null;
            }
        }
    }
}
