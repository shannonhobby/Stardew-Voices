using StardewValley;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System;
using Newtonsoft.Json;
using StardewModdingAPI;
using System.Text;
using StardewVoices.Pathing;
using StardewVoices.Characters;
using StardewVoices.EventManager;
using StardewVoices.Testing;
using StardewVoices.Localization;
using StardewVoices.Audio;

namespace StardewVoices.Dialogue
{
    public class DialogueManager
    {
        private static Dictionary<string, Dictionary<string, string[]>> genericDialogueDictionariesDictionary = new Dictionary<string, Dictionary<string, string[]>>();
        private FestivalHandler festivalHandler { get; }
        private EventHandler eventHandler { get; }
        private CharacterDialogueHandler characterDialogueHandler { get; }
        private TextBubbleHandler textBubbleHandler { get; }
        private ContinuationDialogue _continuationDialogue { get; }
        private PathManager _pathManager { get; }
        private CharacterManager _characterManager { get; }
        private AudioManager _audioManager { get; }
        private GameEvents _gameEvents { get; }
        private TestingManager _testingManager { get; }


        Dictionary<string, string> dialogueWithRandomNounOrAdj = new Dictionary<string, string>();
        // When I have programatically updated the text

        public DialogueManager(PathManager pathManager,
            CharacterManager characterManager, 
            GameEvents gameEvents,
            TestingManager testingManager,
            LocalizationManager localizationManager,
            AudioManager audioManager
            ) {
            if (!Directory.Exists(pathManager.GetPath("Characters").ToString()))
                throw new Exception("Character dialogue folder could not be found");

            _pathManager = pathManager;
            _characterManager = characterManager;
            _gameEvents = gameEvents;
            _testingManager = testingManager;
            _audioManager = audioManager;

            textBubbleHandler = new TextBubbleHandler(this);
            festivalHandler = new FestivalHandler(pathManager, localizationManager);
            eventHandler = new EventHandler(pathManager);
            characterDialogueHandler = new CharacterDialogueHandler(pathManager, characterManager, _testingManager);
            _continuationDialogue = new ContinuationDialogue(characterManager);



            dialogueWithRandomNounOrAdj.Add("Last night I dreamt that my left hand had turned into a gigantic %noun. Does that mean anything?", "Dialogue-fall_Thu_chance_1");
            dialogueWithRandomNounOrAdj.Add("I did, but all I remember is that it had something to do with a %adj %noun. Weird.", "Generic Marriage Dialogue-Indoor_Day_3_sv1");
        }

        // Audio Manager Calls

        public void Play(string lineId, string characterName)
        {
            _audioManager.Play(lineId, characterName);
        }

        public void StopAudioCue()
        {
            _audioManager.StopCue();
        }

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

        public void CheckForTextAboveHead()
        {
            textBubbleHandler.CheckForTextAboveHead();
        }

        public string getIdFromDialogueString(string dialogueString, string currentSpeaker)
        {

            if (!Directory.Exists(_pathManager.GetPathString("Dialogue Jsons")))
                return "Character dialogue folder could not be found";

            if (currentSpeaker == null)
            {
                return $"Current speaker is null for {dialogueString}";
            }

            DialogueDictionary currentSpeakerDialogueDict = _characterManager.GetCharacter(currentSpeaker).GetDialogueDictionary();

            // TODO - this is just hardcoding manully replacing variable strings
            string formattedDialogueString = DialogueHelpers.formatDialogueString(dialogueString);

            try
            {
                string eventDialogueLineId = eventHandler.CheckForDialogueInEvent(formattedDialogueString, currentSpeaker, characterDialogueHandler);
                if(eventDialogueLineId != null)
                {
                    return eventDialogueLineId;
                }

                string festivalDialoguLineId = festivalHandler.CheckForDialogueInFestival(formattedDialogueString, currentSpeaker, characterDialogueHandler);
                if(festivalDialoguLineId != null)
                {
                    return festivalDialoguLineId;
                }

                // If an event happened today there could be "Event End Dialogue" that is
                // stored in the event but is said after it is complete.
                if (eventHandler.eventsToday.Count > 0)
                {
                    return eventHandler.checkEventsThatHappenedTodayForDialogue(formattedDialogueString, currentSpeaker);
                }

                // If not currently in event or festival check all character's dialogue dictionaries
                string foundLineId = characterDialogueHandler.CheckForCharacterDialogueLineId(currentSpeaker, dialogueString);
                if(foundLineId != null)
                {
                    return foundLineId;
                }

                // check if it is a %noun %adjective situation
                foundLineId = checkDialogueWithRandVariable(dialogueString);
                if(foundLineId != null)
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

        private string checkDialogueWithRandVariable(string dialogueLine)
        {
            string[] splitDialogueLine = dialogueLine.Split(" ");
            foreach (string randDialogueLine in dialogueWithRandomNounOrAdj.Keys)
            {
                string[] splitRandDialogueLine = randDialogueLine.Split(" ");
                // TODO - IS THIS LOGIC ANYTHING??
                if (splitDialogueLine.Length == splitRandDialogueLine.Length && splitDialogueLine.Length - splitDialogueLine.Intersect(splitRandDialogueLine).Count() <= 3)
                {
                    string newRandDialogueLIne = randDialogueLine.Replace("%noun", "dragon").Replace("%adj", "giant").Replace("%year", "couple");

                    _gameEvents.changeCurrentDialogueBoxString(newRandDialogueLIne);
                    isReplacementDialogueBox = true;
                    return dialogueWithRandomNounOrAdj[randDialogueLine];
                }
            }

            return null;
        }
    }
}