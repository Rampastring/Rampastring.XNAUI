using System;

namespace Rampastring.XNAUI.PlatformSpecific
{
    internal interface IGameWindowManager
    {
        event EventHandler GameWindowClosing;

        void AllowClosing();
        void CenterOnScreen();
        void FlashWindow();
        IntPtr GetWindowHandle();
        void HideWindow();
        void MaximizeWindow();
        void MinimizeWindow();
        void PreventClosing();
        void SetBorderlessMode(bool value);
        void SetControlBox(bool value);
        void SetIcon(string path);
        void ShowWindow();
        bool HasFocus();
    }
}