namespace Rampastring.XNAUI.XNAControls;
using System;

/// <summary>
/// A callback for storing a delegate and its parameters.
/// </summary>
internal sealed class Callback
{
    public Callback(Delegate d, object[] args)
    {
        this.d = d;
        arguments = args;
    }

    private readonly Delegate d;
    private readonly object[] arguments;

    public void Invoke() => d.DynamicInvoke(arguments);
}