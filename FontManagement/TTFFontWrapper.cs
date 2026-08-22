using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Linq;

namespace Rampastring.XNAUI.FontManagement;

public class TTFFontWrapper : IFont
{
    private const string CapHeightReferenceGlyph = "H";
    internal readonly SpriteFontBase _font;
    private readonly int _verticalCenteringValue;

    public TTFFontWrapper(SpriteFontBase font)
    {
        _font = font;
        var bounds = _font.TextBounds(CapHeightReferenceGlyph, Vector2.Zero);
        _verticalCenteringValue = (int)Math.Ceiling(bounds.Y + bounds.Y2);
    }

    public Vector2 MeasureString(string text)
    {
        var bounds = _font.MeasureString(text);
        return new Vector2(bounds.X, bounds.Y);
    }

    /// <summary>
    /// Returns the value <c>V</c> to plug into <c>(controlHeight - V) / 2</c> for
    /// vertical centering. NOT a geometric height: this is <c>top + bottom</c> of the
    /// cap glyph 'H' from the draw origin (i.e. <c>minY + maxY</c> from FontStashSharp's
    /// <c>TextBounds</c>), chosen so the cap-glyph midpoint lands at <c>controlHeight / 2</c>
    /// independent of descenders. The geometric glyph height would be <c>maxY - minY</c>.
    /// </summary>
    public int GetVerticalCenteringValue() => _verticalCenteringValue;

    public int GetTextYPadding(int containerHeight, string text) => string.IsNullOrEmpty(text) ? (containerHeight / 2) : (containerHeight - GetVerticalCenteringValue() - _font.LineHeight * text.Count(c => c == '\n')) / 2;

    public int GetSingleLineTextYPadding(int containerHeight) => (containerHeight - GetVerticalCenteringValue()) / 2;


    public void DrawString(SpriteBatch spriteBatch, string text, Vector2 location, Color color, float scale, float depth)
    {
        var vectorScale = new Vector2(scale, scale);
        var segment = new StringSegment(text);
        spriteBatch.DrawString(_font, segment, location, color, 0f, Vector2.Zero, vectorScale, depth);
    }

    public void DrawString(SpriteBatch spriteBatch, StringSegment text, Vector2 location, Color color, float rotation, Vector2 origin, Vector2 scale, float depth)
    {
        spriteBatch.DrawString(_font, text, location, color, rotation, origin, scale, depth);
    }

    /// <summary>
    /// For TTF fonts, this always returns true because FontStashSharp can dynamically
    /// generate glyphs for any character. If a glyph is not available in the font file,
    /// a replacement glyph (like � or ?) will be rendered instead.
    /// </summary>
    public bool HasCharacter(char c) => true;

    /// <summary>
    /// Returns the string as-is for TTF fonts.
    /// TTF fonts handle all characters through dynamic glyph generation and fallback.
    /// </summary>
    public string GetSafeString(string str) => str;
}
