using System;

namespace Rampastring.XNAUI.Input
{
    public class KeyboardEventArgs : EventArgs
    {
        public KeyboardEventArgs(char character, int lParam)
        {
            this.Character = character;
            this.Param = lParam;
        }

        public char Character { get; private set; }

        public int Param { get; private set; }

        public int RepeatCount
        {
            get { return Param & 0xffff; }
        }

        public bool ExtendedKey
        {
            get { return (Param & (1 << 24)) > 0; }
        }

        public bool AltPressed
        {
            get { return (Param & (1 << 29)) > 0; }
        }

        public bool PreviousState
        {
            get { return (Param & (1 << 30)) > 0; }
        }

        public bool TransitionState
        {
            get { return (Param & (1 << 31)) > 0; }
        }
    }
}
