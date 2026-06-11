using StardewModdingAPI;
using StardewVoices.Utils.ParsingHelpers;
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

        public FestivalDialogueExtractor(IModHelper helper)
        {
            this.helper = helper;
            FestivalDates = GetAllFestivalDates();
        }

        public string[] GetAllFestivalDates()
        {
            Dictionary<string, string> festivalDates = helper.GameContent.Load<Dictionary<string, string>>($"Data/Festivals/FestivalDates");
            return festivalDates.Keys.ToArray();
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
                    if (!kvp.Key.Contains(characterName)) continue;

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
            // Split on _ then keep even indices (0, 2, 4...) — the NPC lines
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
    }
}
