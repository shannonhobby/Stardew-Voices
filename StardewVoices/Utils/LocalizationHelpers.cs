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


        // Accepts either a localized or an English name and returns the English one.
        // Names not in the localized map (already English, or not a known NPC) are
        // returned unchanged.
        public static string GetName(string name)
        {
            if (name != null
                && Constants.SpecialCharacterLanguages.Contains(_currentLanguage)
                && _localizedToEnglishNameMap.TryGetValue(name, out string englishName))
            {
                return englishName;
            }

            return name;
        }

        public static string GetLanguageJsonExtension()
        {
            if (_currentLanguage == "en")
            {
                return "json";
            }

            return $"{_currentLanguage}-{Constants.languageDict[_currentLanguage]}.json";

        }

        // Accepts either a localized or an English name and returns the localized one.
        // Names with no localized entry (e.g. one-off event actors) are returned unchanged.
        public static string GetLocalizedName(string name)
        {
            if (name == null || !Constants.SpecialCharacterLanguages.Contains(_currentLanguage))
            {
                return name;
            }

            string englishName = GetName(name);
            if (englishName == "LeoMainland")
            {
                englishName = "Leo";
            }

            if (Constants.LocalizedNpcNames.TryGetValue(englishName, out Dictionary<string, string> localizedCharacterNames)
                && localizedCharacterNames.TryGetValue(_currentLanguage, out string localizedCharacterName))
            {
                return localizedCharacterName;
            }

            return name;
        }
    }
}