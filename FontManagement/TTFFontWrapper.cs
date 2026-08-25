using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Linq;
using System.Text;

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
        var measuredSize = _font.MeasureString(text);

        // FontStashSharp reports the bottom of the lowest glyph as the Y dimension,
        // making equally formatted strings have different heights depending on their
        // characters. Use the font's line height to match SpriteFont.MeasureString and
        // keep text parts on a shared line aligned to the same draw origin.
        int height = text.Length == 0 ? 0 : _font.LineHeight * (text.Count(static c => c == '\n') + 1);
        return new Vector2(measuredSize.X, height);
    }

    /// <summary>
    /// Returns the value <c>V</c> to plug into <c>(controlHeight - V) / 2</c> for
    /// vertical centering. NOT a geometric height: this is <c>top + bottom</c> of the
    /// cap glyph 'H' from the draw origin (i.e. <c>minY + maxY</c> from FontStashSharp's
    /// <c>TextBounds</c>), chosen so the cap-glyph midpoint lands at <c>controlHeight / 2</c>
    /// independent of descenders. The geometric glyph height would be <c>maxY - minY</c>.
    /// </summary>
    public int GetVerticalCenteringValue() => _verticalCenteringValue;

    public int GetTextYPadding(int containerHeight, string text) => string.IsNullOrEmpty(text) ? (containerHeight / 2) : (containerHeight - GetVerticalCenteringValue() - _font.LineHeight * text.Count(static c => c == '\n')) / 2;

    public int GetSingleLineTextYPadding(int containerHeight) => (containerHeight - GetVerticalCenteringValue()) / 2;


    public void DrawString(SpriteBatch spriteBatch, string text, Vector2 location, Color color, float scale, float depth)
    {
        var vectorScale = new Vector2(scale, scale);
        text = GetSafeString(text);
        spriteBatch.DrawString(_font, text, location, color, 0f, Vector2.Zero, vectorScale, depth);
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
    /// Returns a sanitized string safe for rendering. It replaces unpaired surrogates
    /// with U+FFFD so FontStashSharp's UTF-16 -> UTF-32 conversion does not throw.
    /// </summary>
    public string GetSafeString(string str)
    {
        // Some fonts render `\r` as a visible character, e.g., Unifont. Therefore, we normalize newlines.
        str = str.Replace("\r\n", "\n").Replace('\r', '\n');

        // We also sanitize invalid UTF-16 surrogate pairs so FontStashSharp's UTF-16 -> UTF-32 conversion cannot throw.
        return SanitizeStringForRendering(str);
    }

    private static string SanitizeStringForRendering(string str)
    {
        if (str is null)
            throw new ArgumentNullException(nameof(str));

        if (str.Length == 0)
            return str;

        int firstBad = -1;
        for (int i = 0; i < str.Length; i++)
        {
            char c = str[i];
            if (char.IsHighSurrogate(c))
            {
                if (i + 1 < str.Length && char.IsLowSurrogate(str[i + 1]))
                {
                    i++;
                    continue;
                }
                firstBad = i;
                break;
            }
            if (char.IsLowSurrogate(c))
            {
                firstBad = i;
                break;
            }
        }

        if (firstBad < 0)
            return str;

#if DEBUG
        System.Diagnostics.Debug.WriteLine($"There is still an unpaired surrogate at index {firstBad} in string \"{str}\". Have you called GetSafeString before rendering?");
#endif

        var sb = new StringBuilder(str.Length);
        if (firstBad > 0)
            sb.Append(str, 0, firstBad);

        for (int i = firstBad; i < str.Length; i++)
        {
            char c = str[i];

            if (char.IsHighSurrogate(c))
            {
                if (i + 1 < str.Length && char.IsLowSurrogate(str[i + 1]))
                {
                    sb.Append(c);
                    sb.Append(str[i + 1]);
                    i++;
                }
                else
                {
                    sb.Append('\uFFFD');
                }
            }
            else if (char.IsLowSurrogate(c))
            {
                sb.Append('\uFFFD');
            }
            else
            {
                sb.Append(c);
            }
        }

        return sb.ToString();
    }
}
