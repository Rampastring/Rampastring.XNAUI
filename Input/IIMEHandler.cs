using Rampastring.XNAUI.XNAControls;
using System;

namespace Rampastring.XNAUI.Input;

/// <summary>
/// Interface for outside components implementing Input Method Editor (IME) support.
/// </summary>
public interface IIMEHandler
{
    /// <summary>
    /// Determines whether the IME (not the IME handler) is enabled.
    /// </summary>
    bool Enabled { get; }

    void RegisterXNATextBox(XNATextBox sender, Action<char> handleCharInput);

    void KillXNATextBox(XNATextBox sender);

    void OnSelectedChanged(XNATextBox sender);

    bool HandleCharacterInput(XNATextBox sender);

    bool HandleScrollLeftKey(XNATextBox sender);
    bool HandleScrollRightKey(XNATextBox sender);

    bool HandleBackspaceKey(XNATextBox sender);
    bool HandleDeleteKey(XNATextBox sender);
    bool HandleEnterKey(XNATextBox sender);
    bool HandleEscapeKey(XNATextBox sender);

    bool GetDrawCompositionText(XNATextBox sender, out string composition, out int compositionCursorPosition);
}
