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

    void OnCharacterInput(XNATextBox sender, out bool handled);

    void OnScrollLeftKey(XNATextBox sender, out bool handled);
    void OnScrollRightKey(XNATextBox sender, out bool handled);

    void OnBackspaceKey(XNATextBox sender, out bool handled);
    void OnDeleteKey(XNATextBox sender, out bool handled);

    void OnDrawCompositionText(XNATextBox sender, out bool drawCompositionText, out string composition, out int compositionCursorPosition);
}
