using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;

namespace Rampastring.XNAUI.Input
{
    public class Cursor : DrawableGameComponent
    {
        public Cursor(WindowManager windowManager)
            : base(windowManager.Game)
        {
            previousMouseState = Mouse.GetState();
            RemapColor = Color.White;
            this.windowManager = windowManager;
        }

        public event EventHandler LeftClickEvent;

        public Point Location { get; set; }
        Point DrawnLocation { get; set; }

        public bool HasMoved { get; set; }
        public bool IsOnScreen { get; set; }

        public Texture2D[] Textures;

        public int TextureIndex { get; set; }

        MouseState previousMouseState;
        public bool LeftClicked { get; set; }

        public bool RightClicked { get; set; }

        public bool LeftPressed { get; set; }

        public bool HasFocus { get; set; }

        public bool Disabled { get; set; }

        public int ScrollWheelValue { get; set; }

        public Color RemapColor { get; set; }

        WindowManager windowManager;

        public override void Initialize()
        {
        }

        public override void Update(GameTime gameTime)
        {
            MouseState ms = Mouse.GetState();

#if WINDOWSGL
            DrawnLocation = new Point(ms.X, ms.Y) - Game.Window.ClientBounds.Location;
#else
            DrawnLocation = new Point(ms.X, ms.Y);
#endif

            if (!HasFocus || Disabled)
            {
                LeftClicked = false;
                RightClicked = false;
                LeftPressed = false;
                return;
            }

            Point location = DrawnLocation;

            //if (location.X < 0 || location.Y < 0 ||
            //    location.X > windowManager.ResolutionWidth ||
            //    location.X > windowManager.ResolutionHeight)
            //{
            //    IsOnScreen = false;
            //}
            //else
                IsOnScreen = true;

            location = new Point(location.X - windowManager.SceneXPosition, location.Y - windowManager.SceneYPosition);
            location = new Point((int)(location.X / windowManager.ScaleRatio), (int)(location.Y / windowManager.ScaleRatio));

            HasMoved = (location != Location);

            Location = location;

            ScrollWheelValue = (ms.ScrollWheelValue - previousMouseState.ScrollWheelValue) / 40;

            LeftPressed = ms.LeftButton == ButtonState.Pressed;

            LeftClicked = !LeftPressed && previousMouseState.LeftButton == ButtonState.Pressed;

            if (LeftClicked)
                LeftClickEvent?.Invoke(this, EventArgs.Empty);

            RightClicked = ms.RightButton == ButtonState.Released && previousMouseState.RightButton == ButtonState.Pressed;

            previousMouseState = ms;
        }

        public override void Draw(GameTime gameTime)
        {
            Texture2D texture = Textures[TextureIndex];

            Renderer.DrawTexture(texture,
                new Rectangle(DrawnLocation.X, DrawnLocation.Y, texture.Width, texture.Height), RemapColor);
        }
    }
}
