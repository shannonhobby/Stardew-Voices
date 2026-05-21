using StardewModdingAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StardewValley;
using StardewVoices.Utils;

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

        public StardewValley.Dialogue[] GetAllDialogueForCharacter(NPC character)
        {
            return character.CurrentDialogue?.ToArray();
        }

        private List<StardewValley.Dialogue> GetEngagementDialogueForCharacter(NPC character)
        {
            try
            {
                List<StardewValley.Dialogue> dialogue = new List<StardewValley.Dialogue>();
                dialogue.Add(character.TryGetDialogue($"{character.Name}0"));
                dialogue.Add(character.TryGetDialogue($"{character.Name}1"));
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
                List<StardewValley.Dialogue> dialogue = new List<StardewValley.Dialogue>();
                
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
                List<StardewValley.Dialogue> dialogue = new List<StardewValley.Dialogue>();
        
                return dialogue;
            }
            catch (Exception e)
            {
                ErrorLogger.Error($"Error getting Marriage dialogue. {e}");
                return null;
            }
        }

        private List<StardewValley.Dialogue> GetAllPossibleGossipDialogues(NPC character)
        {

        }

        private StardewValley.Dialogue GetRainyDialogue(NPC character)
        {
            return StardewValley.Dialogue.TryGetDialogue(character, "Characters\\Dialogue\\rainy:" + character.GetDialogueSheetName());
        }

        private StardewValley.Dialogue GetDivorceDialogue(NPC character)
        {
            return StardewValley.Dialogue.TryGetDialogue(character, "Characters\\Dialogue\\" + character.GetDialogueSheetName() + ":divorced");
        }

        private List<StardewValley.Dialogue> GetDailyDialogue(NPC character)
        {
            GetSeasonalDialogue(character);
            GetGenericDialogue(character);
        }

        private List<StardewValley.Dialogue> GetSeasonalDialogue(NPC character)
        {
            List<string> seasons = new List<string>() { "spring", "summer", "fall", "winter" };
            List<int> heartLevels = new List<int>() { 2, 4, 6, 8, 10 };
           
            foreach (string season in seasons)
            {
                // winter_4
                for (int day = 1; day <= 28 ; day++) {
                
                }

                for(int dayOfweek = 0; dayOfweek < 7; dayOfweek++)
                {
                    // summer_Wed

                    //  summer_Thu4 {season_dayHeartLevel}
                    foreach (int heartLevel in heartLevels)
                    {

                    }
                }
            
            }


            for (int dayOfweek = 0; dayOfweek < 7; dayOfweek++)
            {
                // Thur


                //  Wed6
                foreach (int heartLevel in heartLevels)
                {

                }
            }

            foreach (int heartLevel in heartLevels)
            {
                // Can literally just be "6"
            }

            // inlaw?? only for certain characters

            // what about eventseen
        }

        private List<StardewValley.Dialogue> GetGenericDialogue(NPC character)
        {

        }
    }
}
