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
/// For TrueType fonts, FontManager uses a single shared FontSystem that enables
/// automatic character fallback across multiple font files. When a character is not
/// found in the primary font, it automatically falls back to other loaded fonts.
/// </para>
/// <para>
/// The Fonts.ini file format supports:
/// <list type="bullet">
/// <item>[TextShaping] - Optional HarfBuzz text shaping</item>
/// <item>[FontSources] - Optional explicit fallback font files</item>
/// <item>[Fonts] - Font index definitions with Size and Type</item>
/// </list>
/// </para>
/// </remarks>
public static class FontManager
{
    private static List<IFont> fonts;
    private static FontSystem fontSystem;
    private static TextShapingSettings textShapingSettings = new();
    private static HashSet<string> loadedFontSources = new(StringComparer.OrdinalIgnoreCase);
    private static bool fontIndexesDefined;

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
    /// Loads fonts from all asset search paths.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Loading happens in two phases:
    /// </para>
    /// <para>
    /// Phase 1: Collect all TrueType font sources from all search paths.
    /// Sources are added in search path order, so translation fonts have
    /// higher fallback priority than base fonts.
    /// </para>
    /// <para>
    /// Phase 2: Create FontIndex entries from the first Fonts.ini found,
    /// or fall back to legacy SpriteFont loading if no Fonts.ini exists.
    /// </para>
    /// </remarks>
    public static void LoadFonts(ContentManager contentManager)
    {
        fonts ??= [];
        fonts.Clear();
        loadedFontSources.Clear();
        fontIndexesDefined = false;

        // Reset text shaping settings
        textShapingSettings = new TextShapingSettings();

        // Phase 1: Collect all font sources and text shaping settings from all Fonts.ini files
        CollectFontSources();

        // Create the shared FontSystem with collected settings
        fontSystem = CreateFontSystem();

        // Add all collected font sources to the FontSystem
        AddCollectedFontSourcesToFontSystem();

        string originalContentRoot = contentManager.RootDirectory;

        // Phase 2: Create FontIndex entries from first Fonts.ini, or use legacy loading
        foreach (string searchPath in AssetLoader.AssetSearchPaths)
        {
            string baseDir = SafePath.GetDirectory(searchPath).FullName;
            string iniPath = Path.Combine(baseDir, "Fonts.ini");

            if (File.Exists(iniPath))
            {
                if (!fontIndexesDefined)
                {
                    CreateFontIndexesFromIni(iniPath, contentManager, searchPath, baseDir);
                    fontIndexesDefined = true;
                }
            }
            else if (!fontIndexesDefined)
            {
                // Try legacy SpriteFont loading only if no Fonts.ini has been processed yet
                int fontsBeforeLoad = fonts.Count;
                LoadLegacySpriteFonts(contentManager, searchPath, baseDir);

                if (fonts.Count > fontsBeforeLoad)
                    fontIndexesDefined = true;
            }
        }

        contentManager.SetRootDirectory(originalContentRoot);

        Logger.Log($"FontManager: Loaded {fonts.Count} font indexes, {loadedFontSources.Count} TTF sources");
    }

    /// <summary>
    /// Collects all font sources and text shaping settings from Fonts.ini files across all search paths.
    /// </summary>
    private static void CollectFontSources()
    {
        bool textShapingLoaded = false;

        foreach (string searchPath in AssetLoader.AssetSearchPaths)
        {
            string baseDir = SafePath.GetDirectory(searchPath).FullName;
            string iniPath = Path.Combine(baseDir, "Fonts.ini");

            if (!File.Exists(iniPath))
                continue;

            var iniFile = new IniFile(iniPath);

            // Load text shaping settings from first Fonts.ini that has them
            if (!textShapingLoaded && iniFile.SectionExists("TextShaping"))
            {
                LoadTextShapingSettings(iniFile);
                textShapingLoaded = true;
            }

            CollectExplicitFontSources(iniFile, searchPath);

            CollectFontSourcesFromFontEntries(iniFile, searchPath);
        }
    }

    /// <summary>
    /// Collects font sources from the [FontSources] section.
    /// </summary>
    private static void CollectExplicitFontSources(IniFile iniFile, string searchPath)
    {
        if (!iniFile.SectionExists("FontSources"))
            return;

        int sourceCount = iniFile.GetIntValue("FontSources", "Count", 0);

        for (int i = 0; i < sourceCount; i++)
        {
            string sourcePath = iniFile.GetStringValue("FontSources", $"Source{i}", "");
            if (string.IsNullOrEmpty(sourcePath))
                continue;

            string fullPath = SafePath.GetFile(searchPath, sourcePath).FullName;
            if (File.Exists(fullPath) && !loadedFontSources.Contains(fullPath))
            {
                loadedFontSources.Add(fullPath);
                Logger.Log($"FontManager: Queued font source: {sourcePath}");
            }
        }
    }

    /// <summary>
    /// Collects font sources from [Font*] entries that specify TrueType fonts.
    /// </summary>
    private static void CollectFontSourcesFromFontEntries(IniFile iniFile, string searchPath)
    {
        int fontCount = iniFile.GetIntValue("Fonts", "Count", 0);

        for (int i = 0; i < fontCount; i++)
        {
            string section = $"Font{i}";
            string fontTypeStr = iniFile.GetStringValue(section, "Type", nameof(FontType.SpriteFont));

            if (!Enum.TryParse<FontType>(fontTypeStr, true, out var fontType))
                continue;

            if (fontType != FontType.TrueType)
                continue;

            string fontPath = iniFile.GetStringValue(section, "Path", "");
            if (string.IsNullOrEmpty(fontPath))
                continue;

            string fullPath = SafePath.GetFile(searchPath, fontPath).FullName;
            if (File.Exists(fullPath) && !loadedFontSources.Contains(fullPath))
            {
                loadedFontSources.Add(fullPath);
                Logger.Log($"FontManager: Queued font source from Font{i}: {fontPath}");
            }
        }
    }

    /// <summary>
    /// Adds all collected font sources to the shared FontSystem.
    /// </summary>
    private static void AddCollectedFontSourcesToFontSystem()
    {
        foreach (string fontPath in loadedFontSources)
        {
            try
            {
                fontSystem.AddFont(File.ReadAllBytes(fontPath));
                Logger.Log($"FontManager: Added font source to FontSystem: {Path.GetFileName(fontPath)}");
            }
            catch (Exception ex)
            {
                Logger.Log($"FontManager: Failed to add font source {fontPath}: {ex.Message}");
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
    /// </summary>
    private static void CreateFontIndexesFromIni(string iniPath, ContentManager contentManager, string searchPath, string baseDir)
    {
        var iniFile = new IniFile(iniPath);
        int fontCount = iniFile.GetIntValue("Fonts", "Count", 0);

        Logger.Log($"FontManager: Creating {fontCount} font indexes from {iniPath}");

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
                    // For TTF, we use the shared FontSystem which already has all sources loaded
                    if (loadedFontSources.Count == 0)
                    {
                        Logger.Log($"FontManager: Warning - Font{i} is TrueType but no font sources are loaded");
                        continue;
                    }

                    fonts.Add(new TTFFontWrapper(fontSystem.GetFont(size)));
                    Logger.Log($"FontManager: Created FontIndex {fonts.Count - 1}: TTF size {size}");
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
