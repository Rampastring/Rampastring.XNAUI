using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Rampastring.XNAUI.Input
{
    /// <summary>
    /// A class for handling the keyboard.
    /// </summary>
    public class RKeyboard : GameComponent
    {
        public RKeyboard(Game game)
            : base(game)
        {
            PressedKeys = new List<Keys>();
            KeyboardState = Keyboard.GetState();
        }

        public delegate void KeyPressedEventHandler(object sender, KeyPressEventArgs e);
        public event KeyPressedEventHandler OnKeyPressed;

        public KeyboardState KeyboardState;

        Keys[] DownKeys = new Keys[0];

        public List<Keys> PressedKeys;

        public override void Update(GameTime gameTime)
        {
            KeyboardState = Keyboard.GetState();
            PressedKeys.Clear();

            foreach (Keys key in DownKeys)
            {
                if (KeyboardState.IsKeyUp(key))
                {
                    DoKeyPress(key);
                    PressedKeys.Add(key);
                    
                }
            }

            DownKeys = KeyboardState.GetPressedKeys();
        }

        void DoKeyPress(Keys key)
        {
            if (OnKeyPressed != null)
                OnKeyPressed(this, new KeyPressEventArgs(key));
        }

        public bool IsKeyHeldDown(Keys key)
        {
            return DownKeys.Contains(key);
        }

        public bool IsCtrlHeldDown()
        {
            return IsKeyHeldDown(Keys.RightControl) ||
                        IsKeyHeldDown(Keys.LeftControl);
        }
    }

    public class KeyPressEventArgs : EventArgs
    {
        public KeyPressEventArgs(Keys key)
        {
            PressedKey = key;
        }

        public Keys PressedKey { get; set; }
    }
}
