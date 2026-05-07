using Newtonsoft.Json;
using StardewValley;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System;

namespace StardewVoices.Dialogue
{
    public class EventHandler
    {
        private List<Dictionary<string, string>> eventsToday = new List<Dictionary<string, string>>();

        /// <summary>The in-game event detected on the last update tick.</summary>
        internal Event lastEvent = null;
        internal Dictionary<string, string> currentEventDict = null;
        internal Dictionary<string, string> eventResponseDict = null;

        public EventHandler()
        {

        }

        public Dictionary<string, string> CheckForEvent()
        {

        }

        private string checkEventsThatHappenedTodayForDialogue(string dialogueString, string currentSpeaker)
        {
            try
            {
                foreach (Dictionary<string, string> dictionary in this.eventsToday)
                {
                    if (dictionary.ContainsKey(dialogueString) && dictionary[dialogueString].Contains(currentSpeaker))
                    {
                        return dictionary[dialogueString];
                    }
                }

                return $"Event end dialogue not found for {currentSpeaker}: {dialogueString}";
            }
            catch (Exception ex)
            {
                return $"Event end dialogue not found for {currentSpeaker}: {dialogueString}, Exception: {ex}";
            }

        }

        private string checkForDialogueInNearbyEvents(string dialogueString, string currentSpeaker, string currentEventId)
        {

            try
            {
                if (Game1.currentLocation != null)
                {
                    string eventPath = Path.Combine(eventDialogueFolderPath, Game1.currentLocation.Name);
                    if (!Directory.Exists(eventPath))
                    {
                        return $"Directory not found at {eventPath}";
                    }

                    string[] eventFilePaths = Directory.GetFiles(eventPath);


                    foreach (string file in eventFilePaths)
                    {

                        string jsonString = File.ReadAllText(file, Encoding.UTF8);
                        Dictionary<string, string> tempEventDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonString);

                        // It needs to contain the dialogue line AND it has to be said by the right person
                        if (tempEventDict.ContainsKey(dialogueString) && tempEventDict[dialogueString].Contains(currentSpeaker))
                        {
                            this.eventResponseDict = tempEventDict;
                            return tempEventDict[dialogueString];
                        }
                    }

                    return $"Event dialogue not found for: {dialogueString} for {currentSpeaker}";
                }

                return $"Event dialogue not found for: {dialogueString} for {currentSpeaker}. No location.";
            }
            catch (Exception ex)
            {
                return $"Event dialogue not found for: {dialogueString} for {currentSpeaker}. Exception: {ex}";
            }

        }
    }
}