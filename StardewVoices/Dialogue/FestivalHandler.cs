using Newtonsoft.Json;
using StardewModdingAPI;
using System.Collections.Generic;
using System.Text;
using System;
using StardewVoices.Pathing;
using System.IO;


namespace StardewVoices.Dialogue
{
    public class FestivalHandler
    {
        private Dictionary<string, string> festivalNameToDateDict = new Dictionary<string, string>();

        /// <summary>The in-game festival detected on the last update tick.</summary>
        public string lastFestival { get; set; }
        /// <summary>Dialogue for the currently active festival.</summary>
        public DialogueDictionary currentFestivalDict { get; set; }

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
    }
}