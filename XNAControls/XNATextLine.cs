namespace Rampastring.XNAUI.XNAControls;

using System.Collections.Generic;

/// <summary>
/// A text line.
/// </summary>
internal readonly struct XNATextLine
{
    public XNATextLine(List<XNATextPart> parts)
    {
        Parts = parts;
    }

    public List<XNATextPart> Parts { get; }

    public int Width
    {
        get
        {
            int width = 0;
            Parts.ForEach(p => width += p.Width);
            return width;
        }
    }

    public int Height
    {
        get
        {
            int height = 0;
            Parts.ForEach(p => height = p.Height > height ? p.Height : height);
            return height;
        }
    }

    public void AddPart(XNATextPart part) => Parts.Add(part);
}