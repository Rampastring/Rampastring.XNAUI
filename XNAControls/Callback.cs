using System;

namespace Rampastring.XNAUI.XNAControls;

/// <summary>
/// A callback for storing a delegate and its parameters.
/// </summary>
internal class Callback
{
    public Callback(Delegate d, object[] args)
    {
        this.d = d;
        this.arguments = args;
    }

    private Delegate d;
    private object[] arguments;

    public void Invoke()
    {
        //Logger.Log("Executing callback " + d.Method.Name);
        d.DynamicInvoke(arguments);
    }
}
