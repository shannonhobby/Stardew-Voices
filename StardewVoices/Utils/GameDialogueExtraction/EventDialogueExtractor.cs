using StardewModdingAPI;
using StardewVoices.FileGeneration.Models;
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
                        string[] parsedLines = CharacterDialogueParser.ParseDialogue(rawLine);
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
                        string[] parsedLines = CharacterDialogueParser.ParseDialogue(rawLine);
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

        public List<(string Location, string EventId)> GetAllEventsForCharacter(string characterName)
        {
            var results = new List<(string Location, string EventId)>();

            foreach (string locationName in EventLocations)
            {
                Dictionary<string, string> gameEventData = LoadGameEventData(locationName);
                if (gameEventData == null) continue;

                foreach (var kvp in gameEventData)
                {
                    string eventId = kvp.Key.Split('/')[0];
                    if (!kvp.Value.Contains($"speak {characterName} \"")) continue;

                    results.Add((locationName, eventId));
                }
            }

            return results;
        }

        public List<EventLine> GetEventDialogue(string locationName, string eventId)
        {
            Dictionary<string, string> gameEventData = LoadGameEventData(locationName);
            if (gameEventData == null) return null;
            List<EventLine> results = new List<EventLine>();

            foreach (var kvp in gameEventData)
            {
                string id = kvp.Key.Split('/')[0];
                if (eventId == id)
                {
                    Dictionary<string, int> speakerLineCount = new Dictionary<string, int>();
                    List<(string line, string characterName)> speakLines = ParseSpeakLines(kvp.Value);
                    if (speakLines.Count == 0) continue;

                    foreach ((string line, string characterName) in speakLines)
                    {
                        if (!speakerLineCount.ContainsKey(characterName))
                        {
                            speakerLineCount.Add(characterName, 1);
                        }

                        string[] parsedLines = CharacterDialogueParser.ParseDialogue(line);
                        if (parsedLines != null && parsedLines.Length > 0)
                        {
                            string eventLineId = GenerateEventLineId(locationName, eventId, characterName, speakerLineCount);
                            foreach (string dialogueBox in parsedLines)
                                results.Add(new EventLine(eventLineId, characterName,dialogueBox));
                        }
                        else
                        {
                            ErrorLogger.Warn($"Error parsing event dialogue: {line}");
                        }
                    }
                }
            }

            return results;
        }

        public enum EventActionType
        {
            Speak,
            TextAboveHead,
            EndDialogue
        }

        public static string GenerateEventLineId(
            string locationName,
            string sceneName,
            string speakerName, 
            Dictionary<string, int> speakerLineCounts,
            EventActionType actionType = EventActionType.Speak
        )
        {
            if (!speakerLineCounts.ContainsKey(speakerName))
                speakerLineCounts[speakerName] = 1;
            else
                speakerLineCounts[speakerName]++;

            int n = speakerLineCounts[speakerName];

            string suffix = actionType switch
            {
                EventActionType.Speak => $"_event{n}",
                EventActionType.TextAboveHead => $"_textAboveHeadEvent{n}",
                EventActionType.EndDialogue => $"_endDialogue{n}",
                _ => throw new ArgumentOutOfRangeException(nameof(actionType))
            };

            return $"{locationName}_{sceneName}_{speakerName}{suffix}";
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


        // Extracts the displayed text from every `speak {characterName} "..."` agnostic of character name.
        // Splits on the Stardew page-break token so each page becomes its own entry.
        // TODO - ALSO NEED TEXT ABOVE HEAD AND END DIALOGUE
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
        // TODO - ALSO NEED TEXT ABOVE HEAD AND END DIALOGUE

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
