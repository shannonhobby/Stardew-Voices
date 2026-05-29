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
        public int DialogueVolume { get; set; } = 100;

        public int AbigailVolume { get; set; } = 100;
        public int AlexVolume { get; set; } = 100;
        public int ElliottVolume { get; set; } = 100;
        public int EmilyVolume { get; set; } = 100;
        public int HaleyVolume { get; set; } = 100;
        public int HarveyVolume { get; set; } = 100;
        public int LeahVolume { get; set; } = 100;
        public int MaruVolume { get; set; } = 100;
        public int PennyVolume { get; set; } = 100;
        public int SamVolume { get; set; } = 100;
        public int SebastianVolume { get; set; } = 100;
        public int ShaneVolume { get; set; } = 100;


        /****
         ** Language settings
         ****/
        public string Language { get; set; } = "en";
    }
}
