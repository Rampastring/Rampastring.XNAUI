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

    void RegisterXNATextBox(XNATextBox box, Action<char> handleCharInput);

    void KillXNATextBox(XNATextBox box);

    void OnXNATextBoxSelectedChanged(XNATextBox sender);

    bool ShouldIMEHandleCharacterInput(XNATextBox sender);

    bool ShouldIMEHandleScrollKey(XNATextBox sender);

    bool ShouldIMEHandleBackspaceOrDeleteKey_WithSideEffect(XNATextBox sender);

    bool ShouldDrawCompositionText(XNATextBox sender, out string composition, out int compositionCursorPosition);
}
