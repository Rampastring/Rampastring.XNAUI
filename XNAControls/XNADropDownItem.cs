using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Rampastring.XNAUI.XNAControls;

/// <summary>
/// A drop-down / context menu item.
/// </summary>
public class XNADropDownItem
{
    public Color? TextColor { get; set; }

    public Texture2D Texture { get; set; }

    public string Text { get; set; }

    /// <summary>
    /// An object for containing custom info in the drop down item.
    /// </summary>
    public object Tag { get; set; }
    public bool Selectable { get; set; } = true;

    public float Alpha
    {
        get => field;
        set
        {
            if (value < 0.0f)
                field = 0.0f;
            else if (value > 1.0f)
                field = 1.0f;
            else
                field = value;
        }
    } = 1.0f;
}
