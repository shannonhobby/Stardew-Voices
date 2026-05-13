using System.Collections.Generic;
using System.Linq;
using System.Text;
using System;
using StardewVoices.Localization;
using StardewModdingAPI;
using StardewValley;
using Newtonsoft.Json;
using System.IO;

namespace StardewVoices.Dialogue
{
    public static class DialogueHelpers
    {
        internal static Dictionary<string, string[]> GetDictionaryFromPath(string path)
        {
            try
            {
                string jsonString = File.ReadAllText(path, Encoding.UTF8);
                return JsonConvert.DeserializeObject<Dictionary<string, string[]>>(jsonString).ToDictionary(
                                kvp => DialogueHelpers.Normalize(kvp.Key),
                                kvp => kvp.Value
                            );
            }
            catch
            {
                // error handling
                return null;
            }

        }

        internal static Dictionary<string, string> GetEventDictionaryFromPath(string path)
        {
            try
            {
                string jsonString = File.ReadAllText(path, Encoding.UTF8);
                return JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonString).ToDictionary(
                                kvp => DialogueHelpers.Normalize(kvp.Key),
                                kvp => kvp.Value
                            );
            }
            catch
            {
                // error handling
                return null;
            }

        }

        internal static Boolean StringContainsSupportedCharacter(string input)
        {
            for (int i = 0; i < Constants.supportedCharacters.Count; i++)
            {
                string characterName = GetLocalizedName(supportedCharacters[i]);


                if (input.Contains(characterName))
                {
                    return true;
                }
            }

            return false;
        }


        internal static string GetSupportedCharacterFromString(string input)
        {
            for (int i = 0; i < supportedCharacters.Count; i++)
            {
                string characterName = GetLocalizedName(supportedCharacters[i]);
                if (input.Contains(characterName))
                {
                    return supportedCharacters[i];
                }
            }

            return "Could not find supported character in string :I";
        }

        internal static string ContainsCloseEnoughLine(Dictionary<string, string> dictionary, string line)
        {
            if (dictionary.ContainsKey(line))
            {
                return line;
            }
            string[] splitDialogueString = line.Split(" ");
            foreach (string key in dictionary.Keys)
            {
                string[] splitKey = key.Split(" ");

                if (splitDialogueString.Length - key.Count(word => word == '@') > 1 && splitDialogueString.Intersect(splitKey).Count() > 0 && splitDialogueString.Length - splitDialogueString.Intersect(splitKey).Count() <= Math.Ceiling(splitDialogueString.Length * .25))
                {
                    // TODO FIX 
                    /*if (!key.Contains("female_farmer") && player.Gender == Gender.Female)
                    {
                        return key.Replace("male", "female");
                    } else if (key.Contains("female_farmer") && player.Gender == Gender.Male)
                    {
                        return key.Replace("female", "male");
                    }
                    else
                    {
                        return key;
                    }*/
                }
            }
            return "Not found";
        }

        internal static string DialogueContainsCloseEnoughLine(Dictionary<string, string[]> dictionary, string line)
        {
            if (dictionary.ContainsKey(line))
            {
                return line;
            }
            string[] splitDialogueString = line.Split(" ");
            foreach (string key in dictionary.Keys)
            {
                string[] splitKey = key.Split(" ");

                foreach (string value in dictionary[key])
                {
                    if (splitDialogueString.Length - key.Count(word => word == '@') > 1 && splitDialogueString.Intersect(splitKey).Count() > 0 && splitDialogueString.Length - splitDialogueString.Intersect(splitKey).Count() <= Math.Ceiling(splitDialogueString.Length * .25))
                    {
                        return key;
                    }
                }
            }
            return "Not found";
        }


        private Dictionary<string, string[]> NormalizeDictionary(Dictionary<string, string[]> dictionary)
        {
            Dictionary<string, string[]> normalized = new Dictionary<string, string[]>();
            foreach (var kvp in dictionary)
            {
                var normalizedKey = Normalize(kvp.Key);
                normalized[normalizedKey] = kvp.Value;
            }

            return normalized;
        }



        internal static string Normalize(string input)
        {
            return new string(input
                .Normalize(NormalizationForm.FormC)
                .Where(c => !char.IsControl(c))
                .ToArray())
            .Trim();
        }



        internal static bool DialogueHasContinuation(string dialogueId)
        {
            foreach (string id in continuationDialogue)
            {
                if (dialogueId == id)
                {
                    return true;
                }
            }

            return false;
        }


        internal static string formatDialogueString(string dialogueString)
        {
            dialogueString = dialogueString.Replace(Game1.player.Name, "@");
            dialogueString = dialogueString.Replace(Game1.getFarm().Name, "%farm");
            if (currentEventDict == null)
            {
                dialogueString = dialogueString.Replace("the second baby", "%kid2");
                dialogueString = dialogueString.Replace("baby", "%kid1");
            }



            /// TODO: remove hardcoded Abigail stuff!
            // Hardcode for that abigail sprit board scene
            if (dialogueString.Contains("It's spelling out...") || dialogueString.Contains("I <"))
            {
                dialogueString = dialogueString.Replace(Game1.player.Name.Substring(0, Math.Max(0, Game1.player.Name.Length / 2)), "%firstnameletter");
            }

            for (int i = 0; i < Game1.player.getChildren().Count; i++)
            {
                dialogueString = dialogueString.Replace(Game1.player.getChildren()[i].Name, $"%kid{i + 1}");
            }

            return Normalize(dialogueString);
        }

        private static bool IdHasSeasonInIt(string id)
        {
            return id.ToLower().Contains("fall") || id.ToLower().Contains("winter") || id.ToLower().Contains("spring") || id.ToLower().Contains("summer");
        }

        private static bool IdHasDayOfWeekInIt(string id)
        {
            return id.ToLower().Contains("mon") || id.ToLower().Contains("tue") || id.ToLower().Contains("wed") || id.ToLower().Contains("thur") || id.ToLower().Contains("fri") || id.ToLower().Contains("sat") || id.ToLower().Contains("sun");
        }

        // Abigail has two instances of this: event_grave (I've lived in the valley my whole life, but I've never really done anything memorable. I want to go on an adventure!) 
        // and PurchasedItem_Abigail_QualityLow vs high
        // We're gonna hard code for these situations a lil bit :(
        internal static string getCorrectIdFromListOfLineIds(string[] dialogueIdStrings, string currentSpeaker, string dialogueContext)
        {
            foreach (string id in dialogueIdStrings)
            {
                if (dialogueContext == "Extra Dialogue-" || dialogueContext == "Generic Marriage Dialogue")
                {
                    // Right now just hard code quality low vs high to high
                    if (id.Contains("QualityHigh") && id.Contains(currentSpeaker))
                    {
                        return id;
                    }
                }

                // If the id has a day of the week and/or season we should check to see if it's right
                if (Game1.Date != null && (IdHasDayOfWeekInIt(id) || IdHasSeasonInIt(id)))
                {
                    if (IdHasSeasonInIt(id))
                    {
                        if (!id.Contains(Game1.Date.SeasonKey))
                        {
                            continue;
                        }
                    }

                    if (IdHasDayOfWeekInIt(id))
                    {
                        if (!id.Contains(Game1.Date.DayOfWeek.ToString().Substring(0, 3)) && !id.Contains(Game1.Date.DayOfWeek.ToString().Substring(0, 4)))
                        {
                            continue;
                        }

                    }

                    return id;
                }

                // The last line id will show which event choice was made last and then we can just choose
                // The new id that continues that event.
                if (lastLineId != null && id.Contains(lastLineId))
                {
                    return id;
                }
            }

            Monitor.Log("Correct line id not found from list of line ids taking first one: " + String.Join(", ", dialogueIdStrings), LogLevel.Debug);

            return dialogueIdStrings[0];

        }

    }
}