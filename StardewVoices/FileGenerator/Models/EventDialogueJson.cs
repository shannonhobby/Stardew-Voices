using StardewVoices.Dialogue;
using StardewVoices.Pathing;
using System;
using System.Collections.Generic;
using xTile.Dimensions;
using System.IO;
using StardewVoices.Utils;


namespace StardewVoices.FileGeneration.Models
{
    internal class EventDialogueJson
    {
        public string EventId { get; }
        public string Location { get; }

        public string PathString { get; }
        private PathNode EventLocationFolder { get; }

        public Dictionary<string, string> EventLines
             = new Dictionary<string, string>();

        public EventDialogueJson(string eventId, string location, PathManager pathManager)
        {
            EventId = eventId;
            Location = location;

            // Create Generated Files folder if it doesn't exist
            if (pathManager.GetPath(Constants.GENERATED_FILES_FOLDER_NAME) == null)
            {
                pathManager.GetPath("root").CreateFolder(Constants.GENERATED_FILES_FOLDER_NAME);
            }
            PathNode GeneratedFilesFolder = pathManager.GetPath(Constants.GENERATED_FILES_FOLDER_NAME);



            PathString = GetPathString(GeneratedFilesFolder.FullPath, eventId, location);

            EventLocationFolder = SetupFolderStructure(GeneratedFilesFolder);
            
        }

        public void WriteOut()
        {
            EventLocationFolder.CreateJsonFile(EventId + ".json", EventLines);
        }

        public void AddLine(string lineId, string dialogue)
        {
            try
            {
                if(!EventLines.ContainsKey(dialogue))
                {
                    EventLines.Add(dialogue, lineId);
                }
            } catch (Exception e)
            {
                ErrorLogger.Warn($"Error adding dialogue line to: {PathString}. {e}");
            }
        }

     
        private string GetPathString(string generatedFilesPathString, string eventId, string location)
        {
            string eventsFolderPath = Path.Combine(generatedFilesPathString, "Events");
            string locationFolderPath = Path.Combine(eventsFolderPath, location);
            return Path.Combine(locationFolderPath, eventId + ".json");
        }

        private PathNode SetupFolderStructure(PathNode generatedFilesFolder)
        {

            generatedFilesFolder.CreateFolder("Events");
            PathNode EventsFolder = generatedFilesFolder.GetChild("Events");
            EventsFolder.CreateFolder(Location);

            return EventsFolder.GetChild(Location);
        }
    }
}
