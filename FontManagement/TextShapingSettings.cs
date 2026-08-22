using Rampastring.Tools;
using System;

namespace Rampastring.XNAUI.FontManagement;

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

    public void ReadSettingsFromIniSection(IniSection iniSection)
    {
        Enabled = iniSection.GetBooleanValue(nameof(Enabled), false);

        // When shaping is enabled, probe the native HarfBuzz library once and disable shaping if it
        // cannot be loaded here - a missing native binary, or a path the .NET Framework loader cannot
        // represent - so the client renders unshaped instead of crashing during font loading (which
        // would otherwise throw "Unable to load library 'libHarfBuzzSharp'").
        if (Enabled && !IsNativeTextShaperAvailable())
            Enabled = false;

        EnableBiDi = iniSection.GetBooleanValue(nameof(EnableBiDi), true);
        CacheSize = Math.Max(1, iniSection.GetIntValue(nameof(CacheSize), 100));

        Logger.Log($"Text shaping settings: Enabled={Enabled}, BiDi={EnableBiDi}, CacheSize={CacheSize}");
    }

    /// <summary>
    /// Probes whether the native HarfBuzz library can actually be loaded in the current process.
    /// The managed HarfBuzzSharp assembly can load successfully while its native counterpart
    /// (libHarfBuzzSharp) cannot - for example when the native binary was not deployed, or when the
    /// .NET Framework build runs from a path that cannot be represented in the system ANSI code page.
    /// Detecting this once, up front, lets the font system fall back to unshaped rendering with a
    /// clear log entry instead of throwing in the middle of font loading.
    /// </summary>
    private static bool IsNativeTextShaperAvailable()
    {
        try
        {
            using var probe = new HarfBuzzSharp.Buffer();
            return true;
        }
        catch (Exception ex)
        {
            // The real cause (e.g. "Unable to load library 'libHarfBuzzSharp'") is typically wrapped
            // in a TypeInitializationException from HarfBuzzApi's static initializer; unwrap it so the
            // log states the actual reason rather than a generic type-initializer message.
            Exception rootCause = ex;
            while (rootCause.InnerException != null)
                rootCause = rootCause.InnerException;

            Logger.Log($"FontManager: Native HarfBuzz text shaper is unavailable; continuing without text shaping. Reason: {rootCause.Message}");
            return false;
        }
    }
}
