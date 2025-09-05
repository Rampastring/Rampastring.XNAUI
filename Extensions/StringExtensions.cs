using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;

using Rampastring.XNAUI.I18N;

namespace Rampastring.XNAUI.Extensions;

/// <summary>
/// Extensions of <see cref="string"/> class.
/// </summary>
public static class StringExtensions
{
    private const string ESCAPED_INI_NEWLINE_PATTERN = $"\\{XNAUIConstants.INI_NEWLINE_PATTERN}";
    private const string ESCAPED_SEMICOLON = "\\semicolon";

    /// <summary>
    /// Converts a regular string to an INI representation of it.
    /// </summary>
    /// <param name="raw">Input string.</param>
    /// <returns>INI-safe string.</returns>
    public static string ToIniString(this string raw)
    {
        if (raw.Contains(ESCAPED_INI_NEWLINE_PATTERN, StringComparison.InvariantCulture))
            throw new ArgumentException($"The string contains an illegal character sequence! ({ESCAPED_INI_NEWLINE_PATTERN})");

        if (raw.Contains(ESCAPED_SEMICOLON, StringComparison.InvariantCulture))
            throw new ArgumentException($"The string contains an illegal character sequence! ({ESCAPED_SEMICOLON})");

        return raw
            .Replace(XNAUIConstants.INI_NEWLINE_PATTERN, ESCAPED_INI_NEWLINE_PATTERN)
            .Replace(";", ESCAPED_SEMICOLON)
            .Replace(Environment.NewLine, "\n")
            .Replace("\n", XNAUIConstants.INI_NEWLINE_PATTERN);
    }

    /// <summary>
    /// Converts an INI-safe string to a normal string.
    /// </summary>
    /// <param name="iniString">Input INI string.</param>
    /// <returns>Regular string.</returns>
    public static string FromIniString(this string iniString)
    {
        return iniString
            .Replace(ESCAPED_INI_NEWLINE_PATTERN, XNAUIConstants.INI_NEWLINE_PATTERN)
            .Replace(ESCAPED_SEMICOLON, ";")
            .Replace(XNAUIConstants.INI_NEWLINE_PATTERN, Environment.NewLine);
    }

    /// <summary>
    /// Looks up a translated string for the specified key.
    /// </summary>
    /// <param name="defaultValue">The default string value as a fallback.</param>
    /// <param name="key">The unique key name.</param>
    /// <param name="notify">Whether to add this key and value to the list of missing key-values.</param>
    /// <returns>The translated string value.</returns>
    /// <remarks>
    /// This method is referenced by <c>TranslationNotifierGenerator</c> in order to check if the const
    /// values that are not initialized on client start automatically are missing (via notification
    /// mechanism implemented down the call chain). Do not change the signature or move the method out
    /// of the namespace it's currently defined in. If you do - you have to also edit the generator
    /// source code to match.
    /// </remarks>
    public static string L10N(this string defaultValue, string key, bool notify = true)
        => string.IsNullOrEmpty(defaultValue)
            ? defaultValue
            : Translation.Instance.LookUp(key, defaultValue, notify);
}
