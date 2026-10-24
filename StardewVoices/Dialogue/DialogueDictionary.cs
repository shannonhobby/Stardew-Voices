
using System.Collections.Generic;
using System;
using Newtonsoft.Json;
using StardewVoices.Characters;
using StardewValley;
using StardewVoices.Testing;
using StardewVoices.Utils;
using Microsoft.Xna.Framework.Input;
using Newtonsoft.Json.Serialization;
using System.Text.RegularExpressions;
using System.Linq;

namespace StardewVoices.Dialogue
{
    /// <summary>
    /// Type for dialogue dictionaries with multiple groupings / types of dialogue.
    /// I.e. Generic Dialogue: Schedule, Gift Tastes, Rainy (etc) which will map the corresponding
    /// to its line->line id dictionary.
    /// </summary>
    public class DialogueDictionary
    {
        private Dictionary<string, Dictionary<string, string[]>> _data { get; }

        public DialogueDictionary(string jsonString)
        {
            _data = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, string[]>>>(jsonString);
        }

        public DialogueDictionary()
        {
            _data = new Dictionary<string, Dictionary<string, string[]>>();
        }

        public string CheckDictionaryForDialogueLine(string type, string dialogueLine, string currentSpeaker)
        {
            Dictionary<string, string[]> dialogueDict = GetType(type);

            if (dialogueDict.ContainsKey(dialogueLine))
            {
                if (dialogueDict[dialogueLine].Length > 1) {
                    return DialogueHelpers.getCorrectIdFromListOfLineIds(dialogueDict[dialogueLine], currentSpeaker, type);
                }

                return dialogueDict[dialogueLine][0];
            }
            else
            {
                string foundLine = CheckDictionaryForVariableLines(dialogueDict, dialogueLine, currentSpeaker, type);
                if(foundLine != null)
                {
                    return foundLine;
                }
            }

            return null;
        }

        // Caches each key's compiled regex (see GetVariableLinePattern) so repeated lookups for
        // the same key don't rebuild it every time. Keys never change after construction, so
        // nothing here ever goes stale.
        private readonly Dictionary<string, Regex> _variableLinePatternCache = new();

        /// <summary>
        /// Finds the placeholder key (one containing "%...", "{...}", or "@" for the farmer's
        /// name) whose non-placeholder words all match the dialogue line exactly, with each
        /// placeholder absorbing whatever words sit in its place. Returns null if no key matches,
        /// or if more than one does — we'd rather play nothing than guess wrong.
        /// </summary>
        private string CheckDictionaryForVariableLines(Dictionary<string, string[]> dict, string dialogueLine, string currentSpeaker, string type)
        {
            string matchedKey = null;

            foreach (string key in dict.Keys)
            {
                if (!key.Contains("{") && !key.Contains("%") && !key.Contains("@"))
                {
                    continue;
                }

                Regex pattern = GetVariableLinePattern(key);
                if (pattern == null || !pattern.IsMatch(dialogueLine))
                {
                    continue;
                }

                if (matchedKey != null)
                {
                    ErrorLogger.Warn($"Ambiguous placeholder match for \"{dialogueLine}\": both \"{matchedKey}\" and \"{key}\" fit. Playing neither.");
                    return null;
                }

                matchedKey = key;
            }

            return matchedKey != null ? dict[matchedKey][0] : null;
        }

        // Max words a placeholder can absorb (e.g. a multi-word farm/kid/pet name). Only applies
        // when literal text follows the placeholder to anchor the end of the match — a
        // placeholder with nothing after it (e.g. "Hi {0}!") has no such anchor, so it's capped
        // at one word instead, or a short key could swallow an entire unrelated sentence.
        private const int MaxPlaceholderWords = 4;

        private Regex GetVariableLinePattern(string key)
        {
            if (_variableLinePatternCache.TryGetValue(key, out Regex cached))
            {
                return cached;
            }

            string[] words = key.Split(" ");
            List<string> patternParts = new();
            bool hasAnchorWord = false;
            int i = 0;

            while (i < words.Length)
            {
                if (!TryParsePlaceholderWord(words[i], out string lead, out string trail))
                {
                    patternParts.Add(Regex.Escape(words[i]));
                    if (words[i].Any(char.IsLetterOrDigit)) hasAnchorWord = true;
                    i++;
                    continue;
                }

                // Punctuation touching the placeholder (e.g. "Hi {0}!") must match exactly, so
                // it's escaped into the same pattern part as the gap rather than swallowed by
                // it. It's punctuation-only by construction (never letters/digits), so it
                // doesn't count toward hasAnchorWord — a key like "{0}!" has no real text and
                // would otherwise match any short exclamation ending in "!".
                string wordPattern = lead.Length > 0 ? Regex.Escape(lead) : "";

                // Adjacent bare placeholder words (rare) collapse into a single gap.
                while (trail.Length == 0
                    && i + 1 < words.Length
                    && TryParsePlaceholderWord(words[i + 1], out string nextLead, out string nextTrail)
                    && nextLead.Length == 0)
                {
                    i++;
                    trail = nextTrail;
                }

                bool hasFollowingAnchor = trail.Length > 0 || i + 1 < words.Length;
                wordPattern += hasFollowingAnchor
                    ? $@"\S+(?:\s+\S+){{0,{MaxPlaceholderWords - 1}}}"
                    : @"\S+";

                if (trail.Length > 0)
                {
                    wordPattern += Regex.Escape(trail);
                }

                patternParts.Add(wordPattern);
                i++;
            }

            Regex pattern = hasAnchorWord ? new Regex("^" + string.Join(@"\s+", patternParts) + "$") : null;
            _variableLinePatternCache[key] = pattern;
            return pattern;
        }

        private static readonly Regex PlaceholderMarkerRegex = new Regex(@"%\w+|\{\d+\}|@");

        /// <summary>
        /// True if word is one placeholder marker (%word, {n}, or @) plus only punctuation
        /// before/after it. On success, lead/trail are that surrounding punctuation (may be empty).
        /// </summary>
        private static bool TryParsePlaceholderWord(string word, out string lead, out string trail)
        {
            Match marker = PlaceholderMarkerRegex.Match(word);
            if (!marker.Success)
            {
                lead = trail = null;
                return false;
            }

            string candidateLead = word.Substring(0, marker.Index);
            string candidateTrail = word.Substring(marker.Index + marker.Length);
            if (candidateLead.Any(char.IsLetterOrDigit) || candidateTrail.Any(char.IsLetterOrDigit))
            {
                lead = trail = null;
                return false;
            }

            lead = candidateLead;
            trail = candidateTrail;
            return true;
        }

        public string CheckAllDictionariesForDialogueLine(string  dialogueLine, string currentSpeaker, TestingManager testingManager)
        {
            string foundDialogueLine = null;

            // During testing/validation the farmer usually isn't engaged or
            // married, so the engagement / *_marriage gates in ShouldCheckDialogue
            // would skip those types. Check every type instead so those lines can
            // still be validated.
            bool checkAllTypes = testingManager != null
                && (testingManager.IsTestingWorldLoaded() || testingManager.ValidationIsRunning);

            foreach (string type in _data.Keys)
            {
                if (ShouldCheckDialogue(type, currentSpeaker) || checkAllTypes)
                {
                    foundDialogueLine = CheckDictionaryForDialogueLine(type, dialogueLine, currentSpeaker);
                    if (foundDialogueLine != null)
                    {
                        return foundDialogueLine;
                    }
                }
            }
            return foundDialogueLine;
        }

        private Boolean ShouldCheckDialogue(string type, string currentSpeaker)
        {
            return ShouldCheckEngagementDialogue(type) || ShouldCheckMarriageDialogue(type, currentSpeaker) || DialogueIsNotMarriageOrEngagement(type);
        }

        private Boolean DialogueIsNotMarriageOrEngagement(string type)
        {
            return type != "marriage_dialogue" && type != "generic_marriage" && type != "engagement";
        } 

        private Boolean ShouldCheckMarriageDialogue(string type, string currentSpeaker)
        {
            return type == "marriage_dialogue" || type == "generic_marriage" &&
                    Game1.player.getSpouse()?.getName() == currentSpeaker;
        }

        private Boolean ShouldCheckEngagementDialogue(string type)
        {
            return type == "engagement" && Game1.player.isEngaged();
        }

        public void AddType(string type, Dictionary<string, string[]> dialogue)
        {
            _data[type] = dialogue;
        }

        /// <summary>The lines of one dialogue type, or an empty set if this character has none of that type.</summary>
        public Dictionary<string, string[]> GetType(string type) 
        {
            return _data.TryGetValue(type, out Dictionary<string, string[]> lines) ? lines : new Dictionary<string, string[]>();
        }

        public Dictionary<string, Dictionary<string, string[]>> Data()
        {
            return this._data;
        }

        public IReadOnlyList<string> GetLineId(string type, string line)
        {
            return _data.TryGetValue(type, out var categories) &&
                   categories.TryGetValue(line, out var list)
                ? list
                : Array.Empty<string>();
        }

        public void PrintDictionary()
        {
            foreach (string key in this._data.Keys)
            {
                ErrorLogger.Trace($"{key}: {String.Join(",", this._data[key])}");
            }
        }
    }
}