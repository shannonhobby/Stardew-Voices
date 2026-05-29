using StardewVoices.Dialogue;
using System.Text.RegularExpressions;

namespace StardewVoices.Utils.ParsingHelpers
{
    public static class DialogueParsingHelper
    {
        public static string[] ParseDialogue(string s)
        {
            string parsed = ParseQuestionDialogue(s);
            parsed = ParsePDialogue(parsed);
            parsed = StripEmotionCodes(parsed);
            parsed = DialogueHelpers.Normalize(parsed);

            if (HasGenderVariants(parsed))
                return GenerateGenderVariants(parsed);

            if (parsed.Contains("|"))
                return parsed.Split("|");

            if (parsed.Contains("^"))
                return parsed.Split("^");

            return new string[] { parsed };
        }

        private static string ParseQuestionDialogue(string s)
        {
            if (s.Contains("$q"))
            {
                Match match = Regex.Match(s, @"\$q(?:[^#]*)#(.*?)#");
                if (match.Success)
                    return match.Groups[1].Value;
            }
            return s;
        }

        // $p 38#First dialogue line.|Second dialogue line
        private static string ParsePDialogue(string s)
        {
            if (s.Contains("$p"))
                return s.Split("#")[1];
            return s;
        }

        private static string StripEmotionCodes(string text)
        {
            return Regex.Replace(text, @"\$\w+", "").Trim();
        }

        private static string[] GenerateGenderVariants(string input)
        {
            var pattern = @"\$\{([^}^]+)\^([^}^]+)\}\$";
            string first = Regex.Replace(input, pattern, m => m.Groups[1].Value);
            string second = Regex.Replace(input, pattern, m => m.Groups[2].Value);
            return new string[] { first, second };
        }

        private static bool HasGenderVariants(string input)
        {
            return Regex.IsMatch(input, @"\$\{([^}^]+)\^([^}^]+)\}\$");
        }
    }
}
