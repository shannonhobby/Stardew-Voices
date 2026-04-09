using StardewModdingAPI.Utilities;
using StardewModdingAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// Reference: https://github.com/CJBok/SDV-Mods/blob/master/CJBCheatsMenu/Framework/Models/ModConfig.cs
namespace StardewVoices
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
         ** Keyboard buttons
         ****/
        /// <summary>The button which opens the menu.</summary>
        public KeybindList OpenMenuKey { get; set; } = new(SButton.V);

        /****
         ** Menu settings
         ****/
        public float DialogueVolume { get; set; } = 3f;

        /****
         ** Language settings
         ****/
        public string Language { get; set; } = "en";
    }
}
