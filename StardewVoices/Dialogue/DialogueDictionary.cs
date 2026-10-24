
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

        // Every placeholder key (e.g. "Hey @, do you think %kid1 is right?") gets turned into a
        // regex so it can be matched against live dialogue text (see GetVariableLinePattern).
        // CheckDictionaryForVariableLines re-checks every placeholder key for every line of
        // dialogue the game shows, so without caching we'd rebuild the same regex from scratch
        // over and over. This dictionary stores each key's regex the first time it's built, keyed
        // by the literal key string, so later lookups for that same key just reuse it. The keys
        // never change after this DialogueDictionary is constructed, so a cached entry never goes
        // stale and never needs to be removed.
        private readonly Dictionary<string, Regex> _variableLinePatternCache = new();

        /// <summary>
        /// Finds a placeholder key (one containing a "%...", "{...}", or "@" token — "@" is the
        /// farmer's name) that the live dialogue line matches once its placeholder slot(s) are set
        /// aside. Every *non-placeholder* word must line up exactly, in order, with the dialogue
        /// line; a placeholder itself may absorb one or more words, since a substituted name (farm,
        /// kid, pet, player) isn't always a single word. A key with no non-placeholder word at all
        /// is skipped, since it wouldn't actually distinguish this line from any other.
        ///
        /// If more than one placeholder key fits the line, we can't tell which is actually right,
        /// so this returns null rather than guessing — playing no audio is a smaller problem than
        /// playing the wrong line.
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

        // A substituted name (farm, kid, pet, player) or item is realistically never more than
        // a handful of words — but that's only a safe assumption when something anchors the
        // *end* of the substitution. A placeholder followed by more literal text (e.g.
        // "%kid1 is right?") is bounded: the regex still has to find "is right?" right after
        // it, so it can't run away. A placeholder with nothing after it in the key (e.g.
        // "Hi {0}!", "Hey, @.") has no such anchor — any cap on it still lets a short,
        // unrelated key swallow an entire longer, unrelated sentence just because both start
        // the same way (e.g. "Hey, got any eggs?" fit inside a 4-word cap meant for names).
        // So an un-anchored trailing placeholder is restricted to exactly one word, same as
        // if multi-word support didn't exist for it.
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

                // Punctuation stuck directly to the placeholder (e.g. "Hi {0}!", "Hey, @.")
                // is real text the dialogue line has to match exactly — it must anchor the
                // regex, not get swallowed into the placeholder's own gap. It's built into
                // the SAME pattern part as the gap (not pushed as its own part) since there's
                // no actual whitespace between them in the key to justify the \s+ a separate
                // part would get joined with below.
                //
                // lead/trail are punctuation-only by construction (TryParsePlaceholderWord
                // rejects letters/digits in them), so neither counts toward hasAnchorWord: a
                // key like "{0}!" is ALL placeholder + punctuation, no real text, so it can't
                // distinguish this line from any other ending in "!" — same reasoning as a
                // bare "%book" key with nothing around it at all. Without this, "{0}!" would
                // compile into "ends with !, 1-4 words", matching every one-line exclamation
                // (e.g. every "spouse attacked" reaction: "Die!", "Get out!", "Yuck!"...).
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

        // A word is a placeholder word if it's made of exactly one placeholder marker
        // (%word, {n}, or @) plus punctuation before/after it — never word characters, so a
        // marker embedded in ordinary text (not expected to occur) isn't mistaken for one.
        // On success, lead/trail hold that surrounding punctuation (either may be empty) —
        // literal text the caller must anchor around the placeholder's match gap.
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