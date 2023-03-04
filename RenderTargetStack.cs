namespace Rampastring.XNAUI;

using System;
using Microsoft.Xna.Framework.Graphics;

/// <summary>
/// Handles render targets.
/// </summary>
internal static class RenderTargetStack
{
    public static void Initialize(RenderTarget2D finalRenderTarget, GraphicsDevice graphicsDevice)
    {
        currentGraphicsDevice = graphicsDevice;
        currentContext = new(finalRenderTarget, null);
    }

    internal static void InitDetachedScaledControlRenderTarget(int renderWidth, int renderHeight)
    {
        DetachedScaledControlRenderTarget?.Dispose();

        DetachedScaledControlRenderTarget = new(
            currentGraphicsDevice,
            renderWidth,
            renderHeight,
            false,
            SurfaceFormat.Color,
            DepthFormat.None,
            0,
            RenderTargetUsage.DiscardContents);
    }

    /// <summary>
    /// A render target for controls that
    /// are detached and have scaling applied to them.
    /// </summary>
    public static RenderTarget2D DetachedScaledControlRenderTarget { get; private set; }

    private static RenderContext currentContext;

    private static GraphicsDevice currentGraphicsDevice;

    public static void PushRenderTarget(RenderTarget2D renderTarget, SpriteBatchSettings newSettings)
    {
        Renderer.EndDraw();
        var context = new RenderContext(renderTarget, currentContext);
        currentContext = context;
        currentGraphicsDevice.SetRenderTarget(renderTarget);
        Renderer.PushSettingsInternal();
        Renderer.CurrentSettings = newSettings;
        Renderer.BeginDraw();
    }

    public static void PopRenderTarget()
    {
        currentContext = currentContext.PreviousContext;

        if (currentContext == null)
        {
            throw new InvalidOperationException("No render context left! This usually " +
                "indicates that a control with an unique render target has " +
                "double-popped their render target.");
        }

        Renderer.EndDraw();
        Renderer.PopSettingsInternal();
        currentGraphicsDevice.SetRenderTarget(currentContext.RenderTarget);
        Renderer.BeginDraw();
    }
}