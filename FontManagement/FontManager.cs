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

/// <summary>
/// Manages font loading and rendering for the UI system.
/// Supports both SpriteFont and TrueType fonts with automatic fallback.
/// </summary>
/// <remarks>
/// <para>
/// For TrueType fonts, FontManager creates a separate FontSystem for each font index.
/// Each FontSystem has a primary font (specified via Path) and fallback fonts (from [FallbackFonts]).
/// When a character is not found in the primary font, it automatically falls back to other loaded fonts.
/// </para>
/// <para>
/// The Fonts.ini file format supports:
/// <list type="bullet">
/// <item>[TextShaping] - Optional HarfBuzz text shaping configuration</item>
/// <item>[FallbackFonts] - Optional fallback font files used when primary font lacks a character</item>
/// <item>[Fonts] - Font index definitions with Size, Type, and optional Path</item>
/// </list>
/// </para>
/// </remarks>
public static class FontManager
{
    private static List<IFont> fonts;
    private static List<FontSystem> fontSystems = new();
    private static TextShapingSettings textShapingSettings = new();
    private static List<string> fallbackFontPaths = new();

    public static void Initialize()
    {
        fonts = [];
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
    /// Creates a new FontSystem with current text shaping settings.
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

    /// <summary>
    /// Loads fonts from the first Fonts.ini found in asset search paths.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Loading happens in two phases:
    /// </para>
    /// <para>
    /// Phase 1: Load configuration from the first Fonts.ini:
    /// - [TextShaping] settings
    /// - [FallbackFonts] paths (used by all TrueType font indexes)
    /// - [Fonts] definitions (type, path, size)
    /// </para>
    /// <para>
    /// Phase 2: Create font indexes:
    /// - For TrueType fonts: Create a FontSystem with primary font first, then fallback fonts
    /// - For SpriteFonts: Load the .xnb file
    /// </para>
    /// </remarks>
    public static void LoadFonts(ContentManager contentManager)
    {
        fonts ??= [];
        fonts.Clear();
        fontSystems.Clear();
        fallbackFontPaths.Clear();

        // Reset text shaping settings
        textShapingSettings = new TextShapingSettings();

        string originalContentRoot = contentManager.RootDirectory;
        bool fontsIniFound = false;

        foreach (string searchPath in AssetLoader.AssetSearchPaths)
        {
            string baseDir = SafePath.GetDirectory(searchPath).FullName;
            string iniPath = Path.Combine(baseDir, "Fonts.ini");

            if (File.Exists(iniPath))
            {
                Logger.Log($"FontManager: Loading fonts from {iniPath}");
                LoadFontsFromIni(iniPath, contentManager, searchPath, baseDir);
                fontsIniFound = true;
                break; // Stop after first Fonts.ini found
            }
        }

        // Fall back to legacy SpriteFont loading if no Fonts.ini found
        if (!fontsIniFound)
        {
            Logger.Log("FontManager: No Fonts.ini found, attempting legacy SpriteFont loading");
            foreach (string searchPath in AssetLoader.AssetSearchPaths)
            {
                string baseDir = SafePath.GetDirectory(searchPath).FullName;
                int fontsBeforeLoad = fonts.Count;
                LoadLegacySpriteFonts(contentManager, searchPath, baseDir);

                if (fonts.Count > fontsBeforeLoad)
                    break; // Stop after first path with legacy fonts
            }
        }

        contentManager.SetRootDirectory(originalContentRoot);

        Logger.Log($"FontManager: Loaded {fonts.Count} font indexes with {fontSystems.Count} FontSystems");
    }

    /// <summary>
    /// Loads fonts from a specific Fonts.ini file.
    /// </summary>
    private static void LoadFontsFromIni(string iniPath, ContentManager contentManager, string searchPath, string baseDir)
    {
        var iniFile = new IniFile(iniPath);

        // Load text shaping settings
        if (iniFile.SectionExists("TextShaping"))
        {
            LoadTextShapingSettings(iniFile);
        }

        LoadFallbackFonts(iniFile, searchPath);

        CreateFontIndexesFromIni(iniFile, contentManager, searchPath, baseDir);
    }

    /// <summary>
    /// Loads fallback font paths from the [FallbackFonts] section.
    /// These fonts are added to all TrueType font indexes after their primary font.
    /// </summary>
    private static void LoadFallbackFonts(IniFile iniFile, string searchPath)
    {
        if (!iniFile.SectionExists("FallbackFonts"))
        {
            Logger.Log("FontManager: No [FallbackFonts] section found");
            return;
        }

        int fallbackCount = iniFile.GetIntValue("FallbackFonts", "Count", 0);
        Logger.Log($"FontManager: Loading {fallbackCount} fallback fonts");

        for (int i = 0; i < fallbackCount; i++)
        {
            string fallbackPath = iniFile.GetStringValue("FallbackFonts", $"Fallback{i}", "");
            if (string.IsNullOrEmpty(fallbackPath))
                continue;

            string fullPath = SafePath.GetFile(searchPath, fallbackPath).FullName;
            if (File.Exists(fullPath))
            {
                fallbackFontPaths.Add(fullPath);
                Logger.Log($"FontManager: Added fallback font: {fallbackPath}");
            }
            else
            {
                Logger.Log($"FontManager: Fallback font not found: {fullPath}");
            }
        }
    }

    private static void LoadTextShapingSettings(IniFile iniFile)
    {
        textShapingSettings.Enabled = iniFile.GetBooleanValue("TextShaping", "Enabled", false);
        textShapingSettings.EnableBiDi = iniFile.GetBooleanValue("TextShaping", "EnableBiDi", true);
        textShapingSettings.CacheSize = iniFile.GetIntValue("TextShaping", "CacheSize", 100);

        if (textShapingSettings.CacheSize < 1)
            textShapingSettings.CacheSize = 100;

        Logger.Log($"FontManager: Text shaping settings: Enabled={textShapingSettings.Enabled}, BiDi={textShapingSettings.EnableBiDi}, CacheSize={textShapingSettings.CacheSize}");
    }

    /// <summary>
    /// Creates FontIndex entries from a Fonts.ini file.
    /// For each TrueType font, creates a separate FontSystem with primary font first, then fallback fonts.
    /// </summary>
    private static void CreateFontIndexesFromIni(IniFile iniFile, ContentManager contentManager, string searchPath, string baseDir)
    {
        int fontCount = iniFile.GetIntValue("Fonts", "Count", 0);

        Logger.Log($"FontManager: Creating {fontCount} font indexes");

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
                    CreateTrueTypeFontIndex(i, fontPath, size, searchPath);
                    break;

                case FontType.SpriteFont:
                    contentManager.SetRootDirectory(baseDir);
                    string sfName = Path.GetFileNameWithoutExtension(fontPath);
                    LoadSpriteFont(contentManager, searchPath, sfName);
                    break;
            }
        }
    }

    /// <summary>
    /// Creates a TrueType font index with its own FontSystem.
    /// The FontSystem contains the primary font (if specified) followed by fallback fonts.
    /// </summary>
    private static void CreateTrueTypeFontIndex(int fontIndex, string primaryFontPath, int size, string searchPath)
    {
        FontSystem fontSystem = CreateFontSystem();
        fontSystems.Add(fontSystem);

        bool hasPrimaryFont = false;

        // Add primary font first
        if (!string.IsNullOrEmpty(primaryFontPath))
        {
            string fullPath = SafePath.GetFile(searchPath, primaryFontPath).FullName;
            if (File.Exists(fullPath))
            {
                try
                {
                    fontSystem.AddFont(File.ReadAllBytes(fullPath));
                    Logger.Log($"FontManager: Font{fontIndex} - Added primary font: {primaryFontPath}");
                    hasPrimaryFont = true;
                }
                catch (Exception ex)
                {
                    Logger.Log($"FontManager: Font{fontIndex} - Failed to load primary font {primaryFontPath}: {ex.Message}");
                }
            }
            else
            {
                Logger.Log($"FontManager: Font{fontIndex} - Primary font not found: {fullPath}");
            }
        }

        // Add fallback fonts
        int fallbacksAdded = 0;
        foreach (string fallbackPath in fallbackFontPaths)
        {
            try
            {
                fontSystem.AddFont(File.ReadAllBytes(fallbackPath));
                fallbacksAdded++;
            }
            catch (Exception ex)
            {
                Logger.Log($"FontManager: Font{fontIndex} - Failed to load fallback font {fallbackPath}: {ex.Message}");
            }
        }

        if (fallbacksAdded > 0)
        {
            Logger.Log($"FontManager: Font{fontIndex} - Added {fallbacksAdded} fallback fonts");
        }

        // Create the font wrapper
        if (hasPrimaryFont || fallbacksAdded > 0)
        {
            fonts.Add(new TTFFontWrapper(fontSystem.GetFont(size)));
            string primaryInfo = hasPrimaryFont ? $"primary: {Path.GetFileName(primaryFontPath)}" : "no primary";
            Logger.Log($"FontManager: Created FontIndex {fonts.Count - 1}: TrueType size {size} ({primaryInfo}, {fallbacksAdded} fallbacks)");
        }
        else
        {
            Logger.Log($"FontManager: Font{fontIndex} - No fonts loaded (no primary and no fallbacks), skipping");
        }
    }

    /// <summary>
    /// Loads a SpriteFont and adds it to the font list.
    /// </summary>
    private static void LoadSpriteFont(ContentManager contentManager, string searchPath, string fontName)
    {
        if (SafePath.GetFile(searchPath, $"{fontName}.xnb").Exists)
        {
            var font = contentManager.Load<SpriteFont>(fontName);
            font.DefaultCharacter ??= '?';
            fonts.Add(new SpriteFontWrapper(font));
            Logger.Log($"FontManager: Created FontIndex {fonts.Count - 1}: SpriteFont {fontName}");
        }
        else
        {
            Logger.Log($"FontManager: SpriteFont file not found: {fontName}.xnb");
        }
    }

    /// <summary>
    /// Loads legacy SpriteFonts (SpriteFont0, SpriteFont1, etc.) from a search path.
    /// </summary>
    private static void LoadLegacySpriteFonts(ContentManager contentManager, string searchPath, string baseDir)
    {
        contentManager.SetRootDirectory(baseDir);

        int startIndex = fonts.Count;
        while (true)
        {
            string sfName = string.Format(CultureInfo.InvariantCulture, "SpriteFont{0}", fonts.Count - startIndex);
            if (!SafePath.GetFile(searchPath, FormattableString.Invariant($"{sfName}.xnb")).Exists)
                break;

            var font = contentManager.Load<SpriteFont>(sfName);
            font.DefaultCharacter ??= '?';
            fonts.Add(new SpriteFontWrapper(font));
            Logger.Log($"FontManager: Created FontIndex {fonts.Count - 1}: Legacy SpriteFont {sfName}");
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
