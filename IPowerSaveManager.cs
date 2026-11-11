namespace Rampastring.XNAUI;

/// <summary>
/// Receives redraw requests from the window manager and its controls.
/// </summary>
public interface IPowerSaveManager
{
    /// <summary>
    /// Requests the game to draw a frame at the next normal drawing opportunity.
    /// Multiple requests before a draw can be satisfied by the same frame.
    /// </summary>
    void RequestRedraw();
}
