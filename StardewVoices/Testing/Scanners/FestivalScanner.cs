using StardewModdingAPI;
using StardewValley;
using System;
using System.Collections.Generic;
using StardewVoices.Testing.Models;
using StardewVoices.Utils;
using StardewVoices.Pathing;
using System.IO;
using StardewVoices.Dialogue;
using Newtonsoft.Json;
using System.Text;

namespace StardewVoices.Testing.Scanners
{
    internal class FestivalScanner
    {
        private readonly IModHelper helper;
        private readonly DialogueManager dialogueManager;
        private static readonly List<string> FestivalDates = new()
        {
            "spring13", "spring24", "summer11", "summer28", "fall16", "fall27", "winter8", "winter25"
        };

        private static readonly string[] PageBreakTokens = { "#$b#", "#$e#" };

        public FestivalScanner(IModHelper helper, DialogueManager dialogueManager)
        {
            this.helper = helper;
            this.dialogueManager = dialogueManager;
        }

        public List<DialogueTestCase> GetFestivalTestCasesForCharacter(string characterName)
        {
            var testCases = new List<DialogueTestCase>();

            foreach (string festivalDate in FestivalDates)
            {

                Dictionary<string, string> festivalDict = LoadGameFestivalData(festivalDate);
                if (festivalDict == null)
                {
                    ErrorLogger.Warn($"Failed to load festival JSON: {festivalDate}");
                    continue;
                }

                foreach (var kvp in festivalDict)
                {
                    if (kvp.Key.Contains(characterName))
                    {
                        var lines = new List<string>();
                        lines.AddRange(kvp.Value.Split(PageBreakTokens, StringSplitOptions.RemoveEmptyEntries));
                        if (lines.Count == 0) { continue; }
                        foreach (var line in lines)
                        {
                            string[] parsedString = DialogueParsingHelper.ParseDialogue(line);
                            if (parsedString != null && parsedString.Length > 0)
                            {
                                foreach (string splitLine in parsedString)
                                {
                                    testCases.Add(new DialogueTestCase(characterName, DialogueType.Festival, splitLine, festivalDate, dialogueManager, helper));
                                }
                            }
                            else
                            {
                                ErrorLogger.Warn($"Error parsing dialogue: {line}");
                            }
                        }

                    }
                }
            }

            return testCases;
        }

        private Dictionary<string, string> LoadGameFestivalData(string festivalDate)
        {
            try
            {
               return helper.GameContent.Load<Dictionary<string, string>>($"Data/Festivals/{festivalDate}");
            }
            catch
            {
                ErrorLogger.Warn($"Error finding festival data for date: {festivalDate}");
                return null;
            }
        }


    }
}
