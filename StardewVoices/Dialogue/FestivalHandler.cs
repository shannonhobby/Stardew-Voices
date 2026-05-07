using Newtonsoft.Json;
using StardewModdingAPI;
using System.Collections.Generic;
using System.Text;
using System;
using StardewVoices.Pathing;
using System.IO;
using StardewValley;


namespace StardewVoices.Dialogue
{
    public class FestivalHandler
    {
        private Dictionary<string, string> festivalNameToDateDict = new Dictionary<string, string>();

        /// <summary>The in-game festival detected on the last update tick.</summary>
        public string lastFestival { get; set; }
        /// <summary>Dialogue for the currently active festival.</summary>
        public Dictionary<string, string[]> currentFestivalDict { get; set; }

        public FestivalHandler(PathManager pathManager)
        {
            try
            {
                string jsonString = File.ReadAllText(pathManager.GetPathString(pathManager.FestivalDatesJsonName), Encoding.UTF8);
                this.festivalNameToDateDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonString);
            }
            catch (Exception ex)
            {
                this.Monitor.Log($"Error setting up festival dictionaries: {ex}. Audio will not play.", LogLevel.Error);

            }
        }

        public string CheckForDialogueInFestival(string dialogue, string currentSpeaker, CharacterDialogueHandler characterDialogueHandler)
        {
            if (IsInFestival())
            {

                if (this.currentFestivalDict.ContainsKey(dialogue))
                {
                    if (this.currentFestivalDict[dialogue].Length > 1)
                    {
                        return DialogueHelpers.getCorrectIdFromListOfLineIds(this.currentFestivalDict[dialogue], currentSpeaker, "Festival Dialogue-");
                    }

                    return this.currentFestivalDict[dialogue][0];
                } else
                {
                    return characterDialogueHandler.CheckEverydayCharacterDictionariesForDialogue(currentSpeaker, dialogue);
                }
            }
            else
            {
                return null;
            }
        }

        public Boolean IsInFestival()
        {
            return Game1.CurrentEvent != null && Game1.CurrentEvent.isFestival && this.currentFestivalDict != null;
        }
    }
}