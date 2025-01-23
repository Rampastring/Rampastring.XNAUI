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
    /// Determines whether the IME (not the IME handler) is enabled.
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

    void OnXNATextBoxSelectedChanged(XNATextBox sender);

    bool ShouldIMEHandleCharacterInput(XNATextBox sender);

    bool ShouldIMEHandleScrollKey(XNATextBox sender);

    bool ShouldIMEHandleBackspaceOrDeleteKey_WithSideEffect(XNATextBox sender);

}
