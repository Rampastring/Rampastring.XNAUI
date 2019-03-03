using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Rampastring.Tools;
using System;

namespace Rampastring.XNAUI.XNAControls
{
    public class XNAPanel : XNAControl
    {
        public XNAPanel(WindowManager windowManager) : base(windowManager)
        {
        }

        public PanelBackgroundImageDrawMode PanelBackgroundDrawMode = PanelBackgroundImageDrawMode.STRETCHED;

        public virtual Texture2D BackgroundTexture { get; set; }

        private Color? _borderColor;

        public Color BorderColor
        {
            get
            {
                if (_borderColor.HasValue)
                    return _borderColor.Value;

                return UISettings.ActiveSettings.PanelBorderColor;
            }
            set { _borderColor = value; }
        }

        public bool DrawBorders { get; set; } = true;

        /// <summary>
        /// If this is set, the XNAPanel will render itself on a separate render target.
        /// After the rendering is complete, it'll set this render target to be the
        /// primary render target.
        /// </summary>
        //public RenderTarget2D OriginalRenderTarget { get; set; }

        //RenderTarget2D renderTarget;

        Texture2D BorderTexture { get; set; }

        /// <summary>
        /// The panel's transparency changing rate per 100 milliseconds.
        /// If the panel is transparent, it'll become non-transparent at this rate.
        /// </summary>
        public float AlphaRate = 0.0f;

        public override void Initialize()
        {
            base.Initialize();

            BorderTexture = AssetLoader.CreateTexture(Color.White, 1, 1);

            //renderTarget = new RenderTarget2D(GraphicsDevice, 
            //    WindowManager.Instance.RenderResolutionX, 
            //    WindowManager.Instance.RenderResolutionY);
        }

        protected override void ParseAttributeFromINI(IniFile iniFile, string key, string value)
        {
            switch (key)
            {
                case "BorderColor":
                    BorderColor = AssetLoader.GetColorFromString(value);
                    return;
                case "DrawMode":
                    if (value == "Tiled")
                        PanelBackgroundDrawMode = PanelBackgroundImageDrawMode.TILED;
                    else
                        PanelBackgroundDrawMode = PanelBackgroundImageDrawMode.STRETCHED;
                    return;
                case "AlphaRate":
                    AlphaRate = Conversions.FloatFromString(value, 0.01f);
                    return;
                case "BackgroundTexture":
                    BackgroundTexture = AssetLoader.LoadTexture(value);
                    return;
                case "DrawBorders":
                    DrawBorders = Conversions.BooleanFromString(value, true);
                    return;
                case "Padding":
                    string[] parts = value.Split(',');
                    int left = Int32.Parse(parts[0]);
                    int top = Int32.Parse(parts[1]);
                    int right = Int32.Parse(parts[2]);
                    int bottom = Int32.Parse(parts[3]);
                    ClientRectangle = new Rectangle(X - left, Y - top,
                        Width + left + right, Height + top + bottom);
                    foreach (XNAControl child in Children)
                    {
                        child.ClientRectangle = new Rectangle(child.X + left,
                            child.Y + top, child.Width, child.Height);
                    }
                    return;
            }

            base.ParseAttributeFromINI(iniFile, key, value);
        }

        public override void Update(GameTime gameTime)
        {
            Alpha += AlphaRate * (float)(gameTime.ElapsedGameTime.TotalMilliseconds / 100.0);

            base.Update(gameTime);
        }

        protected void DrawPanel()
        {
            Color color = RemapColor;

            if (BackgroundTexture != null)
            {
                if (PanelBackgroundDrawMode == PanelBackgroundImageDrawMode.TILED)
                {
                    if (Renderer.CurrentSettings.SamplerState != SamplerState.LinearWrap &&
                        Renderer.CurrentSettings.SamplerState != SamplerState.PointWrap)
                    {
                        Renderer.PushSettings(new SpriteBatchSettings(Renderer.CurrentSettings.SpriteSortMode,
                            Renderer.CurrentSettings.BlendState, SamplerState.LinearWrap));

                        DrawTexture(BackgroundTexture, new Rectangle(0, 0, Width, Height), color);

                        Renderer.PopSettings();
                    }
                    else
                    {
                        DrawTexture(BackgroundTexture, new Rectangle(0, 0, Width, Height), color);
                    }
                }
                else
                {
                    DrawTexture(BackgroundTexture, new Rectangle(0, 0, Width, Height), color);
                }
            }
        }

        protected void DrawPanelBorders()
        {
            DrawRectangle(new Rectangle(0, 0, Width, Height), BorderColor);
        }

        public override void Draw(GameTime gameTime)
        {
            DrawPanel();

            if (DrawBorders)
                DrawPanelBorders();

            base.Draw(gameTime);
        }

        public override Color GetColorWithAlpha(Color baseColor)
        {
            if (Parent == null)
                return base.GetColorWithAlpha(baseColor);

            return new Color(baseColor.R, baseColor.G, baseColor.B, Math.Min((int)(Math.Pow(Alpha, 0.5) * 255.0f), 255));
        }
    }

    public enum PanelBackgroundImageDrawMode
    {
        TILED,
        STRETCHED
    }
}
