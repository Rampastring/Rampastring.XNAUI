// MonoGame.IMEHelper.Common
//   Copyright (c) 2020 ryancheung

using System;

using Microsoft.Xna.Framework;

using Rampastring.XNAUI.PlatformSpecific;

namespace Rampastring.XNAUI.Input;

internal abstract class IMEHandler
{
    private string composition = string.Empty;

    /// <summary>
    /// Check if text composition is enabled currently.
    /// </summary>
    public abstract bool Enabled { get; protected set; }

    /// <summary>
    /// Composition String
    /// </summary>
    public virtual string Composition
    {
        get => composition;
        protected set
        {
            string old = composition;
            composition = value;
            CompositionChanged?.Invoke(null, new(old, value));
        }
    }
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
    public event EventHandler<CompositionChangedEventArgs> CompositionChanged;

    public static IMEHandler Create(Game game)
    {
#if !GL
        return new WinFormsIMEHandler(game);
#else
        return new SdlIMEHandler(game);
#endif
    }

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
    /// Trigger a text input event.
    /// </summary>
    /// <param name="character"></param>
    protected virtual void OnTextInput(char character)
        => TextInput?.Invoke(this, character);
}
