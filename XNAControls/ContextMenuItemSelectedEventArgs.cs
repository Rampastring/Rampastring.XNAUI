namespace Rampastring.XNAUI.XNAControls;

using System;

public class ContextMenuItemSelectedEventArgs : EventArgs
{
    public ContextMenuItemSelectedEventArgs(int itemIndex)
    {
        ItemIndex = itemIndex;
    }

    public int ItemIndex { get; }
}