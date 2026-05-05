using System.Collections.Generic;

namespace StardewVoices.Localization
{
    public class LocalizationManager
    {
        private string currentLanguage;
        private static Dictionary<string, string> LocalizedToEnglishNameMap;

        public LocalizationManager() { }

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
            if (Constants.SpecialCharacterLanguages.Contains(currentLanguage))
            {
                LocalizedToEnglishNameMap.TryGetValue(name, out string localizedCharacterName);
                return localizedCharacterName;
            }
            else
            {
                return name;
            }
        }

        public string GetLanguageJsonExtension(string language)
        {
            if (language == "en")
            {
                return "json";
            }

            return $"{language}-{Constants.languageDict[language]}.json";

        }

        public string GetLocalizedName(string name)
        {
            if (Constants.SpecialCharacterLanguages.Contains(currentLanguage))
            {
                Constants.LocalizedNpcNames.TryGetValue(name, out Dictionary<string, string> localizedCharacterNames);
                localizedCharacterNames.TryGetValue(currentLanguage, out string localizedCharacterName);
                return localizedCharacterName;
            }
            else
            {
                return name;
            }
        }
    }
}