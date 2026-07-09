using StardewVoices.Pathing;
using StardewVoices.Utils.ParsingHelpers;
using System.Collections.Generic;
using System.IO;
using DialogueLines = System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<string>>;

namespace StardewVoices.FileGeneration.Models
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

            PathNode root = pathManager.GeneratedFilesRoot;
            PathString = GetPathString(root.FullPath, parentFolder, fileName);
            ParentFolder = SetupFolderStructure(root, parentFolder);
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

        public void AddLine(LabelledDialogueLine dialogueLine, string type="")
        {
            if (!isCharacterJson || type == "")
            {
                AddMiscLine(dialogueLine);
            }

            // If the type of dialogue isn't in the dict already add it
            if (!CharacterDialogueDictionary.ContainsKey(type))
            {
                CharacterDialogueDictionary[type] = new DialogueLines();
            }

            CharacterDialogueDictionary.TryGetValue(type, out DialogueLines dict);

            if (dict.ContainsKey(dialogueLine.Text))
            {
                if (!dict[dialogueLine.Text].Contains(dialogueLine.LineId))
                {
                    dict[dialogueLine.Text].Add(dialogueLine.LineId);
                }
            } else
            {
               dict.Add(dialogueLine.Text, new List<string>() { dialogueLine.LineId });
            }
        }

        public void AddContinuationDialogue(string continuationLineId)
        {

            if (CharacterDialogueDictionary.ContainsKey("continuation_dialogue"))
            {
                CharacterDialogueDictionary["continuation_dialogue"]["continuation_dialogue"].Add(continuationLineId);
            }
            else
            {
                CharacterDialogueDictionary.Add("continuation_dialogue", 
                    new Dictionary<string, List<string>>() { {"continuation_dialogue", new List<string>() { continuationLineId } } });
            }
        }

        public void AddMiscLine(LabelledDialogueLine dialogueLine)
        {

            if (DialogueDictionary.ContainsKey(dialogueLine.Text))
            {
                if (!DialogueDictionary[dialogueLine.Text].Contains(dialogueLine.LineId))
                {
                    DialogueDictionary[dialogueLine.Text].Add(dialogueLine.LineId);

                }
            }
            else
            {
                DialogueDictionary.Add(dialogueLine.Text, new List<string>() { dialogueLine.LineId });
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
