namespace Rampastring.XNAUI.Input;

using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Globalization;
using Windows.Win32.UI.WindowsAndMessaging;

/// <summary>
/// Handles text input. XNA does not have a built-in system for text input.
/// Note: MonoGame has a way of detecting text input,
/// via the Game.Window.TextInput event:
/// http://www.gamedev.net/topic/457783-xna-getting-text-from-keyboard/.
/// </summary>
public static class KeyboardEventInput
{
    public delegate void CharEnteredHandler(object sender, KeyboardEventArgs e);

    /// <summary>
    /// Event raised when a character has been entered.
    /// </summary>
    public static event CharEnteredHandler CharEntered;

    private static bool initialized;
    private static nint prevWndProc;
    private static WNDPROC hookProcDelegate;
    private static HIMC hIMC;

    /// <summary>
    /// Initialize the TextInput with the given GameWindow.
    /// </summary>
    /// <param name="window">The XNA window to which text input should be linked.</param>
#if !NETFRAMEWORK
    [System.Runtime.Versioning.SupportedOSPlatform("windows5.1.2600")]
#endif
    public static void Initialize(GameWindow window)
    {
        if (initialized)
            throw new InvalidOperationException("KeyboardEventInput.Initialize can only be called once!");

        hookProcDelegate = HookProc;

        nint result = PInvoke.SetWindowLongA((HWND)window.Handle, WINDOW_LONG_PTR_INDEX.GWL_WNDPROC, (int)Marshal.GetFunctionPointerForDelegate(hookProcDelegate));

        if (result == IntPtr.Zero)
            throw new Win32Exception(Marshal.GetLastWin32Error());

        prevWndProc = result;
        hIMC = PInvoke.ImmGetContext((HWND)window.Handle);
        initialized = true;
    }

    private static LRESULT HookProc(HWND windowHandle, uint msg, WPARAM param1, LPARAM param2)
    {
        Delegate xnaDelegate = Marshal.GetDelegateForFunctionPointer(prevWndProc, typeof(Action));
        var wndProcDelegate = (WndProcDelegate)Delegate.CreateDelegate(typeof(WndProcDelegate), xnaDelegate.Target, xnaDelegate.Method, false);
        LRESULT returnCode = PInvoke.CallWindowProcW(wndProcDelegate, windowHandle, msg, param1, param2);

        switch (msg)
        {
            case PInvoke.WM_GETDLGCODE:
                returnCode = (LRESULT)(returnCode | PInvoke.DLGC_WANTALLKEYS);
                break;

            case PInvoke.WM_CHAR:
                CharEntered?.Invoke(null, new((char)param1.Value, (int)param2.Value));
                break;

            case PInvoke.WM_IME_SETCONTEXT:
                if (param1 == 1)
                    PInvoke.ImmAssociateContext(windowHandle, hIMC);
                break;

            case PInvoke.WM_INPUTLANGCHANGE:
                PInvoke.ImmAssociateContext(windowHandle, hIMC);
                returnCode = (LRESULT)1;
                break;
        }

        return returnCode;
    }
}