using Microsoft.Xna.Framework;
using Rampastring.XNAUI.XNAControls;
using System;

namespace Rampastring.XNAUI.Input;

/// <summary>
/// Event args for an event that contains a character.
/// </summary>
public class CharacterEventArgs : EventArgs
{
    public CharacterEventArgs(char character)
    {
        Character = character;
    }

    public char Character { get; }
}

public class CompositionChangedEventArgs : EventArgs
{
    public CompositionChangedEventArgs(string oldValue, string newValue)
    {
        OldValue = oldValue;
        NewValue = newValue;
    }

    public string OldValue { get; }
    public string NewValue { get; }
}

/// <summary>
/// Interface for outside components implementing Input Method Editor (IME) support.
/// </summary>
public interface IIMEHandler
{
    /// <summary>
    /// Gets or sets the control that is currently the focus of the IME.
    /// </summary>
    XNAControl IMEFocus { get; set; }

    /// <summary>
    /// Invoke when the IMM service emits characters.
    /// </summary>
    event EventHandler<CharacterEventArgs> CharInput;

    /// <summary>
    /// Invoke when the text composition is changed.
    /// </summary>
    event EventHandler<CompositionChangedEventArgs> CompositionChanged;

    /// <summary>
    /// Determines whether the IME handler is enabled.
    /// </summary>
    bool Enabled { get; }

    /// <summary>
    /// IME text composition string.
    /// </summary>
    string Composition { get; set; }

    /// <summary>
    /// Caret position of the composition.
    /// </summary>
    int CompositionCursorPosition { get; set; }

    /// <summary>
    /// Enables the system IMM service to support composited character input.
    /// Called when the library expects text input from a user and IME is enabled.
    /// </summary>
    void StartTextComposition();

    /// <summary>
    /// Stops the system IMM service.
    /// </summary>
    void StopTextComposition();

    /// <summary>
    /// Sets the rectangle used for typing Unicode text inputs with IME.
    /// </summary>
    void SetTextInputRectangle(Rectangle rectangle);
}
