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

namespace StardewVoices.Dialogue
{
    public class DialogueManager
    {
        private static Dictionary<string, Dictionary<string, string[]>> genericDialogueDictionariesDictionary = new Dictionary<string, Dictionary<string, string[]>>();
        private FestivalHandler _festivalHandler { get; }
        private EventHandler _eventHandler { get; }
        private CharacterDialogueHandler _characterDialogueHandler { get; }
        private ContinuationDialogue _continuationDialogue { get; }
        private PathManager _pathManager { get; }
        private CharacterManager _characterManager { get; }
        private GameEvents _gameEvents { get; }

        Dictionary<string, string> dialogueWithRandomNounOrAdj = new Dictionary<string, string>();
        private bool isReplacementDialogueBox = false;

        public DialogueManager(PathManager pathManager, CharacterManager characterManager, GameEvents gameEvents) {
            if (!Directory.Exists(pathManager.GetPath("Characters").ToString()))
                throw new Exception("Character dialogue folder could not be found");

            _pathManager = pathManager;
            _characterManager = characterManager;
            _gameEvents = gameEvents;

            _festivalHandler = new FestivalHandler(pathManager);
            _eventHandler = new EventHandler();
            _characterDialogueHandler = new CharacterDialogueHandler(pathManager, characterManager);
            _continuationDialogue = new ContinuationDialogue(characterManager);



            dialogueWithRandomNounOrAdj.Add("Last night I dreamt that my left hand had turned into a gigantic %noun. Does that mean anything?", "Dialogue-fall_Thu_chance_1");
            dialogueWithRandomNounOrAdj.Add("I did, but all I remember is that it had something to do with a %adj %noun. Weird.", "Generic Marriage Dialogue-Indoor_Day_3_sv1");
        }

        private string getIdFromDialogueString(string dialogueString, string currentSpeaker)
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
                string eventDialogue = _eventHandler.CheckForDialogueInEvent(formattedDialogueString, currentSpeaker, _characterDialogueHandler);
                if(eventDialogue != null)
                {
                    return eventDialogue;
                }

                string festivalDialogue = _festivalHandler.CheckForDialogueInFestival(formattedDialogueString, currentSpeaker, _characterDialogueHandler);
                if(festivalDialogue != null)
                {
                    return festivalDialogue;
                }
                // If not currently in event or festival check all dialogue dictionaries
                else
                {
                    // LEFT OFF HERE!!!!!! 5/7
                    List<Dictionary<string, string[]>> characterDialogueDictionaries = new List<Dictionary<string, string[]>>();
                    List<string> dialogueDictionaryNames = new List<string>();


                    Dictionary<string, string[]> engagementDialogueDict = currentSpeakerDialogue["engagement"];
                    Dictionary<string, string[]> genericMarriageDialogueDict = currentSpeakerDialogue["generic_marriage"];
                    Dictionary<string, string[]> extraDialogueDict = currentSpeakerDialogue["extra"];
                    Dictionary<string, string[]> rainyDialogueDict = currentSpeakerDialogue["rainy"];


                    Dictionary<string, string[]> marriageDialogueDict = currentSpeakerDialogue["marriage_dialogue"];
                    Dictionary<string, string[]> npcGiftTastesDialogueDict = currentSpeakerDialogue["gift_tastes"];

                    if (Game1.player.isEngaged() || testingWorldLoaded)
                    {
                        characterDialogueDictionaries.Add(engagementDialogueDict);
                        dialogueDictionaryNames.Add("Engagement_Dialogue-");
                    }

                    if (Game1.player.getSpouse()?.getName() == currentSpeaker || testingWorldLoaded)
                    {
                        characterDialogueDictionaries.Add(marriageDialogueDict);
                        dialogueDictionaryNames.Add("Marriage_Dialogue-");

                        characterDialogueDictionaries.Add(genericMarriageDialogueDict);
                        dialogueDictionaryNames.Add("Generic_Marriage_Dialogue-");
                    }

                    characterDialogueDictionaries.Add(dialogueDict);
                    dialogueDictionaryNames.Add("Dialogue-");

                    characterDialogueDictionaries.Add(extraDialogueDict);
                    dialogueDictionaryNames.Add("Extra_Dialogue-");

                    characterDialogueDictionaries.Add(rainyDialogueDict);
                    dialogueDictionaryNames.Add("Rainy-");

                    characterDialogueDictionaries.Add(npcGiftTastesDialogueDict);
                    dialogueDictionaryNames.Add("NpcGiftTastes-");

                    characterDialogueDictionaries.Add(miscDialogueDict);
                    dialogueDictionaryNames.Add("Internal_Dialogue-");

                    string[] splitDialogueString = formattedDialogueString.Split(" ");

                    for (int i = 0; i < characterDialogueDictionaries.Count; i++)
                    {
                        if (characterDialogueDictionaries[i].ContainsKey(formattedDialogueString))
                        {
                            if (characterDialogueDictionaries[i][formattedDialogueString].Length > 1)
                            {
                                return DialogueHelpers.getCorrectIdFromListOfLineIds(characterDialogueDictionaries[i][formattedDialogueString], currentSpeaker, dialogueDictionaryNames[i]);
                            }

                            return characterDialogueDictionaries[i][formattedDialogueString][0];
                        }

                        if (dialogueDictionaryNames[i] == "Extra_Dialogue-" || dialogueDictionaryNames[i] == "Internal_Dialogue-")
                        {
                            foreach (string key in characterDialogueDictionaries[i].Keys)
                            {
                                if (!key.Contains("{") && !key.Contains("%"))
                                {
                                    continue;
                                }

                                string[] splitKey = key.Split(" ");


                                // Check to see if the amount of shared words is ALMOST equivalent to the length minus the number of substitutions possible (i've only seen up to 3)
                                if (splitDialogueString.Intersect(splitKey).Count() > 0 && splitDialogueString.Length == splitKey.Length && splitDialogueString.Length - splitDialogueString.Intersect(splitKey).Count() <= 3)
                                {
                                    // Check for dad / mom / name stuff
                                    if (splitDialogueString.Contains("Dad") && !splitKey.Contains("Dad")
                                        || splitDialogueString.Contains("Mom") && !splitKey.Contains("Mom")
                                        || (!splitDialogueString.Contains("Dad") && !splitDialogueString.Contains("Mom") && splitKey.Contains("Dad") && splitKey.Contains("Mom")))
                                    {
                                        continue;
                                    }

                                    // check for she/ he stuff her/him
                                    if ((!splitDialogueString.Contains("Dad") && !splitDialogueString.Contains("Mom"))
                                        && (
                                               formattedDialogueString.ToLower().Contains("she") && !key.ToLower().Contains("she")
                                            || formattedDialogueString.ToLower().Contains(" he ") && !key.ToLower().Contains(" he ")
                                            || formattedDialogueString.ToLower().Contains("him") && !key.ToLower().Contains("him")
                                            || formattedDialogueString.ToLower().Contains("her") && !key.ToLower().Contains("her")
                                           )
                                       )
                                    {
                                        continue;
                                    }

                                    if (characterDialogueDictionaries[i][key].Length > 1)
                                    {
                                        return this.getCorrectIdFromListOfLineIds(characterDialogueDictionaries[i][key], currentSpeaker, dialogueDictionaryNames[i]);
                                    }

                                    return characterDialogueDictionaries[i][key][0];

                                }
                            }
                        }

                    }

                    if (_eventHandler.eventsToday.Count > 0)
                    {
                        return _eventHandler.checkEventsThatHappenedTodayForDialogue(formattedDialogueString, currentSpeaker);
                    }

                    // check if it is a %noun %adjective situation
                    foreach (string randDialogueLine in dialogueWithRandomNounOrAdj.Keys)
                    {
                        string[] splitRandDialogueLine = randDialogueLine.Split(" ");
                        if (splitDialogueString.Length == splitRandDialogueLine.Length && splitDialogueString.Length - splitDialogueString.Intersect(splitRandDialogueLine).Count() <= 3)
                        {
                            string newRandDialogueLIne = randDialogueLine.Replace("%noun", "dragon").Replace("%adj", "giant").Replace("%year", "couple");

                            this.changeCurrentDialogueBoxString(newRandDialogueLIne);
                            isReplacementDialogueBox = true;
                            return dialogueWithRandomNounOrAdj[randDialogueLine];
                        }

                    }

                    return $"Line not found for {currentSpeaker}: {formattedDialogueString}";
                }

            }
            catch (Exception ex)
            {
                return $"Line not found for {currentSpeaker}: {formattedDialogueString}. Exception: {ex}";
            }
        }
    }
}