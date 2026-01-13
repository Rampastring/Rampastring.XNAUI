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
}
