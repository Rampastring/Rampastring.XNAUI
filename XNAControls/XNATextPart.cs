namespace Rampastring.XNAUI.XNAControls;

using Microsoft.Xna.Framework;

/// <summary>
/// A part of a potentially long line of text.
/// </summary>
public struct XNATextPart
{
    public XNATextPart(string text, int fontIndex, float scale, Color? color, bool underlined)
    {
        _text = text;
        _fontIndex = fontIndex;
        _scale = scale;
        _color = color;
        Underlined = underlined;
        Size = Point.Zero;
        UpdateSize();
    }

    public XNATextPart(string text)
        : this(text, 0, 1.0f, null, false)
    {
    }

    public XNATextPart(string text, int fontIndex, Color? color)
        : this(text, fontIndex, 1.0f, color, false)
    {
    }

    private string _text;

    public string Text
    {
        get => _text;
        set
        {
            _text = value;
            UpdateSize();
        }
    }

    private int _fontIndex;

    public int FontIndex
    {
        get => _fontIndex;
        set
        {
            _fontIndex = value;
            UpdateSize();
        }
    }

    private float _scale;

    public float Scale
    {
        get => _scale;
        set
        {
            _scale = value;
            UpdateSize();
        }
    }

    private Color? _color;

    public Color Color
    {
        get => _color ?? UISettings.ActiveSettings.TextColor;
        set => _color = value;
    }

    public bool Underlined { get; set; }

    public Point Size { get; private set; }

    private void UpdateSize()
    {
        Vector2 size = Renderer.GetTextDimensions(_text, FontIndex) * Scale;
        Size = new((int)size.X, (int)size.Y);
    }

    public int Width => (int)(Size.X * Scale);

    public int Height => (int)(Size.Y * Scale);

    public void Draw(Point point)
    {
        Renderer.DrawStringWithShadow(Text, FontIndex, new(point.X, point.Y), Color, Scale);
        if (Underlined)
        {
            Renderer.DrawRectangle(new(point.X, point.Y, (int)(Size.X * Scale), 1), Color);
        }
    }
}