using StardewModdingAPI;
using StardewVoices.FileGeneration.Models;
using StardewVoices.Utils.ParsingHelpers;
using System;
using System.Collections.Generic;
using System.Linq;
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

        /// <summary>
        /// Returns events for a character grouped and ordered for script generation.
        /// "heart_events" — this character's own heart events, sorted lowest to highest friendship level,
        ///                   with any response events (fork/switchEvent branches) inserted directly after
        ///                   the parent event that references them.
        /// "guest_events"  — every other event in which this character speaks, in discovery order,
        ///                   also with responses inserted after their parent.
        /// </summary>
        public List<(string locationName, string eventId)> GetEventOrderForCharacter(
            string characterName,
            List<(string locationName, string eventId)> allEvents)
        {
            var heartEvents = new List<(int friendshipLevel, string locationName, string eventId)>();
            var guestEvents = new List<(string locationName, string eventId)>();
            var locationCache = new Dictionary<string, Dictionary<string, string>>();

            foreach (var (locationName, eventId) in allEvents)
            {
                if (!locationCache.TryGetValue(locationName, out var locationData))
                {
                    locationData = LoadGameEventData(locationName);
                    if (locationData == null) continue;
                    locationCache[locationName] = locationData;
                }

                // Find the full raw key (e.g. "423502/f Elliott 1500") for this event
                var entry = locationData.FirstOrDefault(kvp => kvp.Key.Split('/')[0] == eventId);
                if (entry.Key == null) continue;

                // Response events (no '/') are appended after their parent — skip at top level
                if (!entry.Key.Contains('/')) continue;

                Match heartMatch = Regex.Match(entry.Key, $@"/f {Regex.Escape(characterName)} (\d+)");
                if (heartMatch.Success)
                    heartEvents.Add((int.Parse(heartMatch.Groups[1].Value), locationName, eventId));
                else
                    guestEvents.Add((locationName, eventId));
            }

            heartEvents.Sort((a, b) => a.friendshipLevel.CompareTo(b.friendshipLevel));

            var heartList = new List<(string locationName, string eventId)>();
            var guestList = new List<(string locationName, string eventId)>();
            var seen = new HashSet<string>();

            foreach (var (_, locationName, eventId) in heartEvents)
            {
                if (!seen.Add(eventId)) continue;
                heartList.Add((locationName, eventId));
                var entry = locationCache[locationName].FirstOrDefault(kvp => kvp.Key.Split('/')[0] == eventId);
                if (entry.Key != null)
                    AppendResponseEvents(locationCache[locationName], locationName, entry.Value, heartList, seen);
            }

            foreach (var (locationName, eventId) in guestEvents)
            {
                if (!seen.Add(eventId)) continue;
                guestList.Add((locationName, eventId));
                var entry = locationCache[locationName].FirstOrDefault(kvp => kvp.Key.Split('/')[0] == eventId);
                if (entry.Key != null)
                    AppendResponseEvents(locationCache[locationName], locationName, entry.Value, guestList, seen);
            }
            heartList.Add((null, "STARTING GUEST LIST"));
            heartList.AddRange(guestList);

            return heartList;
        }

        // Walks a script for fork/switchEvent commands and inserts their target events
        // directly after the parent in the list, recursing for nested responses.
        private void AppendResponseEvents(
            Dictionary<string, string> locationData,
            string locationName,
            string eventScript,
            List<(string locationName, string eventId)> target,
            HashSet<string> seen)
        {
            foreach (string command in eventScript.Split('/'))
            {
                string trimmed = command.Trim();
                string responseId = null;

                if (trimmed.StartsWith("fork "))
                    responseId = trimmed.Substring(5).Trim().Split(' ')[0];
                else if (trimmed.StartsWith("switchEvent "))
                    responseId = trimmed.Substring(12).Trim().Split(' ')[0];

                if (string.IsNullOrEmpty(responseId)) continue;
                if (!locationData.TryGetValue(responseId, out string responseScript)) continue;
                if (!seen.Add(responseId)) continue;

                target.Add((locationName, responseId));
                AppendResponseEvents(locationData, locationName, responseScript, target, seen);
            }
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
                    bool isResponseEvent = !kvp.Key.Contains('/');
                    Dictionary<string, int> speakerLineCount = new Dictionary<string, int>();
                    List<(string line, string characterName)> speakLines = ParseSpeakLines(kvp.Value);
                    if (speakLines.Count == 0) continue;

                    foreach ((string line, string characterName) in speakLines)
                    {
                        // Generate the base ID first so ParseDialogueWithIds can append
                        // gender suffixes (_male_farmer / _female_farmer) when needed.
                        // Note: GenerateEventLineId handles speakerLineCount initialisation
                        // internally — do NOT pre-initialise here or the first line gets _event2.
                        string baseId = GenerateEventLineId(locationName, eventId, characterName, speakerLineCount);
                        var parsedPairs = CharacterDialogueParser.ParseDialogueWithIds(line, baseId);
                        if (parsedPairs != null && parsedPairs.Length > 0)
                        {
                            foreach (var (lineId, dialogue) in parsedPairs)
                            {
                                // Event keys with no "/" are branch events reached only after
                                // a player made a dialogue choice (e.g. "DadWeird", "beatGame").
                                // Mirror Python's event_line_array_to_dictionary "/" check:
                                // all lines from such events are prefixed with "response_".
                                string finalId = isResponseEvent ? "response_" + lineId : lineId;
                                results.Add(new EventLine(finalId, characterName, dialogue));
                            }
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
