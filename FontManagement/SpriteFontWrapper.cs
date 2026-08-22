using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Linq;
using System.Text;

namespace Rampastring.XNAUI.FontManagement;

public class SpriteFontWrapper : IFont
{
    internal readonly SpriteFont _font;

    public SpriteFontWrapper(SpriteFont font)
    {
        _font = font;
    }

    public Vector2 MeasureString(string text) => _font.MeasureString(text);

    public int GetTextYPadding(int containerHeight, string text) => string.IsNullOrEmpty(text) ? (containerHeight / 2) : GetTextYPadding(containerHeight, MeasureStringY(text));
    private int GetTextYPadding(int containerHeight, int textYDimension) => (containerHeight - textYDimension) / 2 - 1; // Use `- 1` to manually adjust for vertical centering.
    public int GetSingleLineTextYPadding(int containerHeight) => GetTextYPadding(containerHeight, _font.LineSpacing);

    public int MeasureStringY(string text) => _font.LineSpacing * (text.Count(static c => c == '\n') + 1);

    public void DrawString(SpriteBatch spriteBatch, string text, Vector2 location, Color color, float scale, float depth) =>
        spriteBatch.DrawString(_font, text, location, color, 0f, Vector2.Zero, scale, SpriteEffects.None, depth);

    public void DrawString(SpriteBatch spriteBatch, StringSegment text, Vector2 location, Color color, float rotation, Vector2 origin, Vector2 scale, float depth) =>
        spriteBatch.DrawString(_font, text.ToString(), location, color, rotation, origin, scale.X, SpriteEffects.None, depth);

    public bool HasCharacter(char c) => _font.Characters.Contains(c);

    public string GetSafeString(string str) => str.Replace("\r\n", "\n").Replace('\r', '\n');
}
