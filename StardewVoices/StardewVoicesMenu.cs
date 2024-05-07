using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using Microsoft.Xna.Framework.Audio;
using StardewValley.Menus;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Input;
using System.Linq;
using System;
using StardewVoices;


namespace StardewVoices
{
    // Reference https://github.com/CJBok/SDV-Mods/blob/master/CJBCheatsMenu/Framework/CheatsMenu.cs
    public class StardewVoicesMenu : IClickableMenu
    {
        private readonly List<ClickableComponent> OptionSlots = new();
        private readonly List<OptionsElement> Options = new();
        private readonly OptionsSlider DialogueVolumeSlider = new OptionsSlider("Dialogue Volume", 1);
        private float volume = 1f;
        private AudioCategory dialogueCategory;

        /*********
         ** Public methods
         *********/
        /// <summary>Construct an instance.</summary>
        public StardewVoicesMenu(AudioCategory dialogueCategory)
        {
            this.Options.Add(this.DialogueVolumeSlider);
            this.dialogueCategory = dialogueCategory;
        }

        /// <summary>Exit the menu if that's allowed for the current state.</summary>
        public void ExitIfValid()
        {
            if (this.readyToClose() && !GameMenu.forcePreventClose)
            {
                Game1.exitActiveMenu();
                Game1.playSound("bigDeSelect");
            }
        }

        /// <summary>Whether controller-style menus should be disabled for this menu.</summary>
        public override bool overrideSnappyMenuCursorMovementBan()
        {
            return true;
        }


        public void changeSliderOption(int value)
        {
            this.volume = (float)value / 100f;
            this.dialogueCategory.SetVolume(this.volume);
        }

        /// <summary>Handle a button press by the player.</summary>
        /// <param name="key">The button that was pressed.</param>
        public override void receiveKeyPress(Keys key)
        {
        }
        }
}