using Rampastring.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Rampastring.XNAUI.DXControls
{
    /// <summary>
    /// A callback for storing a delegate and its parameters.
    /// </summary>
    class Callback
    {
        public Callback(Delegate d, object[] args)
        {
            this.d = d;
            this.arguments = args;
        }

        Delegate d;

        object[] arguments;

        public void Invoke()
        {
            Logger.Log("Executing callback " + d.Method.Name);
            d.DynamicInvoke(arguments);
        }
    }
}
