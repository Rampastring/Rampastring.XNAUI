#if WINFORMS
using System;
using System.Drawing;
using System.Drawing.Text;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace Rampastring.XNAUI.FontManagement;

/// <summary>
/// Loads the raw data of an installed Windows font through GDI or the Windows font
/// registration without redistributing the font file with the application.
/// </summary>
internal static class WindowsSystemFontLoader
{
    private const string FontsRegistryPath = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Fonts";
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

        if (TryLoadFontDataThroughGdi(familyName.Trim(), style, out fontData, out string gdiErrorMessage))
            return true;

        // GDI+ does not expose every registered font under its Windows registration name.
        // For example, "Segoe UI Variable" is exposed as separate Text, Display, and Small
        // families. Fall back to the font registration so these fonts can still be loaded.
        if (TryLoadRegisteredFontData(familyName.Trim(), style, out fontData, out string registryErrorMessage))
            return true;

        errorMessage = $"{gdiErrorMessage} {registryErrorMessage}";
        return false;
    }

    private static bool TryLoadFontDataThroughGdi(string familyName, FontStyle style, out byte[] fontData,
        out string errorMessage)
    {
        fontData = null;
        errorMessage = null;

        using var installedFonts = new InstalledFontCollection();
        FontFamily selectedFamily = null;

        foreach (FontFamily installedFamily in installedFonts.Families)
        {
            if (string.Equals(installedFamily.Name, familyName, StringComparison.OrdinalIgnoreCase))
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

    private static bool TryLoadRegisteredFontData(string familyName, FontStyle style, out byte[] fontData,
        out string errorMessage)
    {
        fontData = null;
        errorMessage = null;

        string registeredFontName = GetRegisteredFontName(familyName, style);
        RegistryHive[] registryHives = [RegistryHive.CurrentUser, RegistryHive.LocalMachine];
        RegistryView[] registryViews = Environment.Is64BitOperatingSystem
            ? [RegistryView.Registry64, RegistryView.Registry32]
            : [RegistryView.Registry32];

        foreach (RegistryHive registryHive in registryHives)
        {
            foreach (RegistryView registryView in registryViews)
            {
                try
                {
                    using RegistryKey baseKey = RegistryKey.OpenBaseKey(registryHive, registryView);
                    using RegistryKey fontsKey = baseKey.OpenSubKey(FontsRegistryPath);
                    if (fontsKey == null)
                        continue;

                    foreach (string valueName in fontsKey.GetValueNames())
                    {
                        if (!string.Equals(RemoveFontTechnologySuffix(valueName), registeredFontName,
                            StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        if (fontsKey.GetValue(valueName) is not string registeredPath)
                            continue;

                        foreach (string fontPath in GetPossibleFontPaths(registeredPath, registryHive))
                        {
                            if (!File.Exists(fontPath))
                                continue;

                            fontData = File.ReadAllBytes(fontPath);
                            return true;
                        }
                    }
                }
                catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException ||
                                           ex is System.Security.SecurityException)
                {
                    errorMessage = ex.Message;
                }
            }
        }

        errorMessage ??= $"No installed font registration named '{registeredFontName}' was found.";
        return false;
    }

    private static string GetRegisteredFontName(string familyName, FontStyle style)
    {
        if (style == FontStyle.Regular)
            return familyName;

        if (style == (FontStyle.Bold | FontStyle.Italic))
            return $"{familyName} Bold Italic";

        return $"{familyName} {style}";
    }

    private static string RemoveFontTechnologySuffix(string registryValueName)
    {
        string name = registryValueName.TrimEnd().TrimEnd(';').TrimEnd();
        int suffixStart = name.LastIndexOf(" (", StringComparison.Ordinal);

        return suffixStart >= 0 && name.EndsWith(")", StringComparison.Ordinal)
            ? name.Substring(0, suffixStart)
            : name;
    }

    private static string[] GetPossibleFontPaths(string registeredPath, RegistryHive registryHive)
    {
        string expandedPath = Environment.ExpandEnvironmentVariables(registeredPath.Trim().Trim('"'));
        if (Path.IsPathRooted(expandedPath))
            return [expandedPath];

        string systemFontsPath = Environment.GetFolderPath(Environment.SpecialFolder.Fonts);
        if (registryHive == RegistryHive.CurrentUser)
        {
            string userFontsPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Microsoft", "Windows", "Fonts");

            return [Path.Combine(userFontsPath, expandedPath), Path.Combine(systemFontsPath, expandedPath)];
        }

        return [Path.Combine(systemFontsPath, expandedPath)];
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
