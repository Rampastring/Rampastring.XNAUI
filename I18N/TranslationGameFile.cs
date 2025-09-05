using System;

// https://stackoverflow.com/questions/64749385/predefined-type-system-runtime-compilerservices-isexternalinit-is-not-defined
namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit { }
}

namespace Rampastring.XNAUI.I18N
{
    /// <summary>
    /// Describes a file to try and copy into the game folder with a translation.
    /// </summary>
    /// <param name="Source">A path to copy from, relative to the selected translation folder.</param>
    /// <param name="Target">A path to copy to, relative to root folder of the game/mod.</param>
    /// <param name="Checked">Whether to include this file in the integrity checks.</param>
    public readonly record struct TranslationGameFile(string Source, string Target, bool Checked);
}
