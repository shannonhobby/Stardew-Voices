using System.Collections.Generic;

namespace StardewVoices.Localization
{
    public class LocalizationManager
    {
        public string CurrentLanguage { get; }
        private static Dictionary<string, string> LocalizedToEnglishNameMap;

        public LocalizationManager(string currentLanguage)
        {
            CurrentLanguage = currentLanguage;
            SetupLocalizedToEnglishMap();
        }

        private void SetupLocalizedToEnglishMap()
        {
            var result = new Dictionary<string, string>();

            foreach (var englishEntry in Constants.LocalizedNpcNames)
            {
                string englishName = englishEntry.Key;
                var localizedDict = englishEntry.Value;

                foreach (var localizedEntry in localizedDict)
                {
                    string localizedName = localizedEntry.Value;

                    if (!result.ContainsKey(localizedName))
                    {
                        result[localizedName] = englishName;
                    }
                }
            }

            LocalizedToEnglishNameMap = result;
        }


        public string GetName(string name)
        {
            if (Constants.SpecialCharacterLanguages.Contains(CurrentLanguage))
            {
                LocalizedToEnglishNameMap.TryGetValue(name, out string localizedCharacterName);
                return localizedCharacterName;
            }
            else
            {
                return name;
            }
        }

        public string GetLanguageJsonExtension()
        {
            if (CurrentLanguage == "en")
            {
                return "json";
            }

            return $"{CurrentLanguage}-{Constants.languageDict[CurrentLanguage]}.json";

        }

        public string GetLocalizedName(string name)
        {
            if (Constants.SpecialCharacterLanguages.Contains(CurrentLanguage))
            {
                Constants.LocalizedNpcNames.TryGetValue(name, out Dictionary<string, string> localizedCharacterNames);
                localizedCharacterNames.TryGetValue(CurrentLanguage, out string localizedCharacterName);
                return localizedCharacterName;
            }
            else
            {
                return name;
            }
        }
    }
}