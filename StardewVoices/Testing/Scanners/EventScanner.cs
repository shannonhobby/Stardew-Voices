using StardewModdingAPI;
using StardewVoices.Dialogue;
using StardewVoices.Pathing;
using StardewVoices.Testing.Models;
using StardewVoices.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace StardewVoices.Testing.Scanners
{
    internal class EventScanner
    {
        private readonly IModHelper helper;
        private readonly DialogueManager dialogueManager;

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

        public EventScanner(IModHelper helper, DialogueManager dialogueManager)
        {
            this.helper = helper;
            this.dialogueManager = dialogueManager;
        }

        public List<DialogueTestCase> GetEventTestCasesForCharacter(string characterName)
        {
            var testCases = new List<DialogueTestCase>();

            foreach (string locationName in EventLocations)
            {
                Dictionary<string, string> gameEventData = LoadGameEventData(locationName);
                if (gameEventData == null) continue;

                foreach (var kvp in gameEventData)
                {
                    string eventId = kvp.Key.Split('/')[0];
                    string eventScript = kvp.Value;

                    List<string> speakLines = ParseSpeakLines(eventScript, characterName);
                    if (speakLines.Count == 0) continue;
                    foreach (string rawLine in speakLines)
                    {
                        string[] parsedString = DialogueParsingHelper.ParseDialogue(rawLine);

                        if (parsedString != null && parsedString.Length > 0)
                        {
                            foreach (string splitLine in parsedString)
                            {
                                testCases.Add(new DialogueTestCase(characterName, DialogueType.Event, splitLine, $"{locationName}/{eventId}", dialogueManager, helper));
                            }
                        }
                        else
                        {
                            ErrorLogger.Warn($"Error parsing dialogue: {rawLine}");
                        }
                    }
                }
            }

            return testCases;
        }

        private Dictionary<string, string> LoadGameEventData(string locationName)
        {
            try
            {
                return helper.GameContent.Load<Dictionary<string, string>>($"Data/Events/{locationName}");
            }
            catch
            {
                ErrorLogger.Warn($"Error finding event data for location: {locationName}");
                return null;
            }
        }

        // Extracts the displayed text from every `speak {characterName} "..."` command.
        // Splits on the Stardew page-break token so each page becomes its own test case.
        private List<string> ParseSpeakLines(string eventScript, string characterName)
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
