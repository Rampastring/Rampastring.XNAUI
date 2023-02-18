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

    private Color? borderColor;

    public Color BorderColor
    {
        get => borderColor ?? UISettings.ActiveSettings.PanelBorderColor;
        set => borderColor = value;
    }

    private Color? filledColor;

    public Color FilledColor
    {
        get => filledColor ?? UISettings.ActiveSettings.AltColor;
        set => filledColor = value;
    }

    private Color? unfilledColor;

    public Color UnfilledColor
    {
        get => unfilledColor ?? new Color(FilledColor.R / 3, FilledColor.G / 3, FilledColor.B / 3, FilledColor.A);
        set => unfilledColor = value;
    }

    public int Maximum { get; set; }

    public bool SmoothBackwardTransition { get; set; }

    public bool SmoothForwardTransition { get; set; }

    public int SmoothTransitionRate { get; set; } = 1;

    private int value;

    public int Value
    {
        get => value;

        set => this.value = value > Maximum ? Maximum : value;
    }

    private int shownValue;

    public override void Update(GameTime gameTime)
    {
        if (shownValue < value)
        {
            shownValue = SmoothForwardTransition ? Math.Min(shownValue + SmoothTransitionRate, value) : value;
        }
        else if (shownValue > value)
        {
            shownValue = SmoothBackwardTransition ? Math.Max(0, shownValue - SmoothTransitionRate) : value;
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