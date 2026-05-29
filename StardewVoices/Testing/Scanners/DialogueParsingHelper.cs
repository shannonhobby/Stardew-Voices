using StardewVoices.Dialogue;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace StardewVoices.Testing.Scanners
{
    internal static class DialogueParsingHelper
    {
        public static string[] ParseDialogue(string s)
        {
            string parsedString = s;
            parsedString = ParseQuestionDialogue(s);
            parsedString = ParsePDialogue(parsedString);
            parsedString = StripEmotionCodes(parsedString);
            parsedString = DialogueHelpers.Normalize(parsedString);

            string[] splitString = null;
            if (HasGenderVariants(parsedString))
            {
                splitString = GenerateGenderVariants(parsedString);
            }
            else if (parsedString.Contains("|"))
            {
                splitString = parsedString.Split("|");
            }
            else if (parsedString.Contains("^"))
            {
                splitString = parsedString.Split("^");
            }

            return splitString != null ? splitString : new string[] {parsedString} ;
        }

        private static string ParseQuestionDialogue(string s)
        {
            if (s.Contains("$q"))
            {
                Match match = Regex.Match(s, @"\$q(?:[^#]*)#(.*?)#");
                if (match.Success)
                {
                   return match.Groups[1].Value;
                }
            }

            return s;
        }

        // $p 38#First dialogue line.|Second dialogue line
        private static string ParsePDialogue(string s)
        {
            if (s.Contains("$p")) {
                return s.Split("#")[1];
            }
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
