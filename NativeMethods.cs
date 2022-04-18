using System;
using System.Runtime.InteropServices;

namespace Rampastring.XNAUI
{
    class NativeMethods
    {
        [DllImport("user32.dll", CharSet = CharSet.Unicode, ThrowOnUnmappableChar = true)]
        static extern IntPtr LoadCursorFromFile(string lpFileName);

        public static IntPtr LoadCursor(string filePath)
        {
            return LoadCursorFromFile(filePath);
        }
    }
}
