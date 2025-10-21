using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;

namespace Rampastring.XNAUI.Input;

/// <summary>
/// A class for handling the keyboard.
/// </summary>
public class RKeyboard : GameComponent
{
    public RKeyboard(Game game)
        : base(game)
    {
        KeyboardState = Keyboard.GetState();
    }

    public delegate void KeyPressedEventHandler(object sender, KeyPressEventArgs e);

    /// <summary>
    /// Triggered when a key has been pressed, iow. pressed down and then released.
    /// </summary>
    public event KeyPressedEventHandler OnKeyPressed;

    /// <summary>
    /// Triggered when a key has been first pressed down.
    /// </summary>
    public event KeyPressedEventHandler OnKeyDown;

    public KeyboardState KeyboardState;

    public List<Keys> PressedKeys = new List<Keys>();
    public List<Keys> DownKeys = new List<Keys>();

    public override void Update(GameTime gameTime)
    {
        KeyboardState = Keyboard.GetState();
        PressedKeys.Clear();

        foreach (Keys key in DownKeys)
        {
            if (key == Keys.None)
                continue; // Work-around a MonoGame bug in OGL mode

            if (KeyboardState.IsKeyUp(key))
            {
                DoKeyPress(key);
                PressedKeys.Add(key);
            }
        }

        var newDownKeys = KeyboardState.GetPressedKeys();
        Span<bool> isNewKey = stackalloc bool[newDownKeys.Length];

        // Gather which of the pressed keys were pushed down on this frame for the first time.
        // This must be done before DownKeys is assigned to match the keys pressed down on this frame (or otherwise we'd have nothing to compare to),
        // but before DoKeyDown is called for the new keys so that any potential keyboard event handlers firing on this frame can check DownKeys with the updated state.
        for (int i = 0; i < newDownKeys.Length; i++)
        {
            // Stack-allocated memory is not initialized so we need to write over all entries in the span.
            isNewKey[i] = !DownKeys.Contains(newDownKeys[i]);
        }

        // Set DownKeys to match the keys pressed down on this frame.
        DownKeys.Clear();
        DownKeys.AddRange(newDownKeys);

        // Call DoKeyDown for the keys that were newly pressed down on this frame.
        for (int i = 0; i < DownKeys.Count; i++)
        {
            if (isNewKey[i])
                DoKeyDown(DownKeys[i]);
        }
    }

    private void DoKeyPress(Keys key)
    {
        if (OnKeyPressed != null)
        {
            Delegate[] delegates = OnKeyPressed.GetInvocationList();
            var args = new KeyPressEventArgs(key);
            for (int i = 0; i < delegates.Length; i++)
            {
                delegates[i].DynamicInvoke(this, args);
                if (args.Handled)
                    return;
            }
        }
    }

    private void DoKeyDown(Keys key)
    {
        if (OnKeyDown != null)
        {
            Delegate[] delegates = OnKeyDown.GetInvocationList();
            var args = new KeyPressEventArgs(key);
            for (int i = 0; i < delegates.Length; i++)
            {
                delegates[i].DynamicInvoke(this, args);
                if (args.Handled)
                    return;
            }
        }
    }

    public bool IsKeyHeldDown(Keys key)
    {
        return DownKeys.Contains(key);
    }

    public bool IsCtrlHeldDown()
    {
        return IsKeyHeldDown(Keys.RightControl) || IsKeyHeldDown(Keys.LeftControl);
    }

    public bool IsShiftHeldDown()
    {
        return IsKeyHeldDown(Keys.RightShift) || IsKeyHeldDown(Keys.LeftShift);
    }

    public bool IsAltHeldDown()
    {
        return IsKeyHeldDown(Keys.RightAlt) || IsKeyHeldDown(Keys.LeftAlt);
    }
}

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
