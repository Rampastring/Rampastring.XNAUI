namespace Rampastring.XNAUI.Input;

using System;
using Microsoft.Xna.Framework.Input;

public class KeyPressEventArgs : EventArgs
{
    public KeyPressEventArgs(Keys key)
    {
        PressedKey = key;
    }

    public Keys PressedKey { get; set; }

    // If set, the key press event won't be forwarded on to following subscribers.
    public bool Handled { get; set; }
}