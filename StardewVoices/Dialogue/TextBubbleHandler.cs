using StardewValley;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System;

namespace StardewVoices.Dialogue
{
    public class TextBubbleHandler {
        private Dictionary<int, string> eventCommandDict = new Dictionary<int, string>();
        private int lastEventCommand;
        public bool eventCommandsHaveBeenLogged = false;

        public TextBubbleHandler()
        {
        }

        public (String commandDialogue, String currentSpeaker)? CheckForTextAboveHead()
        {
            if (!eventCommandsHaveBeenLogged && Game1.CurrentEvent != null)
            {
                LogEventCommands();
            }

            // We are currently in an event and the command has changed.
            if (Game1.CurrentEvent != null && this.lastEventCommand != Game1.CurrentEvent.currentCommand)
            {

                string currentCommand = GetCurrentCommand();

                if (!string.IsNullOrEmpty(currentCommand) 
                    && currentCommand.Contains("textAboveHead") 
                    && (DialogueHelpers.StringContainsSupportedCharacter(currentCommand) || currentCommand.Contains("ClothesTherapyCharacters")))
                {
                    Regex rx = new Regex(@"""(.*?)""");
                    string commandDialogue;

                    string currentSpeaker = DialogueHelpers.GetSupportedCharacterFromString(currentCommand);

                    if (currentSpeaker.Contains("Could not find"))
                    {
                        this.lastEventCommand = Game1.CurrentEvent.currentCommand;
                        return null;
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
                        return null;
                    }

                    // TODO - remove hardcoded Abigail Logic
                    if (currentCommand.Contains("ClothesTherapyCharacters") && commandDialogue != "It's sweaty in here..." && currentSpeaker == "Abigail")
                    {
                        this.lastEventCommand = Game1.CurrentEvent.currentCommand;
                        return null;
                    }


                    this.lastEventCommand = Game1.CurrentEvent.currentCommand;
                    return (commandDialogue, currentSpeaker);
                }
                this.lastEventCommand = Game1.CurrentEvent.currentCommand;
            }

            return null;
        }

        private void LogEventCommands()
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

        private string GetCurrentCommand()
        {
            // The current command and last command are more than one count apart
            // (this can result in skipped commands, we need to check everything that has happened since
            // the last update tick for textAboveHead commands)
            if (Game1.CurrentEvent.currentCommand - this.lastEventCommand > 1)
            {
                for (int i = 0; i <= Game1.CurrentEvent.currentCommand - this.lastEventCommand; i++)
                {
                    string currentCommand = "";

                    try
                    {
                        currentCommand = eventCommandDict[this.lastEventCommand + i];
                    }
                    catch (Exception e)
                    {
                        continue;
                    }

                    if (currentCommand.Contains("textAboveHead")
                        && (DialogueHelpers.StringContainsSupportedCharacter(currentCommand) || currentCommand.Contains("ClothesTherapyCharacters")))
                    {
                        return currentCommand;

                    }

                }

                return "";
            }
            else
            {
                try
                {
                    return eventCommandDict[Game1.CurrentEvent.currentCommand];
                }
                catch (Exception e)
                {
                    this.lastEventCommand = Game1.CurrentEvent.currentCommand;
                    return "";
                }
            }

        }


    }
}