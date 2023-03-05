namespace Rampastring.XNAUI.XNAControls;

using System;
using Microsoft.Xna.Framework;
using Rampastring.Tools;

/// <summary>
/// A static label control.
/// </summary>
public class XNALabel : XNAControl
{
    public XNALabel(WindowManager windowManager)
        : base(windowManager)
    {
    }

    private Color? _textColor;

    public Color TextColor
    {
        get => _textColor ?? UISettings.ActiveSettings.TextColor;

        set => _textColor = value;
    }

    public int FontIndex { get; set; }

    public float TextShadowDistance { get; set; } = UISettings.ActiveSettings.TextShadowDistance;

    private Vector2 _anchorPoint = Vector2.Zero;

    /// <summary>
    /// Determines the point that the text is placed around
    /// depending on TextAnchor.
    /// </summary>
    public Vector2 AnchorPoint
    {
        get => _anchorPoint;
        set
        {
            _anchorPoint = value;
            RefreshClientRectangle();
        }
    }

    /// <summary>
    /// Determines the position of the label's text relative to AnchorPoint.
    /// </summary>
    public LabelTextAnchorInfo TextAnchor { get; set; }

    public override string Text
    {
        get => base.Text;

        set
        {
            base.Text = value;
            RefreshClientRectangle();
        }
    }

    private void RefreshClientRectangle()
    {
        if (!string.IsNullOrEmpty(base.Text))
        {
            Vector2 textSize = Renderer.GetTextDimensions(Text, FontIndex);

            Width = (int)textSize.X;
            Height = (int)textSize.Y;

            if (TextAnchor != LabelTextAnchorInfo.NONE)
            {
                X = (int)AnchorPoint.X;
                Y = (int)AnchorPoint.Y;

                if ((TextAnchor & LabelTextAnchorInfo.HORIZONTAL_CENTER) == LabelTextAnchorInfo.HORIZONTAL_CENTER)
                {
                    X = (int)(AnchorPoint.X - (textSize.X / 2));
                }
                else if ((TextAnchor & LabelTextAnchorInfo.RIGHT) == LabelTextAnchorInfo.RIGHT)
                {
                    X = (int)AnchorPoint.X;
                }
                else if ((TextAnchor & LabelTextAnchorInfo.LEFT) == LabelTextAnchorInfo.LEFT)
                {
                    X = (int)(AnchorPoint.X - textSize.X);
                }

                if ((TextAnchor & LabelTextAnchorInfo.VERTICAL_CENTER) == LabelTextAnchorInfo.VERTICAL_CENTER)
                {
                    Y = (int)(AnchorPoint.Y - (textSize.Y / 2));
                }
                else if ((TextAnchor & LabelTextAnchorInfo.TOP) == LabelTextAnchorInfo.TOP)
                {
                    Y = (int)(AnchorPoint.Y - textSize.Y);
                }
                else if ((TextAnchor & LabelTextAnchorInfo.BOTTOM) == LabelTextAnchorInfo.BOTTOM)
                {
                    Y = (int)AnchorPoint.Y;
                }
            }
        }
    }

    protected override void ParseControlINIAttribute(IniFile iniFile, string key, string value)
    {
        switch (key)
        {
            case "RemapColor":
            case "TextColor":
                TextColor = AssetLoader.GetColorFromString(value);
                return;
            case "FontIndex":
                FontIndex = Conversions.IntFromString(value, 0);
                return;
            case "AnchorPoint":
                string[] point = value.Split(',');

                if (point.Length == 2)
                {
                    AnchorPoint = new(
                        Conversions.FloatFromString(point[0], 0f),
                        Conversions.FloatFromString(point[1], 0f));
                }

                return;
            case "TextAnchor":
                bool success = Enum.TryParse(value, out LabelTextAnchorInfo info);

                if (success)
                    TextAnchor = info;

                return;
            case "TextShadowDistance":
                TextShadowDistance = Conversions.FloatFromString(value, TextShadowDistance);
                return;
        }

        base.ParseControlINIAttribute(iniFile, key, value);
    }

    public override void Draw(GameTime gameTime)
    {
        DrawLabel();

        base.Draw(gameTime);
    }

    protected void DrawLabel()
    {
        if (!string.IsNullOrEmpty(Text))
            DrawStringWithShadow(Text, FontIndex, Vector2.Zero, TextColor, 1.0f, TextShadowDistance);
    }
}