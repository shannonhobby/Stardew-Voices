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
        internal List<Dictionary<string, string>> eventsToday = new List<Dictionary<string, string>>();

        /// <summary>The in-game event detected on the last update tick.</summary>
        internal Event lastEvent = null;
        internal Dictionary<string, string> currentEventDict = null;
        // TODO - i forget what this is for exactly but it's important
        internal Dictionary<string, string> eventResponseDict = null;

        public EventHandler()
        {

        }

        public string CheckForDialogueInEvent(string dialogue, string currentSpeaker, CharacterDialogueHandler characterDialogueHandler)
        {
            if(EventsIsHappening())
            {
                if (this.currentEventDict.ContainsKey(dialogue))
                {
                    return this.currentEventDict[dialogue];
                }
            } else
            {
                // this doesn't really do anything? might be safe to remove
                string closeEnoughKey = DialogueHelpers.ContainsCloseEnoughLine(this.currentEventDict, dialogue);

                // Check surrounding event files and also the dialogue file to see if the dialogue is there:
                if (this.eventResponseDict != null && this.eventResponseDict.ContainsKey(dialogue))
                {
                    return eventResponseDict[dialogue];
                }

                string eventDialogue = checkForDialogueInNearbyEvents(dialogue, currentSpeaker, Game1.CurrentEvent.id);

                // If it's not found in a nearby event it could be in dialogue
                if (eventDialogue.Contains("not found"))
                {

                    characterDialogueHandler.CheckEverydayCharacterDictionariesForDialogue(currentSpeaker, dialogue);
                }
                else
                {
                    return eventDialogue;
                }

                return $"Event dialogue not found for {dialogue}";
            }

        }

        private Boolean EventsIsHappening()
        {
            return Game1.CurrentEvent != null && this.currentEventDict != null;
        }

        internal string checkEventsThatHappenedTodayForDialogue(string dialogueString, string currentSpeaker)
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