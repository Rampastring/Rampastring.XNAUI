using Microsoft.Xna.Framework;
using Rampastring.Tools;
using System;

namespace Rampastring.XNAUI.XNAControls
{
    /// <summary>
    /// A static label control.
    /// </summary>
    public class XNALabel : XNAControl
    {
        public XNALabel(WindowManager windowManager) : base(windowManager)
        {
        }

        private Color? _textColor;

        public Color TextColor
        {
            get
            {
                return _textColor ?? UISettings.ActiveSettings.TextColor;
            }
            set { _textColor = value; }
        }

        public int FontIndex { get; set; }

        /// <summary>
        /// Determines the point that the text is placed around
        /// depending on TextAnchor.
        /// </summary>
        public Vector2 AnchorPoint { get; set; }

        /// <summary>
        /// Determines the position of the label's text relative to AnchorPoint.
        /// </summary>
        public LabelTextAnchorInfo TextAnchor { get; set; }

        public override string Text
        {
            get
            {
                return base.Text;
            }

            set
            {
                base.Text = value;

                if (!string.IsNullOrEmpty(base.Text))
                {
                    Vector2 textSize = Renderer.GetTextDimensions(Text, FontIndex);

                    switch (TextAnchor)
                    {
                        case LabelTextAnchorInfo.CENTER:
                            ClientRectangle = new Rectangle((int)(AnchorPoint.X - textSize.X / 2),
                                (int)(AnchorPoint.Y - textSize.Y / 2), (int)textSize.X, (int)textSize.Y);
                            break;
                        case LabelTextAnchorInfo.RIGHT:
                            ClientRectangle = new Rectangle((int)AnchorPoint.X, (int)AnchorPoint.Y, (int)textSize.X, (int)textSize.Y);
                            break;
                        case LabelTextAnchorInfo.LEFT:
                            ClientRectangle = new Rectangle((int)(AnchorPoint.X - textSize.X),
                                (int)AnchorPoint.Y, (int)textSize.X, (int)textSize.Y);
                            break;
                        case LabelTextAnchorInfo.NONE:
                            ClientRectangle = new Rectangle(X, Y, (int)textSize.X, (int)textSize.Y);
                            break;
                    }
                }
            }
        }

        public override void Initialize()
        {
            base.Initialize();
        }

        public override void ParseAttributeFromINI(IniFile iniFile, string key, string value)
        {
            switch (key)
            {
                case "RemapColor":
                case "TextColor":
                    string[] colors = value.Split(',');
                    TextColor = AssetLoader.GetColorFromString(value);
                    return;
                case "FontIndex":
                    FontIndex = Conversions.IntFromString(value, 0);
                    return;
                case "AnchorPoint":
                    string[] point = value.Split(',');

                    if (point.Length == 2)
                    {
                        AnchorPoint = new Vector2(Conversions.FloatFromString(point[0], 0f),
                            Conversions.FloatFromString(point[1], 0f));
                    }

                    return;
                case "TextAnchor":
                    LabelTextAnchorInfo info;
                    bool success = Enum.TryParse(value, out info);

                    if (success)
                        TextAnchor = info;

                    return;
            }

            base.ParseAttributeFromINI(iniFile, key, value);
        }

        public override void Draw(GameTime gameTime)
        {
            DrawLabel();

            base.Draw(gameTime);
        }

        protected void DrawLabel()
        {
            if (!string.IsNullOrEmpty(Text))
                DrawStringWithShadow(Text, FontIndex, Vector2.Zero, TextColor);
        }
    }

    /// <summary>
    /// An enum for determining which part of a text is anchored to a specific point.
    /// </summary>
    public enum LabelTextAnchorInfo
    {
        NONE,
        LEFT,
        CENTER,
        RIGHT
    }
}
