namespace Rampastring.XNAUI.XNAControls;

using System;
using Microsoft.Xna.Framework;

public class XNAProgressBar : XNAControl
{
    public XNAProgressBar(WindowManager windowManager)
        : base(windowManager)
    {
    }

    public int BorderWidth { get; set; } = 1;

    private Color? _borderColor;

    public Color BorderColor
    {
        get => _borderColor ?? UISettings.ActiveSettings.PanelBorderColor;
        set => _borderColor = value;
    }

    private Color? _filledColor;

    public Color FilledColor
    {
        get => _filledColor ?? UISettings.ActiveSettings.AltColor;
        set => _filledColor = value;
    }

    private Color? _unfilledColor;

    public Color UnfilledColor
    {
        get => _unfilledColor ?? new Color(FilledColor.R / 3, FilledColor.G / 3, FilledColor.B / 3, FilledColor.A);
        set => _unfilledColor = value;
    }

    public int Maximum { get; set; }

    public bool SmoothBackwardTransition { get; set; }

    public bool SmoothForwardTransition { get; set; }

    public int SmoothTransitionRate { get; set; } = 1;

    private int _value;

    public int Value
    {
        get => _value;

        set => _value = value > Maximum ? Maximum : value;
    }

    private int shownValue;

    public override void Update(GameTime gameTime)
    {
        if (shownValue < _value)
        {
            shownValue = SmoothForwardTransition ? Math.Min(shownValue + SmoothTransitionRate, _value) : _value;
        }
        else if (shownValue > _value)
        {
            shownValue = SmoothBackwardTransition ? Math.Max(0, shownValue - SmoothTransitionRate) : _value;
        }
    }

    public override void Draw(GameTime gameTime)
    {
        Rectangle wrect = RenderRectangle();

        for (int i = 0; i < BorderWidth; i++)
        {
            var rect = new Rectangle(
                wrect.X + i, wrect.Y + i, wrect.Width - i, wrect.Height - i);

            Renderer.DrawRectangle(rect, BorderColor);
        }

        int filledWidth = (int)(shownValue / (double)Maximum * (Width - (BorderWidth * 2)));

        var filledRect = new Rectangle(
            wrect.X + BorderWidth, wrect.Y + BorderWidth, filledWidth, wrect.Height - (BorderWidth * 2));

        Renderer.FillRectangle(filledRect, FilledColor);

        var unfilledRect = new Rectangle(
            wrect.X + BorderWidth + filledWidth,
            wrect.Y + BorderWidth,
            wrect.Width - filledWidth - (BorderWidth * 2),
            wrect.Height - (BorderWidth * 2));

        Renderer.FillRectangle(unfilledRect, UnfilledColor);

        base.Draw(gameTime);
    }
}