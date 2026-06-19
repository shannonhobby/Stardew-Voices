using System.Collections.Generic;

namespace StardewVoices.Utils
{
    public static class LocalizationHelpers
    {
        public static string _currentLanguage;
        private static Dictionary<string, string> _localizedToEnglishNameMap;

        public static void Init(string currentLanguage)
        {
            _currentLanguage = currentLanguage;
            SetupLocalizedToEnglishMap();
        }

        public static string GetCurrentLanguage()
        {
            return _currentLanguage;
        }

        private static void SetupLocalizedToEnglishMap()
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

            _localizedToEnglishNameMap = result;
        }


        public static string GetName(string name)
        {
            if (Constants.SpecialCharacterLanguages.Contains(_currentLanguage))
            {
                _localizedToEnglishNameMap.TryGetValue(name, out string localizedCharacterName);
                return localizedCharacterName;
            }
            else
            {
                return name;
            }
        }

        public static string GetLanguageJsonExtension()
        {
            if (_currentLanguage == "en")
            {
                return "json";
            }

            return $"{_currentLanguage}-{Constants.languageDict[_currentLanguage]}.json";

        }

        public static string GetLocalizedName(string name)
        {
            if (Constants.SpecialCharacterLanguages.Contains(_currentLanguage))
            {
                if (name == "LeoMainland")
                {
                    name = "Leo";
                }
                Constants.LocalizedNpcNames.TryGetValue(name, out Dictionary<string, string> localizedCharacterNames);
                localizedCharacterNames.TryGetValue(_currentLanguage, out string localizedCharacterName);
                return localizedCharacterName;
            }
            else
            {
                return name;
            }
        }
    }
}