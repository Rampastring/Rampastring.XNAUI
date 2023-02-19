namespace Rampastring.XNAUI.Input;

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

/// <summary>
/// A class for handling the keyboard.
/// </summary>
public class RKeyboard : GameComponent
{
    public RKeyboard(Game game)
        : base(game)
    {
        PressedKeys = new();
        KeyboardState = Keyboard.GetState();
    }

    public delegate void KeyPressedEventHandler(object sender, KeyPressEventArgs e);

    public event KeyPressedEventHandler OnKeyPressed;

    public KeyboardState KeyboardState;
    private Keys[] downKeys = Array.Empty<Keys>();

    public readonly List<Keys> PressedKeys;

    public override void Update(GameTime gameTime)
    {
        KeyboardState = Keyboard.GetState();
        PressedKeys.Clear();

        foreach (Keys key in downKeys)
        {
            if (key == Keys.None)
                continue; // Work-around a MonoGame bug in OGL mode

            if (KeyboardState.IsKeyUp(key))
            {
                DoKeyPress(key);
                PressedKeys.Add(key);
            }
        }

        downKeys = KeyboardState.GetPressedKeys();
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

    public bool IsKeyHeldDown(Keys key) => downKeys.Contains(key);

    public bool IsCtrlHeldDown() => IsKeyHeldDown(Keys.RightControl) || IsKeyHeldDown(Keys.LeftControl);

    public bool IsShiftHeldDown() => IsKeyHeldDown(Keys.RightShift) || IsKeyHeldDown(Keys.LeftShift);

    public bool IsAltHeldDown() => IsKeyHeldDown(Keys.RightAlt) || IsKeyHeldDown(Keys.LeftAlt);
}