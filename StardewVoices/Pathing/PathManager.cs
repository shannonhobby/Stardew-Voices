using System.Collections.Generic;
using System.IO;
using StardewValley;
using StardewVoices.Utils;

namespace StardewVoices.Pathing
{
    public class PathManager
    {
        internal PathNode ModPath { get; }
        internal PathNode GeneratedFilesRoot { get; }
        internal PathNode InputRoot { get; }
        internal string FestivalDatesJsonName { get; }

        public PathManager(string modPath)
        {
            ModPath = new PathNode("root", modPath);

            // Where mod-pack authors drop their pasted-dialogue input files for sv_parse_pack —
            // lives inside Stardew Voices itself, not the pack, so the author just has to put the
            // file in the right place rather than the command having to go find it.
            InputRoot = ModPath.CreateChild("Input");

            // Generated files output — language subfolder for non-English so each
            // language sits alongside the others under Generated Files/.
            PathNode generatedFilesNode = ModPath.CreateChild(Constants.GENERATED_FILES_FOLDER_NAME);
            string language = LocalizationHelpers.GetCurrentLanguage();
            GeneratedFilesRoot = (language != LocalizedContentManager.LanguageCode.en.ToString())
                ? generatedFilesNode.CreateChild(language)
                : generatedFilesNode;

            PathNode InverseDialogueFolderPathNode = ModPath.CreateChild("Dialogue Jsons");
            if (language != LocalizedContentManager.LanguageCode.en.ToString())
            {
                InverseDialogueFolderPathNode = InverseDialogueFolderPathNode.CreateChild(language);
            }

            InverseDialogueFolderPathNode.CreateChild("Events");
            InverseDialogueFolderPathNode.CreateChild("Characters");

            // Festival + festival date setup
            PathNode FestivalFolderPathNode = InverseDialogueFolderPathNode.CreateChild("Festivals");
            FestivalDatesJsonName = $"FestivalDates.{LocalizationHelpers.GetLanguageJsonExtension()}";
            FestivalFolderPathNode.CreateChild(FestivalDatesJsonName);
            
            
            // Setup Generic Dialogue Folder
            PathNode GenericDialoguePathNode = InverseDialogueFolderPathNode.CreateChild("Generic");
            GenericDialoguePathNode.CreateChild("EngagementDialogue.json");
            GenericDialoguePathNode.CreateChild("ExtraDialogue.json");
            GenericDialoguePathNode.CreateChild("Marriage Dialogue Generic.json");
            GenericDialoguePathNode.CreateChild("rainy.json");

            InverseDialogueFolderPathNode.CreateChild("variable_lines.json");
        }

        internal PathNode GetPath(string name)
        {
            return ModPath.GetChild(name);
        }

        internal string GetPathString(string name)
        {
            return ModPath.GetChild(name).ToString();
        }
    }
}