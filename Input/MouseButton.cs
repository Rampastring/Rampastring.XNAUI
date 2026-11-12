using System;

namespace Rampastring.XNAUI.Input;

/// <summary>
/// Identifies mouse buttons tracked by <see cref="Cursor"/>.
/// </summary>
[Flags]
public enum MouseButton
{
    /// <summary>No mouse buttons.</summary>
    None = 0,

    /// <summary>The left mouse button.</summary>
    Left = 1 << 0,

    /// <summary>The right mouse button.</summary>
    Right = 1 << 1,

    /// <summary>The middle mouse button.</summary>
    Middle = 1 << 2,

    /// <summary>The first side button.</summary>
    XButton1 = 1 << 3,

    /// <summary>The second side button.</summary>
    XButton2 = 1 << 4
}
