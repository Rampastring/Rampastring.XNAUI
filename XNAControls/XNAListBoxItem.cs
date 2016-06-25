using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Rampastring.XNAUI.XNAControls
{
    public class XNAListBoxItem
    {
        public XNAListBoxItem() { }

        public XNAListBoxItem(string text)
        {
            Text = text;
            TextColor = UISettings.AltColor;
        }

        public XNAListBoxItem(string text, Color textColor)
        {
            Text = text;
            TextColor = textColor;
        }

        public Color TextColor { get; set; }

        public Color BackgroundColor { get; set; }

        public Texture2D Texture { get; set; }

        public bool IsHeader { get; set; }

        public string Text { get; set; }

        /// <summary>
        /// Stores optional custom data associated with the list box item.
        /// </summary>
        public object Tag { get; set; }

        public int TextYPadding { get; set; }

        public int TextXPadding { get; set; }

        bool selectable = true;
        public bool Selectable
        {
            get { return selectable; }
            set { selectable = value; }
        }

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
