using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Rampastring.XNAUI
{
    public class Cursor : DrawableGameComponent
    {
        public Cursor(Game game)
            : base(game)
        {
            previousMouseState = Mouse.GetState();
            RemapColor = Color.White;
        }

        public Point Location { get; set; }
        Point DrawnLocation { get; set; }

        public bool HasMoved { get; set; }

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

            location = new Point((int)(location.X / WindowManager.Instance.ScaleRatio), (int)(location.Y / WindowManager.Instance.ScaleRatio));
            location = location - new Point(WindowManager.Instance.SceneXPosition, WindowManager.Instance.SceneYPosition);

            HasMoved = (location != Location);

            Location = location;

            ScrollWheelValue = (ms.ScrollWheelValue - previousMouseState.ScrollWheelValue) / 120;

            LeftPressed = ms.LeftButton == ButtonState.Pressed;

            LeftClicked = !LeftPressed && previousMouseState.LeftButton == ButtonState.Pressed;

            RightClicked = ms.RightButton == ButtonState.Released && previousMouseState.RightButton == ButtonState.Pressed;

            previousMouseState = ms;
        }

        public override void Draw(GameTime gameTime)
        {
            Texture2D texture = Textures[TextureIndex];

            Renderer.DrawTexture(texture,
                new Rectangle(DrawnLocation, new Point(texture.Width, texture.Height)), RemapColor);
        }
    }
}
