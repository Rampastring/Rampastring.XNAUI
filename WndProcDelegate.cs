namespace Rampastring.XNAUI;

using System;
using System.Runtime.InteropServices;

// Microsoft.Xna.Framework.Input.WindowMessageHooker.Hook.WndProcDelegate:
// private delegate IntPtr WndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);
[UnmanagedFunctionPointer(CallingConvention.Winapi)]
internal delegate IntPtr WndProcDelegate(IntPtr wnd, uint msg, IntPtr param1, IntPtr param2);