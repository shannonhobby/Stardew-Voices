using Microsoft.Xna.Framework.Audio;
using StardewValley;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System;

namespace StardewVoices.Dialogue
{
    public static class TextBubbleHandler {
        private Dictionary<int, string> eventCommandDict = new Dictionary<int, string>();



        private int lastEventCommand;
        private bool eventCommandsHaveBeenLogged = false;
        private void CheckForTextAboveHead()
        {
            if (!eventCommandsHaveBeenLogged && Game1.CurrentEvent != null)
            {
                int count = 0;
                eventCommandDict = new Dictionary<int, string>();
                foreach (string eventCommand in Game1.CurrentEvent.eventCommands)
                {
                    // Sometimes event commands have multiple commands within them which is fun and exciting so I have to split it up 
                    if (eventCommand.Contains("\\"))
                    {
                        string[] splitEventCommand = eventCommand.Split('\\');
                        foreach (string command in splitEventCommand)
                        {
                            eventCommandDict.Add(count, command);
                            count++;
                        }
                    }
                    else
                    {
                        eventCommandDict.Add(count, eventCommand);
                        count++;
                    }


                }
                eventCommandsHaveBeenLogged = true;
            }


            if (Game1.CurrentEvent != null && this.lastEventCommand != Game1.CurrentEvent.currentCommand)
            {

                string currentCommand = "";
                if (Game1.CurrentEvent.currentCommand - this.lastEventCommand > 1)
                {
                    for (int i = 0; i <= Game1.CurrentEvent.currentCommand - this.lastEventCommand; i++)
                    {
                        try
                        {
                            currentCommand = eventCommandDict[this.lastEventCommand + i];
                        }
                        catch (Exception e)
                        {
                            continue;
                        }

                        if (currentCommand.Contains("textAboveHead") && (StringContainsSupportedCharacter(currentCommand) || currentCommand.Contains("ClothesTherapyCharacters")))
                        {
                            break;
                        }

                    }
                }
                else
                {
                    try
                    {
                        currentCommand = eventCommandDict[Game1.CurrentEvent.currentCommand];
                    }
                    catch (Exception e)
                    {
                        this.lastEventCommand = Game1.CurrentEvent.currentCommand;
                        return;
                    }
                }

                if (currentCommand.Contains("textAboveHead") && (StringContainsSupportedCharacter(currentCommand) || currentCommand.Contains("ClothesTherapyCharacters")))
                {
                    Regex rx = new Regex(@"""(.*?)""");
                    string commandDialogue;

                    string currentSpeaker = GetSupportedCharacterFromString(currentCommand);

                    if (currentSpeaker.Contains("Could not find"))
                    {
                        this.lastEventCommand = Game1.CurrentEvent.currentCommand;
                        return;
                    }

                    try
                    {
                        Match m = rx.Match(currentCommand);
                        commandDialogue = m.Value.Replace("\"", "");
                    }
                    catch (Exception e)
                    {
                        //  Monitor.Log($"Error extracting dialogue from textAboveHead event command: {e}", LogLevel.Debug);
                        this.lastEventCommand = Game1.CurrentEvent.currentCommand;
                        return;
                    }


                    if (currentCommand.Contains("ClothesTherapyCharacters") && commandDialogue != "It's sweaty in here..." && currentSpeaker == "Abigail")
                    {
                        this.lastEventCommand = Game1.CurrentEvent.currentCommand;
                        return;
                    }

                    if (this.currentEventDict != null)
                    {

                        string textAboveHeadLineId = getIdFromDialogueString(this.formatDialogueString(commandDialogue), currentSpeaker);

                        if (textAboveHeadLineId.Contains("not found"))
                        {
                            // this.Monitor.Log(textAboveHeadLineId, LogLevel.Debug);
                            this.lastEventCommand = Game1.CurrentEvent.currentCommand;
                            return;
                        }

                        this.currentCue = this.getCueFromDialogueId(textAboveHeadLineId, currentSpeaker);

                        if (this.currentCue == null)
                        {
                            //   this.Monitor.Log($"Cue could not be found for textAboveHead line: {textAboveHeadLineId}", LogLevel.Debug);
                            this.lastEventCommand = Game1.CurrentEvent.currentCommand;
                            return;
                        }
                        else
                        {
                            if (this.lastCue != null && this.lastCue.IsPlaying)
                            {
                                this.lastCue.Stop(AudioStopOptions.Immediate);
                            }

                            this.currentCue.Play();

                        }
                    }
                }

                this.lastEventCommand = Game1.CurrentEvent.currentCommand;
            }
        }
    }
}