using System.Collections.Generic;

namespace StardewVoices
{
    public static class Constants
    {
        public static List<string> supportedCharacters = new()
        {
            "Abigail",
            "Alex",
            "Elliott",
            "Emily",
            "Haley",
            "Harvey",
            "Leah",
            "Maru",
            "Penny",
            "Sam",
            "Sebastian",
            "Shane",
        };

        public static List<string> LoveInterests = new()
        {
            "Abigail",
            "Alex",
            "Elliott",
            "Emily",
            "Haley",
            "Harvey",
            "Leah",
            "Maru",
            "Penny",
            "Sam",
            "Sebastian",
            "Shane",
        };

        public static List<string> DialogueTypes = new()
        {
            "engagement",
            "extra",
            "generic_marriage",
            "rainy",
        };

        public static Dictionary<string, string> DialogueTypeToFileName = new()
        {
            { "engagement", "EngagementDialogue.json" },
            { "extra","ExtraDialogue.json" },
            { "generic_marriage", "Marriage Dialogue Generic.json" },
            { "rainy", "rainy.json" },
        };

        public static readonly List<string> SpecialCharacterLanguages = new List<string>
         {
            "ko",
            "ja",
            "ru",
            "zh"
         };

        public static readonly Dictionary<string, Dictionary<string, string>> LocalizedNpcNames = new()
          {
            { "Abigail", new Dictionary<string, string> { { "ko", "애비게일" }, { "ja", "アビゲイル" }, { "ru", "Абигейл" }, { "zh", "阿比盖尔" } } },
            { "Alex", new Dictionary<string, string> { { "ko", "알렉스" }, { "ja", "アレックス" }, { "ru", "Алекс" }, { "zh", "亚历克斯" } } },
            { "Elliott", new Dictionary<string, string> { { "ko", "엘리엇" }, { "ja", "エリオット" }, { "ru", "Эллиот" }, { "zh", "艾利欧特" } } },
            { "Emily", new Dictionary<string, string> { { "ko", "에밀리" }, { "ja", "エミリー" }, { "ru", "Эмили" }, { "zh", "艾米丽" } } },
            { "Haley", new Dictionary<string, string> { { "ko", "헤일리" }, { "ja", "ヘイリー" }, { "ru", "Хэйли" }, { "zh", "海莉" } } },
            { "Harvey", new Dictionary<string, string> { { "ko", "하비" }, { "ja", "ハーヴィー" }, { "ru", "Харви" }, { "zh", "哈维" } } },
            { "Leah", new Dictionary<string, string> { { "ko", "레아" }, { "ja", "リア" }, { "ru", "Лея" }, { "zh", "莉亚" } } },
            { "Maru", new Dictionary<string, string> { { "ko", "마루" }, { "ja", "マル" }, { "ru", "Мару" }, { "zh", "玛鲁" } } },
            { "Penny", new Dictionary<string, string> { { "ko", "페니" }, { "ja", "ペニー" }, { "ru", "Пенни" }, { "zh", "潘妮" } } },
            { "Sam", new Dictionary<string, string> { { "ko", "샘" }, { "ja", "サム" }, { "ru", "Сэм" }, { "zh", "山姆" } } },
            { "Sebastian", new Dictionary<string, string> { { "ko", "세바스찬" }, { "ja", "セバスチャン" }, { "ru", "Себастиан" }, { "zh", "塞巴斯蒂安" } } },
            { "Shane", new Dictionary<string, string> { { "ko", "셰인" }, { "ja", "シェーン" }, { "ru", "Шейн" }, { "zh", "谢恩" } } }
        };

        public static readonly Dictionary<string, string> languageDisplayNames = new()
            {
                { "en", "English" },
                { "de", "Deutsch" },
                { "es", "Español" },
                { "fr", "Français" },
                { "hu", "Magyar" },
                { "it", "Italiano" },
                { "ja", "Japanese (日本語)" },
                { "ko", "Korean (한국어)" },
                { "pt", "Português" },
                { "ru", "Russian (Русский)" },
                { "tr", "Türkçe" },
                { "zh", "Chinese (中文)" }
            };

        public static Dictionary<string, string> languageDict = new Dictionary<string, string>()
            {
                { "de", "DE" },
                { "es", "ES" },
                { "fr", "FR" },
                { "hu", "HU" },
                { "it", "IT" },
                { "ja", "JP" },
                { "ko", "KR" },
                { "pt", "BR" },
                { "ru", "RU" },
                { "tr", "TR" },
                { "zh", "CN" },
            };

    }
}