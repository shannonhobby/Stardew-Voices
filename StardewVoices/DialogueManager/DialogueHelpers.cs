using System.Collections.Generic;
using System.Linq;
using System.Text;
using System;
using StardewVoices.Localization;
using StardewModdingAPI;
using StardewValley;

namespace StardewVoices.DialogueManager
{
    public static class DialogueHelpers
    {

        public static Boolean StringContainsSupportedCharacter(string input)
        {
            for (int i = 0; i < this.supportedCharacters.Count; i++)
            {
                string characterName = GetLocalizedName(this.supportedCharacters[i]);


                if (input.Contains(characterName))
                {
                    return true;
                }
            }

            return false;
        }


        public static string GetSupportedCharacterFromString(string input)
        {
            for (int i = 0; i < this.supportedCharacters.Count; i++)
            {
                string characterName = GetLocalizedName(supportedCharacters[i]);
                if (input.Contains(characterName))
                {
                    return this.supportedCharacters[i];
                }
            }

            return "Could not find supported character in string :I";
        }

        public static string ContainsCloseEnoughLine(Dictionary<string, string> dictionary, string line)
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
                    /*if (!key.Contains("female_farmer") && this.player.Gender == Gender.Female)
                    {
                        return key.Replace("male", "female");
                    } else if (key.Contains("female_farmer") && this.player.Gender == Gender.Male)
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

        public static string DialogueContainsCloseEnoughLine(Dictionary<string, string[]> dictionary, string line)
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
                var normalizedKey = this.Normalize(kvp.Key);
                normalized[normalizedKey] = kvp.Value;
            }

            return normalized;
        }



        public static string Normalize(string input)
        {
            return new string(input
                .Normalize(NormalizationForm.FormC)
                .Where(c => !char.IsControl(c))
                .ToArray())
            .Trim();
        }



        public static bool DialogueHasContinuation(string dialogueId)
        {
            foreach (string id in this.continuationDialogue)
            {
                if (dialogueId == id)
                {
                    return true;
                }
            }

            return false;
        }


        private string formatDialogueString(string dialogueString)
        {
            dialogueString = dialogueString.Replace(Game1.player.Name, "@");
            dialogueString = dialogueString.Replace(Game1.getFarm().Name, "%farm");
            if (this.currentEventDict == null)
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

            return this.Normalize(dialogueString);
        }

        private bool IdHasSeasonInIt(string id)
        {
            return id.ToLower().Contains("fall") || id.ToLower().Contains("winter") || id.ToLower().Contains("spring") || id.ToLower().Contains("summer");
        }

        private bool IdHasDayOfWeekInIt(string id)
        {
            return id.ToLower().Contains("mon") || id.ToLower().Contains("tue") || id.ToLower().Contains("wed") || id.ToLower().Contains("thur") || id.ToLower().Contains("fri") || id.ToLower().Contains("sat") || id.ToLower().Contains("sun");
        }

        // Abigail has two instances of this: event_grave (I've lived in the valley my whole life, but I've never really done anything memorable. I want to go on an adventure!) 
        // and PurchasedItem_Abigail_QualityLow vs high
        // We're gonna hard code for these situations a lil bit :(
        private string getCorrectIdFromListOfLineIds(string[] dialogueIdStrings, string currentSpeaker, string dialogueContext)
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
                if (Game1.Date != null && (this.IdHasDayOfWeekInIt(id) || this.IdHasSeasonInIt(id)))
                {
                    if (this.IdHasSeasonInIt(id))
                    {
                        if (!id.Contains(Game1.Date.SeasonKey))
                        {
                            continue;
                        }
                    }

                    if (this.IdHasDayOfWeekInIt(id))
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
                if (this.lastLineId != null && id.Contains(this.lastLineId))
                {
                    return id;
                }
            }

            this.Monitor.Log("Correct line id not found from list of line ids taking first one: " + String.Join(", ", dialogueIdStrings), LogLevel.Debug);

            return dialogueIdStrings[0];

        }

    }
}