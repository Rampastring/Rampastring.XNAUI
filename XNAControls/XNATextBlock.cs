using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace Rampastring.XNAUI.XNAControls
{
    /// <summary>
    /// A panel with text.
    /// </summary>
    public class XNATextBlock : XNAPanel
    {
        public XNATextBlock(WindowManager windowManager) : base(windowManager)
        {
            RemapColor = UISettings.TextColor;
        }

        public override string Text
        {
            get
            {
                return base.Text;
            }

            set
            {
                base.Text = Renderer.FixText(value, FontIndex, ClientRectangle.Width - TextXMargin * 2).Text;
            }
        }

        public int FontIndex { get; set; }

        private int _textXMargin = 3;

        public int TextXMargin
        {
            get { return _textXMargin; }
            set { _textXMargin = value; }
        }

        private int _textYPosition = 3;

        public int TextYPosition
        {
            get { return _textYPosition; }
            set { _textYPosition = value; }
        }

        public override void Draw(GameTime gameTime)
        {
            DrawPanel();

            if (!string.IsNullOrEmpty(Text))
            {
                var windowRectangle = WindowRectangle();

                Renderer.DrawStringWithShadow(Text, FontIndex,
                    new Vector2(windowRectangle.X + TextXMargin, windowRectangle.Y + TextYPosition), RemapColor);
            }

            for (int i = 0; i < Children.Count; i++)
            {
                if (Children[i].Visible)
                {
                    Children[i].Draw(gameTime);
                }
            }
        }
    }
}
