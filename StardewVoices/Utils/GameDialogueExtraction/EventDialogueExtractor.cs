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


        private static readonly List<string> eventSpokenLines = new()
        {
            "speak", "textAboveHead", "end dialogue"
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
                    // Includes speak, textAboveHead, and end-dialogue lines together, in the
                    // order they appear in the raw script, so the shared per-character counter
                    // below advances across all three types instead of resetting per type.
                    List<(string line, string characterName, EventActionType actionType)> speakLines =
                        ParseSpeakingLines(kvp.Value);
                    if (speakLines.Count == 0) continue;

                    // The raw key's last precondition often carries a value that the scene name
                    // needs to preserve (e.g. "733330/f Sam 750/.../y 1" -> "733330_1").
                    // eventId alone (the part before the first '/') drops that segment, so
                    // rebuild sceneName from the full key here rather than reusing eventId.
                    string[] keyTokens = kvp.Key.Split(' ');
                    string sceneName = keyTokens.Length > 1
                        ? $"{eventId}_{keyTokens[^1]}"
                        : eventId;

                    foreach ((string line, string characterName, EventActionType actionType) in speakLines)
                    {
                        // Generate the base ID first so ParseDialogueWithIds can append
                        // gender suffixes (_male_farmer / _female_farmer) when needed.
                        // Note: GenerateEventLineId handles speakerLineCount initialisation
                        // internally — do NOT pre-initialise here or the first line gets _event2.
                        string baseId = GenerateEventLineId(locationName, sceneName, characterName, speakerLineCount, actionType);
                        var parsedPairs = CharacterDialogueParser.ParseDialogueWithIds(line, baseId);
                        if (parsedPairs != null && parsedPairs.Length > 0)
                        {
                            foreach (var (lineId, dialogue) in parsedPairs)
                            {
                                // Event keys with no "/" are branch events reached only after
                                // a player made a dialogue choice (e.g. "DadWeird", "beatGame").
                                // All lines from such events are prefixed with "response_".
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
                EventActionType.TextAboveHead => $"_textAboveHead{n}",
                EventActionType.EndDialogue => $"_endDialogue{n}",
                _ => throw new ArgumentOutOfRangeException(nameof(actionType))
            };

            return $"{locationName}_{sceneName}_{speakerName}{suffix}";
        }

        /// <summary>
        /// Returns the raw event-script dictionary for a location.
        /// Used by Pass 2 (EventDialogueReconciler) to build the fork graph.
        /// </summary>
        public Dictionary<string, string> GetLocationData(string locationName)
            => LoadGameEventData(locationName);

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

        // One regex per command type, keyed by the same EventActionType returned to callers.
        // textAboveHead can carry trailing numeric args after the quoted text (display
        // duration, pre-timer, width), so its text capture isn't end-anchored like the others.
        private static readonly Dictionary<EventActionType, Regex> SpeakingLineRegexes = new()
        {
            { EventActionType.Speak,         new Regex(@"^speak (\S+) ""(.+)""$") },
            { EventActionType.TextAboveHead, new Regex(@"^textAboveHead (\S+) ""(.*?)""") },
            { EventActionType.EndDialogue,   new Regex(@"^end dialogue (\S+) ""(.+)""$") },
        };

        // Extracts speak, textAboveHead, and end-dialogue commands together, in the order
        // they appear in the raw script. Used by GetEventDialogue so the per-character line
        // counter in GenerateEventLineId advances across all three types instead of resetting
        // per type. Splits each command's text on the Stardew page-break token so each page
        // becomes its own entry, same as ParseSpeakLines.
        private List<(string line, string characterName, EventActionType actionType)> ParseSpeakingLines(string eventScript)
        {
            var lines = new List<(string line, string characterName, EventActionType actionType)>();

            foreach (string command in eventScript.Split('/'))
            {
                string trimmed = command.Trim();

                EventActionType? actionType = trimmed switch
                {
                    var s when s.StartsWith("speak ") => EventActionType.Speak,
                    var s when s.StartsWith("textAboveHead ") => EventActionType.TextAboveHead,
                    var s when s.StartsWith("end dialogue ") => EventActionType.EndDialogue,
                    _ => null,
                };
                if (actionType == null) continue;

                Match m = SpeakingLineRegexes[actionType.Value].Match(trimmed);
                if (!m.Success) continue;

                string characterName = m.Groups[1].Value;
                string[] pages = m.Groups[2].Value.Split(PageBreakTokens, StringSplitOptions.RemoveEmptyEntries);
                foreach (string page in pages)
                    lines.Add((page, characterName, actionType.Value));
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
