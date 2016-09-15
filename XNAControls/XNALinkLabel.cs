using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Rampastring.Tools;

namespace Rampastring.XNAUI.XNAControls
{
    /// <summary>
    /// A label that is underlined and changes its color when hovered on.
    /// </summary>
    public class XNALinkLabel : XNALabel
    {
        public XNALinkLabel(WindowManager windowManager) : base(windowManager)
        {
            IdleColor = UISettings.TextColor;
            HoverColor = UISettings.AltColor;
        }

        public Color IdleColor { get; set; }
        public Color HoverColor { get; set; }

        private bool _drawUnderline = true;

        /// <summary>
        /// Determines whether the label's text is drawn as underlined.
        /// </summary>
        public bool DrawUnderline
        {
            get { return _drawUnderline; }
            set { _drawUnderline = value; }
        }

        protected override void ParseAttributeFromINI(IniFile iniFile, string key, string value)
        {
            switch (key)
            {
                case "IdleColor":
                    IdleColor = AssetLoader.GetColorFromString(value);
                    return;
                case "HoverColor":
                    HoverColor = AssetLoader.GetColorFromString(value);
                    return;
            }

            base.ParseAttributeFromINI(iniFile, key, value);
        }

        public override void Initialize()
        {
            RemapColor = IdleColor;

            base.Initialize();
        }

        public override void OnMouseEnter()
        {
            RemapColor = HoverColor;

            base.OnMouseEnter();
        }

        public override void OnMouseLeave()
        {
            RemapColor = IdleColor;

            base.OnMouseLeave();
        }

        public override void Draw(GameTime gameTime)
        {
            DrawLabel();

            var displayRectangle = WindowRectangle();

            if (Enabled && DrawUnderline)
            {
                Renderer.FillRectangle(new Rectangle(
                    displayRectangle.X, displayRectangle.Bottom, displayRectangle.Width, 1),
                    RemapColor);
            }

            DrawChildren(gameTime);
        }
    }
}
