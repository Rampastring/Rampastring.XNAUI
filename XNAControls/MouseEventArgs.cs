namespace Rampastring.XNAUI.XNAControls;

using System;
using Microsoft.Xna.Framework;

public class MouseEventArgs : EventArgs
{
    public MouseEventArgs(Point relativeLocation)
    {
        RelativeLocation = relativeLocation;
    }

    /// <summary>
    /// The point of the mouse cursor relative to the control.
    /// </summary>
    public Point RelativeLocation { get; set; }
}