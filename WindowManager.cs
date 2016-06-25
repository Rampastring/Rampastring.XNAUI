using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Rampastring.XNAUI.XNAControls;
using Rampastring.Tools;
using Microsoft.Xna.Framework.Content;
using System.Drawing;
using Color = Microsoft.Xna.Framework.Color;
using Rectangle = Microsoft.Xna.Framework.Rectangle;
using System.IO;
using Rampastring.XNAUI.Input;

namespace Rampastring.XNAUI
{
    public class WindowManager : DrawableGameComponent
    {
        public WindowManager(Game game, GraphicsDeviceManager graphics) : base(game)
        {
            this.graphics = graphics;
        }

        public delegate void GameFormClosingEventHandler(object sender, FormClosingEventArgs e);
        public event GameFormClosingEventHandler GameFormClosing;

        public Input.Cursor Cursor;
        public RKeyboard Keyboard;

        List<XNAControl> Controls = new List<XNAControl>();

        List<Callback> Callbacks = new List<Callback>();

        private readonly object locker = new object();

        int resolutionWidth = 800;
        int resolutionHeight = 600;
        public int ResolutionWidth
        {
            get { return resolutionWidth; }
        }

        public int ResolutionHeight
        {
            get { return resolutionHeight; }
        }

        int renderResX = 800;
        int renderResY = 600;

        public int RenderResolutionX
        {
            get { return renderResX; }
        }

        public int RenderResolutionY
        {
            get { return renderResY; }
        }

        bool _hasFocus = true;
        public bool HasFocus
        {
            get { return _hasFocus; }
        }

        public double ScaleRatio = 1.0;

        public int SceneXPosition = 0;
        public int SceneYPosition = 0;

        GraphicsDeviceManager graphics;

        Form gameForm;
        RenderTarget2D renderTarget;

        public void SetRenderResolution(int x, int y)
        {
            renderResX = x;
            renderResY = y;

            double xRatio = (resolutionWidth) / (double)x;
            double yRatio = (resolutionHeight) / (double)y;

            double ratio;

            int texturePositionX = 0;
            int texturePositionY = 0;
            int textureHeight = 0;
            int textureWidth = 0;

            if (xRatio > yRatio)
            {
                ratio = yRatio;
                textureHeight = resolutionHeight;
                textureWidth = (int)(x * ratio);
                texturePositionX = (int)(resolutionWidth - textureWidth) / 2;
            }
            else
            {
                ratio = xRatio;
                textureWidth = resolutionWidth;
                textureHeight = (int)(y * ratio);
                texturePositionY = (int)(resolutionHeight - textureHeight) / 2;
            }

            ScaleRatio = ratio;
            SceneXPosition = texturePositionX;
            SceneYPosition = texturePositionY;

            Logger.Log("Scale ratio: " + ScaleRatio);

            renderTarget = new RenderTarget2D(GraphicsDevice, renderResX, renderResY, false, SurfaceFormat.Color,
                DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
        }

        public void Initialize(ContentManager content, string contentPath)
        {
            Cursor = new Input.Cursor(this);
            Keyboard = new RKeyboard(Game);
            Renderer.Initialize(GraphicsDevice, content, contentPath);

            KeyboardEventInput.Initialize(Game.Window);

            gameForm = (Form)Form.FromHandle(Game.Window.Handle);

            if (gameForm != null)
            {
                gameForm.FormClosing += GameForm_FormClosing_Event;
            }
        }

        /// <summary>
        /// Schedules a delegate to be executed on the next game loop frame, 
        /// on the main game thread.
        /// </summary>
        /// <param name="d">The delegate.</param>
        /// <param name="args">The arguments to be passed on to the delegate.</param>
        public void AddCallback(Delegate d, params object[] args)
        {
            lock (locker)
                Callbacks.Add(new Callback(d, args));
        }

        private void GameForm_FormClosing_Event(object sender, FormClosingEventArgs e)
        {
            if (GameFormClosing != null)
                GameFormClosing(this, e);
        }

        /// <summary>
        /// Adds a control into the WindowManager, on the last place
        /// in the list of controls.
        /// </summary>
        /// <param name="control">The control to add.</param>
        public void AddAndInitializeControl(XNAControl control)
        {
            if (Controls.Contains(control))
            {
                throw new Exception("WindowManager.AddAndInitializeControl: Control " + control.Name + " already exists!");
            }
            
            control.Initialize();
            Controls.Add(control);
        }

        /// <summary>
        /// Inserts a control into the WindowManager on the first place
        /// in the list of controls.
        /// </summary>
        /// <param name="control">The control to insert.</param>
        public void InsertAndInitializeControl(XNAControl control)
        {
            if (Controls.Contains(control))
            {
                throw new Exception("WindowManager.InsertAndInitializeControl: Control " + control.Name + " already exists!");
            }

            Controls.Insert(0, control);
        }

        public void CenterControlOnScreen(XNAControl control)
        {
            control.ClientRectangle = new Rectangle((RenderResolutionX - control.ClientRectangle.Width) / 2,
                (RenderResolutionY - control.ClientRectangle.Height) / 2, control.ClientRectangle.Width, control.ClientRectangle.Height);
        }

        public void CenterOnScreen()
        {
            if (gameForm == null)
                return;

            gameForm.DesktopLocation = new System.Drawing.Point(
                (Screen.PrimaryScreen.Bounds.Width - gameForm.Size.Width) / 2,
                (Screen.PrimaryScreen.Bounds.Height - gameForm.Size.Height) / 2);
        }

        public void SetBorderlessMode(bool value)
        {
            if (gameForm == null)
                return;

            if (value)
                gameForm.FormBorderStyle = FormBorderStyle.None;
            else
                gameForm.FormBorderStyle = FormBorderStyle.FixedSingle;
        }

        public void MinimizeWindow()
        {
            if (gameForm == null)
                return;

            gameForm.WindowState = FormWindowState.Minimized;
        }

        public void MaximizeWindow()
        {
            if (gameForm == null)
                return;

            gameForm.WindowState = FormWindowState.Normal;
        }

        public void HideWindow()
        {
            if (gameForm == null)
                return;

            gameForm.Hide();
        }

        public void ShowWindow()
        {
            if (gameForm == null)
                return;

            gameForm.Show();
        }

        public void FlashWindow()
        {
            if (gameForm == null)
                return;

            WindowFlasher.FlashWindowEx(gameForm);
        }

        public void SetIcon(string path)
        {
            if (gameForm == null)
                return;

            gameForm.Icon = Icon.ExtractAssociatedIcon(path);
        }

        public void SetWindowTitle(string title)
        {
            if (gameForm == null)
                return;

            gameForm.Text = title;
        }

        public IntPtr GetWindowHandle()
        {
            if (gameForm == null)
                return IntPtr.Zero;

            return gameForm.Handle;
        }

        /// <summary>
        /// Enables or disables the "control box" (minimize/maximize/close buttons) for the game form.
        /// </summary>
        /// <param name="value">True to enable the control box, false to disable it.</param>
        public void SetControlBox(bool value)
        {
            if (gameForm == null)
                return;

            gameForm.ControlBox = value;
        }

        public void PreventClosing()
        {
            if (gameForm == null)
                return;

            gameForm.FormClosing += GameForm_FormClosing;
        }

        private void GameForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            e.Cancel = true;
        }

        public void AllowClosing()
        {
            if (gameForm == null)
                return;

            gameForm.FormClosing -= GameForm_FormClosing;
        }

        public void RemoveControl(XNAControl control)
        {
            Controls.Remove(control);
        }

        public void SetVSync(bool value)
        {
            graphics.SynchronizeWithVerticalRetrace = value;
        }

        public void SetFinalRenderTarget()
        {
            GraphicsDevice.SetRenderTarget(renderTarget);
        }

        /// <summary>
        /// Attempt to set the display mode to the desired resolution.  Itterates through the display
        /// capabilities of the default graphics adapter to determine if the graphics adapter supports the
        /// requested resolution.  If so, the resolution is set and the function returns true.  If not,
        /// no change is made and the function returns false.
        /// </summary>
        /// <param name="iWidth">Desired screen width.</param>
        /// <param name="iHeight">Desired screen height.</param>
        /// <param name="bFullScreen">True if you wish to go to Full Screen, false for Windowed Mode.</param>
        public bool InitGraphicsMode(int iWidth, int iHeight, bool bFullScreen)
        {
            Logger.Log("InitGraphicsMode: " + iWidth + "x" + iHeight);
            resolutionWidth = iWidth;
            resolutionHeight = iHeight;
            // If we aren't using a full screen mode, the height and width of the window can
            // be set to anything equal to or smaller than the actual screen size.
            if (bFullScreen == false)
            {
                if ((iWidth <= GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width)
                    && (iHeight <= GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height))
                {
                    graphics.PreferredBackBufferWidth = iWidth;
                    graphics.PreferredBackBufferHeight = iHeight;
                    graphics.IsFullScreen = bFullScreen;
                    graphics.ApplyChanges();
                    return true;
                }
            }
            else
            {
                // If we are using full screen mode, we should check to make sure that the display
                // adapter can handle the video mode we are trying to set.  To do this, we will
                // iterate thorugh the display modes supported by the adapter and check them against
                // the mode we want to set.
                foreach (DisplayMode dm in GraphicsAdapter.DefaultAdapter.SupportedDisplayModes)
                {
                    // Check the width and height of each mode against the passed values
                    if ((dm.Width == iWidth) && (dm.Height == iHeight))
                    {
                        // The mode is supported, so set the buffer formats, apply changes and return
                        graphics.PreferredBackBufferWidth = iWidth;
                        graphics.PreferredBackBufferHeight = iHeight;
                        graphics.IsFullScreen = bFullScreen;
                        graphics.ApplyChanges();
                        return true;
                    }
                }
            }
            return false;
        }

        public override void Update(GameTime gameTime)
        {
#if WINDOWSGL
            _hasFocus = true;
#else
            _hasFocus = (System.Windows.Forms.Form.ActiveForm != null);
#endif
            Cursor.HasFocus = _hasFocus;

            lock (locker)
            {
                foreach (Callback c in Callbacks)
                    c.Invoke();

                Callbacks.Clear();
            }

            XNAControl activeControl = null;

            if (_hasFocus)
                Keyboard.Update(gameTime);

            Cursor.Update(gameTime);

            for (int i = Controls.Count - 1; i > -1; i--)
            {
                XNAControl control = Controls[i];

                if (_hasFocus && control.Visible && 
                    (activeControl == null &&
                    control.ClientRectangle.Contains(Cursor.Location)
                    ||
                    control.Focused))
                {
                    control.IsActive = true;
                    activeControl = control;
                }
                else
                    control.IsActive = false;

                if (control.Enabled)
                    control.Update(gameTime);
            }

            base.Update(gameTime);
        }

        public override void Draw(GameTime gameTime)
        {
            GraphicsDevice.SetRenderTarget(renderTarget);

            GraphicsDevice.Clear(Color.Black);

            Renderer.BeginDraw();

            for (int i = 0; i < Controls.Count; i++)
            {
                if (Controls[i].Visible)
                    Controls[i].Draw(gameTime);
            }

            Renderer.EndDraw();

            if (Keyboard.PressedKeys.Contains(Microsoft.Xna.Framework.Input.Keys.F12))
            {
                FileStream fs = File.Create(Environment.CurrentDirectory + "\\image.png");
                renderTarget.SaveAsPng(fs, renderTarget.Width, renderTarget.Height);
                fs.Close();
            }

            GraphicsDevice.SetRenderTarget(null);

            GraphicsDevice.Clear(Color.Black);

            Renderer.BeginDraw();

            //Renderer.DrawTexture(renderTarget, Vector2.Zero, 0.02f, Vector2.Zero, new Vector2(1f, 1f), Color.White);

            Renderer.DrawTexture(renderTarget, new Rectangle(SceneXPosition, SceneYPosition,
                resolutionWidth - (SceneXPosition * 2), resolutionHeight - (SceneYPosition * 2)), Color.White);

            //if (!String.IsNullOrEmpty(activeControlText))
            //    Renderer.DrawStringWithShadow(activeControlText, 0, Vector2.Zero, Color.White);

            if (Cursor.Visible)
                Cursor.Draw(gameTime);

            Renderer.EndDraw();

            base.Draw(gameTime);
        }
    }
}
