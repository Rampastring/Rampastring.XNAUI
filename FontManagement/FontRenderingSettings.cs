using FontStashSharp;
using Rampastring.Tools;
using System;

namespace Rampastring.XNAUI.FontManagement;

/// <summary>
/// Configuration for FontStashSharp glyph rasterization.
/// </summary>
public class FontRenderingSettings
{
    /// <summary>
    /// Horizontal blur kernel size used by FontStashSharp when rasterizing glyphs.
    /// Maps to <c>FontSystemSettings.KernelWidth</c>. Must be non-negative.
    /// </summary>
    public int KernelWidth { get; set; } = 0;

    /// <summary>
    /// Vertical blur kernel size used by FontStashSharp when rasterizing glyphs.
    /// Maps to <c>FontSystemSettings.KernelHeight</c>. Must be non-negative.
    /// </summary>
    public int KernelHeight { get; set; } = 0;

    /// <summary>
    /// Multiplier applied to the rasterization size of each glyph.
    /// Values > 1 produce sharper output when text is drawn at scales above 1.0
    /// at the cost of a larger atlas footprint.
    /// </summary>
    public float FontResolutionFactor { get; set; } = 1f;

    /// <summary>
    /// Width of each FontStashSharp atlas page, in pixels.
    /// </summary>
    public int TextureWidth { get; set; } = 1024;

    /// <summary>
    /// Height of each FontStashSharp atlas page, in pixels.
    /// </summary>
    public int TextureHeight { get; set; } = 1024;

    /// <summary>
    /// How rasterized glyph pixels are produced.
    /// <see cref="GlyphRenderResult.Premultiplied"/> matches a premultiplied-alpha SpriteBatch,
    /// <see cref="GlyphRenderResult.NonPremultiplied"/> matches AlphaBlend, and
    /// <see cref="GlyphRenderResult.NoAntialiasing"/> produces hard 1-bit edges for pixel-art fonts.
    /// </summary>
    public GlyphRenderResult GlyphRenderResult { get; set; } = GlyphRenderResult.Premultiplied;

    public void ReadSettingsFromIniSection(IniSection iniSection)
    {
        int kernelWidth = iniSection.GetIntValue(nameof(KernelWidth), 0);
        int kernelHeight = iniSection.GetIntValue(nameof(KernelHeight), 0);
        float resolutionFactor = iniSection.GetSingleValue(nameof(FontResolutionFactor), 1f);
        int textureWidth = iniSection.GetIntValue(nameof(TextureWidth), 1024);
        int textureHeight = iniSection.GetIntValue(nameof(TextureHeight), 1024);
        string glyphResultStr = iniSection.GetStringValue(nameof(GlyphRenderResult), nameof(GlyphRenderResult.Premultiplied));

        if (kernelWidth < 0)
            kernelWidth = 0;
        if (kernelHeight < 0)
            kernelHeight = 0;
        if (resolutionFactor < 0f)
            resolutionFactor = 0f;
        if (textureWidth < 1)
            textureWidth = 1;
        if (textureHeight < 1)
            textureHeight = 1;

        if (!Enum.TryParse<GlyphRenderResult>(glyphResultStr, true, out var glyphResult))
            glyphResult = GlyphRenderResult.Premultiplied;

        KernelWidth = kernelWidth;
        KernelHeight = kernelHeight;
        FontResolutionFactor = resolutionFactor;
        TextureWidth = textureWidth;
        TextureHeight = textureHeight;
        GlyphRenderResult = glyphResult;

        Logger.Log($"Font rendering settings: KernelWidth={KernelWidth}, KernelHeight={KernelHeight}, FontResolutionFactor={FontResolutionFactor}, TextureSize={TextureWidth}x{TextureHeight}, GlyphRenderResult={GlyphRenderResult}");
    }
}
