namespace Rampastring.XNAUI.XNAControls;

public enum PanelBackgroundImageDrawMode
{
    /// <summary>
    /// The texture is tiled to fill the whole surface of the panel.
    /// </summary>
    TILED,

    /// <summary>
    /// The texture is stretched to fill the whole surface of the panel.
    /// </summary>
    STRETCHED,

    /// <summary>
    /// The texture is drawn once, centered on the panel.
    /// If the texture is too large for the panel, parts
    /// that would end up outside of the panel are cut off.
    /// </summary>
    CENTERED
}