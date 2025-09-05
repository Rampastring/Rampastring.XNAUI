using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rampastring.XNAUI.Extensions;

/// <summary>
/// Extensions of <see cref="System.Text.Encoding"/> class.
/// </summary>
public static class EncodingExt
{
    static EncodingExt()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        ANSI = Encoding.GetEncoding(0);
    }

    /// <summary>
    /// Gets the legacy ANSI encoding (not Windows-1252 and also not any specific encoding).
    /// ANSI doesn't mean a specific codepage, it means the default non-Unicode codepage which can be changed from Control Panel.
    /// </summary>
    public static Encoding ANSI { get; }

    /// <summary>
    /// UTF-8 encoding without binary object marker.
    /// </summary>
    public static Encoding UTF8NoBOM { get; } = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

    public const string ENCODING_AUTO_DETECT = "Auto";

#nullable enable
    public static Encoding? GetEncodingWithAuto(string? encodingName)
    {
        if (encodingName is null)
            return UTF8NoBOM;

        if (encodingName.Equals(ENCODING_AUTO_DETECT, System.StringComparison.InvariantCultureIgnoreCase))
            return null;

        Encoding encoding = Encoding.GetEncoding(encodingName);

        // We don't expect UTF-8 BOM for the string "UTF-8"
        if (encoding is UTF8Encoding)
            encoding = UTF8NoBOM;

        return encoding;
    }

    public static string EncodingWithAutoToString(Encoding? encoding)
    {
        if (encoding is null)
            return ENCODING_AUTO_DETECT;

        // To find a name that can be passed to the GetEncoding method, use the WebName property.
        return encoding.WebName;
    }
#nullable disable
}
