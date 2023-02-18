namespace Rampastring.XNAUI;

using Microsoft.Xna.Framework.Graphics;

internal sealed class RenderContext
{
    public RenderContext(RenderTarget2D renderTarget, RenderContext previousContext)
    {
        RenderTarget = renderTarget;
        PreviousContext = previousContext;
    }

    public RenderTarget2D RenderTarget { get; }

    public RenderContext PreviousContext { get; }
}