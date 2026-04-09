using System;
using System.Collections.Generic;
using StardewValley;
using Microsoft.Xna.Framework.Audio;
using StardewValley.Menus;
using Microsoft.Xna.Framework.Input;
using System.Linq;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using StardewModdingAPI.Framework.Logging;
using System.Diagnostics.CodeAnalysis;
using StardewModdingAPI;
using System.Reflection.Emit;
using Newtonsoft.Json.Linq;
using System.IO;




namespace StardewVoices
{
    // Reference https://github.com/CJBok/SDV-Mods/blob/master/CJBCheatsMenu/Framework/CheatsMenu.cs
    // and https://github.com/janavarro95/Stardew_Valley_Mods/blob/d7d567a72f5feaf3a5a5afd3d054ac9598727a97/GeneralMods/HappyBirthday/Framework/Menus/BirthdayMenu.cs
    public class Menu : IClickableMenu
    {
        private readonly List<ClickableComponent> OptionSlots = new();
        private readonly List<OptionsElement> Options = new();
        private readonly OptionsSlider DialogueVolumeSlider = new OptionsSlider("Dialogue Volume", 100);
        private ModConfig config;
        private IModHelper helper;

        private static string configPath = "mods\\StardewVoices\\config.json";


        public float volume;

        private AudioCategory dialogueCategory;

        /*********
         ** Public methods
         *********/
        /// <summary>Construct an instance.</summary>
        public Menu(AudioCategory dialogueCategory, ModConfig config, IModHelper helper)
        {
            this.dialogueCategory = dialogueCategory;
            this.config = config;
            this.helper = helper;
            this.volume = this.config.DialogueVolume;

            Game1.playSound("bigSelect");

            this.ResetComponents();

            this.DialogueVolumeSlider.value = (int)((volume));

            this.AddOptions();
        }

        /*********
          ** Private methods
          *********/
        /// <summary>Initialize or reinitialize the UI components.</summary>
        private void ResetComponents()
        {
            // set dimensions
            this.width = (500) + IClickableMenu.borderWidth * 2;
            this.height = (300) + IClickableMenu.borderWidth * 2;
            this.xPositionOnScreen = Game1.uiViewport.Width / 2 - (this.width) / 2;
            this.yPositionOnScreen = Game1.uiViewport.Height / 2 - this.height / 2;
            this.AddOptions();

        }

        private void AddOptions()
        {
            this.Options.Clear();
            this.Options.Add(new OptionsElement("Stardew Voices"));
            this.Options.Add(this.DialogueVolumeSlider);

            this.OptionSlots.Clear();
            this.OptionSlots.Add(new ClickableComponent(new Rectangle(this.xPositionOnScreen + Game1.tileSize / 4, (this.yPositionOnScreen + Game1.tileSize * 5 / 4 + Game1.pixelZoom + 1 * ((this.height - Game1.tileSize * 2) / 2)) - 100, this.width - Game1.tileSize / 2, (this.height - Game1.tileSize * 2) / 2 + Game1.pixelZoom), string.Concat(0)));
            this.OptionSlots.Add(new ClickableComponent(new Rectangle(this.xPositionOnScreen + Game1.tileSize / 4, (this.yPositionOnScreen + Game1.tileSize * 5 / 4 + Game1.pixelZoom + 1 * ((this.height - Game1.tileSize * 2) / 2)) + 20, this.width - Game1.tileSize / 2, (this.height - Game1.tileSize * 2) / 2 + Game1.pixelZoom), string.Concat(1)));

        }

        /// <summary>Handle the game window being resized.</summary>
        /// <param name="oldBounds">The previous window bounds.</param>
        /// <param name="newBounds">The new window bounds.</param>
        public override void gameWindowSizeChanged(Rectangle oldBounds, Rectangle newBounds)
        {
            this.ResetComponents();
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
  
   
        /// <summary>Handle the player pressing a keyboard button.</summary>
        /// <param name="key">The key that was pressed.</param>
        public override void receiveKeyPress(Keys key)
        {
            // exit menu
            if (Game1.options.menuButton.Contains(new InputButton(key)))
                this.ExitIfValid();
        }

        public void changeSliderOption(float value)
        {
            this.dialogueCategory.SetVolume(value);
            this.config.DialogueVolume = value;
            this.helper.WriteConfig(this.config);
        }

        /// <summary>The method invoked when the player left-clicks on the menu.</summary>
        /// <param name="x">The X-position of the cursor.</param>
        /// <param name="y">The Y-position of the cursor.</param>
        /// <param name="playSound">Whether to enable sound.</param>
        public override void receiveLeftClick(int x, int y, bool playSound = true)
        {

            base.receiveLeftClick(x, y, playSound);

            if (this.OptionSlots[1].bounds.Contains(x,y))
            {
                this.leftClickHeld(x, y);
            }
        }

        /// <summary>Handle the player holding the left mouse button.</summary>
        /// <param name="x">The cursor's X pixel position.</param>
        /// <param name="y">The cursor's Y pixel position.</param>
        public override void leftClickHeld(int x, int y)
        {
            if (GameMenu.forcePreventClose)
                return;
            base.leftClickHeld(x, y);

            if (this.OptionSlots[1].bounds.Contains(x, y))
            {
                if (x < this.OptionSlots[1].bounds.X)
                {
                    this.volume = 0f;
                }
                else if (x > this.OptionSlots[1].bounds.Right)
                {
                    this.volume = 3f;
                }
                else
                {
                    this.volume = (float)((x - this.OptionSlots[1].bounds.X) / (float)(this.OptionSlots[1].bounds.Width) * 3);
                }
                this.DialogueVolumeSlider.value = (int)(this.volume);
                this.changeSliderOption(this.volume);
            }
        }


        /// <summary>The method invoked when the player right-clicks on the lookup UI.</summary>
        /// <param name="x">The X-position of the cursor.</param>
        /// <param name="y">The Y-position of the cursor.</param>
        /// <param name="playSound">Whether to enable sound.</param>
        public override void receiveRightClick(int x, int y, bool playSound = true) { }


        /// <summary>Draw the menu to the screen.</summary>
        /// <param name="spriteBatch">The sprite batch being drawn.</param>
        public override void draw(SpriteBatch spriteBatch)
        {
            if (!Game1.options.showMenuBackground)
                spriteBatch.Draw(Game1.fadeToBlackRect, Game1.graphics.GraphicsDevice.Viewport.Bounds, Color.Black * 0.4f);
            base.draw(spriteBatch);

            Game1.drawDialogueBox(this.xPositionOnScreen, this.yPositionOnScreen, this.width, this.height, false, true);
            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.FrontToBack, BlendState.AlphaBlend, SamplerState.PointClamp);

            // Title
            //Utility.drawTextWithShadow(spriteBatch, "Stardew Voices", Game1.dialogueFont, new Vector2(), Game1.textColor, 1f, 0.1f);
            this.Options[0].draw(spriteBatch, this.OptionSlots[0].bounds.X, this.OptionSlots[0].bounds.Y);

            // Slider
            this.Options[1].draw(spriteBatch, this.OptionSlots[1].bounds.X, this.OptionSlots[1].bounds.Y);

            // draw cursor
            this.drawMouse(spriteBatch);
        }

    }
}