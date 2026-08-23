using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Rampastring.Tools;
using Rampastring.XNAUI.Extensions;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace Rampastring.XNAUI.FontManagement;

/// <summary>
/// Manages font loading and rendering for the UI system.
/// Supports both SpriteFont and TrueType fonts with automatic fallback.
/// </summary>
/// <remarks>
/// <para>
/// For TrueType fonts, FontManager creates a separate FontSystem for each font index.
/// Each FontSystem has a primary font (specified via Path or a Windows system font family)
/// and fallback fonts (from [FallbackFonts]).
/// When a character is not found in the primary font, it automatically falls back to other loaded fonts.
/// </para>
/// <para>
/// The Fonts.ini file format supports:
/// <list type="bullet">
/// <item>[TextShaping] - Optional HarfBuzz text shaping configuration</item>
/// <item>[FallbackFonts] - Optional fallback font files used when primary font lacks a character</item>
/// <item>[Fonts] - Font index definitions with Size, Type, and source-specific settings.
/// SystemFont entries use Family and optional Style values on Windows; their optional Path is a file fallback.</item>
/// </list>
/// </para>
/// </remarks>
public static class FontManager
{
    private static List<IFont> fonts;
    private static List<FontSystem> fontSystems = new();
    private static TextShapingSettings textShapingSettings = new();
    private static FontRenderingSettings fontRenderingSettings = new();

    /// <summary>
    /// When set before <see cref="LoadFonts"/> runs, skips the Fonts.ini search and
    /// loads only legacy SpriteFontN.xnb assets. Lets applications offer an opt-out from
    /// TrueType rendering.
    /// </summary>
    public static bool UseSpriteFonts { get; set; }

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
    /// Gets the current font rendering settings.
    /// </summary>
    public static FontRenderingSettings GetFontRenderingSettings() => fontRenderingSettings;

    /// <summary>
    /// Checks if text shaping is currently enabled.
    /// </summary>
    public static bool IsTextShapingEnabled() => textShapingSettings.Enabled;

    /// <summary>
    /// Creates a new FontSystem with current text shaping settings.
    /// </summary>
    private static FontSystem CreateFontSystem()
    {
        var settings = new FontSystemSettings()
        {
            KernelWidth = fontRenderingSettings.KernelWidth,
            KernelHeight = fontRenderingSettings.KernelHeight,
            FontResolutionFactor = fontRenderingSettings.FontResolutionFactor,
            TextureWidth = fontRenderingSettings.TextureWidth,
            TextureHeight = fontRenderingSettings.TextureHeight,
            GlyphRenderResult = fontRenderingSettings.GlyphRenderResult,
            UseEmToPixelsScale = true
        };

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

    public static Vector2 MeasureString(string text, int fontIndex)
    {
        if (fontIndex < 0 || fontIndex >= fonts.Count)
            throw new IndexOutOfRangeException($"Invalid font index. {fonts.Count} fonts loaded, requested index: {fontIndex}");

        return fonts[fontIndex].MeasureString(text);
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
    /// - For TrueType and system fonts: Create a FontSystem with primary font first, then fallback fonts
    /// - For SpriteFonts: Load the .xnb file
    /// </para>
    /// </remarks>
    /// <param name="contentManager">Content manager used to load SpriteFont assets.</param>
    /// <param name="minimumFontResolutionFactor">
    /// Optional minimum rasterization factor required by the display scale. The value
    /// configured in <c>Fonts.ini</c> is preserved when it is higher.
    /// </param>
    public static void LoadFonts(ContentManager contentManager, float? minimumFontResolutionFactor = null)
    {
        fonts ??= [];
        fonts.Clear();
        fontSystems.Clear();
        fallbackFontPaths.Clear();

        // Reset text shaping and rendering settings
        textShapingSettings = new TextShapingSettings();
        fontRenderingSettings = new FontRenderingSettings();

        string originalContentRoot = contentManager.RootDirectory;
        bool fontsIniFound = false;

        if (UseSpriteFonts)
        {
            Logger.Log($"{nameof(FontManager)}: {nameof(UseSpriteFonts)} is set, skipping Fonts.ini search");
        }
        else
        {
            foreach (string searchPath in AssetLoader.AssetSearchPaths)
            {
                string baseDir = SafePath.GetDirectory(searchPath).FullName;
                string iniPath = Path.Combine(baseDir, "Fonts.ini");

                if (File.Exists(iniPath))
                {
                    Logger.Log($"{nameof(FontManager)}: Loading fonts from {iniPath}");
                    LoadFontsFromIni(iniPath, contentManager, searchPath, baseDir, minimumFontResolutionFactor);
                    fontsIniFound = true;
                    break; // Stop after first Fonts.ini found
                }
            }
        }

        // Apply the resolution-factor override even when no Fonts.ini was found
        // (legacy SpriteFont path), so callers like Renderer.ReloadFontsForScale
        // still take effect.
        if (!fontsIniFound && minimumFontResolutionFactor.HasValue)
        {
            fontRenderingSettings.FontResolutionFactor = Math.Max(fontRenderingSettings.FontResolutionFactor, minimumFontResolutionFactor.Value);
        }

        // Fall back to legacy SpriteFont loading if no Fonts.ini found
        if (!fontsIniFound)
        {
            Logger.Log($"{nameof(FontManager)}: No Fonts.ini found, attempting SpriteFont loading");
            foreach (string searchPath in AssetLoader.AssetSearchPaths)
            {
                string baseDir = SafePath.GetDirectory(searchPath).FullName;
                int fontsBeforeLoad = fonts.Count;
                LoadSpriteFonts(contentManager, searchPath, baseDir);

                if (fonts.Count > fontsBeforeLoad)
                    break; // Stop after first path with legacy fonts
            }
        }

        contentManager.SetRootDirectory(originalContentRoot);

        Logger.Log($"{nameof(FontManager)}: Loaded {fonts.Count} font indexes with {fontSystems.Count} FontSystems");
    }

    /// <summary>
    /// Loads fonts from a specific Fonts.ini file.
    /// </summary>
    private static void LoadFontsFromIni(string iniPath, ContentManager contentManager, string searchPath, string baseDir, float? minimumFontResolutionFactor)
    {
        var iniFile = new IniFile(iniPath);

        // Load text shaping settings
        var textShapingSection = iniFile.GetSection("TextShaping");
        if (textShapingSection != null)
        {
            textShapingSettings.ReadSettingsFromIniSection(textShapingSection);
        }

        LoadFallbackFonts(iniFile, searchPath);

        // Load font rendering settings
        var fontRenderingSection = iniFile.GetSection("FontRendering");
        if (fontRenderingSection != null)
        {
            fontRenderingSettings.ReadSettingsFromIniSection(fontRenderingSection);
        }

        // Override after ini load so the runtime value wins
        if (minimumFontResolutionFactor.HasValue)
        {
            fontRenderingSettings.FontResolutionFactor = Math.Max(fontRenderingSettings.FontResolutionFactor, minimumFontResolutionFactor.Value);
        }

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

    /// <summary>
    /// Creates FontIndex entries from a Fonts.ini file.
    /// For each TrueType font, creates a separate FontSystem with primary font first, then fallback fonts.
    /// </summary>
    private static void CreateFontIndexesFromIni(IniFile iniFile, ContentManager contentManager, string searchPath, string baseDir)
    {
        int i = 0;
        while (true)
        {
            IniSection section = iniFile.GetSection($"Font{i}");
            if (section == null)
                break;

            string fontPath = section.GetStringValue("Path", "");
            int size = section.GetIntValue("Size", 16);
            string fontTypeStr = section.GetStringValue("Type", nameof(FontType.SpriteFont));

            if (!Enum.TryParse<FontType>(fontTypeStr, true, out var fontType))
                fontType = FontType.SpriteFont;

            switch (fontType)
            {
                case FontType.TrueType:
                    CreateTrueTypeFontIndex(i, fontPath, size, searchPath);
                    break;

                case FontType.SystemFont:
                    string fontFamily = section.GetStringValue("Family", "");
                    string fontStyle = section.GetStringValue("Style", "Regular");
                    CreateTrueTypeFontIndex(i, fontPath, size, searchPath, fontFamily, fontStyle);
                    break;

                case FontType.SpriteFont:
                    contentManager.SetRootDirectory(baseDir);
                    string sfName = Path.GetFileNameWithoutExtension(fontPath);
                    LoadSpriteFont(contentManager, searchPath, sfName);
                    break;
            }

            i++;
        }

        Logger.Log($"FontManager: Created {i} font indexes");
    }

    /// <summary>
    /// Creates a TrueType font index with its own FontSystem.
    /// The FontSystem contains the Windows system font (if specified), the file-based primary
    /// font (if specified), and the shared fallback fonts, in that order.
    /// </summary>
    private static void CreateTrueTypeFontIndex(int fontIndex, string primaryFontPath, int size, string searchPath,
        string systemFontFamily = null, string systemFontStyle = null)
    {
        FontSystem fontSystem = CreateFontSystem();
        fontSystems.Add(fontSystem);

        bool hasPrimaryFont = false;
        bool hasSystemFont = false;

        if (!string.IsNullOrWhiteSpace(systemFontFamily))
        {
#if WINFORMS
            if (WindowsSystemFontLoader.TryLoadFontData(systemFontFamily, systemFontStyle, out byte[] fontData, out string errorMessage))
            {
                try
                {
                    fontSystem.AddFont(fontData);
                    Logger.Log($"FontManager: Font{fontIndex} - Added system font: {systemFontFamily} ({systemFontStyle})");
                    hasPrimaryFont = true;
                    hasSystemFont = true;
                }
                catch (Exception ex)
                {
                    Logger.Log($"FontManager: Font{fontIndex} - Failed to load system font {systemFontFamily} ({systemFontStyle}): {ex.Message}");
                }
            }
            else
            {
                Logger.Log($"FontManager: Font{fontIndex} - Failed to load system font {systemFontFamily} ({systemFontStyle}): {errorMessage}");
            }
#else
            Logger.Log($"FontManager: Font{fontIndex} - System font {systemFontFamily} is unavailable on this platform; trying file fallbacks");
#endif
        }

        // A Path on a SystemFont entry acts as the first file fallback. On a regular
        // TrueType entry it remains the primary font, preserving existing behavior.
        if (!string.IsNullOrEmpty(primaryFontPath))
        {
            string fullPath = SafePath.GetFile(searchPath, primaryFontPath).FullName;
            if (File.Exists(fullPath))
            {
                try
                {
                    fontSystem.AddFont(File.ReadAllBytes(fullPath));
                    string fontRole = hasPrimaryFont ? "file fallback" : "primary font";
                    Logger.Log($"FontManager: Font{fontIndex} - Added {fontRole}: {primaryFontPath}");
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
            string primaryInfo;
            if (hasSystemFont)
                primaryInfo = $"system font: {systemFontFamily} ({systemFontStyle})";
            else if (hasPrimaryFont)
                primaryInfo = $"primary: {Path.GetFileName(primaryFontPath)}";
            else
                primaryInfo = "no primary";

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
    /// Loads SpriteFonts (SpriteFont0, SpriteFont1, etc.) from a search path.
    /// </summary>
    private static void LoadSpriteFonts(ContentManager contentManager, string searchPath, string baseDir)
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
            throw new IndexOutOfRangeException($"Invalid font index. {fonts.Count} fonts loaded, requested index: {fontIndex}");

        var font = fonts[fontIndex];

        if (str == null)
            throw new ArgumentNullException(nameof(str));

        if (string.IsNullOrEmpty(str) || font.MeasureString(str).X <= maxWidth)
            return str;

        // Binary search for the maximum number of characters that fit within maxWidth.
        // Assumes string width is monotonically non-decreasing as the string length increases,
        // which holds for all standard fonts.
        // This reduces complexity from O(n) to O(log n) compared to removing one character at a time.

        // Warning: Copilot said: The binary search relies on prefix width being monotonic with length,
        // but that’s not guaranteed with kerning and/or HarfBuzz text shaping (both are used in this codebase).
        // In such cases it’s possible for a longer prefix to measure narrower than a shorter one,
        // making the <= maxWidth predicate non-monotonic and causing the search to return a prefix
        // that is not the longest-fitting (or potentially not fitting at all, depending on the path).
        // We accept this risk for now.
        int low = 0;
        int high = str.Length - 1;

        while (low < high)
        {
            int mid = (low + high + 1) / 2; // Round up to avoid infinite loop when low + 1 == high
            if (font.MeasureString(str.SubstringSurrogateAware(0, mid)).X <= maxWidth)
                low = mid;
            else
                high = mid - 1;
        }

        return str.SubstringSurrogateAware(0, low);
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

    public static int GetTextYPadding(string text, int fontIndex, int containerHeight)
    {
        if (fontIndex < 0 || fontIndex >= fonts.Count)
            throw new IndexOutOfRangeException($"Invalid font index. {fonts.Count} fonts loaded, requested index: {fontIndex}");

        return fonts[fontIndex].GetTextYPadding(containerHeight, text);
    }

    public static int GetSingleLineTextYPadding(int fontIndex, int containerHeight)
    {
        if (fontIndex < 0 || fontIndex >= fonts.Count)
            throw new IndexOutOfRangeException($"Invalid font index. {fonts.Count} fonts loaded, requested index: {fontIndex}");
        return fonts[fontIndex].GetSingleLineTextYPadding(containerHeight);
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
