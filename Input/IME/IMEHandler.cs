// MonoGame.IMEHelper.Common
//   Copyright (c) 2020 ryancheung

using System;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Rampastring.XNAUI.Input.IME
{
    internal abstract class IMEHandler
    {
        public static IMEHandler Create(Game game)
        {
#if !GL
            return new WinForms.WinFormsIMEHandler(game);
#else
            return new Sdl.SdlIMEHandler(game);
#endif
        }

        /// <summary>
        /// Check if text composition is enabled currently.
        /// </summary>
        public abstract bool Enabled { get; protected set; }

        /// <summary>
        /// Enable the system IMM service to support composited character input.
        /// This should be called when you expect text input from a user and you support languages
        /// that require an IME (Input Method Editor).
        /// </summary>
        /// <seealso cref="StopTextComposition" />
        public abstract void StartTextComposition();

        /// <summary>
        /// Stop the system IMM service.
        /// </summary>
        /// <seealso cref="StartTextComposition" />
        public abstract void StopTextComposition();

        /// <summary>
        /// Use this function to set the rectangle used to type Unicode text inputs if IME supported.
        /// In SDL2, this method call gives the OS a hint for where to show the candidate text list,
        /// since the OS doesn't know where you want to draw the text you received via SDL_TEXTEDITING event.
        /// </summary>
        public virtual void SetTextInputRect(in Rectangle rect)
        { }

        /// <summary>
        /// Composition String
        /// </summary>
        public virtual string Composition { get; protected set; } = string.Empty;

        /// <summary>
        /// Caret position of the composition
        /// </summary>
        public virtual int CompositionCursorPos { get; protected set; }

        /// <summary>
        /// Invoked when the IMM service emit character input event.
        /// </summary>
        /// <seealso cref="StartTextComposition" />
        /// <seealso cref="StopTextComposition" />
        public event EventHandler<char> TextInput;

        /// <summary>
        /// Trigger a text input event.
        /// </summary>
        /// <param name="character"></param>
        protected virtual void OnTextInput(char character)
        {
            TextInput?.Invoke(this, character);
        }
    }
}