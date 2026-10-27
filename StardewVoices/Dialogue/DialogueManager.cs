using StardewValley.Monsters;
using StardewVoices.Audio;
using StardewVoices.Characters;
using StardewVoices.Pathing;
using StardewVoices.Testing;
using StardewVoices.Utils;
using System;
using System.Collections.Generic;
using System.IO;

namespace StardewVoices.Dialogue
{
    public class DialogueManager
    {
        private static Dictionary<string, Dictionary<string, string[]>> genericDialogueDictionariesDictionary = new Dictionary<string, Dictionary<string, string[]>>();
        private FestivalHandler festivalHandler { get; }
        private EventHandler eventHandler { get; }
        private CharacterDialogueHandler characterDialogueHandler { get; }
        private TextBubbleHandler textBubbleHandler { get; }
        private PathManager _pathManager { get; }
        private CharacterManager _characterManager { get; }
        private TestingManager _testingManager { get; }
        public VariableLineResolver VariableLineResolver { get; }

        public DialogueManager(PathManager pathManager,
            CharacterManager characterManager,
            VoicePackManager voicePackManager,
            TestingManager testingManager,
            VariableLineResolver variableLineResolver
            ) {
            if (!Directory.Exists(pathManager.GetPath("Characters").ToString()))
                throw new Exception("Character dialogue folder could not be found");

            _pathManager = pathManager;
            _characterManager = characterManager;
            _testingManager = testingManager;
            VariableLineResolver = variableLineResolver;

            textBubbleHandler = new TextBubbleHandler();
            festivalHandler = new FestivalHandler(pathManager);
            eventHandler = new EventHandler(pathManager);
            characterDialogueHandler = new CharacterDialogueHandler(pathManager, characterManager, voicePackManager, _testingManager);
            ContinuationDialogueHelper.Init(characterManager);
        }

        // Variable Lines



        // Events

        public void EndEvent()
        {
            eventHandler.currentEventDict = null;
            eventHandler.eventResponseDict = null;

            textBubbleHandler.eventCommandsHaveBeenLogged = false;
        }

        public void StartEvent(string eventFilePath)
        {
            eventHandler.currentEventDict = DialogueHelpers.GetEventDictionaryFromPath(eventFilePath);
        }

        public Dictionary<string, string> GetCurrentEventDict()
        {
            return eventHandler.currentEventDict;
        }

        public void SetCurrentEventDictToNull()
        {
            eventHandler.currentEventDict = null;
        }

        public void ResetEventsToday()
        {
            eventHandler.eventsToday = new List<Dictionary<string, string>>();
        }


        //Festivals
        public string GetLastFestival()
        {
            return festivalHandler.lastFestival;
        }

        public void SetFestival(string festivalName)
        {
            festivalHandler.SetFestival(festivalName);
            eventHandler.currentEventDict = null;
        }

        public void ResetFestival()
        {
            festivalHandler.ResetFestival();
        }

        public (String textAboveHeadLineId, String currentSpeaker)? CheckForTextAboveHead()
        {
             var textAboveHeadDialogueAndSpeaker = textBubbleHandler.CheckForTextAboveHead();


            if (GetCurrentEventDict() != null && textAboveHeadDialogueAndSpeaker != null && textAboveHeadDialogueAndSpeaker?.commandDialogue != null)
            {

                string textAboveHeadLineId = getIdFromDialogueString(DialogueHelpers.formatDialogueString(textAboveHeadDialogueAndSpeaker?.commandDialogue), textAboveHeadDialogueAndSpeaker?.currentSpeaker);

                return (textAboveHeadLineId, textAboveHeadDialogueAndSpeaker?.currentSpeaker);
            }

            return null;
        }

        public string getIdFromDialogueString(string dialogueString, string currentSpeaker)
        {

            if (!Directory.Exists(_pathManager.GetPathString("Dialogue Jsons")))
                return "Character dialogue folder could not be found";

            if (currentSpeaker == null)
            {
                return $"Current speaker is null for {dialogueString}";
            }

            if (dialogueString == null)
            {
                return $"Dialogue String is null.";
            }


            // TODO - this is just hardcoding manully replacing variable strings
            string formattedDialogueString = DialogueHelpers.formatDialogueString(dialogueString);

            try
            {
                string eventDialogueLineId = eventHandler.CheckForDialogueInEvent(formattedDialogueString, currentSpeaker, characterDialogueHandler, _testingManager.ValidationIsRunning);
                if(eventDialogueLineId != null)
                {
                    return eventDialogueLineId;
                }

                string festivalDialoguLineId = festivalHandler.CheckForDialogueInFestival(formattedDialogueString, currentSpeaker, characterDialogueHandler, _testingManager.ValidationIsRunning);
                if(festivalDialoguLineId != null)
                {
                    return festivalDialoguLineId;
                }


                // If not currently in event or festival check all character's dialogue dictionaries
                string foundLineId = characterDialogueHandler.CheckForCharacterDialogueLineId(currentSpeaker, dialogueString);
               

                if(foundLineId != null)
                {
                    // This checks for variable dialogue lines and
                    // chooses a random audio file id configured in
                    // variable_lines.json
                    return VariableLineResolver.Resolve(foundLineId, currentSpeaker);
                }


                // If an event happened today there could be "Event End Dialogue" that is
                // stored in the event but is said after it is complete.
                if (eventHandler.eventsToday.Count > 0)
                {
                    foundLineId = eventHandler.checkEventsThatHappenedTodayForDialogue(formattedDialogueString, currentSpeaker);
                }

                if (foundLineId != null)
                {
                    return foundLineId;
                }

                return $"Line not found for {currentSpeaker}: {formattedDialogueString}";

            }
            catch (Exception ex)
            {
                return $"Line not found for {currentSpeaker}: {formattedDialogueString}. Exception: {ex}";
            }
        }

    }
}