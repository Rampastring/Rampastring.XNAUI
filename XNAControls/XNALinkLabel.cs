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
        /// <summary>
        /// Creates a new link label.
        /// </summary>
        /// <param name="windowManager">The window manager.</param>
        public XNALinkLabel(WindowManager windowManager) : base(windowManager)
        {
        }

        private Color? _idleColor;

        /// <summary>
        /// The color of the label when it's not hovered on.
        /// </summary>
        public Color IdleColor
        {
            get
            {
                return _idleColor ?? UISettings.ActiveSettings.TextColor;
            }
            set { _idleColor = value; if (!IsActive) RemapColor = value; }
        }

        private Color? _hoverColor;

        /// <summary>
        /// The color of the label when it's hovered on.
        /// </summary>
        public Color HoverColor
        {
            get
            {
                return _hoverColor ?? UISettings.ActiveSettings.AltColor;
            }
            set { _hoverColor = value; if (IsActive) RemapColor = value; }
        }

        /// <summary>
        /// Determines whether the label's text is drawn as underlined.
        /// </summary>
        public bool DrawUnderline { get; set; } = true;

        public override void ParseAttributeFromINI(IniFile iniFile, string key, string value)
        {
            switch (key)
            {
                case "IdleColor":
                    IdleColor = AssetLoader.GetColorFromString(value);
                    return;
                case "HoverColor":
                    HoverColor = AssetLoader.GetColorFromString(value);
                    return;
                case "DrawUnderline":
                    DrawUnderline = Conversions.BooleanFromString(value, DrawUnderline);
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

            var displayRectangle = RenderRectangle();

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
