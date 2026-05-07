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

            DialogueDictionary currentSpeakerDialogueDict;
            Dictionary<string, string[]> dialogueDict;
            Dictionary<string, string[]> miscDialogueDict;


            try
            {
                currentSpeakerDialogueDict = _characterManager.GetCharacter(currentSpeaker).GetDialogueDictionary();

                dialogueDict = currentSpeakerDialogueDict.GetType("dialogue");
                miscDialogueDict = currentSpeakerDialogueDict.GetType("misc");

            }
            catch (Exception ex)
            {
                return $"Dialogue dictionaries not found for {currentSpeaker}. Exception: {ex}";
            }

            string formattedDialogueString = this.formatDialogueString(dialogueString);

            // if in event
            try
            {
                if (Game1.CurrentEvent != null && this.currentEventDict != null)
                {
                    if (this.currentEventDict.ContainsKey(formattedDialogueString))
                    {
                        return this.currentEventDict[formattedDialogueString];
                    }
                    else
                    {
                        string closeEnoughKey = ContainsCloseEnoughLine(this.currentEventDict, formattedDialogueString);
                        if (!closeEnoughKey.Contains("Not found"))
                        {
                            return this.currentEventDict[closeEnoughKey];
                        }

                        // Check surrounding event files and also the dialogue file to see if the dialogue is there:
                        if (this.eventResponseDict != null && this.eventResponseDict.ContainsKey(formattedDialogueString))
                        {
                            return eventResponseDict[formattedDialogueString];

                        }

                        string eventDialogue = this.checkForDialogueInNearbyEvents(formattedDialogueString, currentSpeaker, Game1.CurrentEvent.id);

                        // If it's not found in a nearby event it could be in dialogue
                        if (eventDialogue.Contains("not found"))
                        {
                            if (dialogueDict.ContainsKey(formattedDialogueString))
                            {

                                if (dialogueDict[formattedDialogueString].Length > 1)
                                {
                                    return this.getCorrectIdFromListOfLineIds(dialogueDict[formattedDialogueString], currentSpeaker, "Dialogue-");
                                }

                                return dialogueDict[formattedDialogueString][0];
                            }
                            else if (miscDialogueDict.ContainsKey(formattedDialogueString))
                            {
                                return this.getCorrectIdFromListOfLineIds(miscDialogueDict[formattedDialogueString], currentSpeaker, "Internal Dialogue-");
                            }
                            /**else
                            {
                                closeEnoughKey = DialogueContainsCloseEnoughLine(dialogueDict, formattedDialogueString);
                                 if (!closeEnoughKey.Contains("Not found"))
                                {
                                    if (dialogueDict[closeEnoughKey].Length > 1)
                                    {
                                        return this.getCorrectIdFromListOfLineIds(dialogueDict[closeEnoughKey], currentSpeaker, "Dialogue-");
                                    }else
                                    {
                                        return dialogueDict[closeEnoughKey][0];

                                    }
                                 }
                            } */

                        }
                        else
                        {
                            return eventDialogue;
                        }

                        return $"Event dialogue not found for {formattedDialogueString}";

                    }
                }

                else if (Game1.CurrentEvent != null && Game1.CurrentEvent.isFestival && this.currentFestivalDict != null)
                {
                    if (this.currentFestivalDict.ContainsKey(formattedDialogueString))
                    {
                        if (this.currentFestivalDict[formattedDialogueString].Length > 1)
                        {
                            return this.getCorrectIdFromListOfLineIds(this.currentFestivalDict[formattedDialogueString], currentSpeaker, "Festival Dialogue-");
                        }

                        return this.currentFestivalDict[formattedDialogueString][0];
                    }
                    else
                    {
                        if (dialogueDict.ContainsKey(formattedDialogueString))
                        {
                            if (dialogueDict[formattedDialogueString].Length > 1)
                            {
                                return this.getCorrectIdFromListOfLineIds(dialogueDict[formattedDialogueString], currentSpeaker, "Dialogue-");
                            }

                            return dialogueDict[formattedDialogueString][0];
                        }

                        if (miscDialogueDict.ContainsKey(formattedDialogueString))
                        {
                            if (miscDialogueDict[formattedDialogueString].Length > 1)
                            {
                                return this.getCorrectIdFromListOfLineIds(miscDialogueDict[formattedDialogueString], currentSpeaker, "Internal Dialogue-");
                            }

                            return miscDialogueDict[formattedDialogueString][0];
                        }

                        return $"Festival dialogue not found for {formattedDialogueString}";
                    }
                }

                // If not currently in event or festival check all dialogue dictionaries
                else
                {
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
                                return this.getCorrectIdFromListOfLineIds(characterDialogueDictionaries[i][formattedDialogueString], currentSpeaker, dialogueDictionaryNames[i]);
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

                    if (this.eventsToday.Count > 0)
                    {
                        return this.checkEventsThatHappenedTodayForDialogue(formattedDialogueString, currentSpeaker);
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