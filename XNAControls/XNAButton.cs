using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Rampastring.Tools;
using System;

namespace Rampastring.XNAUI.XNAControls
{
    /// <summary>
    /// A basic button.
    /// </summary>
    public class XNAButton : XNAControl
    {
        public XNAButton(WindowManager windowManager) : base(windowManager) 
        {
            AlphaRate = UISettings.ActiveSettings.DefaultAlphaRate;
        }

        public Texture2D IdleTexture { get; set; }

        public Texture2D HoverTexture { get; set; }

        public EnhancedSoundEffect HoverSoundEffect { get; set; }

        public EnhancedSoundEffect ClickSoundEffect { get; set; }

        public float AlphaRate { get; set; }
        float idleTextureAlpha = 1.0f;
        float hoverTextureAlpha = 0.0f;
        ButtonAnimationMode animationMode;

        public Keys HotKey { get; set; }

        public int FontIndex { get; set; }
        public bool AllowClick { get; set; } = true;

        string _text = String.Empty;
        public override string Text
        {
            get { return _text; }
            set
            {
                _text = value;
                if (AdaptiveText)
                {
                    CalculateTextPosition();
                }
            }
        }

        public int TextXPosition { get; set; }
        public int TextYPosition { get; set; }

        private Color? _textColorIdle;

        public Color TextColorIdle
        {
            get
            {
                return _textColorIdle ?? UISettings.ActiveSettings.ButtonTextColor;
            }
            set
            {
                _textColorIdle = value;

                if (!IsActive)
                    textColor = value;
            }
        }

        private Color? _textColorHover;

        public Color TextColorHover
        {
            get
            {
                return _textColorHover ?? UISettings.ActiveSettings.ButtonHoverColor;
            }
            set { _textColorHover = value; }
        }

        private Color? _textColorDisabled;

        public Color TextColorDisabled
        {
            get
            {
                return _textColorDisabled ?? UISettings.ActiveSettings.DisabledItemColor;
            }
            set { _textColorDisabled = value; }
        }

        /// <summary>
        /// The current color of the button's text.
        /// </summary>
        Color textColor = Color.White;

        public bool AdaptiveText { get; set; } = true;

        public override void OnMouseEnter()
        {
            base.OnMouseEnter();

            if (!AllowClick || Cursor.LeftDown)
                return;

            HoverSoundEffect?.Play();

            if (HoverTexture != null)
            {
                animationMode = ButtonAnimationMode.HIGHLIGHT;
                idleTextureAlpha = 0.5f;
                hoverTextureAlpha = 0.75f;
            }

            textColor = TextColorHover;
        }

        public override void OnMouseLeave()
        {
            base.OnMouseLeave();

            if (!AllowClick)
                return;

            if (HoverTexture != null)
            {
                animationMode = ButtonAnimationMode.RETURN;
                idleTextureAlpha = 0.75f;
                hoverTextureAlpha = 0.5f;
            }

            textColor = TextColorIdle;
        }

        public override void OnLeftClick()
        {
            if (!AllowClick)
                return;

            ClickSoundEffect?.Play();

            base.OnLeftClick();
        }

        public override void Initialize()
        {
            base.Initialize();

            if (IdleTexture != null && Width == 0 && Height == 0)
            {
                ClientRectangle = new Rectangle(X, Y,
                    IdleTexture.Width, IdleTexture.Height);
            }

            textColor = TextColorIdle;
        }

        protected override void OnClientRectangleUpdated()
        {
            if (AdaptiveText)
            {
                CalculateTextPosition();
            }
            
            base.OnClientRectangleUpdated();
        }

        private void CalculateTextPosition()
        {
            Vector2 textSize = Renderer.GetTextDimensions(_text, FontIndex);

            if (textSize.X < Width)
            {
                TextXPosition = (int)((Width - textSize.X) / 2);
            }
            else if (textSize.X > Width)
            {
                TextXPosition = (int)((textSize.X - Width) / -2);
            }

            if (textSize.Y < Height)
            {
                TextYPosition = (int)((Height - textSize.Y) / 2);
            }
            else if (textSize.Y > Height)
            {
                TextYPosition = Convert.ToInt32((textSize.Y - Height) / -2);
            }
        }

        protected override void ParseAttributeFromINI(IniFile iniFile, string key, string value)
        {
            switch (key)
            {
                case "TextColorIdle":
                    TextColorIdle = AssetLoader.GetColorFromString(value);
                    textColor = TextColorIdle;
                    return;
                case "TextColorHover":
                    TextColorHover = AssetLoader.GetColorFromString(value);
                    return;
                case "HoverSoundEffect":
                    HoverSoundEffect = new EnhancedSoundEffect(value);
                    return;
                case "ClickSoundEffect":
                    ClickSoundEffect = new EnhancedSoundEffect(value);
                    return;
                case "AdaptiveText":
                    AdaptiveText = Conversions.BooleanFromString(value, true);
                    return;
                case "AlphaRate":
                    AlphaRate = Conversions.FloatFromString(value, 0.01f);
                    return;
                case "FontIndex":
                    FontIndex = Conversions.IntFromString(value, 0);
                    if (AdaptiveText)
                        CalculateTextPosition();
                    return;
                case "IdleTexture":
                    IdleTexture = AssetLoader.LoadTexture(iniFile.GetStringValue(Name, "IdleTexture", String.Empty));
                    ClientRectangle = new Rectangle(X, Y,
                        IdleTexture.Width, IdleTexture.Height);
                    if (AdaptiveText)
                        CalculateTextPosition();
                    return;
                case "HoverTexture":
                    HoverTexture = AssetLoader.LoadTexture(iniFile.GetStringValue(Name, "HoverTexture", String.Empty));
                    return;
            }

            base.ParseAttributeFromINI(iniFile, key, value);
        }

        public override void Kill()
        {
            base.Kill();

            if (IdleTexture != null)
                IdleTexture.Dispose();

            if (HoverTexture != null)
                HoverTexture.Dispose();

            if (HoverSoundEffect != null)
                HoverSoundEffect.Dispose();

            if (ClickSoundEffect != null)
                ClickSoundEffect.Dispose();
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            float alphaRate = AlphaRate * (float)(gameTime.ElapsedGameTime.TotalMilliseconds / 10.0);

            if (animationMode == ButtonAnimationMode.HIGHLIGHT)
            {
                idleTextureAlpha -= alphaRate;
                if (idleTextureAlpha < 0.0f)
                {
                    idleTextureAlpha = 0.0f;
                }

                hoverTextureAlpha += alphaRate;
                if (hoverTextureAlpha >= 1.0f)
                {
                    hoverTextureAlpha = 1.0f;
                }
            }
            else
            {
                hoverTextureAlpha -= alphaRate;
                if (hoverTextureAlpha < 0.0f)
                {
                    hoverTextureAlpha = 0.0f;
                }

                idleTextureAlpha += alphaRate;
                if (idleTextureAlpha >= 1.0f)
                {
                    idleTextureAlpha = 1.0f;
                }
            }

            if (Parent != null && Parent.IsActive && Keyboard.PressedKeys.Contains(HotKey))
                OnLeftClick();
        }

        public override void Draw(GameTime gameTime)
        {
            if (IdleTexture != null)
            {
                if (idleTextureAlpha > 0f)
                    DrawTexture(IdleTexture, Point.Zero, 
                        new Color(RemapColor.R, RemapColor.G, RemapColor.B, (int)(idleTextureAlpha * Alpha * 255)));

                if (HoverTexture != null && hoverTextureAlpha > 0f)
                    DrawTexture(HoverTexture, Point.Zero, 
                        new Color(RemapColor.R, RemapColor.G, RemapColor.B, (int)(hoverTextureAlpha * Alpha * 255)));
            }

            Vector2 textPosition = new Vector2(TextXPosition, TextYPosition);

            if (!Enabled || !AllowClick)
                DrawStringWithShadow(_text, FontIndex, textPosition, TextColorDisabled);
            else
                DrawStringWithShadow(_text, FontIndex, textPosition, GetColorWithAlpha(textColor));

            base.Draw(gameTime);
        }
    }

    enum ButtonAnimationMode
    {
        NONE,
        HIGHLIGHT,
        RETURN
    }
}
