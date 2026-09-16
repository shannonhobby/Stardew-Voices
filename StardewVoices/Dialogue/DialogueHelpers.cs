using System.Collections.Generic;
using System.Linq;
using System.Text;
using System;
using StardewVoices.Utils;
using StardewModdingAPI;
using StardewValley;
using Newtonsoft.Json;
using System.IO;
using System.Text.RegularExpressions;

namespace StardewVoices.Dialogue
{
    public static class DialogueHelpers
    {
        private static List<string> _supportedCharacters = new();
        private static List<string> _charactersToGenerate = new();


        public static void Init(List<string> supportedCharacters, List<string> charactersToGenerate)
        {
            _supportedCharacters = supportedCharacters;
            _charactersToGenerate = charactersToGenerate;
        }

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
                ErrorLogger.Error($"Could not find dictionary from path: {path}");
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
                ErrorLogger.Error($"Could not find dictionary from path: {path}");
                return null;
            }

        }

        internal static Boolean StringContainsSupportedCharacter(string input)
        {
            for (int i = 0; i < _supportedCharacters.Count; i++)
            {
                string characterName = LocalizationHelpers.GetLocalizedName(_supportedCharacters[i]);


                if (input.Contains(characterName))
                {
                    return true;
                }
            }

            return false;
        }

        internal static Boolean StringContainsCharacterToGenerate(string input)
        {
            for (int i = 0; i < _charactersToGenerate.Count; i++)
            {
                string characterName = LocalizationHelpers.GetName(_charactersToGenerate[i]);
                if (characterName == null)
                {
                    return false;
                }

                if (input.Contains(characterName))
                {
                    return true;
                }
            }

            return false;
        }



        internal static string GetSupportedCharacterFromString(string input)
        {
            for (int i = 0; i < _supportedCharacters.Count; i++)
            {
                string characterName = LocalizationHelpers.GetLocalizedName(_supportedCharacters[i]);
                if (input.Contains(characterName))
                {
                    return _supportedCharacters[i];
                }
            }

            return "Could not find supported character in string :I";
        }

        internal static Boolean StringContainAnyCharacter(string input)
        {
            for (int i = 0; i < Constants.AllCharacterNames.Count; i++)
            {
                string characterName = LocalizationHelpers.GetLocalizedName(Constants.AllCharacterNames[i]);


                if (input.Contains(characterName))
                {
                    return true;
                }
            }

            return false;
        }


        internal static string GetAnyCharacterFromString(string input)
        {
            for (int i = 0; i < Constants.AllCharacterNames.Count; i++)
            {
                string characterName = LocalizationHelpers.GetLocalizedName(Constants.AllCharacterNames[i]);
                if (input.Contains(characterName))
                {
                    return Constants.AllCharacterNames[i];
                }
            }

            return "Could not find supported character in string :I";
        }

        internal static string ContainsCloseEnoughLine(Dictionary<string, string> dictionary, string line)
        {
            if (dictionary != null && line != null && dictionary.ContainsKey(line))
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
            if (dictionary != null && line != null && dictionary.ContainsKey(line))
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


        private static Dictionary<string, string[]> NormalizeDictionary(Dictionary<string, string[]> dictionary)
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




        // Plain string.Replace matches substrings, so a farm/player/kid name that's also
        // a prefix of an unrelated word (e.g. a farm named "Farm" inside "Farming") would
        // get corrupted mid-word. Only replace where the match isn't glued to another
        // letter on either side.
        private static string ReplaceWholeWord(string input, string target, string replacement)
        {
            if (string.IsNullOrEmpty(target)) return input;
            return Regex.Replace(input, $@"(?<!\p{{L}}){Regex.Escape(target)}(?!\p{{L}})", replacement.Replace("$", "$$"));
        }

        internal static string formatDialogueString(string dialogueString)
        {
            dialogueString = ReplaceWholeWord(dialogueString, Game1.player.Name, "@");
            dialogueString = ReplaceWholeWord(dialogueString, Game1.getFarm().Name, "%farm");
            dialogueString = Regex.Replace(dialogueString, @"\[.*?\]", "").Trim();

            // If in event replace baby and the second baby??? what??
            /*if (currentEventDict == null)
            {
                dialogueString = dialogueString.Replace("the second baby", "%kid2");
                dialogueString = dialogueString.Replace("baby", "%kid1");
            }
            */


            /// TODO: remove hardcoded Abigail stuff!
            // Hardcode for that abigail sprit board scene
            if (dialogueString.Contains("It's spelling out...") || dialogueString.Contains("I <"))
            {
                dialogueString = dialogueString.Replace(Game1.player.Name.Substring(0, Math.Max(0, Game1.player.Name.Length / 2)), "%firstnameletter");
            }

            for (int i = 0; i < Game1.player.getChildren().Count; i++)
            {
                dialogueString = ReplaceWholeWord(dialogueString, Game1.player.getChildren()[i].Name, $"%kid{i + 1}");
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

        // TODO - major refactor here
        // Abigail has two instances of this: event_grave (I've lived in the valley my whole life, but I've never really done anything memorable. I want to go on an adventure!) 
        // and PurchasedItem_Abigail_QualityLow vs high
        // We're gonna hard code for these situations a lil bit :(
        internal static string getCorrectIdFromListOfLineIds(string[] dialogueIdStrings, string currentSpeaker, string dialogueContext)
        {
            foreach (string id in dialogueIdStrings)
            {
                if (dialogueContext == "extra" || dialogueContext == "generic_marriage")
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


                // TODO
                // The last line id will show which event choice was made last and then we can just choose
                // The new id that continues that event.
                /*if (lastLineId != null && id.Contains(lastLineId))
                {
                    return id;
                }
                */
            }

            ErrorLogger.Trace("Correct line id not found from list of line ids taking first one: " + String.Join(", ", dialogueIdStrings));

            return dialogueIdStrings[0];

        }

    }
}