namespace Rampastring.XNAUI.PlatformSpecific;

#if WINFORMS
using System;

#endif
internal interface IGameWindowManager
{
#if WINFORMS
    event EventHandler GameWindowClosing;

    void AllowClosing();

#if !NETFRAMEWORK
    [System.Runtime.Versioning.SupportedOSPlatform("windows5.1.2600")]
#endif
    void FlashWindow();

    IntPtr GetWindowHandle();

    void HideWindow();

    void MaximizeWindow();

    void MinimizeWindow();

    void PreventClosing();

    void SetControlBox(bool value);

    void SetIcon(string path);

    void ShowWindow();

#endif
    bool HasFocus();

    void CenterOnScreen();

    void SetBorderlessMode(bool value);
}