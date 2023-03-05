namespace Rampastring.XNAUI.XNAControls;

using Microsoft.Xna.Framework;
using Rampastring.Tools;

/// <summary>
/// A panel with text.
/// </summary>
public class XNATextBlock : XNAPanel
{
    public XNATextBlock(WindowManager windowManager)
        : base(windowManager)
    {
    }

    public override string Text
    {
        get => base.Text;

        set => base.Text = Renderer.FixText(value, FontIndex, Width - (TextXMargin * 2)).Text;
    }

    private Color? _textColor;

    public Color TextColor
    {
        get => _textColor ?? UISettings.ActiveSettings.TextColor;

        set => _textColor = value;
    }

    public int FontIndex { get; set; }

    public int TextXMargin { get; set; } = 3;

    public int TextYPosition { get; set; } = 3;

    protected override void ParseControlINIAttribute(IniFile iniFile, string key, string value)
    {
        switch (key)
        {
            case "TextColor":
                TextColor = AssetLoader.GetColorFromString(value);
                return;
        }

        base.ParseControlINIAttribute(iniFile, key, value);
    }

    public override void Draw(GameTime gameTime)
    {
        DrawPanel();

        if (!string.IsNullOrEmpty(Text))
        {
            DrawStringWithShadow(
                Text, FontIndex, new(TextXMargin, TextYPosition), TextColor);
        }

        if (DrawBorders)
            DrawPanelBorders();

        DrawChildren(gameTime);
    }
}