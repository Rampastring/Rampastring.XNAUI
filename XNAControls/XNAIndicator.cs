using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Rampastring.Tools;
using System;
using System.Collections.Generic;

namespace Rampastring.XNAUI.XNAControls
{
    /// <summary>
    /// An indicator with dynamic textures pool.
    /// </summary>
    public class XNAIndicator : XNAControl
    {
        private const int TEXT_PADDING_DEFAULT = 5;

        /// <summary>
        /// Creates a new indicator.
        /// </summary>
        /// <param name="windowManager">The window manager.</param>
        public XNAIndicator(WindowManager windowManager) : base(windowManager)
        {
            AlphaRate = UISettings.ActiveSettings.IndicatorAlphaRate * 2.0;
        }

        /// <summary>
        /// Contains a pool of textures for indicator.
        /// </summary>
        public Dictionary<string, Texture2D> Textures { get; set; }


        protected Texture2D _oldTexture = null;
        protected Texture2D _currentTexture = null;

        /// <summary>
        /// Determines the currently displayed texture.
        /// </summary>
        protected Texture2D CurrentTexture
        {
            get { return _currentTexture; }
            set
            {
                if (value != _currentTexture)
                {
                    _oldTexture = _currentTexture;
                    _currentTexture = value;
                    textureAlpha = 0.0;
                }
            }
        }

        /// <summary>
        /// The index of the text font.
        /// </summary>
        public int FontIndex { get; set; }

        /// <summary>
        /// The space, in pixels, between the indicator and its text.
        /// </summary>
        public int TextPadding { get; set; } = TEXT_PADDING_DEFAULT;

        private Color? _idleColor;

        /// <summary>
        /// The color of the indicator's text when it's not hovered on.
        /// </summary>
        public Color IdleColor
        {
            get => _idleColor ?? UISettings.ActiveSettings.TextColor;
            set { _idleColor = value; }
        }

        private Color? _highlightColor;

        /// <summary>
        /// The color of the indicator's text when it's hovered on.
        /// </summary>
        public Color HighlightColor
        {
            get => _highlightColor ?? UISettings.ActiveSettings.AltColor;
            set
            { _highlightColor = value; }
        }

        public double AlphaRate { get; set; }

        /// <summary>
        /// Gets or sets the text of the indicator.
        /// </summary>
        public override string Text
        {
            get
            {
                return base.Text;
            }

            set
            {
                base.Text = value;
                SetTextPositionAndSize();
            }
        }

        /// <summary>
        /// The Y coordinate of the indicator text
        /// relative to the location of the indicator.
        /// </summary>
        protected int TextLocationY { get; set; }


        private double textureAlpha = 1.0;


        public override void Initialize()
        {
            if (Textures == null || Textures.Count == 0)
                Textures = UISettings.ActiveSettings.IndicatorTextures;

            if (_oldTexture == null)
                _oldTexture = Textures[UISettings.ActiveSettings.IndicatorInitialTextureKey];

            if (_currentTexture == null)
                _currentTexture = Textures[UISettings.ActiveSettings.IndicatorInitialTextureKey];

            SetTextPositionAndSize();

            base.Initialize();
        }

        public void SwitchTexture(string key)
        {
            if (Textures.ContainsKey(key))
            {
                CurrentTexture = Textures[key];
            }
            else
            {
                Logger.Log("XNAIndicator: Tried to switch to non-existing texture " + key + "at indicator " + Name +  "!");
            }
        }

        protected override void ParseAttributeFromINI(IniFile iniFile, string key, string value)
        {
            switch (key)
            {
                case "FontIndex":
                    FontIndex = Conversions.IntFromString(value, 0);
                    return;
                case "HighlightColor":
                    HighlightColor = AssetLoader.GetColorFromString(value);
                    return;
                case "AlphaRate":
                    AlphaRate = Conversions.DoubleFromString(value, AlphaRate);
                    return;
            }

            base.ParseAttributeFromINI(iniFile, key, value);
        }

        /// <summary>
        /// Updates the size of the indicator and the vertical position of its text.
        /// </summary>
        protected virtual void SetTextPositionAndSize()
        {
            if (Textures == null || Textures.Count == 0)
                return;

            var enumerator = Textures.Values.GetEnumerator();
            enumerator.MoveNext();
            Texture2D texture2D = enumerator.Current;

            if (!string.IsNullOrEmpty(Text))
            {
                Vector2 textDimensions = Renderer.GetTextDimensions(Text, FontIndex);

                TextLocationY = (texture2D.Height - (int)textDimensions.Y) / 2 - 1;

                Width = (int)textDimensions.X + TEXT_PADDING_DEFAULT + texture2D.Width;
                Height = Math.Max((int)textDimensions.Y, texture2D.Height);
            }
            else
            {
                Width = texture2D.Width;
                Height = texture2D.Height;
            }
        }

        /// <summary>
        /// Updates the indicator's alpha each frame.
        /// </summary>
        public override void Update(GameTime gameTime)
        {
            double alphaRate = AlphaRate * (gameTime.ElapsedGameTime.TotalMilliseconds / 10.0);
            textureAlpha = Math.Min(textureAlpha + alphaRate, 1.0);
            
            base.Update(gameTime);
        }

        /// <summary>
        /// Draws the indicator.
        /// </summary>
        public override void Draw(GameTime gameTime)
        {
            int checkBoxYPosition = 0;
            int textYPosition = TextLocationY;

            if (TextLocationY < 0)
            {
                // If the text is higher than the checkbox texture (textLocationY < 0), 
                // let's draw the text at the top of the client
                // rectangle and the check-box in the middle of the text.
                // This is necessary for input to work properly.
                checkBoxYPosition -= TextLocationY;
                textYPosition = 0;
            }

            if (!string.IsNullOrEmpty(Text))
            {
                Color textColor = IsActive ? HighlightColor : IdleColor;

                DrawStringWithShadow(Text, FontIndex,
                    new Vector2(CurrentTexture.Width + TextPadding, textYPosition),
                    textColor);
            }

            // Don't draw old texture if new one is fully opaque
            if (textureAlpha < 1.0)
            {
                DrawTexture(_oldTexture,
                    new Rectangle(0, checkBoxYPosition,
                    _oldTexture.Width, _oldTexture.Height), Color.White);
            }

            // Don't draw new texture if it's fully invisible
            if (textureAlpha > 0.0)
            {
                DrawTexture(_currentTexture,
                    new Rectangle(0, checkBoxYPosition,
                    _currentTexture.Width, _currentTexture.Height),
                    new Color(255, 255, 255, (int)(textureAlpha * 255)));
            }

            base.Draw(gameTime);
        }
    }
}
