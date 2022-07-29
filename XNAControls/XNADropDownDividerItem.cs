using System;

namespace Rampastring.XNAUI.XNAControls
{
    public class XNADropDownDividerItem : XNADropDownItem
    {
        /// <summary>
        /// This centers the Y position of the line, relative to its own height.
        /// </summary>
        public int LineY => (int)Math.Ceiling((ItemHeight ?? 0) / (double)2);

        public XNADropDownDividerItem()
        {
            ItemHeight = 6;
            Selectable = false;
        }
    }
}
