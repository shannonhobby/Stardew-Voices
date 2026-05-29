using StardewModdingAPI;
using StardewValley;
using StardewVoices.Dialogue;
using System;
using System.Collections.Generic;

namespace StardewVoices.Utils.GameDialogueExtraction
{
    public class CharacterDialogueExtractor
    {
        private readonly IModHelper helper;

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

        private List<StardewValley.Dialogue> GetGenericDialogue(NPC character)
        {
            try
            {
                List<StardewValley.Dialogue> dialogue = GetMovieDialogue(character);
                if (dialogue.Count == 0)
                    ErrorLogger.Warn($"No movie dialogue found for {character.Name}.");
                dialogue.AddRange(GetExtraDialogue(character));
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
