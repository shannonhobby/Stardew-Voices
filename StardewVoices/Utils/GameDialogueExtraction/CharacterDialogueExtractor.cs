using Microsoft.VisualBasic;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.GameData.Characters;
using StardewValley.Quests;
using StardewVoices.Dialogue;
using System;
using System.Collections.Generic;
using System.IO;

namespace StardewVoices.Utils.GameDialogueExtraction
{
    public class CharacterDialogueExtractor
    {
        private readonly IModHelper helper;

        private readonly Dictionary<string, List<string>> stringsFromCsFileRules = new Dictionary<string, List<string>>()
        {
            {"Abigail", new List<string>() {"AbigailGame.cs.11896-11917", "ItemDeliveryQuest.cs.13597-13602"} },
            {"Maru", new List<string>() { "ItemDeliveryQuest.cs.13585-13587" } },
            {"Sam", new List<string>() { "ItemDeliveryQuest.cs.13577" } },
            {"Haley", new List<string>() { "ItemDeliveryQuest.cs.13566" } },
            {"Elliott", new List<string>() { "Event.cs.1257-1260" } },
            {"Grandpa", new List<string>() { "Event.cs.1306-1318", "GrandpaStory.cs.12026-12055" } },
            {"Marnie", new List<string>() {
                "Event.cs.1798",
                "PurchaseAnimalsMenu.cs.11311-11314",
                "ShopMenu.cs.11507-11508"
            } },
            {"Gus", new List<string>() { "ShopMenu.cs.11512-11514" } },
            {"Marlon", new List<string>() { "ShopMenu.cs.11517-11525",
                "SlayMonsterQuest.cs.13730-13744",
                "ItemRecovery_Description",
                "ItemRecovery_Engaged",
                "ItemRecovery_Engaged_Stack",
            } },
            {"Robin", new List<string>() { "NPC.cs.3926-3927", "ShopMenu.cs.11460-11464", "ResourceCollectionQuest.cs.13677-13683" } },
            {"Clint", new List<string>() { "ShopMenu.cs.11469-11474", "ResourceCollectionQuest.cs.13655-13673", "Tool.cs.14317" } },
            {"Willy", new List<string>() { "ShopMenu.cs.11477-11478", "FishingQuest.cs.13256-13268" } },
            {"Pierre", new List<string>() { "ShopMenu.cs.11481-11489" } },
            {"Wizard", new List<string>() { "ItemDeliveryQuest.cs.13555", "SlayMonsterQuest.cs.13760" } },
            {"Krobus", new List<string>() { "NPC.cs.3990", "Krobus_Engaged", "Krobus_AfterWedding" } },
            {"Lewis", new List<string>() { "Utility.cs.5367-5379" } },
            {"EVERYONE", new List<string>() {
                "ItemDeliveryQuest.cs.13526-13528",
                "ItemDeliveryQuest.cs.13530-13544",
                "NPC.cs.4274-4281",
                "NPC.cs.4293-4294",
            } },
            {"LOVE INTERESTS", new List<string>() {
                "Event.cs.1632-1635",
                "Game1.cs.2782", 
                // Rejection / Accept proposals
                "NPC.cs.3956-3980", 
                // TODO:"I heard you secretly gave {0} a gift today": Add different names
                "NPC.cs.3985",
                "NPC.cs.4001",
                "NPC.cs.4406-4500",
                "MultiplePetBowls_watered",
            } },
            {"CHILDREN", new List<string>() {"Event.cs.1497-1498"} },

        };

        public CharacterDialogueExtractor(IModHelper helper)
        {
            this.helper = helper;
        }

        public Dictionary<DialogueType, List<StardewValley.Dialogue>> GetAllDialogueForCharacter(NPC character)
        {
            return new Dictionary<DialogueType, List<StardewValley.Dialogue>>()
            {
                { DialogueType.Dialogue,    GetCharacterDialogue(character) },
                { DialogueType.Engagement,  GetEngagementDialogue(character) },
                { DialogueType.Rainy,       new List<StardewValley.Dialogue>() { GetRainyDialogue(character) } },
                { DialogueType.Marriage,    GetMarriageDialogue(character) },
                { DialogueType.Generic,     GetGenericDialogue(character) },
                { DialogueType.Schedule,    GetScheduleDialogue(character) },
                { DialogueType.SpeechBubble, GetSpeechBubbleDialogue(character) }
            };
        }

        private List<StardewValley.Dialogue> GetEngagementDialogue(NPC character)
        {
            try
            {
                return new List<StardewValley.Dialogue>
                {
                    character.TryGetDialogue($"{character.Name}0"),
                    character.TryGetDialogue($"{character.Name}1"),
                };
            }
            catch (Exception e)
            {
                ErrorLogger.Error($"Error getting engagement dialogue for {character.Name}. {e}");
                return null;
            }
        }

        private List<StardewValley.Dialogue> GetMarriageDialogue(NPC character)
        {
            try
            {
                List<StardewValley.Dialogue> dialogue = GetDialogueFromGameContent(character, $"Characters/Dialogue/MarriageDialogue{character.name}");
                if (dialogue.Count == 0)
                    ErrorLogger.Warn($"No marriage dialogue found for {character.Name}.");

                dialogue.AddRange(GetGenericMarriageDialogue(character));
                return dialogue;
            }
            catch (Exception e)
            {
                ErrorLogger.Error($"Error getting marriage dialogue for {character.Name}. {e}");
                return null;
            }
        }

        private List<StardewValley.Dialogue> GetGenericMarriageDialogue(NPC character)
        {
            try
            {
                return GetOnlyDialogueWithCharacterNameInId(character, "Characters/Dialogue/MarriageDialogue");
            }
            catch (Exception e)
            {
                ErrorLogger.Error($"Error getting generic marriage dialogue for {character.Name}. {e}");
                return null;
            }
        }

        private StardewValley.Dialogue GetRainyDialogue(NPC character)
        {
            return StardewValley.Dialogue.TryGetDialogue(character, "Characters\\Dialogue\\rainy:" + character.GetDialogueSheetName());
        }

        private List<StardewValley.Dialogue> GetCharacterDialogue(NPC character)
        {
            List<StardewValley.Dialogue> dialogueList = GetDialogueFromGameContent(character, $"Characters/Dialogue/{character.getName()}");
            if (dialogueList.Count == 0)
                ErrorLogger.Warn($"No character dialogue found for {character.Name}.");
            return dialogueList;
        }

        // TODO - NOT DONE!! DIDN'T GET EVERYTHING
        private List<StardewValley.Dialogue> GetGenericDialogue(NPC character)
        {
            try
            {
                List<StardewValley.Dialogue> dialogue = GetMovieDialogue(character);
                if (dialogue.Count == 0)
                    ErrorLogger.Warn($"No movie dialogue found for {character.Name}.");
                dialogue.AddRange(GetExtraDialogue(character));


                //LEFT OFF HERE - GETTING ALL THE GENERIC DIALOGUE


                return dialogue;
            }
            catch (Exception e)
            {
                ErrorLogger.Error($"Error getting generic dialogue for {character.Name}. {e}");
                return null;
            }
        }

        private List<StardewValley.Dialogue> GetMovieDialogue(NPC character)
        {
            List<StardewValley.Dialogue> dialogueList = GetOnlyDialogueWithCharacterNameInId(character, "Strings/MovieReactions");
            if (dialogueList.Count == 0)
                ErrorLogger.Warn($"No movie reaction dialogue found for {character.Name}.");
            return dialogueList;
        }

        private List<StardewValley.Dialogue> GetExtraDialogue(NPC character)
        {
            Dictionary<string, string> dialogueData = helper.GameContent.Load<Dictionary<string, string>>("Data/ExtraDialogue");
            List<StardewValley.Dialogue> dialogueList = new List<StardewValley.Dialogue>();

            foreach (string key in dialogueData.Keys)
            {
                if (StringContainsCharacter(key, character.getName())
                    || StringContainsSpouse(key)
                    || key.Contains("NewChild")
                    || key.Contains("LostItemQuest")
                    || key.Equals("Town_DumpsterDiveComment_Teen")
                    || key.Equals("Town_DumpsterDiveComment_Adult"))
                {
                    dialogueList.Add(character.TryGetDialogue(key));
                }
            }

            return dialogueList;
        }

        private List<StardewValley.Dialogue> Get16strings(NPC character)
        {
            List<StardewValley.Dialogue> dialogueList = GetOnlyDialogueWithCharacterNameInId(character, "Strings/1_6_Strings");
            if (dialogueList.Count == 0)
                ErrorLogger.Warn($"No 1.6 dialogue found for {character.Name}.");
            return dialogueList;
        }

        private List<StardewValley.Dialogue> GetStringsFromCharactersFiles(NPC character)
        {
            try
            {
                Dictionary<string, string> dialogueData = helper.GameContent.Load<Dictionary<string, string>>("Strings/Characters");
                List<StardewValley.Dialogue> dialogueList = new List<StardewValley.Dialogue>();

                foreach (string key in dialogueData.Keys)
                {
                    if (ShouldAddLineFromCharactersFiles(key, character))
                        dialogueList.Add(character.TryGetDialogue(key));
                }

                return dialogueList;
            }
            catch (Exception e)
            {
                ErrorLogger.Warn($"Could not load dialogue from 'Strings / Characters' for {character}. {e}");
                return new List<StardewValley.Dialogue>();
            }
        }

        private bool ShouldAddLineFromCharactersFiles(string lineId, NPC character)
        {
            /*
             * Should add line from StringFromCsFiles.json if:
             * 1. Character's name is in the line id
             * 2. The line id is "WipedMemory" or "Divorced" and the character is romanceable
             * 3. The line id contains "Saloon" and the character is Alex, Kent, or George (maybe?? not sure TODO)
             * 4. The line id is a MovieInvite line for the appropriate person (see helper function below)
             */
            return StringContainsCharacter(lineId, character.Name)
                || (lineId.Contains("WipedMemory") || lineId.Contains("Divorced")) && character.datable.Value == true
                || (lineId.Contains("Saloon") && (character.Name == "Kent" || character.Name == "Alex" || character.Name == "George"))
                || ShouldAddMovieInviteLineForCharacter(lineId, character);
        }

        private bool ShouldAddMovieInviteLineForCharacter(string lineId, NPC character)
        {
            /**
             * Add MovieInvite line iff:
             * 1. MovieInvite is in id.
             * 2. "child" classifier is not in the id if the character is not a child.
             * 3. The line id has a different character in it.
             * 4. "Rude" classifier is in the id and the character is not rude.
             */

            if (!lineId.Contains("MovieInvite") 
                || (lineId.Contains("Child") && character.GetData().Age != NpcAge.Child)
                || (lineId.Contains("Rude") && character.GetData().Manner != NpcManner.Rude)
                || (DialogueHelpers.StringContainAnyCharacter(lineId) && !lineId.Contains(character.Name)))
            {
                return false;
            }

            return true;
        }

        private List<StardewValley.Dialogue> GetLinesFromStringsFromCsFile(NPC character)
        {

            // This one is a little different - strings from cs files contains a lot of random string
            // Instead of iterating through all of the file we're going to pull from my 
            // "strings from cs files rules" dictionary I made
            try
            {
                Dictionary<string, string> dialogueData = helper.GameContent.Load<Dictionary<string, string>>("Strings/StringsFromCsFiles");
                List<StardewValley.Dialogue> dialogueList = new List<StardewValley.Dialogue>();

                // Get character specific dialogue
                stringsFromCsFileRules.TryGetValue(character.Name, out List<string> characterSpecificDialogueIds);
                if (characterSpecificDialogueIds != null)
                {
                    foreach (string rule in characterSpecificDialogueIds)
                    {
                        if(rule.Contains("-"))
                        {
                            // add range
                        } else
                        {
                            // add one
                        }
                    }
                }

                // Get everyone dialogue
                
                // Get group dialogue (love interest / child?)

                return dialogueList;
            }
            catch (Exception e)
            {
                ErrorLogger.Warn($"Could not load dialogue from 'Strings / StringsFromCsFiles' for {character}. {e}");
                return new List<StardewValley.Dialogue>();
            }
        }


        private List<string> 


        private List<StardewValley.Dialogue> GetSpeechBubbleDialogue(NPC character)
        {
            List<StardewValley.Dialogue> dialogueList = GetOnlyDialogueWithCharacterNameInId(character, "Strings/SpeechBubbles");
            if (dialogueList.Count == 0)
                ErrorLogger.Warn($"No Speech Bubble dialogue found for {character.Name}.");
            return dialogueList;
        }

        private List<StardewValley.Dialogue> GetScheduleDialogue(NPC character)
        {
            List<StardewValley.Dialogue> dialogueList = GetDialogueFromGameContent(character, $"Strings/schedules/{character.getName()}");
            if (dialogueList.Count == 0)
                ErrorLogger.Warn($"No schedule dialogue found for {character.Name}.");
            return dialogueList;
        }

        private List<StardewValley.Dialogue> GetDialogueFromGameContent(NPC character, string path)
        {
            try
            {
                Dictionary<string, string> dialogueData = helper.GameContent.Load<Dictionary<string, string>>(path);
                List<StardewValley.Dialogue> dialogueList = new List<StardewValley.Dialogue>();
                foreach (string key in dialogueData.Keys)
                    dialogueList.Add(character.TryGetDialogue(key));
                return dialogueList;
            }
            catch (Exception e)
            {
                ErrorLogger.Warn($"Could not load dialogue from '{path}' for {character}. {e}");
                return new List<StardewValley.Dialogue>();
            }
        }

        private List<StardewValley.Dialogue> GetOnlyDialogueWithCharacterNameInId(NPC character, string path)
        {
            try
            {
                Dictionary<string, string> dialogueData = helper.GameContent.Load<Dictionary<string, string>>(path);
                List<StardewValley.Dialogue> dialogueList = new List<StardewValley.Dialogue>();

                foreach (string key in dialogueData.Keys)
                {
                    if (StringContainsCharacter(key, character.getName()))
                        dialogueList.Add(character.TryGetDialogue(key));
                }

                return dialogueList;
            }
            catch (Exception e)
            {
                ErrorLogger.Warn($"Could not load dialogue from '{path}' for {character}. {e}");
                return new List<StardewValley.Dialogue>();
            }
        }

        private bool StringContainsCharacter(string str, string characterName)
        {
            return DialogueHelpers.StringContainsSupportedCharacter(str) && str.Contains(characterName)
                || !DialogueHelpers.StringContainsSupportedCharacter(str);
        }

        private bool StringContainsSpouse(string str)
        {
            // Mines_PlayerKilled_Maru_Spouse is possible and shouldn't trigger for all NPCs
            return !DialogueHelpers.StringContainsSupportedCharacter(str) && str.Contains("Spouse");
        }
    }
}
