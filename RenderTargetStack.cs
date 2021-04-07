using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rampastring.XNAUI
{
    /// <summary>
    /// Handles render targets.
    /// </summary>
    internal static class RenderTargetStack
    {
        public static void Initialize(RenderTarget2D finalRenderTarget, GraphicsDevice _graphicsDevice)
        {
            FinalRenderTarget = finalRenderTarget;
            graphicsDevice = _graphicsDevice;
            
            currentContext = new RenderContext(finalRenderTarget, null);
        }

        internal static void InitDetachedScaledControlRenderTarget(int renderWidth, int renderHeight)
        {
            if (DetachedScaledControlRenderTarget != null)
            {
                DetachedScaledControlRenderTarget.Dispose();
            }

            DetachedScaledControlRenderTarget = new RenderTarget2D(graphicsDevice,
                renderWidth, renderHeight,
                false, SurfaceFormat.Color,
                DepthFormat.None, 0,
                RenderTargetUsage.DiscardContents);
        }

        public static RenderTarget2D FinalRenderTarget { get; internal set; }

        /// <summary>
        /// A render target for controls that
        /// are detached and have scaling applied to them.
        /// </summary>
        public static RenderTarget2D DetachedScaledControlRenderTarget { get; internal set; }

        private static RenderContext currentContext;

        private static GraphicsDevice graphicsDevice;

        public static void PushRenderTarget(RenderTarget2D renderTarget)
        {
            SpriteBatchSettings newSettings =
                new SpriteBatchSettings(SpriteSortMode.Deferred, BlendState.AlphaBlend, null);
            PushRenderTarget(renderTarget, newSettings);
        }

        public static void PushRenderTarget(RenderTarget2D renderTarget, SpriteBatchSettings newSettings)
        {
            Renderer.EndDraw();
            RenderContext context = new RenderContext(renderTarget, currentContext);
            currentContext = context;
            graphicsDevice.SetRenderTarget(renderTarget);
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
            graphicsDevice.SetRenderTarget(currentContext.RenderTarget);
            Renderer.BeginDraw();
        }
    }

    internal class RenderContext
    {
        public RenderContext(RenderTarget2D renderTarget, RenderContext previousContext)
        {
            RenderTarget = renderTarget;
            PreviousContext = previousContext;
        }

        public RenderTarget2D RenderTarget { get; }
        public RenderContext PreviousContext { get; }
    }
}
