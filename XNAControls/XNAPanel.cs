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
            BorderColor = UISettings.PanelBorderColor;
        }

        public PanelBackgroundImageDrawMode DrawMode = PanelBackgroundImageDrawMode.TILED;

        public virtual Texture2D BackgroundTexture { get; set; }

        public Color BorderColor { get; set; }

        bool _drawBorders = true;
        public bool DrawBorders
        {
            get { return _drawBorders; }
            set { _drawBorders = value; }
        }

        /// <summary>
        /// If this is set, the XNAPanel will render itself on a separate render target.
        /// After the rendering is complete, it'll set this render target to be the
        /// primary render target.
        /// </summary>
        //public RenderTarget2D OriginalRenderTarget { get; set; }

        //RenderTarget2D renderTarget;

        Texture2D BorderTexture { get; set; }

        public float AlphaRate = 0.01f;

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
                        DrawMode = PanelBackgroundImageDrawMode.TILED;
                    else
                        DrawMode = PanelBackgroundImageDrawMode.STRETCHED;
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
                    ClientRectangle = new Rectangle(ClientRectangle.X - left, ClientRectangle.Y - top,
                        ClientRectangle.Width + left + right, ClientRectangle.Height + top + bottom);
                    foreach (XNAControl child in Children)
                    {
                        child.ClientRectangle = new Rectangle(child.ClientRectangle.X + left,
                            child.ClientRectangle.Y + top, child.ClientRectangle.Width, child.ClientRectangle.Height);
                    }
                    return;
            }

            base.ParseAttributeFromINI(iniFile, key, value);
        }

        public override void Update(GameTime gameTime)
        {
            Alpha += AlphaRate;

            base.Update(gameTime);
        }

        protected void DrawPanel()
        {
            Color color = GetRemapColor();

            Rectangle windowRectangle = WindowRectangle();

            if (BackgroundTexture != null)
            {
                if (DrawMode == PanelBackgroundImageDrawMode.TILED)
                {
                    for (int x = 0; x < windowRectangle.Width; x += BackgroundTexture.Width)
                    {
                        for (int y = 0; y < windowRectangle.Height; y += BackgroundTexture.Height)
                        {
                            if (x + BackgroundTexture.Width < windowRectangle.Width)
                            {
                                if (y + BackgroundTexture.Height < windowRectangle.Height)
                                {
                                    Renderer.DrawTexture(BackgroundTexture, new Rectangle(windowRectangle.X + x, windowRectangle.Y + y,
                                        BackgroundTexture.Width, BackgroundTexture.Height), color);
                                }
                                else
                                {
                                    Renderer.DrawTexture(BackgroundTexture,
                                        new Rectangle(0, 0, BackgroundTexture.Width, windowRectangle.Height - y),
                                        new Rectangle(windowRectangle.X + x, windowRectangle.Y + y,
                                        BackgroundTexture.Width, windowRectangle.Height - y), color);
                                }
                            }
                            else if (y + BackgroundTexture.Height < windowRectangle.Height)
                            {
                                Renderer.DrawTexture(BackgroundTexture,
                                    new Rectangle(0, 0, windowRectangle.Width - x, BackgroundTexture.Height),
                                    new Rectangle(windowRectangle.X + x, windowRectangle.Y + y,
                                    windowRectangle.Width - x, BackgroundTexture.Height), color);
                            }
                            else
                            {
                                Renderer.DrawTexture(BackgroundTexture,
                                    new Rectangle(0, 0, windowRectangle.Width - x, windowRectangle.Height - y),
                                    new Rectangle(windowRectangle.X + x, windowRectangle.Y + y,
                                    windowRectangle.Width - x, windowRectangle.Height - y), color);
                            }
                        }
                    }
                }
                else
                {
                    Renderer.DrawTexture(BackgroundTexture, windowRectangle, color);
                }
            }
        }

        protected void DrawPanelBorders()
        {
            Renderer.DrawRectangle(WindowRectangle(), GetColorWithAlpha(BorderColor));
        }

        public override void Draw(GameTime gameTime)
        {
            DrawPanel();
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
