#if WINFORMS
using System;
using System.Drawing;
using System.Drawing.Text;
using System.Runtime.InteropServices;

namespace Rampastring.XNAUI.FontManagement;

/// <summary>
/// Loads the raw data of an installed Windows font through GDI without redistributing
/// the font file with the application.
/// </summary>
internal static class WindowsSystemFontLoader
{
    private const uint GdiError = uint.MaxValue;
    private const FontStyle SupportedStyles = FontStyle.Bold | FontStyle.Italic;

    public static bool TryLoadFontData(string familyName, string styleName, out byte[] fontData, out string errorMessage)
    {
        fontData = null;
        errorMessage = null;

        if (string.IsNullOrWhiteSpace(familyName))
        {
            errorMessage = "No font family was specified.";
            return false;
        }

        if (!Enum.TryParse(styleName, true, out FontStyle style) || (style & ~SupportedStyles) != 0)
        {
            errorMessage = $"Unknown font style '{styleName}'. Supported values are Regular, Bold, Italic.";
            return false;
        }

        using var installedFonts = new InstalledFontCollection();
        FontFamily selectedFamily = null;

        foreach (FontFamily installedFamily in installedFonts.Families)
        {
            if (string.Equals(installedFamily.Name, familyName.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                selectedFamily = installedFamily;
                break;
            }
        }

        if (selectedFamily == null)
        {
            errorMessage = "The font family is not installed.";
            return false;
        }

        if (!selectedFamily.IsStyleAvailable(style))
        {
            errorMessage = $"The {style} style is not installed for this font family.";
            return false;
        }

        try
        {
            using var font = new Font(selectedFamily, 16.0f, style, GraphicsUnit.Pixel);
            return TryReadFontData(font, out fontData, out errorMessage);
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            return false;
        }
    }

    private static bool TryReadFontData(Font font, out byte[] fontData, out string errorMessage)
    {
        fontData = null;
        errorMessage = null;

        IntPtr deviceContext = IntPtr.Zero;
        IntPtr fontHandle = IntPtr.Zero;
        IntPtr previousObject = IntPtr.Zero;

        try
        {
            deviceContext = CreateCompatibleDC(IntPtr.Zero);
            if (deviceContext == IntPtr.Zero)
            {
                errorMessage = "Windows could not create a font device context.";
                return false;
            }

            fontHandle = font.ToHfont();
            previousObject = SelectObject(deviceContext, fontHandle);
            if (previousObject == IntPtr.Zero || previousObject == new IntPtr(-1))
            {
                errorMessage = "Windows could not select the requested font.";
                return false;
            }

            uint dataLength = GetFontData(deviceContext, 0, 0, null, 0);
            if (dataLength == 0 || dataLength == GdiError || dataLength > int.MaxValue)
            {
                errorMessage = "Windows could not expose the requested font's TrueType data.";
                return false;
            }

            var data = new byte[dataLength];
            uint bytesRead = GetFontData(deviceContext, 0, 0, data, dataLength);
            if (bytesRead == GdiError || bytesRead != dataLength)
            {
                errorMessage = "Windows did not return the complete TrueType font data.";
                return false;
            }

            fontData = data;
            return true;
        }
        finally
        {
            if (previousObject != IntPtr.Zero && deviceContext != IntPtr.Zero)
                SelectObject(deviceContext, previousObject);

            if (fontHandle != IntPtr.Zero)
                DeleteObject(fontHandle);

            if (deviceContext != IntPtr.Zero)
                DeleteDC(deviceContext);
        }
    }

    [DllImport("gdi32.dll", ExactSpelling = true, SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static extern IntPtr CreateCompatibleDC(IntPtr deviceContext);

    [DllImport("gdi32.dll", ExactSpelling = true, SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static extern bool DeleteDC(IntPtr deviceContext);

    [DllImport("gdi32.dll", ExactSpelling = true, SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static extern bool DeleteObject(IntPtr graphicsObject);

    [DllImport("gdi32.dll", ExactSpelling = true, SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static extern uint GetFontData(IntPtr deviceContext, uint table, uint offset, [Out] byte[] buffer, uint dataLength);

    [DllImport("gdi32.dll", ExactSpelling = true, SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static extern IntPtr SelectObject(IntPtr deviceContext, IntPtr graphicsObject);
}
#endif
