using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rampastring.XNAUI.XNAControls
{
    public enum ControlDrawMode
    {
        /// <summary>
        /// The control is drawn on the same render target with its parent.
        /// </summary>
        NORMAL,

        /// <summary>
        /// The control is drawn on its own render target.
        /// </summary>
        UNIQUE_RENDER_TARGET
    }
}
