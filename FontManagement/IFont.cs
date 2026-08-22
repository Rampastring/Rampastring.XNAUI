using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Rampastring.XNAUI.FontManagement;

public interface IFont
{
    Vector2 MeasureString(string text);

    /// <summary>
    /// Calculates the Y offset for the given text so that it 
    /// is centered on a container of a specific height.
    /// 
    /// Handles multi-line strings. 
    /// For single-line strings, call <see cref="GetSingleLineTextYPadding(int)"/> instead - it's more performant.
    /// </summary>
    int GetTextYPadding(int containerHeight, string text);

    /// <summary>
    /// Calculates the Y offset for a single line of text so that it is centered on a container of a specific height.
    /// </summary>
    int GetSingleLineTextYPadding(int containerHeight);

    void DrawString(SpriteBatch spriteBatch, string text, Vector2 location, Color color, float scale, float depth);
    void DrawString(SpriteBatch spriteBatch, StringSegment text, Vector2 location, Color color, float rotation, Vector2 origin, Vector2 scale, float depth);
    bool HasCharacter(char c);
    string GetSafeString(string str);
}
