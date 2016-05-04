using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Rampastring.XNAUI.DXControls;
using Rampastring.Tools;
using Microsoft.Xna.Framework.Content;
using System.Drawing;
using Color = Microsoft.Xna.Framework.Color;
using Rectangle = Microsoft.Xna.Framework.Rectangle;
using System.IO;

namespace Rampastring.XNAUI
{
    public class WindowManager : DrawableGameComponent
    {
        public WindowManager(Game game, GraphicsDeviceManager graphics) : base(game)
        {
            this.graphics = graphics;

            if (Instance != null)
                throw new Exception("WindowManager already exists!");

            Instance = this;
        }

        public delegate void GameFormClosingEventHandler(object sender, FormClosingEventArgs eventArgs);
        public event GameFormClosingEventHandler GameFormClosing;

        public static WindowManager Instance;

        public Cursor Cursor;
        public RKeyboard Keyboard;

        List<DXControl> Controls = new List<DXControl>();

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

            double intendedRatio = renderResX / (double)renderResY;
            double xyRatio = resolutionWidth / (double)resolutionHeight;

            double ratioDifference = xyRatio - intendedRatio;

            if (ratioDifference > 0.0)
            {
                SceneXPosition = (int)(ratioDifference * resolutionHeight) / 2;
                ScaleRatio = resolutionHeight / (double)renderResY;
            }
            else
            {
                SceneYPosition = (int)(-ratioDifference * resolutionWidth) / 2;
                ScaleRatio = resolutionWidth / (double)renderResX;
            }

            Logger.Log("Scale ratio: " + ScaleRatio);

            renderTarget = new RenderTarget2D(GraphicsDevice, renderResX, renderResY, false, SurfaceFormat.Color,
                DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
        }

        public void Initialize(ContentManager content, string contentPath)
        {
            Cursor = new Cursor(Game);
            Keyboard = new RKeyboard(Game);
            Renderer.Initialize(GraphicsDevice, content, contentPath);

            gameForm = (Form)Form.FromHandle(Game.Window.Handle);

            if (gameForm != null)
            {
                gameForm.FormClosing += GameForm_FormClosing_Event;
            }
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
        public void AddAndInitializeControl(DXControl control)
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
        public void InsertAndInitializeControl(DXControl control)
        {
            if (Controls.Contains(control))
            {
                throw new Exception("WindowManager.InsertAndInitializeControl: Control " + control.Name + " already exists!");
            }

            Controls.Insert(0, control);
        }

        public void CenterControlOnScreen(DXControl control)
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

        public void RemoveControl(DXControl control)
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
            Keyboard.HasFocus = _hasFocus;

            bool activeControlFound = false;

            DXControl activeControl = null;

            if (_hasFocus)
            {
                for (int i = Controls.Count - 1; i > -1; i--)
                {
                    DXControl control = Controls[i];

                    if (control.Visible && (!activeControlFound &&
                        control.ClientRectangle.Contains(Cursor.Location)
                        || 
                        control.Focused))
                    {
                        control.IsActive = true;
                        activeControlFound = true;
                        activeControl = control;
                    }
                    else if (activeControl != control)
                        control.IsActive = false;

                    if (control.Enabled)
                        control.Update(gameTime);
                }

                Keyboard.Update(gameTime);
            }

            Cursor.Update(gameTime);

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

            Renderer.DrawTexture(renderTarget, new Rectangle(SceneXPosition, SceneYPosition,
                resolutionWidth - (SceneXPosition * 2), resolutionHeight - (SceneYPosition * 2)), Color.White);

            //if (!String.IsNullOrEmpty(activeControlText))
            //    Renderer.DrawStringWithShadow(activeControlText, 0, Vector2.Zero, Color.White);

            Cursor.Draw(gameTime);

            Renderer.EndDraw();

            base.Draw(gameTime);
        }
    }
}
