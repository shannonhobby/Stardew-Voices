using StardewVoices.Dialogue;
using StardewVoices.Pathing;
using System;
using System.Collections.Generic;
using System.IO;

using DialogueLines = System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<string>>;

namespace StardewVoices.FileGenerator.Models
{
    public enum DialogueParentFolder
    {
        Festivals,
        Characters,
        Generic
    }

    internal class DialogueJson
    {
        public string FileName { get; }
        public string PathString { get; }
        public PathNode ParentFolder { get; }

        public DialogueLines DialogueDictionary
           = new DialogueLines();

        public Dictionary<string, DialogueLines> CharacterDialogueDictionary
            = new Dictionary<string, DialogueLines>();

        public bool isCharacterJson { get; }

        // context: character, festival, generic
        public DialogueJson(string fileName, DialogueParentFolder parentFolder, PathManager pathManager)
        {
            FileName = fileName;
            isCharacterJson = parentFolder.Equals(DialogueParentFolder.Characters);

            // Create Generated Files folder if it doesn't exist
            if (pathManager.GetPath(Constants.GENERATED_FILES_FOLDER_NAME) == null)
            {
                pathManager.GetPath("root").CreateFolder(Constants.GENERATED_FILES_FOLDER_NAME);
            }
            PathNode generatedFilesFolder = pathManager.GetPath(Constants.GENERATED_FILES_FOLDER_NAME);

            PathString = GetPathString(generatedFilesFolder.FullPath, parentFolder, fileName);
            ParentFolder = SetupFolderStructure(generatedFilesFolder, parentFolder);
        }

        public void WriteOut()
        {
            if(isCharacterJson)
            {
                ParentFolder.CreateJsonFile(FileName + ".json", CharacterDialogueDictionary);

            } else
            {
                ParentFolder.CreateJsonFile(FileName + ".json", DialogueDictionary);
            }
        }

        public void AddLine(string lineId, string dialogue, string type="")
        {
            if (!isCharacterJson)
            {
                AddMiscLine(lineId, dialogue);
            }

            // If the type of dialogue isn't in the dict already add it
            if (!CharacterDialogueDictionary.ContainsKey(type))
            {
                CharacterDialogueDictionary[type] = new DialogueLines();
            }

            CharacterDialogueDictionary.TryGetValue(type, out DialogueLines dict);

            if (dict.ContainsKey(dialogue))
            {
                dict[dialogue].Add(lineId);
            } else
            {
                dict.Add(dialogue, new List<string>() { lineId });
            }
        }

        public void AddMiscLine(string lineId, string dialogue)
        {

            if (DialogueDictionary.ContainsKey(dialogue))
            {
                DialogueDictionary[dialogue].Add(lineId);
            }
            else
            {
                DialogueDictionary.Add(dialogue, new List<string>() { lineId });
            }
        }

        private string GetPathString(string generatedFilesPathString, DialogueParentFolder context, string fileName)
        {
            string ParentFolderPath = Path.Combine(generatedFilesPathString, context.ToString());
            return Path.Combine(ParentFolderPath, fileName + ".json");
        }

        private PathNode SetupFolderStructure(PathNode generatedFilesFolder, DialogueParentFolder parentFolder)
        {

            generatedFilesFolder.CreateFolder(parentFolder.ToString());
            return generatedFilesFolder.GetChild(parentFolder.ToString());
        }
    }
}
