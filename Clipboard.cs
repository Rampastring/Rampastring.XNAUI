using Rampastring.Tools;
using System;
using TextCopy;

namespace Rampastring.XNAUI;

public static class Clipboard
{
    /// <summary>
    /// Attempts to set clipboard text. Returns a boolean which tells whether the operation succeeded.
    /// </summary>
    /// <param name="text">The text to set on the clipboard.</param>
    /// <returns>True if successful, otherwise false.</returns>
    public static bool SetText(string text)
    {
        try
        {
            ClipboardService.SetText(text);
            return true;
        }
        catch (Exception ex)
        {
            Logger.Log("Failed to set clipboard text. Reason: " + ex.Message);
        }

        return false;
    }
}
