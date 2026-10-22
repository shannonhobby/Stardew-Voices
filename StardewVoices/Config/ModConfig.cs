using StardewModdingAPI.Utilities;
using StardewModdingAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// Reference: https://github.com/CJBok/SDV-Mods/blob/master/CJBCheatsMenu/Framework/Models/ModConfig.cs
namespace StardewVoices.Config
{
    /// <summary>The mod configuration model.</summary>
    public class ModConfig
    {
        /*********
         ** Accessors
         *********/
        /// <summary>The default values.</summary>
        public static ModConfig Defaults { get; } = new();

        /****
         ** Volume settings
         ****/
        /// <summary>The one voices volume slider (0-100). Applies to every character.</summary>
        public int DialogueVolume { get; set; } = 100;

        /****
         ** Voice pack settings
         ****/
        /// <summary>
        /// Which voice each character uses when more than one is installed: character name →
        /// a voice pack's UniqueID, or "Built-in". Characters not listed use the default
        /// (the first pack alphabetically that has them, otherwise the built-in voices).
        /// </summary>
        public Dictionary<string, string> CharacterVoices { get; set; } = new();

        /****
         ** Language settings
         ****/
        public string Language { get; set; } = "en";

        /****
          ** Dub-veleper settings
          ****/
        public List<string> CharactersToGenerate { get; set; } = new()
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

        public bool WarpNpcsToHome { get; set; } = false;

    }
}
