using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

namespace Rampastring.XNAUI.XNAControls
{
    public class XNAListBoxItem
    {
        public XNAListBoxItem() { }

        public XNAListBoxItem(string text)
        {
            Text = text;
        }

        public XNAListBoxItem(string text, Color textColor)
        {
            Text = text;
            TextColor = textColor;
        }

        private Color? _textColor;

        public Color TextColor
        {
            get
            {
                if (_textColor.HasValue)
                    return _textColor.Value;

                if (!Selectable)
                    return UISettings.ActiveSettings.DisabledItemColor;

                return UISettings.ActiveSettings.AltColor;
            }
            set { _textColor = value; }
        }

        private Color? _backgroundColor;

        public Color BackgroundColor
        {
            get
            {
                return _backgroundColor ?? UISettings.ActiveSettings.BackgroundColor;
            }
            set { _backgroundColor = value; }
        }

        public Texture2D Texture { get; set; }

        public bool IsHeader { get; set; }

        public string Text { get; set; }

        /// <summary>
        /// Stores optional custom data associated with the list box item.
        /// </summary>
        public object Tag { get; set; }

        public int TextYPadding { get; set; }

        public int TextXPadding { get; set; }
        public bool Selectable { get; set; } = true;

        float alpha = 0.0f;
        public float Alpha
        {
            get { return alpha; }
            set
            {
                if (value < 0.0f)
                    alpha = 0.0f;
                else if (value > 1.0f)
                    alpha = 1.0f;
                else
                    alpha = value;
            }
        }

        public List<string> TextLines = new List<string>();
    }
}
