using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using System.Text;
using Rampastring.Tools;

namespace Rampastring.XNAUI;

public struct SpriteBatchSettings
{
    public SpriteBatchSettings(SpriteSortMode ssm, BlendState bs, SamplerState ss, DepthStencilState dss, RasterizerState rs, Effect effect)
    {
        SpriteSortMode = ssm;
        BlendState = bs;
        SamplerState = ss;
        DepthStencilState = dss;
        RasterizerState = rs;
        Effect = effect;
    }

    public readonly SpriteSortMode SpriteSortMode;
    public readonly SamplerState SamplerState;
    public readonly BlendState BlendState;
    public readonly DepthStencilState DepthStencilState;
    public readonly RasterizerState RasterizerState;
    public readonly Effect Effect;
}

/// <summary>
/// Provides static methods for drawing.
/// </summary>
public static class Renderer
{
    private static SpriteBatch spriteBatch;

    private static List<SpriteFont> fonts;

    private static Texture2D whitePixelTexture;

    private static ContentManager contentManager;

    private static readonly LinkedList<SpriteBatchSettings> settingStack = new LinkedList<SpriteBatchSettings>();

    internal static SpriteBatchSettings CurrentSettings;

    public static SpriteBatchSettings GetCurrentSettings() => CurrentSettings;

    public static void Initialize(GraphicsDevice gd, ContentManager content, string contentPath)
    {
        spriteBatch = new SpriteBatch(gd);
        fonts = new List<SpriteFont>();

        contentManager = content;

        content.RootDirectory = SafePath.GetDirectory(contentPath).FullName;

        int i = 0;
        while (true)
        {
            string sfName = string.Format("SpriteFont{0}", i);

            if (SafePath.GetFile(contentPath, FormattableString.Invariant($"{sfName}.xnb")).Exists)
            {
                fonts.Add(content.Load<SpriteFont>(sfName));
                i++;
                continue;
            }

            break;
        }

        whitePixelTexture = AssetLoader.CreateTexture(Color.White, 1, 1);
    }

    /// <summary>
    /// Allows direct access to the list of loaded fonts.
    /// </summary>
    public static List<SpriteFont> GetFontList() => fonts;

    /// <summary>
    /// Returns a version of the given string where all characters that don't
    /// appear in the given font have been replaced with question marks.
    /// </summary>
    /// <param name="str">The string.</param>
    /// <param name="fontIndex">The index of the font.</param>
    public static string GetSafeString(string str, int fontIndex)
    {
        SpriteFont sf = fonts[fontIndex];

        var sb = new StringBuilder(str);

        for (int i = 0; i < str.Length; i++)
        {
            char c = str[i];

            if (c != '\r' && c != '\n' && !sf.Characters.Contains(c))
                sb.Replace(c, '?');
        }

        return sb.ToString();
    }

    /// <summary>
    /// Returns a that has had its width limited to a specific number.
    /// Characters that'd cross over the width have been cut.
    /// </summary>
    /// <param name="str">The string to limit.</param>
    /// <param name="fontIndex">The index of the font to use.</param>
    /// <param name="maxWidth">The maximum width of the string.</param>
    /// <returns></returns>
    public static string GetStringWithLimitedWidth(string str, int fontIndex, int maxWidth)
    {
        var sb = new StringBuilder(str);
        var spriteFont = fonts[fontIndex];

        while (spriteFont.MeasureString(sb.ToString()).X > maxWidth)
        {
            sb.Remove(sb.Length - 1, 1);
        }

        return sb.ToString();
    }

    public static TextParseReturnValue FixText(string text, int fontIndex, int width)
    {
        return TextParseReturnValue.FixText(fonts[fontIndex], width, text);
    }

    public static List<string> GetFixedTextLines(string text, int fontIndex, int width, bool splitWords = true)
    {
        return TextParseReturnValue.GetFixedTextLines(fonts[fontIndex], width, text, splitWords);
    }

    /// <summary>
    /// Pushes new settings into the renderer's internal stack and applies them.
    /// A call to <see cref="PushSettings(SpriteBatchSettings)"/> should always
    /// be followed by <see cref="PopSettings"/> once drawing with the new settings is done.
    /// </summary>
    /// <param name="settings">The sprite batch settings.</param>
    public static void PushSettings(SpriteBatchSettings settings)
    {
        EndDraw();
        PushSettingsInternal();
        CurrentSettings = settings;
        BeginDrawInternal(CurrentSettings);
    }

    /// <summary>
    /// Pops previous settings from the renderer's internal stack and applies them.
    /// </summary>
    public static void PopSettings()
    {
        EndDraw();
        PopSettingsInternal();
        BeginDrawInternal(CurrentSettings);
    }

    /// <summary>
    /// Changes current rendering settings. This can be called between 
    /// <see cref="PushSettings(SpriteBatchSettings)"/> and <see cref="PopSettings"/>
    /// when you want to draw something with new settings, but there's no reason 
    /// to save those settings.
    /// </summary>
    /// <param name="settings">The sprite batch settings.</param>
    public static void ChangeSettings(SpriteBatchSettings settings)
    {
        EndDraw();
        CurrentSettings = settings;
        BeginDrawInternal(CurrentSettings);
    }

    /// <summary>
    /// Prepares the renderer for drawing a batch of sprites.
    /// </summary>
    public static void BeginDraw()
    {
        BeginDrawInternal(CurrentSettings);
    }

    /// <summary>
    /// Draws the currently queued batch of sprites.
    /// </summary>
    public static void EndDraw()
    {
        spriteBatch.End();
    }

    public static void PushRenderTarget(RenderTarget2D renderTarget) => RenderTargetStack.PushRenderTarget(renderTarget,
        new SpriteBatchSettings(SpriteSortMode.Deferred, BlendState.AlphaBlend, null, null, null, null));

    public static void PushRenderTarget(RenderTarget2D renderTarget, SpriteBatchSettings settings) => RenderTargetStack.PushRenderTarget(renderTarget, settings);

    public static void PopRenderTarget() => RenderTargetStack.PopRenderTarget();

    //BlendState blendState = new BlendState();
    //blendState.AlphaDestinationBlend = Blend.One;
    //blendState.ColorDestinationBlend = Blend.InverseSourceAlpha;
    //blendState.AlphaSourceBlend = Blend.SourceAlpha;
    //blendState.ColorSourceBlend = Blend.SourceAlpha;

    internal static void BeginDrawInternal(SpriteBatchSettings settings) =>
        BeginDrawInternal(settings.SpriteSortMode, settings.BlendState, settings.SamplerState, settings.DepthStencilState, settings.RasterizerState, settings.Effect);

    internal static void BeginDrawInternal(SpriteSortMode ssm, BlendState bs, SamplerState ss, DepthStencilState dss, RasterizerState rs, Effect effect)
    {
#if XNA
        spriteBatch.Begin(ssm, bs, ss, DepthStencilState.Default, RasterizerState.CullNone);
#else
        spriteBatch.Begin(ssm, bs, ss, dss, rs, effect);
#endif
    }

    internal static void PushSettingsInternal()
    {
        settingStack.AddFirst(CurrentSettings);
    }

    internal static void PopSettingsInternal()
    {
        CurrentSettings = settingStack.First.Value;
        settingStack.RemoveFirst();
    }

    internal static void ClearStack()
    {
        settingStack.Clear();
    }

    #region Rendering code

    public static void DrawTexture(Texture2D texture, Rectangle rectangle, Color color)
    {
        spriteBatch.Draw(texture, rectangle, color);
    }

    public static void DrawTexture(Texture2D texture, Rectangle sourceRectangle, Rectangle destinationRectangle, Color color)
    {
        spriteBatch.Draw(texture, destinationRectangle, sourceRectangle, color);
    }

    public static void DrawTexture(Texture2D texture, Rectangle sourceRectangle, Vector2 location, float rotation, Vector2 origin, Vector2 scale, Color color, float layerDepth = 0f)
    {
        spriteBatch.Draw(texture, location, sourceRectangle, color, rotation, origin, scale, SpriteEffects.None, layerDepth);
    }

    public static void DrawTexture(Texture2D texture, Rectangle destinationRectangle, Rectangle? sourceRectangle, Color color, float rotation, Vector2 origin, SpriteEffects effects, float layerDepth)
    {
        spriteBatch.Draw(texture, destinationRectangle, sourceRectangle, color, rotation, origin, effects, layerDepth);
    }

    public static void DrawTexture(Texture2D texture, Vector2 location, float rotation, Vector2 origin, Vector2 scale, Color color, float layerDepth = 0f)
    {
        spriteBatch.Draw(texture, location, null, color, rotation, origin, scale, SpriteEffects.None, layerDepth);
    }

    /// <summary>
    /// Draws a circle's perimiter.
    /// </summary>
    /// <param name="position">The center point of the circle.</param>
    /// <param name="radius">The radius of the circle.</param>
    /// <param name="color">The color of the circle.</param>
    /// <param name="precision">Defines how smooth the circle's perimiter is. 
    /// Larger values make the circle smoother, but have a larger effect on performance.</param>
    /// <param name="thickness">The thickness of the perimiter.</param>
    public static void DrawCircle(Vector2 position, float radius, Color color, int precision = 8, int thickness = 1)
    {
        float angle = 0f;
        float increase = (float)Math.PI * 2f / precision;

        Vector2 point = position + RMath.VectorFromLengthAndAngle(radius, angle);

        for (int i = 0; i <= precision; i++)
        {
            Vector2 nextPoint = position + RMath.VectorFromLengthAndAngle(radius, angle);
            DrawLine(point, nextPoint, color, thickness);
            point = nextPoint;
            angle += increase;
        }
    }

    /// <summary>
    /// Draws a circle where the circle's perimeter is dotted with a texture.
    /// </summary>
    /// <param name="position">The center point of the circle.</param>
    /// <param name="radius">The radius of the circle.</param>
    /// <param name="texture">The texture to dot the circle's perimiter with.</param>
    /// <param name="color">The remap color of the texture.</param>
    /// <param name="precision">How many times the texture is drawn on the perimiter.</param>
    /// <param name="scale">The scale of the drawn texture compared to the size of the texture itself.</param>
    /// <param name="layerDepth">The depth of the texture.</param>
    public static void DrawCircleWithTexture(Vector2 position, float radius,
        Texture2D texture, Color color, int precision = 8, float scale = 1f, float layerDepth = 0f)
    {
        float angle = 0f;
        float increase = (float)Math.PI * 2f / precision;

        Vector2 point = position + RMath.VectorFromLengthAndAngle(radius, angle);

        for (int i = 0; i <= precision; i++)
        {
            DrawTexture(texture, point, 0f,
                new Vector2(texture.Width / 2f, texture.Height / 2f),
                new Vector2(scale, scale), color, layerDepth);
            point = position + RMath.VectorFromLengthAndAngle(radius, angle);
            angle += increase;
        }
    }

    public static void DrawString(string text, int fontIndex, Vector2 location, Color color, float scale = 1.0f, float depth = 0f)
    {
        if (fontIndex >= fonts.Count)
            throw new Exception("Invalid font index: " + fontIndex);

        spriteBatch.DrawString(fonts[fontIndex], text, location, color, 0f, Vector2.Zero, scale, SpriteEffects.None, depth);
    }

    public static void DrawStringWithShadow(string text, int fontIndex, Vector2 location, Color color, float scale = 1.0f, float shadowDistance = 1.0f, float depth = 0f)
    {
        if (fontIndex >= fonts.Count)
            throw new Exception("Invalid font index: " + fontIndex);

#if XNA
        spriteBatch.DrawString(fonts[fontIndex], text,
            new Vector2(location.X + shadowDistance, location.Y + shadowDistance),
            new Color(0, 0, 0, color.A));
#else
        spriteBatch.DrawString(fonts[fontIndex], text,
            new Vector2(location.X + shadowDistance, location.Y + shadowDistance),
            UISettings.ActiveSettings.TextShadowColor * (color.A / 255.0f),
            0f, Vector2.Zero, scale, SpriteEffects.None, depth);
#endif

        spriteBatch.DrawString(fonts[fontIndex], text, location, color, 0f, Vector2.Zero, scale, SpriteEffects.None, depth);
    }

    public static void DrawRectangle(Rectangle rect, Color color, int thickness = 1)
    {
        spriteBatch.Draw(whitePixelTexture, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
        spriteBatch.Draw(whitePixelTexture, new Rectangle(rect.X, rect.Y + thickness, thickness, rect.Height - thickness), color);
        spriteBatch.Draw(whitePixelTexture, new Rectangle(rect.X + rect.Width - thickness, rect.Y, thickness, rect.Height), color);
        spriteBatch.Draw(whitePixelTexture, new Rectangle(rect.X, rect.Y + rect.Height - thickness, rect.Width, thickness), color);
    }

    public static void FillRectangle(Rectangle rect, Color color)
    {
        spriteBatch.Draw(whitePixelTexture, rect, color);
    }

    public static Vector2 GetTextDimensions(string text, int fontIndex)
    {
        if (fontIndex >= fonts.Count)
            throw new Exception("Invalid font index: " + fontIndex);

        return fonts[fontIndex].MeasureString(text);
    }

    public static void DrawLine(Vector2 start, Vector2 end, Color color, int thickness = 1, float depth = 0f)
    {
        Vector2 line = end - start;
        if (thickness > 1)
        {
            Vector2 offset = RMath.VectorFromLengthAndAngle(thickness / 2, RMath.AngleFromVector(line) - (float)Math.PI / 2.0f);
            end += offset;
            start += offset;
        }

        spriteBatch.Draw(whitePixelTexture,
            new Rectangle((int)start.X, (int)start.Y, (int)line.Length(), thickness),
            null, color, (float)Math.Atan2(line.Y, line.X), new Vector2(0, 0), SpriteEffects.None, depth);
    }

    #endregion
}
