using StardewModdingAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StardewValley;
using StardewVoices.Utils;
using System.Text.RegularExpressions;
using StardewVoices.Characters;
using System.IO;
using StardewVoices.Dialogue;
using StardewVoices.Testing.Models;
using Microsoft.Xna.Framework.Input;
using StardewValley.BellsAndWhistles;
using StardewValley.GameData.Characters;
using StardewValley.ItemTypeDefinitions;
using StardewValley.TokenizableStrings;

namespace StardewVoices.Testing.Scanners
{
    /// <summary>Enumerate NPC dialogue assets</summary>
    internal class DialogueScanner
    {
        private readonly IModHelper helper;

        public DialogueScanner(IModHelper helper)
        {
            this.helper = helper;
        }

        public void ScanAllDialogue()
        {

        }
  

        public Dictionary<DialogueType, List<StardewValley.Dialogue>> GetAllDialogueForCharacter(NPC character)
        {
            return new Dictionary<DialogueType, List<StardewValley.Dialogue>>()
            {
                { DialogueType.Dialogue, GetCharacterDialogue(character) },
                { DialogueType.Engagement, GetEngagementDialogueForCharacter(character) },
                { DialogueType.Rainy, new List<StardewValley.Dialogue>(){GetRainyDialogue(character) } },
                { DialogueType.Marriage, GetMarriageDialogueForCharacter(character) },
                { DialogueType.Generic, GetGenericDialogue(character) },
                { DialogueType.Schedule, GetScheduleDialogue(character) },
            };
        }

        private List<StardewValley.Dialogue> GetEngagementDialogueForCharacter(NPC character)
        {
            try
            {
                List<StardewValley.Dialogue> dialogue = new List<StardewValley.Dialogue>
                {
                    character.TryGetDialogue($"{character.Name}0"),
                    character.TryGetDialogue($"{character.Name}1")
                };
                return dialogue;
            } catch (Exception e)
            {
                ErrorLogger.Error($"Error getting engagement dialogue. {e}");
                return null;
            }
        }

        private List<StardewValley.Dialogue> GetMarriageDialogueForCharacter(NPC character)
        {
            try
            {
                List<StardewValley.Dialogue> dialogue = GetDialogueFromGameContent(character, $"Characters/Dialogue/MarriageDialogue{character.name}");
                if (dialogue.Count == 0)
                {
                    ErrorLogger.Warn($"Error retrieving marriage dialogue for {character.Name}");
                }

                dialogue.AddRange(GetGenericMarriageDialogueForCharacter(character));

                return dialogue;
            }
            catch (Exception e)
            {
                ErrorLogger.Error($"Error getting Marriage dialogue. {e}");
                return null;
            }
        }

        private List<StardewValley.Dialogue> GetGenericMarriageDialogueForCharacter(NPC character)
        {
            try
            {
                return GetOnlyDialogueWithCharacterNameInId(character, "Characters/Dialogue/MarriageDialogue");
            }
            catch (Exception e)
            {
                ErrorLogger.Error($"Error getting Generic Marriage dialogue. {e}");
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

            if(dialogueList.Count == 0)
            {
                ErrorLogger.Warn($"Error retrieving character dialogue for {character.Name}");
            }

            return dialogueList;
        }

        private List<StardewValley.Dialogue> GetDialogueFromGameContent(NPC character, string path)
        {
            try
            {
                Dictionary<string, string> dialogueData = helper.GameContent.Load<Dictionary<string, string>>(
                           path
                         );
                List<StardewValley.Dialogue> dialogueList = new List<StardewValley.Dialogue>();

                foreach (string key in dialogueData.Keys)
                {
                    dialogueList.Add(character.TryGetDialogue(key));
                }


                return dialogueList;
            }
            catch (Exception e)
            {
                ErrorLogger.Warn($"Error Retrieving dialogue from: {path} for {character}. {e}");
                return new List<StardewValley.Dialogue>();
            }
         
        }

        private List<StardewValley.Dialogue> GetOnlyDialogueWithCharacterNameInId(NPC character, string path)
        {
            try
            {
                Dictionary<string, string> dialogueData = helper.GameContent.Load<Dictionary<string, string>>(
                           path
                        );
                List<StardewValley.Dialogue> dialogueList = new List<StardewValley.Dialogue>();

                foreach (string key in dialogueData.Keys)
                {
                    // Don't add lines for other characters
                    if (StringContainsCharacter(key, character.getName()))
                    {
                        dialogueList.Add(character.TryGetDialogue(key));

                    }
                }

                return dialogueList;
            }
            catch (Exception e)
            {
                ErrorLogger.Warn($"Error Retrieving dialogue from: {path} for {character}. {e}");
                return new List<StardewValley.Dialogue>();
            }

        }

        private List<StardewValley.Dialogue> GetGenericDialogue(NPC character)
        {
            try
            {
                List<StardewValley.Dialogue> dialogue = GetMovieDialogue(character);
                if (dialogue.Count == 0)
                {
                    ErrorLogger.Warn($"Error retrieving movie dialogue for {character.Name}");
                }

                dialogue.AddRange(GetExtraDialogue(character));


                return dialogue;
            }
            catch (Exception e)
            {
                ErrorLogger.Error($"Error getting Marriage dialogue. {e}");
                return null;
            }
        }

        // For dialogue lines from Data/ExtraDialogue.json
        private List<StardewValley.Dialogue> GetExtraDialogue(NPC character)
        {
            Dictionary<string, string> dialogueData = helper.GameContent.Load<Dictionary<string, string>>(
                         "Data/ExtraDialogue"
                      );
            List<StardewValley.Dialogue> dialogueList = new List<StardewValley.Dialogue>();

            foreach (string key in dialogueData.Keys)
            {
                // Don't add lines for other characters
                if (StringContainsCharacter(key, character.getName())
                    || StringContainsSpouse(key) 
                    || key.Contains("NewChild") 
                    || key.Contains("LostItemQuest")
                    || key.Equals("Town_DumpsterDiveComment_Teen") 
                    ||key.Equals("Town_DumpsterDiveComment_Adult"))
                {
                    dialogueList.Add(character.TryGetDialogue(key));

                }
            }

            return dialogueList;
        }

        private bool StringContainsSpouse(string str)
        {
            // Mines_PlayerKilled_Maru_Spouse is possible and shouldn't trigger for all NPCs
            return !DialogueHelpers.StringContainsSupportedCharacter(str) && str.Contains("Spouse");

        }

        private bool StringContainsCharacter(string str, string characterName)
        {
            return DialogueHelpers.StringContainsSupportedCharacter(str) && str.Contains(characterName)
                    || !DialogueHelpers.StringContainsSupportedCharacter(str);
        }

        private List<StardewValley.Dialogue> GetMovieDialogue(NPC character)
        {
            List<StardewValley.Dialogue> dialogueList = GetOnlyDialogueWithCharacterNameInId(character, $"Strings/MovieReactions");

            if (dialogueList.Count == 0)
            {
                ErrorLogger.Warn($"Error retrieving movie dialogue for {character.Name}");
            }

            return dialogueList; 
        }

        private List<StardewValley.Dialogue> GetScheduleDialogue(NPC character)
        {
            List<StardewValley.Dialogue> dialogueList = GetDialogueFromGameContent(character, $"Strings/schedules/{character.getName()}");

            if (dialogueList.Count == 0)
            {
                ErrorLogger.Warn($"Error retrieving schedule dialogue for {character.Name}");
            }

            return dialogueList;
        }
    }
}
