using System;

namespace Rampastring.XNAUI;

/// <summary>
/// A class that contains constants reused across different XNAUI elements.
/// </summary>
public static class XNAUIConstants
{
    public const double KEYBOARD_SCROLL_REPEAT_TIME = 0.03;
    public const double KEYBOARD_FAST_SCROLL_TRIGGER_TIME = 0.4;

    /// <summary>
    /// Used to denote <see cref="Environment.NewLine"/> in the INI files.
    /// </summary>
    /// <remarks>
    /// Historically Westwood used '@' for this purpose, so we keep it for compatibility.
    /// </remarks>
    public const string INI_NEWLINE_PATTERN = "@";

    /// <summary>
    /// The locale code that corresponds to the language the hardcoded client strings are in.
    /// </summary>
    public const string HARDCODED_LOCALE_CODE = "en";

    public static string TranslationsFolderPath { get; set; } = "Translation";

    public static string TranslationIniName { get; set; } = "Translation.ini";
}