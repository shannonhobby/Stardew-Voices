using StardewModdingAPI;
using StardewVoices.Utils.ParsingHelpers;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace StardewVoices.Utils.GameDialogueExtraction
{
    public class EventDialogueExtractor
    {
        private readonly IModHelper helper;

        // TODO - don't hardcode this incase of mods adding events
        // All locations that contain love interest events.
        private static readonly List<string> EventLocations = new()
        {
            "ArchaeologyHouse", "AnimalShop", "Backwoods", "BathHouse_Pool",
            "Beach", "BusStop", "ElliottHouse", "Farm", "FarmHouse",
            "Forest", "HaleyHouse", "Hospital", "JoshHouse", "LeahHouse",
            "ManorHouse", "Mine", "Mountain", "Railroad", "Saloon",
            "SamHouse", "ScienceHouse", "SebastianRoom", "SeedShop",
            "Town", "Trailer", "Woods",
        };

        private static readonly string[] PageBreakTokens = { "#$b#" };

        public EventDialogueExtractor(IModHelper helper)
        {
            this.helper = helper;
        }


        // Returns (Text, Context) pairs where Context is "{locationName}/{eventId}/{characterName}".
        public List<(string Text, string Context)> GetAllEventDialogue()
        {
            var results = new List<(string Text, string Context)>();

            foreach (string locationName in EventLocations)
            {
                Dictionary<string, string> gameEventData = LoadGameEventData(locationName);
                if (gameEventData == null) continue;

                foreach (var kvp in gameEventData)
                {
                    string eventId = kvp.Key.Split('/')[0];
                    List<(string line, string characterName)> speakLines = ParseSpeakLines(kvp.Value);
                    if (speakLines.Count == 0) continue;

                    foreach ((string rawLine, string characterName) in speakLines)
                    {
                        string[] parsedLines = DialogueParsingHelper.ParseDialogue(rawLine);
                        if (parsedLines != null && parsedLines.Length > 0)
                        {
                            foreach (string line in parsedLines)
                                results.Add((line, $"{locationName}/{eventId}/{characterName}"));
                        }
                        else
                        {
                            ErrorLogger.Warn($"Error parsing event dialogue: {rawLine}");
                        }
                    }
                }
            }

            return results;
        }

        // Returns (Text, Context) pairs where Context is "{locationName}/{eventId}".
        public List<(string Text, string Context)> GetEventDialogueForCharacter(string characterName)
        {
            var results = new List<(string Text, string Context)>();

            foreach (string locationName in EventLocations)
            {
                Dictionary<string, string> gameEventData = LoadGameEventData(locationName);
                if (gameEventData == null) continue;

                foreach (var kvp in gameEventData)
                {
                    string eventId = kvp.Key.Split('/')[0];
                    List<string> speakLines = ParseCharacterSpeakLines(kvp.Value, characterName);
                    if (speakLines.Count == 0) continue;

                    foreach (string rawLine in speakLines)
                    {
                        string[] parsedLines = DialogueParsingHelper.ParseDialogue(rawLine);
                        if (parsedLines != null && parsedLines.Length > 0)
                        {
                            foreach (string line in parsedLines)
                                results.Add((line, $"{locationName}/{eventId}"));
                        }
                        else
                        {
                            ErrorLogger.Warn($"Error parsing event dialogue: {rawLine}");
                        }
                    }
                }
            }

            return results;
        }

        private Dictionary<string, string> LoadGameEventData(string locationName)
        {
            try
            {
                return helper.GameContent.Load<Dictionary<string, string>>($"Data/Events/{locationName}");
            }
            catch
            {
                ErrorLogger.Warn($"Could not load event data for location: {locationName}");
                return null;
            }
        }

        private List<(string line, string characterName)> ParseSpeakLines(string eventScript)
        {
            var lines = new List<(string line, string characterName)>();

            foreach (string command in eventScript.Split('/'))
            {
                string trimmed = command.Trim();
                if (!trimmed.StartsWith("speak ")) continue;

                Match m = Regex.Match(trimmed, @"^speak (\S+) ""(.+)""$");
                if (!m.Success) continue;

                string characterName = m.Groups[1].Value;
                string[] pages = m.Groups[2].Value.Split(PageBreakTokens, StringSplitOptions.RemoveEmptyEntries);
                foreach (string page in pages)
                    lines.Add((page, characterName));
            }

            return lines;
        }

        // Extracts the displayed text from every `speak {characterName} "..."` command.
        // Splits on the Stardew page-break token so each page becomes its own entry.
        private List<string> ParseCharacterSpeakLines(string eventScript, string characterName)
        {
            var lines = new List<string>();
            string speakPrefix = $"speak {characterName} \"";

            foreach (string command in eventScript.Split('/'))
            {
                string trimmed = command.Trim();
                if (!trimmed.StartsWith(speakPrefix)) continue;

                Match m = Regex.Match(trimmed, $@"^speak {Regex.Escape(characterName)} ""(.+)""$");
                if (!m.Success) continue;

                string[] pages = m.Groups[1].Value.Split(PageBreakTokens, StringSplitOptions.RemoveEmptyEntries);
                lines.AddRange(pages);
            }

            return lines;
        }
    }
}
