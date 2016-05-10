using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Rampastring.Tools;
using System;

namespace Rampastring.XNAUI.DXControls
{
    /// <summary>
    /// A check-box.
    /// </summary>
    public class DXCheckBox : DXControl
    {
        const int TEXT_PADDING = 3;

        public DXCheckBox(WindowManager windowManager) : base(windowManager)
        {
            RemapColor = UISettings.TextColor;
            HighlightColor = UISettings.AltColor;
            AlphaRate = UISettings.DefaultAlphaRate * 2.0;
        }

        public Texture2D CheckedTexture { get; set; }
        public Texture2D ClearTexture { get; set; }

        public Texture2D DisabledCheckedTexture { get; set; }
        public Texture2D DisabledClearTexture { get; set; }

        public bool Checked { get; set; }

        bool _allowChecking = true;
        public bool AllowChecking
        {
            get { return _allowChecking; }
            set { _allowChecking = value; }
        }

        public int FontIndex { get; set; }

        public Color HighlightColor { get; set; }

        public double AlphaRate { get; set; }

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

        int textLocationX;
        int textLocationY;

        Color _textColor;

        double checkedAlpha = 0.0;

        public override void Initialize()
        {
            if (CheckedTexture == null)
                CheckedTexture = UISettings.CheckBoxCheckedTexture;

            if (ClearTexture == null)
                ClearTexture = UISettings.CheckBoxClearTexture;

            if (DisabledCheckedTexture == null)
                DisabledCheckedTexture = UISettings.CheckBoxDisabledCheckedTexture;

            if (DisabledClearTexture == null)
                DisabledClearTexture = UISettings.CheckBoxDisabledClearTexture;

            SetTextPositionAndSize();

            _textColor = RemapColor;

            if (Checked)
            {
                checkedAlpha = 1.0;
            }

            base.Initialize();
        }

        protected override void ParseAttributeFromINI(IniFile iniFile, string key, string value)
        {
            switch (key)
            {
                case "FontIndex":
                    FontIndex = Utilities.IntFromString(value, 0);
                    return;
                case "HighlightColor":
                    HighlightColor = AssetLoader.GetColorFromString(value);
                    return;
                case "AlphaRate":
                    AlphaRate = Utilities.DoubleFromString(value, AlphaRate);
                    return;
                case "AllowChecking":
                    AllowChecking = Utilities.BooleanFromString(value, true);
                    return;
                case "Checked":
                    Checked = Utilities.BooleanFromString(value, true);
                    return;
            }

            base.ParseAttributeFromINI(iniFile, key, value);
        }

        private void SetTextPositionAndSize()
        {
            if (CheckedTexture == null)
                return;

            textLocationX = CheckedTexture.Width + TEXT_PADDING;

            if (!String.IsNullOrEmpty(Text))
            {
                Vector2 textDimensions = Renderer.GetTextDimensions(Text, FontIndex);

                textLocationY = (CheckedTexture.Height - (int)textDimensions.Y) / 2;

                ClientRectangle = new Rectangle(ClientRectangle.X, ClientRectangle.Y,
                    (int)textDimensions.X + TEXT_PADDING + CheckedTexture.Width,
                    Math.Max((int)textDimensions.Y, CheckedTexture.Height));
            }
            else
            {
                ClientRectangle = new Rectangle(ClientRectangle.X, ClientRectangle.Y, 
                    CheckedTexture.Width, CheckedTexture.Height);
            }
        }

        public override void OnLeftClick()
        {
            if (AllowChecking)
                Checked = !Checked;

            base.OnLeftClick();
        }

        public override void OnMouseEnter()
        {
            _textColor = HighlightColor;

            base.OnMouseEnter();
        }

        public override void OnMouseLeave()
        {
            _textColor = RemapColor;

            base.OnMouseLeave();
        }

        public override void Update(GameTime gameTime)
        {
            if (Checked)
            {
                checkedAlpha = Math.Min(checkedAlpha + AlphaRate, 1.0);
            }
            else
            {
                checkedAlpha = Math.Max(0.0, checkedAlpha - AlphaRate);
            }

            base.Update(gameTime);
        }

        public override void Draw(GameTime gameTime)
        {
            Texture2D clearTexture;
            Texture2D checkedTexture;

            if (AllowChecking)
            {
                clearTexture = ClearTexture;
                checkedTexture = CheckedTexture;
            }
            else
            {
                clearTexture = DisabledClearTexture;
                checkedTexture = DisabledCheckedTexture;
            }

            Rectangle displayRectangle = WindowRectangle();

            int checkBoxYPosition = displayRectangle.Y;
            int textYPosition = displayRectangle.Y + textLocationY;

            if (textLocationY < 0)
            {
                // If the text is higher than the checkbox texture (textLocationY < 0), 
                // let's draw the text at the top of the client
                // rectangle and the check-box in the middle of the text.
                // This is necessary for input to work properly.
                checkBoxYPosition -= textLocationY;
                textYPosition = displayRectangle.Y;
            }

            if (!String.IsNullOrEmpty(Text))
            {
                Color textColor = _textColor;
                if (AllowChecking)
                    textColor = _textColor;
                else
                    textColor = Color.Gray;

                Renderer.DrawStringWithShadow(Text, FontIndex,
                    new Vector2(displayRectangle.X + checkedTexture.Width + TEXT_PADDING, textYPosition),
                    textColor);
            }

            // Might not be worth it to save one draw-call per frame with a confusing
            // if-else routine, but oh well
            if (checkedAlpha == 0.0)
            {
                Renderer.DrawTexture(clearTexture,
                    new Rectangle(displayRectangle.X, checkBoxYPosition,
                    clearTexture.Width, clearTexture.Height), Color.White);
            }
            else if (checkedAlpha == 1.0)
            {
                Renderer.DrawTexture(checkedTexture,
                    new Rectangle(displayRectangle.X, checkBoxYPosition,
                    clearTexture.Width, clearTexture.Height), 
                    new Color(255, 255, 255, (int)(checkedAlpha * 255)));
            }
            else
            {
                Renderer.DrawTexture(clearTexture,
                    new Rectangle(displayRectangle.X, checkBoxYPosition,
                    clearTexture.Width, clearTexture.Height), Color.White);

                Renderer.DrawTexture(checkedTexture,
                    new Rectangle(displayRectangle.X, checkBoxYPosition,
                    clearTexture.Width, clearTexture.Height),
                    new Color(255, 255, 255, (int)(checkedAlpha * 255)));
            }

            base.Draw(gameTime);
        }
    }
}
