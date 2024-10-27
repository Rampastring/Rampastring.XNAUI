// MonoGame.IMEHelper.Common
//   Copyright (c) 2020 ryancheung

using System;

namespace Rampastring.XNAUI.Input;

internal sealed class CompositionChangedEventArgs(string oldValue, string newValue) : EventArgs
{
    public string OldValue => oldValue;
    public string NewValue => newValue;
}
