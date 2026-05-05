using Newtonsoft.Json;
using StardewModdingAPI;
using System.Collections.Generic;
using System.Text;
using System;

namespace StardewVoices.DialogueManager
{
    public static class FestivalHandler
    {
        private Dictionary<string, string> festivalNameToDateDict = new Dictionary<string, string>();


        /// <summary>The in-game festival detected on the last update tick.</summary>
        private string lastFestival = null;
        private Dictionary<string, string[]> currentFestivalDict = null;

        private void setupFestivalDictionary()
        {
            try
            {
                string jsonString = File.ReadAllText(festivalDatesJsonPath, Encoding.UTF8);
                this.festivalNameToDateDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonString);
            }
            catch (Exception ex)
            {
                this.Monitor.Log($"Error setting up festival dictionaries: {ex}. Audio will not play.", LogLevel.Error);

            }

        }

    }
}