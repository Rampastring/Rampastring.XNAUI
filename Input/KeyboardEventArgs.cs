namespace Rampastring.XNAUI.Input;

using System;

public class KeyboardEventArgs : EventArgs
{
    public KeyboardEventArgs(char character, int param)
    {
        Character = character;
        Param = param;
    }

    public char Character { get; }

    public int Param { get; }

    public int RepeatCount => Param & 0xffff;

    public bool ExtendedKey => (Param & (1 << 24)) > 0;

    public bool AltPressed => (Param & (1 << 29)) > 0;

    public bool PreviousState => (Param & (1 << 30)) > 0;

    public bool TransitionState => (Param & (1 << 31)) > 0;
}