using System.Collections.Generic;

namespace StardewVoices
{
    public static class Constants
    {
        public static string GENERATED_FILES_FOLDER_NAME = "Generated Files";

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

        public static List<string> AllCharacterNames = new()
        {
            "Alex",
            "Elliott",
            "Harvey",
            "Sam",
            "Sebastian",
            "Shane",
            "Abigail",
            "Emily",
            "Haley",
            "Leah",
            "Maru",
            "Penny",
            "Caroline",
            "Clint",
            "Demetrius",
            "Dwarf",
            "Evelyn",
            "George",
            "Gus",
            "Jas",
            "Jodi",
            "Kent",
            "Krobus",
            "Leo",
            "Lewis",
            "Linus",
            "Marnie",
            "Pam",
            "Pierre",
            "Robin",
            "Sandy",
            "Vincent",
            "Willy",
            "Wizard",
            "Birdie",
            "Bouncer",
            "Gil",
            "Governor",
            "Grandpa",
            "Gunther",
            "Henchman",
            "Marlon",
            "Morris",
            "MrQi",
            "Mister Qi",
            "ProfessorSnail",
            "Bear",
        };

        public static List<string> AllDialogueTypes = new()
        {
            "dialogue",
            "misc",
            "engagement",
            "generic_marriage",
            "extra",
            "rainy",
            "marriage_dialogue",
            "gift_tastes",
        };


        public static Dictionary<string, string> ElliottBookTitles = new Dictionary<string, string>() {
            { "mystery", "Blue Tower"},
            { "romance", "Camellia Station"},
            { "default", "The Rise And Fall Of Planet Yazzo"},

        };

        public static Dictionary<string, string> DialogueTypeToDialogueDictName = new()
        {
            { "dialogue", "Dialogue-" },
            { "misc", "Internal_Dialogue-" },
            { "extra", "Extra_Dialogue-" },
            { "engagement", "Engagement_Dialogue-" },
            { "rainy", "Rainy-" },
            { "gift_tastes", "NpcGiftTastes-" },
            { "marriage_dialogue", "Marriage_Dialogue-" },
            { "generic_marriage", "Generic_Marriage_Dialogue-" }
        };

        public static List<string> GenericDialogueTypes = new()
        {
            "engagement",
            "extra",
            "generic_marriage",
            "rainy",
        };

        public static Dictionary<string, string> GenericDialogueTypeToFileName = new()
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
            { "Abigail",   new Dictionary<string, string> { { "ko", "애비게일" },   { "ja", "アビゲイル" },    { "ru", "Абигейл" },    { "zh", "阿比盖尔" } } },
            { "Alex",      new Dictionary<string, string> { { "ko", "알렉스" },     { "ja", "アレックス" },    { "ru", "Алекс" },      { "zh", "亚历克斯" } } },
            { "Caroline",  new Dictionary<string, string> { { "ko", "캐롤라인" },   { "ja", "キャロライン" },  { "ru", "Кэролайн" },   { "zh", "卡洛琳" } } },
            { "Clint",     new Dictionary<string, string> { { "ko", "클린트" },     { "ja", "クリント" },      { "ru", "Клинт" },      { "zh", "克林特" } } },
            { "Demetrius", new Dictionary<string, string> { { "ko", "드미트리우스" }, { "ja", "ディメトリウス" }, { "ru", "Деметриус" },  { "zh", "德米特里厄斯" } } },
            { "Dwarf",     new Dictionary<string, string> { { "ko", "드워프" },     { "ja", "ドワーフ" },      { "ru", "Дварф" },      { "zh", "矮人" } } },
            { "Elliott",   new Dictionary<string, string> { { "ko", "엘리엇" },     { "ja", "エリオット" },    { "ru", "Эллиот" },     { "zh", "艾利欧特" } } },
            { "Emily",     new Dictionary<string, string> { { "ko", "에밀리" },     { "ja", "エミリー" },      { "ru", "Эмили" },      { "zh", "艾米丽" } } },
            { "Evelyn",    new Dictionary<string, string> { { "ko", "에블린" },     { "ja", "エブリン" },      { "ru", "Эвелин" },     { "zh", "艾芙琳" } } },
            { "George",    new Dictionary<string, string> { { "ko", "조지" },       { "ja", "ジョージ" },      { "ru", "Джордж" },     { "zh", "乔治" } } },
            { "Gus",       new Dictionary<string, string> { { "ko", "거스" },       { "ja", "ガス" },          { "ru", "Гас" },        { "zh", "格斯" } } },
            { "Haley",     new Dictionary<string, string> { { "ko", "헤일리" },     { "ja", "ヘイリー" },      { "ru", "Хэйли" },      { "zh", "海莉" } } },
            { "Harvey",    new Dictionary<string, string> { { "ko", "하비" },       { "ja", "ハーヴィー" },    { "ru", "Харви" },      { "zh", "哈维" } } },
            { "Jas",       new Dictionary<string, string> { { "ko", "재스" },       { "ja", "ジャス" },        { "ru", "Джас" },       { "zh", "贾斯" } } },
            { "Jodi",      new Dictionary<string, string> { { "ko", "조디" },       { "ja", "ジョディ" },      { "ru", "Джоди" },      { "zh", "乔迪" } } },
            { "Kent",      new Dictionary<string, string> { { "ko", "켄트" },       { "ja", "ケント" },        { "ru", "Кент" },       { "zh", "肯特" } } },
            { "Krobus",    new Dictionary<string, string> { { "ko", "크로버스" },   { "ja", "クロバス" },      { "ru", "Кробус" },     { "zh", "科罗布斯" } } },
            { "Leah",      new Dictionary<string, string> { { "ko", "레아" },       { "ja", "リア" },          { "ru", "Лея" },        { "zh", "莉亚" } } },
            { "Leo",       new Dictionary<string, string> { { "ko", "레오" },       { "ja", "レオ" },          { "ru", "Лео" },        { "zh", "雷欧" } } },
            { "Lewis",     new Dictionary<string, string> { { "ko", "루이스" },     { "ja", "ルイス" },        { "ru", "Льюис" },      { "zh", "刘易斯" } } },
            { "Linus",     new Dictionary<string, string> { { "ko", "라이너스" },   { "ja", "ライナス" },      { "ru", "Линус" },      { "zh", "莱纳斯" } } },
            { "Marnie",    new Dictionary<string, string> { { "ko", "마니" },       { "ja", "マーニー" },      { "ru", "Марни" },      { "zh", "玛妮" } } },
            { "Maru",      new Dictionary<string, string> { { "ko", "마루" },       { "ja", "マル" },          { "ru", "Мару" },       { "zh", "玛鲁" } } },
            { "Pam",       new Dictionary<string, string> { { "ko", "팸" },         { "ja", "パム" },          { "ru", "Пэм" },        { "zh", "潘姆" } } },
            { "Penny",     new Dictionary<string, string> { { "ko", "페니" },       { "ja", "ペニー" },        { "ru", "Пенни" },      { "zh", "潘妮" } } },
            { "Pierre",    new Dictionary<string, string> { { "ko", "피에르" },     { "ja", "ピエール" },      { "ru", "Пьер" },       { "zh", "皮埃尔" } } },
            { "Robin",     new Dictionary<string, string> { { "ko", "로빈" },       { "ja", "ロビン" },        { "ru", "Робин" },      { "zh", "罗宾" } } },
            { "Sam",       new Dictionary<string, string> { { "ko", "샘" },         { "ja", "サム" },          { "ru", "Сэм" },        { "zh", "山姆" } } },
            { "Sandy",     new Dictionary<string, string> { { "ko", "샌디" },       { "ja", "サンディ" },      { "ru", "Сэнди" },      { "zh", "桑迪" } } },
            { "Sebastian", new Dictionary<string, string> { { "ko", "세바스찬" },   { "ja", "セバスチャン" },  { "ru", "Себастиан" },  { "zh", "塞巴斯蒂安" } } },
            { "Shane",     new Dictionary<string, string> { { "ko", "셰인" },       { "ja", "シェーン" },      { "ru", "Шейн" },       { "zh", "谢恩" } } },
            { "Vincent",   new Dictionary<string, string> { { "ko", "빈센트" },     { "ja", "ヴィンセント" },  { "ru", "Винсент" },    { "zh", "文森特" } } },
            { "Willy",     new Dictionary<string, string> { { "ko", "윌리" },       { "ja", "ウィリー" },      { "ru", "Вилли" },      { "zh", "威利" } } },
            { "Wizard",    new Dictionary<string, string> { { "ko", "마법사" },     { "ja", "魔術師" },        { "ru", "Волшебник" },  { "zh", "法师" } } },
             // Non-social / secondary NPCs
            { "Birdie",        new Dictionary<string, string> { { "ko", "버디" },       { "ja", "バーディー" },    { "ru", "Пташка" },              { "zh", "贝啼" } } },
            { "Bouncer",       new Dictionary<string, string> { { "ko", "문지기" },     { "ja", "用心棒" },        { "ru", "Вышибала" },            { "zh", "门卫" } } },
            { "Gil",           new Dictionary<string, string> { { "ko", "길" },         { "ja", "ギル" },          { "ru", "Гил" },                 { "zh", "吉尔" } } },
            { "Governor",      new Dictionary<string, string> { { "ko", "도지사" },     { "ja", "市長" },          { "ru", "Губернатор" },          { "zh", "州长" } } },
            { "Grandpa",       new Dictionary<string, string> { { "ko", "할아버지" },   { "ja", "おじいさん" },    { "ru", "Дедушка" },             { "zh", "爷爷" } } },
            { "Gunther",       new Dictionary<string, string> { { "ko", "건터" },       { "ja", "ギュンター" },    { "ru", "Гюнтер" },              { "zh", "冈瑟" } } },
            { "Henchman",      new Dictionary<string, string> { { "ko", "심복" },       { "ja", "部下" },          { "ru", "Приспешник" },          { "zh", "仆从" } } },
            { "Marlon",        new Dictionary<string, string> { { "ko", "말론" },       { "ja", "マーロン" },      { "ru", "Марлон" },              { "zh", "马龙" } } },
            { "Morris",        new Dictionary<string, string> { { "ko", "모리스" },     { "ja", "モーリス" },      { "ru", "Моррис" },              { "zh", "莫里斯" } } },
            { "Mister Qi",     new Dictionary<string, string> { { "ko", "미스터 치" },  { "ja", "ミスターQi" },   { "ru", "Мистер Ки" },           { "zh", "齐先生" } } },
            { "MrQi",     new Dictionary<string, string> { { "ko", "미스터 치" },  { "ja", "ミスターQi" },   { "ru", "Мистер Ки" },           { "zh", "齐先生" } } },
            { "ProfessorSnail",new Dictionary<string, string> { { "ko", "스네일 박사" }, { "ja", "スネイル教授" },  { "ru", "Профессор Улиткинс" },  { "zh", "蜗牛教授" } } },
            { "Professor Snail",new Dictionary<string, string> { { "ko", "스네일 박사" }, { "ja", "スネイル教授" },  { "ru", "Профессор Улиткинс" },  { "zh", "蜗牛教授" } } },
            { "Bear",          new Dictionary<string, string> { { "ko", "곰" },         { "ja", "クマ" },          { "ru", "Медведь" },             { "zh", "熊" } } },
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