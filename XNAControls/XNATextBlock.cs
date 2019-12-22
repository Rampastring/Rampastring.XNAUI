using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Rampastring.Tools;

namespace Rampastring.XNAUI.XNAControls
{
    /// <summary>
    /// A panel with text.
    /// </summary>
    public class XNATextBlock : XNAPanel
    {
        public XNATextBlock(WindowManager windowManager) : base(windowManager)
        {
        }

        public override string Text
        {
            get
            {
                return base.Text;
            }

            set
            {
                base.Text = Renderer.FixText(value, FontIndex, Width - TextXMargin * 2).Text;
            }
        }

        private Color? _textColor;

        public Color TextColor
        {
            get
            {
                if (_textColor.HasValue)
                    return _textColor.Value;

                return UISettings.ActiveSettings.TextColor;
            }
            set { _textColor = value; }
        }

        public int FontIndex { get; set; }

        public int TextXMargin { get; set; } = 3;

        public int TextYPosition { get; set; } = 3;

        public override void ParseAttributeFromINI(IniFile iniFile, string key, string value)
        {
            switch (key)
            {
                case "TextColor":
                    TextColor = AssetLoader.GetColorFromString(value);
                    return;
            }

            base.ParseAttributeFromINI(iniFile, key, value);
        }

        public override void Draw(GameTime gameTime)
        {
            DrawPanel();

            if (!string.IsNullOrEmpty(Text))
            {
                var windowRectangle = RenderRectangle();

                DrawStringWithShadow(Text, FontIndex,
                    new Vector2(TextXMargin, TextYPosition), TextColor);
            }

            if (DrawBorders)
                DrawPanelBorders();

            DrawChildren(gameTime);
        }
    }
}
