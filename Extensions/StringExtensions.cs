using System;

namespace Rampastring.XNAUI.Extensions;

/// <summary>
/// Extension methods for <see cref="string"/>.
/// </summary>
public static class StringExtensions
{
    /// <summary>
    /// Returns a substring of this string starting at <paramref name="start"/> and containing at most
    /// <paramref name="maxLength"/> UTF-16 code units, ensuring the result does not end with an orphaned high surrogate.
    /// </summary>
    /// <param name="str">The string to slice.</param>
    /// <param name="start">The zero-based start index of the substring.</param>
    /// <param name="maxLength">Maximum number of UTF-16 code units to include in the result. Must be non-negative.</param>
    /// <returns>The safe substring.</returns>
    public static string SubstringSurrogateAware(this string str, int start, int maxLength)
    {
        if (str == null)
            throw new ArgumentNullException(nameof(str));
        if (start < 0 || start > str.Length)
            throw new ArgumentOutOfRangeException(nameof(start), $"{nameof(start)} must be within the bounds of the string.");
        if (maxLength < 0)
            throw new ArgumentOutOfRangeException(nameof(maxLength), $"{nameof(maxLength)} must be non-negative.");

        int available = str.Length - start;
        int length = maxLength < available ? maxLength : available;
        if (length > 0 && char.IsHighSurrogate(str[start + length - 1]))
            length--;

        return str.Substring(start, length);
    }
}
