using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.GameData.Characters;
using StardewValley.ItemTypeDefinitions;
using StardewValley.TokenizableStrings;
using System.Collections.Generic;
using System.Linq;

namespace .StardewVoices.Utils.GameDialogueExtraction
{
    public class GossipDialogueExtractor
    {
        public List<StardewValley.Dialogue> GetAllPossibleGossipDialogues(NPC character)
        {
            var results = new List<StardewValley.Dialogue>();

            CharacterData data = character.GetData();
            if (data?.FriendsAndFamily == null)
                return results;

            foreach (var (relativeKey, rawRelationshipLabel) in data.FriendsAndFamily)
            {
                NPC relativeNPC = Game1.getCharacterFromName(relativeKey);
                string relativeName = relativeNPC?.displayName ?? NPC.GetDisplayName(relativeKey);

                CharacterData relativeData;
                bool relativeIsMale = relativeNPC != null
                    ? relativeNPC.gender.Value == Gender.Male
                    : NPC.TryGetData(relativeKey, out relativeData) && relativeData.Gender == Gender.Male;

                string relationshipLabel = TokenParser.ParseText(rawRelationshipLabel);
                if (string.IsNullOrWhiteSpace(relationshipLabel))
                    relationshipLabel = null;

                if (!Game1.NPCGiftTastes.TryGetValue(relativeKey, out string giftTasteData))
                    continue;

                string[] giftFields = giftTasteData.Split('/');

                string nameOrRelationship = relationshipLabel == null || LocalizedContentManager.CurrentLanguageCode == LocalizedContentManager.LanguageCode.ja
                    ? relativeName
                    : relativeIsMale
                        ? Game1.LoadStringByGender(character.Gender, "Strings\\StringsFromCSFiles:NPC.cs.4079", relationshipLabel)
                        : Game1.LoadStringByGender(character.Gender, "Strings\\StringsFromCSFiles:NPC.cs.4080", relationshipLabel);

                string speakerIntro = Game1.content.LoadString("Strings\\StringsFromCSFiles:NPC.cs.4083", nameOrRelationship);

                // --- LOVED items (array[1]) ---
                string[] lovedItems = ArgUtility.SplitBySpace(ArgUtility.Get(giftFields, 1));
                foreach (string itemId in lovedItems)
                {
                    if (itemId == null || itemId.StartsWith("-")) continue;

                    // Special case: Penny won't gossip about Pam drinking
                    if (character.Name == "Penny" && relativeKey == "Pam"
                        && (itemId == "303" || itemId == "346" || itemId == "348" || itemId == "459"))
                        continue;

                    ParsedItemData itemData = ItemRegistry.GetData(itemId);
                    if (itemData == null) continue;

                    string itemName = itemData.DisplayName;
                    string baseText = speakerIntro + Game1.content.LoadString("Strings\\StringsFromCSFiles:NPC.cs.4084", itemName);

                    var texts = new List<string>();

                    if (character.Age == 2) // Child
                    {
                        string childText = Game1.content.LoadString("Strings\\StringsFromCSFiles:NPC.cs.4086", nameOrRelationship, itemName)
                            + (relativeIsMale
                                ? Game1.LoadStringByGender(character.Gender, "Strings\\StringsFromCSFiles:NPC.cs.4088")
                                : Game1.LoadStringByGender(character.Gender, "Strings\\StringsFromCSFiles:NPC.cs.4089"));
                        texts.Add(childText);
                    }
                    else
                    {
                        // 4 adult main variants (random.Next(5) but case 4 falls through to baseText)
                        texts.Add(Game1.LoadStringByGender(character.Gender, "Strings\\StringsFromCSFiles:NPC.cs.4091", nameOrRelationship, itemName));
                        texts.Add(relativeIsMale
                            ? Game1.content.LoadString("Strings\\StringsFromCSFiles:NPC.cs.4094", nameOrRelationship, itemName)
                            : Game1.content.LoadString("Strings\\StringsFromCSFiles:NPC.cs.4097", nameOrRelationship, itemName));
                        texts.Add(relativeIsMale
                            ? Game1.content.LoadString("Strings\\StringsFromCSFiles:NPC.cs.4100", nameOrRelationship, itemName)
                            : Game1.content.LoadString("Strings\\StringsFromCSFiles:NPC.cs.4103", nameOrRelationship, itemName));
                        texts.Add(Game1.content.LoadString("Strings\\StringsFromCSFiles:NPC.cs.4106", nameOrRelationship, itemName));
                        texts.Add(baseText);

                        // Cross-product: every main variant × every suffix (the 65% branch in original)
                        var suffixes = new List<string>
                        {
                            relativeIsMale
                                ? Game1.content.LoadString("Strings\\StringsFromCSFiles:NPC.cs.4109")
                                : Game1.content.LoadString("Strings\\StringsFromCSFiles:NPC.cs.4111"),
                            relativeIsMale
                                ? Game1.LoadStringByGender(character.Gender, "Strings\\StringsFromCSFiles:NPC.cs.4113")
                                : Game1.LoadStringByGender(character.Gender, "Strings\\StringsFromCSFiles:NPC.cs.4115"),
                            relativeIsMale
                                ? Game1.LoadStringByGender(character.Gender, "Strings\\StringsFromCSFiles:NPC.cs.4113")
                                : Game1.content.LoadString("Strings\\StringsFromCSFiles:NPC.cs.4116"),
                            relativeIsMale
                                ? Game1.content.LoadString("Strings\\StringsFromCSFiles:NPC.cs.4118")
                                : Game1.content.LoadString("Strings\\StringsFromCSFiles:NPC.cs.4120"),
                            Game1.content.LoadString("Strings\\StringsFromCSFiles:NPC.cs.4125"),
                            relativeIsMale
                                ? Game1.content.LoadString("Strings\\StringsFromCSFiles:NPC.cs.4126")
                                : Game1.content.LoadString("Strings\\StringsFromCSFiles:NPC.cs.4128"),
                        };

                        texts.AddRange(texts.SelectMany(t => suffixes.Select(s => t + s)).ToList());

                        // Abigail special-case override
                        if (relativeKey == "Abigail")
                            texts.Add(Game1.content.LoadString("Strings\\StringsFromCSFiles:NPC.cs.4128", relativeName, itemName));
                    }

                    foreach (string t in texts)
                    {
                        string final = CapitalizeFirst(t) + "%revealtaste:" + relativeKey + ":" + itemId;
                        results.Add(new StardewValley.Dialogue(character, null, final));
                    }
                }

                // --- HATED items (array[7], fallback to Universal_Hate) ---
                string[] hatedItems = ArgUtility.SplitBySpace(ArgUtility.Get(giftFields, 7));
                if (hatedItems.Length == 0)
                {
                    if (Game1.NPCGiftTastes.TryGetValue("Universal_Hate", out string universalHate))
                        hatedItems = ArgUtility.SplitBySpace(ArgUtility.Get(universalHate.Split('/'), 7));
                }

                foreach (string itemId in hatedItems)
                {
                    if (itemId == null || itemId.StartsWith("-")) continue;

                    ParsedItemData itemData = ItemRegistry.GetData(itemId);
                    if (itemData == null) continue;

                    string itemName = itemData.DisplayName;
                    string negAdj = Lexicon.getRandomNegativeFoodAdjective();

                    var texts = new List<string>();

                    string baseHateText = speakerIntro + (relativeIsMale
                        ? Game1.content.LoadString("Strings\\StringsFromCSFiles:NPC.cs.4135", itemName, negAdj)
                        : Game1.content.LoadString("Strings\\StringsFromCSFiles:NPC.cs.4138", itemName, negAdj));

                    if (character.Age == 2) // Child
                    {
                        texts.Add(relativeIsMale
                            ? Game1.LoadStringByGender(character.Gender, "Strings\\StringsFromCSFiles:NPC.cs.4141", relativeName, itemName)
                            : Game1.LoadStringByGender(character.Gender, "Strings\\StringsFromCSFiles:NPC.cs.4144", relativeName, itemName));
                    }
                    else
                    {
                        // 3 adult main variants (random.Next(4), case 3 falls through to baseHateText)
                        texts.Add(Game1.LoadStringByGender(character.Gender, "Strings\\StringsFromCSFiles:NPC.cs.4147", nameOrRelationship, itemName));
                        texts.Add(Game1.LoadStringByGender(character.Gender, "Strings\\StringsFromCSFiles:NPC.cs.4146")
                            + Game1.LoadStringByGender(character.Gender, "Strings\\StringsFromCSFiles:NPC.cs.4147", nameOrRelationship, itemName));
                        texts.Add(relativeIsMale
                            ? Game1.LoadStringByGender(character.Gender, "Strings\\StringsFromCSFiles:NPC.cs.4149", nameOrRelationship, itemName)
                            : Game1.LoadStringByGender(character.Gender, "Strings\\StringsFromCSFiles:NPC.cs.4153", nameOrRelationship, itemName));
                        texts.Add(relativeIsMale
                            ? Game1.LoadStringByGender(character.Gender, "Strings\\StringsFromCSFiles:NPC.cs.4152", nameOrRelationship, itemName)
                            : Game1.LoadStringByGender(character.Gender, "Strings\\StringsFromCSFiles:NPC.cs.4154", nameOrRelationship, itemName));
                        texts.Add(relativeIsMale
                            ? Game1.content.LoadString("Strings\\StringsFromCSFiles:NPC.cs.4161", nameOrRelationship, itemName)
                            : Game1.content.LoadString("Strings\\StringsFromCSFiles:NPC.cs.4164", nameOrRelationship, itemName));
                        texts.Add(baseHateText);

                        var hateSuffixes = new List<string>
                        {
                            Game1.content.LoadString("Strings\\StringsFromCSFiles:NPC.cs.4170"),
                            Game1.content.LoadString("Strings\\StringsFromCSFiles:NPC.cs.4171"),
                            relativeIsMale
                                ? Game1.content.LoadString("Strings\\StringsFromCSFiles:NPC.cs.4172")
                                : Game1.content.LoadString("Strings\\StringsFromCSFiles:NPC.cs.4174"),
                            relativeIsMale
                                ? Game1.content.LoadString("Strings\\StringsFromCSFiles:NPC.cs.4176")
                                : Game1.content.LoadString("Strings\\StringsFromCSFiles:NPC.cs.4178"),
                            Game1.content.LoadString("Strings\\StringsFromCSFiles:NPC.cs.4180"),
                        };

                        texts.AddRange(texts.SelectMany(t => hateSuffixes.Select(s => t + s)).ToList());

                        // Lewis special-case override
                        if (character.Name == "Lewis")
                            texts.Add(Game1.content.LoadString("Strings\\StringsFromCSFiles:NPC.cs.4182", relativeName, itemName));
                    }

                    foreach (string t in texts)
                        results.Add(new StardewValley.Dialogue(character, null, t));
                }
            }

            return results;
        }

        private static string CapitalizeFirst(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            try { return s.Substring(0, 1).ToUpper() + s.Substring(1); }
            catch { return s; }
        }
    }
}
