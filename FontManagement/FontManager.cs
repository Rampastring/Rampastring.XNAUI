using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Rampastring.Tools;

namespace Rampastring.XNAUI.FontManagement;

public static class FontManager
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

    /// <summary>
    /// Helper method to load a SpriteFont and add it to the font list.
    /// </summary>
    private static void LoadSpriteFont(ContentManager contentManager, string searchPath, string fontName)
    {
        if (SafePath.GetFile(searchPath, $"{fontName}.xnb").Exists)
        {
            var font = contentManager.Load<SpriteFont>(fontName);
            font.DefaultCharacter ??= '?';
            fonts.Add(new SpriteFontWrapper(font));
            Logger.Log($"Loaded SpriteFont: {fontName}");
        }
        else
        {
            Logger.Log($"SpriteFont file not found: {fontName}.xnb");
        }
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
                    if (!File.Exists(fullFontPath))
                        throw new FileNotFoundException($"TTF font file not found: {fullFontPath}");

                    fontSystem.AddFont(File.ReadAllBytes(fullFontPath));
                    fonts.Add(new TTFFontWrapper(fontSystem.GetFont(size)));
                    Logger.Log($"Loaded TTF font: {fontPath} (size: {size})");
                    break;

                case FontType.SpriteFont:
                    contentManager.SetRootDirectory(baseDir);
                    string sfName = Path.GetFileNameWithoutExtension(fontPath);
                    LoadSpriteFont(contentManager, searchPath, sfName);
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
