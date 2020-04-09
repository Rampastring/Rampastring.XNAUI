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

        public override void ParseAttributeFromINI(IniFile iniFile, string key, string value)
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
                        //Renderer.PushSettings(new SpriteBatchSettings(Renderer.CurrentSettings.SpriteSortMode,
                        //    Renderer.CurrentSettings.BlendState, SamplerState.LinearWrap));

                        //DrawTexture(BackgroundTexture, new Rectangle(0, 0, Width, Height), color);

                        //Renderer.PopSettings();
                        // ^ the above should work, but actually doesn't for some reason -
                        // the texture is just scaled instead
                        // it should have much higher performance than repeating the texture manually

                        for (int x = 0; x < Width; x += BackgroundTexture.Width)
                        {
                            for (int y = 0; y < Height; y += BackgroundTexture.Height)
                            {
                                if (x + BackgroundTexture.Width < Width)
                                {
                                    if (y + BackgroundTexture.Height < Height)
                                    {
                                        DrawTexture(BackgroundTexture, new Rectangle(x, y,
                                            BackgroundTexture.Width, BackgroundTexture.Height), color);
                                    }
                                    else
                                    {
                                        DrawTexture(BackgroundTexture,
                                            new Rectangle(0, 0, BackgroundTexture.Width, Height - y),
                                            new Rectangle(x, y,
                                            BackgroundTexture.Width, Height - y), color);
                                    }
                                }
                                else if (y + BackgroundTexture.Height < Height)
                                {
                                    DrawTexture(BackgroundTexture,
                                        new Rectangle(0, 0, Width - x, BackgroundTexture.Height),
                                        new Rectangle(x, y,
                                        Width - x, BackgroundTexture.Height), color);
                                }
                                else
                                {
                                    DrawTexture(BackgroundTexture,
                                        new Rectangle(0, 0, Width - x, Height - y),
                                        new Rectangle(x, y,
                                        Width - x, Height - y), color);
                                }
                            }
                        }
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
    }

    public enum PanelBackgroundImageDrawMode
    {
        TILED,
        STRETCHED
    }
}
