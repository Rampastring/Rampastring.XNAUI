using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Rampastring.XNAUI.DXControls
{
    /// <summary>
    /// A drop-down / context menu item.
    /// </summary>
    public class DXDropDownItem
    {
        public Color TextColor { get; set; }

        public Texture2D Texture { get; set; }

        public string Text { get; set; }

        /// <summary>
        /// An object for containing custom info in the drop down item.
        /// </summary>
        public object Tag { get; set; }

        bool selectable = true;
        public bool Selectable
        {
            get { return selectable; }
            set { selectable = value; }
        }

        float alpha = 1.0f;
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
    }
}
