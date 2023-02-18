namespace Rampastring.XNAUI.XNAControls;

using System;

/// <summary>
/// An enum for determining which part of a text is anchored to a specific point.
/// </summary>
[Flags]
public enum LabelTextAnchorInfo
{
    NONE = 0,

    /// <summary>
    /// The text is anchored to be to the left of the given point.
    /// </summary>
    LEFT = 1,

    /// <summary>
    /// The text is anchored to be to the right of the given point.
    /// </summary>
    RIGHT = 2,

    /// <summary>
    /// The text is horizontally centered on the given point.
    /// </summary>
    HORIZONTAL_CENTER = 4,

    /// <summary>
    /// The text is anchored to be just above the given point.
    /// </summary>
    TOP = 8,

    /// <summary>
    /// The text is anchored to be just below the given point.
    /// </summary>
    BOTTOM = 16,

    /// <summary>
    /// The text is vertical centered on the given point.
    /// </summary>
    VERTICAL_CENTER = 32,

    /// <summary>
    /// The text is both horizontally and vertically centered on the given point.
    /// </summary>
    CENTER = HORIZONTAL_CENTER | VERTICAL_CENTER
}