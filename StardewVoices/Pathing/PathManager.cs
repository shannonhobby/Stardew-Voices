using System.Collections.Generic;
using System.IO;
using StardewValley;
using StardewVoices.Localization;

namespace StardewVoices.Pathing
{
    public class PathManager
    {
        internal PathNode ModPath { get; }
        internal string FestivalDatesJsonName { get; }

        public PathManager(string modPath, LocalizationManager localizationManager) 
        {
            ModPath = new PathNode("root", modPath);
            ModPath.CreateChild("Audio");
            PathNode InverseDialogueFolderPathNode = ModPath.CreateChild("Dialogue Jsons");
            if (localizationManager.CurrentLanguage != LocalizedContentManager.LanguageCode.en.ToString())
            {
                InverseDialogueFolderPathNode = InverseDialogueFolderPathNode.CreateChild(localizationManager.CurrentLanguage);
            }

            InverseDialogueFolderPathNode.CreateChild("Events");
            InverseDialogueFolderPathNode.CreateChild("Characters");

            // Festival + festival date setup
            PathNode FestivalFolderPathNode = InverseDialogueFolderPathNode.CreateChild("Festivals");
            FestivalDatesJsonName = $"FestivalDates.{localizationManager.GetLanguageJsonExtension()}";
            FestivalFolderPathNode.CreateChild(FestivalDatesJsonName);
            
            
            // Setup Generic Dialogue Folder
            PathNode GenericDialoguePathNode = InverseDialogueFolderPathNode.CreateChild("Generic");
            GenericDialoguePathNode.CreateChild("EngagementDialogue.json");
            GenericDialoguePathNode.CreateChild("ExtraDialogue.json");
            GenericDialoguePathNode.CreateChild("Marriage Dialogue Generic.json");
            GenericDialoguePathNode.CreateChild("rainy.json");
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