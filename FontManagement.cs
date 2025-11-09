using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using System.Text;
using Rampastring.Tools;
using System.Globalization;
using FontStashSharp;
using System.IO;

namespace Rampastring.XNAUI;

public enum FontType
{
    SpriteFont,
    TrueType
}

/// <summary>
/// Configuration for HarfBuzz text shaping.
/// Text shaping is required for complex scripts (Arabic, Hebrew, Hindi, etc.)
/// and proper rendering of emoji sequences and ligatures.
/// </summary>
public class TextShapingSettings
{
    /// <summary>
    /// Enable HarfBuzz text shaping for complex scripts.
    /// When enabled, text will be properly shaped for languages that require it.
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// Enable bidirectional text support for mixed LTR/RTL text.
    /// Only applies when Enabled is true.
    /// </summary>
    public bool EnableBiDi { get; set; } = true;

    /// <summary>
    /// Size of the shaped text cache.
    /// Higher values use more memory but reduce shaping overhead for repeated text.
    /// Default: 100
    /// </summary>
    public int CacheSize { get; set; } = 100;
}

public interface IFont
{
    Vector2 MeasureString(string text);
    void DrawString(SpriteBatch spriteBatch, string text, Vector2 location, Color color, float scale, float depth);
    void DrawString(SpriteBatch spriteBatch, StringSegment text, Vector2 location, Color color, float rotation, Vector2 origin, Vector2 scale, float depth);
    bool HasCharacter(char c);
    string GetSafeString(string str);
}

public class SpriteFontWrapper : IFont
{
    internal readonly SpriteFont _font;

    public SpriteFontWrapper(SpriteFont font)
    {
        _font = font;
    }

    public Vector2 MeasureString(string text) => _font.MeasureString(text);

    public void DrawString(SpriteBatch spriteBatch, string text, Vector2 location, Color color, float scale, float depth) =>
        spriteBatch.DrawString(_font, text, location, color, 0f, Vector2.Zero, scale, SpriteEffects.None, depth);

    public void DrawString(SpriteBatch spriteBatch, StringSegment text, Vector2 location, Color color, float rotation, Vector2 origin, Vector2 scale, float depth) =>
        spriteBatch.DrawString(_font, text.ToString(), location, color, rotation, origin, scale.X, SpriteEffects.None, depth);

    public bool HasCharacter(char c) => _font.Characters.Contains(c);

    public string GetSafeString(string str)
    {
        var sb = new StringBuilder(str);
        for (int i = 0; i < str.Length; i++)
        {
            char c = str[i];
            if (c != '\r' && c != '\n' && !HasCharacter(c))
            {
                sb.Replace(c, '?');
            }
        }
        return sb.ToString();
    }
}

public class TTFFontWrapper : IFont
{
    internal readonly SpriteFontBase _font;

    public TTFFontWrapper(SpriteFontBase font)
    {
        _font = font;
    }

    public Vector2 MeasureString(string text)
    {
        var bounds = _font.MeasureString(text);
        return new Vector2(bounds.X, bounds.Y);
    }

    public void DrawString(SpriteBatch spriteBatch, string text, Vector2 location, Color color, float scale, float depth)
    {
        var vectorScale = new Vector2(scale, scale);
        var segment = new StringSegment(text);
        spriteBatch.DrawString(_font, segment, location, color, 0f, Vector2.Zero, vectorScale, depth);
    }

    public void DrawString(SpriteBatch spriteBatch, StringSegment text, Vector2 location, Color color, float rotation, Vector2 origin, Vector2 scale, float depth) =>
        spriteBatch.DrawString(_font, text, location, color, rotation, origin, scale, depth);

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

public static class FontManagement
{
    private static List<IFont> fonts;
    private static FontSystem fontSystem;
    private static TextShapingSettings textShapingSettings = new TextShapingSettings();

    public static void Initialize()
    {
        fonts = new List<IFont>();
    }

    /// <summary>
    /// Gets the current text shaping settings.
    /// </summary>
    public static TextShapingSettings GetTextShapingSettings() => textShapingSettings;

    /// <summary>
    /// Checks if text shaping is currently enabled.
    /// </summary>
    public static bool IsTextShapingEnabled() => textShapingSettings.Enabled;

    /// <summary>
    /// Creates a new FontSystem.
    /// </summary>
    private static FontSystem CreateFontSystem()
    {
        var settings = new FontSystemSettings();

        if (textShapingSettings.Enabled)
        {
            var shaper = new HarfBuzzTextShaper
            {
                EnableBiDi = textShapingSettings.EnableBiDi
            };
            settings.TextShaper = shaper;
            settings.ShapedTextCacheSize = textShapingSettings.CacheSize;
        }

        return new FontSystem(settings);
    }

    public static void LoadFonts(ContentManager contentManager)
    {
        if (fonts == null)
            fonts = new List<IFont>();
        else
            fonts.Clear();

        fontSystem = CreateFontSystem();
        string originalContentRoot = contentManager.RootDirectory;

        foreach (string searchPath in AssetLoader.AssetSearchPaths)
        {
            string baseDir = SafePath.GetDirectory(searchPath).FullName;
            string iniPath = Path.Combine(baseDir, "Fonts.ini");

            if (File.Exists(iniPath))
                LoadFontsFromIni(iniPath, contentManager, searchPath, baseDir);
            else
                LoadLegacySpriteFonts(contentManager, searchPath, baseDir);
        }

        contentManager.SetRootDirectory(originalContentRoot);
    }

    private static void LoadTextShapingSettings(IniFile iniFile)
    {
        textShapingSettings.Enabled = iniFile.GetBooleanValue("TextShaping", "Enabled", false);
        textShapingSettings.EnableBiDi = iniFile.GetBooleanValue("TextShaping", "EnableBiDi", true);
        textShapingSettings.CacheSize = iniFile.GetIntValue("TextShaping", "CacheSize", 100);

        if (textShapingSettings.CacheSize < 1)
            textShapingSettings.CacheSize = 100;

        Logger.Log($"Text shaping settings: Enabled={textShapingSettings.Enabled}, BiDi={textShapingSettings.EnableBiDi}, CacheSize={textShapingSettings.CacheSize}");
    }

    private static void LoadFontsFromIni(string iniPath, ContentManager contentManager, string searchPath, string baseDir)
    {
        var iniFile = new IniFile(iniPath);

        LoadTextShapingSettings(iniFile);
        fontSystem = CreateFontSystem();

        int fontCount = iniFile.GetIntValue("Fonts", "Count", 0);

        for (int i = 0; i < fontCount; i++)
        {
            string section = $"Font{i}";
            string fontPath = iniFile.GetStringValue(section, "Path", "");
            int size = iniFile.GetIntValue(section, "Size", 16);
            string fontTypeStr = iniFile.GetStringValue(section, "Type", nameof(FontType.SpriteFont));

            if (!Enum.TryParse<FontType>(fontTypeStr, true, out var fontType))
                fontType = FontType.SpriteFont;

            switch (fontType)
            {
                case FontType.TrueType:
                    string fullFontPath = SafePath.GetFile(searchPath, fontPath).FullName;
                    if (File.Exists(fullFontPath))
                    {
                        fontSystem.AddFont(File.ReadAllBytes(fullFontPath));
                        fonts.Add(new TTFFontWrapper(fontSystem.GetFont(size)));
                        Logger.Log($"Loaded TTF font: {fontPath} (size: {size})");
                    }
                    else
                    {
                        Logger.Log($"TTF font file not found: {fullFontPath}");
                    }
                    break;

                case FontType.SpriteFont:
                    contentManager.SetRootDirectory(baseDir);
                    string sfName = Path.GetFileNameWithoutExtension(fontPath);
                    if (SafePath.GetFile(searchPath, $"{sfName}.xnb").Exists)
                    {
                        var font = contentManager.Load<SpriteFont>(sfName);
                        font.DefaultCharacter ??= '?';
                        fonts.Add(new SpriteFontWrapper(font));
                        Logger.Log($"Loaded SpriteFont: {sfName}");
                    }
                    else
                    {
                        Logger.Log($"SpriteFont file not found: {sfName}.xnb");
                    }
                    break;
            }
        }
    }

    private static void LoadLegacySpriteFonts(ContentManager contentManager, string searchPath, string baseDir)
    {
        contentManager.SetRootDirectory(baseDir);

        while (true)
        {
            string sfName = string.Format(CultureInfo.InvariantCulture, "SpriteFont{0}", fonts.Count);
            if (!SafePath.GetFile(searchPath, FormattableString.Invariant($"{sfName}.xnb")).Exists)
                break;

            var font = contentManager.Load<SpriteFont>(sfName);
            font.DefaultCharacter ??= '?';
            fonts.Add(new SpriteFontWrapper(font));
            Logger.Log($"Loaded legacy SpriteFont: {sfName}");
        }
    }

    public static List<IFont> GetFontList() => fonts;

    public static string GetSafeString(string str, int fontIndex)
    {
        if (fontIndex < 0 || fontIndex >= fonts.Count)
            throw new IndexOutOfRangeException("Invalid font index.");

        return fonts[fontIndex].GetSafeString(str);
    }

    public static string GetStringWithLimitedWidth(string str, int fontIndex, int maxWidth)
    {
        if (fontIndex < 0 || fontIndex >= fonts.Count)
            throw new IndexOutOfRangeException("Invalid font index.");

        var font = fonts[fontIndex];
        var sb = new StringBuilder(str);

        while (font.MeasureString(sb.ToString()).X > maxWidth && sb.Length > 0)
        {
            sb.Remove(sb.Length - 1, 1);
        }

        return sb.ToString();
    }

    public static TextParseReturnValue FixText(string text, int fontIndex, int width)
    {
        if (fontIndex < 0 || fontIndex >= fonts.Count)
            throw new IndexOutOfRangeException("Invalid font index.");

        IFont font = fonts[fontIndex];
        return TextParseReturnValue.FixText(font, width, text);
    }

    public static List<string> GetFixedTextLines(string text, int fontIndex, int width, bool splitWords = true, bool keepBlankLines = false)
    {
        if (fontIndex < 0 || fontIndex >= fonts.Count)
            throw new IndexOutOfRangeException("Invalid font index.");

        IFont font = fonts[fontIndex];
        return TextParseReturnValue.GetFixedTextLines(font, width, text, splitWords, keepBlankLines);
    }

    public static Vector2 GetTextDimensions(string text, int fontIndex)
    {
        if (fontIndex < 0 || fontIndex >= fonts.Count)
            throw new IndexOutOfRangeException("Invalid font index: " + fontIndex);

        return fonts[fontIndex].MeasureString(text);
    }

    public static void DrawString(SpriteBatch spriteBatch, string text, int fontIndex, Vector2 location, Color color, float scale = 1.0f, float depth = 0f)
    {
        if (fontIndex < 0 || fontIndex >= fonts.Count)
            throw new IndexOutOfRangeException("Invalid font index: " + fontIndex);

        fonts[fontIndex].DrawString(spriteBatch, text, location, color, scale, depth);
    }

    public static void DrawStringWithShadow(SpriteBatch spriteBatch, string text, int fontIndex, Vector2 location, Color color, float scale = 1.0f, float shadowDistance = 1.0f, float depth = 0f)
    {
        if (fontIndex < 0 || fontIndex >= fonts.Count)
            throw new IndexOutOfRangeException("Invalid font index: " + fontIndex);

        Color shadowColor;
#if XNA
        shadowColor = new Color(0, 0, 0, color.A);
#else
        shadowColor = UISettings.ActiveSettings.TextShadowColor * (color.A / 255.0f);
#endif

        fonts[fontIndex].DrawString(spriteBatch, text, new Vector2(location.X + shadowDistance, location.Y + shadowDistance), shadowColor, scale, depth);
        fonts[fontIndex].DrawString(spriteBatch, text, location, color, scale, depth);
    }
}
