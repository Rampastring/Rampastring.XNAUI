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
using System.Diagnostics;

namespace Rampastring.XNAUI
{
    /// <summary>
    /// Manages the game window and all of the game's controls.
    /// </summary>
    public class WindowManager : DrawableGameComponent
    {
        /// <summary>
        /// Creates a new WindowManager.
        /// </summary>
        /// <param name="game">The game.</param>
        /// <param name="graphics">The game's GraphicsDeviceManager.</param>
        public WindowManager(Game game, GraphicsDeviceManager graphics) : base(game)
        {
            this.graphics = graphics;
        }

        /// <summary>
        /// Raised when the game window is closing.
        /// </summary>
        public event EventHandler GameClosing;

        /// <summary>
        /// The input cursor.
        /// </summary>
        public Input.Cursor Cursor { get; private set; }

        /// <summary>
        /// The keyboard.
        /// </summary>
        public RKeyboard Keyboard { get; private set; }

        /// <summary>
        /// The SoundPlayer that is responsible for handling audio.
        /// </summary>
        public SoundPlayer SoundPlayer { get; private set; }

        List<XNAControl> Controls = new List<XNAControl>();

        List<Callback> Callbacks = new List<Callback>();

        private readonly object locker = new object();

        int windowWidth = 800;
        int windowHeight = 600;

        /// <summary>
        /// Returns the width of the game window.
        /// </summary>
        public int WindowWidth
        {
            get { return windowWidth; }
        }

        /// <summary>
        /// Returns the height of the game window.
        /// </summary>
        public int WindowHeight
        {
            get { return windowHeight; }
        }

        int renderResX = 800;
        int renderResY = 600;

        /// <summary>
        /// Returns the width of the back buffer.
        /// </summary>
        public int RenderResolutionX
        {
            get { return renderResX; }
        }

        /// <summary>
        /// Returns the height of the back buffer.
        /// </summary>
        public int RenderResolutionY
        {
            get { return renderResY; }
        }

        bool _hasFocus = true;

        /// <summary>
        /// Gets a boolean that determines whether the game window currently has input focus.
        /// </summary>
        public bool HasFocus
        {
            get { return _hasFocus; }
        }

        public double ScaleRatio = 1.0;

        public int SceneXPosition = 0;
        public int SceneYPosition = 0;

        private XNAControl _selectedControl;

        /// <summary>
        /// Gets or sets the control that is currently selected.
        /// Usually used for controls that need input focus, like text boxes.
        /// </summary>
        public XNAControl SelectedControl
        {
            get { return _selectedControl; }
            set
            {
                XNAControl oldSelectedControl = _selectedControl;
                _selectedControl = value;

                if (oldSelectedControl != _selectedControl)
                {
                    if (_selectedControl != null)
                        _selectedControl.OnSelectedChanged();

                    if (oldSelectedControl != null)
                        oldSelectedControl.OnSelectedChanged();
                }
            }
        }

        private GraphicsDeviceManager graphics;

        private Form gameForm;
        private RenderTarget2D renderTarget;
        private bool closingPrevented = false;

        /// <summary>
        /// Sets the rendering (back buffer) resolution of the game.
        /// Does not affect the size of the actual game window.
        /// </summary>
        /// <param name="x">The width of the back buffer.</param>
        /// <param name="y">The height of the back buffer.</param>
        public void SetRenderResolution(int x, int y)
        {
            renderResX = x;
            renderResY = y;

            RecalculateScaling();
        }

        /// <summary>
        /// Re-calculates the scaling of the rendered screen to fill the window.
        /// </summary>
        private void RecalculateScaling()
        {
            double xRatio = (windowWidth) / (double)renderResX;
            double yRatio = (windowHeight) / (double)renderResY;

            double ratio;

            int texturePositionX = 0;
            int texturePositionY = 0;
            int textureHeight = 0;
            int textureWidth = 0;

            if (xRatio > yRatio)
            {
                ratio = yRatio;
                textureHeight = windowHeight;
                textureWidth = (int)(renderResX * ratio);
                texturePositionX = (int)(windowWidth - textureWidth) / 2;
            }
            else
            {
                ratio = xRatio;
                textureWidth = windowWidth;
                textureHeight = (int)(renderResY * ratio);
                texturePositionY = (int)(windowHeight - textureHeight) / 2;
            }

            ScaleRatio = ratio;
            SceneXPosition = texturePositionX;
            SceneYPosition = texturePositionY;

            renderTarget = new RenderTarget2D(GraphicsDevice, renderResX, renderResY, false, SurfaceFormat.Color,
                DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
        }

        /// <summary>
        /// Closes the game.
        /// </summary>
        public void CloseGame()
        {
            GameClosing?.Invoke(this, EventArgs.Empty);
            Game.Exit();
        }

        /// <summary>
        /// Restarts the game.
        /// </summary>
        public void RestartGame()
        {
            Logger.Log("Restarting game.");

#if !XNA
            // MonoGame takes ages to unload assets compared to XNA; sometimes MonoGame
            // can take over 8 seconds while XNA takes only 1 second
            // This is a bit dirty, but at least it makes the MonoGame build exit quicker
            GameClosing?.Invoke(this, EventArgs.Empty);
            Application.DoEvents();
            Process.Start(Application.ExecutablePath);
            Environment.Exit(0);
#else
            Application.Restart();
#endif
        }

        /// <summary>
        /// Initializes the WindowManager.
        /// </summary>
        /// <param name="content">The game content manager.</param>
        /// <param name="contentPath">The path where the ContentManager should load files from (including SpriteFont files).</param>
        public void Initialize(ContentManager content, string contentPath)
        {
            base.Initialize();

            Cursor = new Input.Cursor(this);
            Keyboard = new RKeyboard(Game);
            Renderer.Initialize(GraphicsDevice, content, contentPath);
            SoundPlayer = new SoundPlayer(Game);

#if XNA
            KeyboardEventInput.Initialize(Game.Window);
#endif

            gameForm = (Form)Control.FromHandle(Game.Window.Handle);

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
            GameClosing?.Invoke(this, EventArgs.Empty);
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

        /// <summary>
        /// Centers a control on the game window.
        /// </summary>
        /// <param name="control">The control to center.</param>
        public void CenterControlOnScreen(XNAControl control)
        {
            control.ClientRectangle = new Rectangle((RenderResolutionX - control.ClientRectangle.Width) / 2,
                (RenderResolutionY - control.ClientRectangle.Height) / 2, control.ClientRectangle.Width, control.ClientRectangle.Height);
        }

        /// <summary>
        /// Centers the game window on the screen.
        /// </summary>
        public void CenterOnScreen()
        {
            int x = (Screen.PrimaryScreen.Bounds.Width - Game.Window.ClientBounds.Width) / 2;
            int y = (Screen.PrimaryScreen.Bounds.Height - Game.Window.ClientBounds.Height) / 2;

#if XNA
            if (gameForm == null)
                return;

            gameForm.DesktopLocation = new System.Drawing.Point(x, y);
#else
            Game.Window.Position = new Microsoft.Xna.Framework.Point(x, y);
#endif
        }

        /// <summary>
        /// Enables or disables borderless windowed mode.
        /// </summary>
        /// <param name="value">A boolean that determines whether borderless 
        /// windowed mode should be enabled.</param>
        public void SetBorderlessMode(bool value)
        {

#if !XNA
            Game.Window.IsBorderless = value;
#else
            if (value)
                gameForm.FormBorderStyle = FormBorderStyle.None;
            else
                gameForm.FormBorderStyle = FormBorderStyle.FixedSingle;
#endif
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

        /// <summary>
        /// Flashes the game window on the taskbar (Windows only).
        /// </summary>
        public void FlashWindow()
        {
            if (gameForm == null)
                return;

            WindowFlasher.FlashWindowEx(gameForm);
        }

        /// <summary>
        /// Sets the icon of the game window to an icon that exists on a specific
        /// file path.
        /// </summary>
        /// <param name="path">The path to the icon file.</param>
        public void SetIcon(string path)
        {
            if (gameForm == null)
                return;
            
            gameForm.Icon = Icon.ExtractAssociatedIcon(path);
        }

        /// <summary>
        /// Returns the IntPtr handle of the game window on Windows.
        /// On other platforms, returns IntPtr.Zero.
        /// </summary>
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

        /// <summary>
        /// Prevents the user from closing the game form by Alt-F4.
        /// </summary>
        public void PreventClosing()
        {
            if (gameForm == null)
                return;

            if (!closingPrevented)
                gameForm.FormClosing += GameForm_FormClosing;
            closingPrevented = true;
        }

        private void GameForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            e.Cancel = true;
        }

        /// <summary>
        /// Allows the user to close the game form by Alt-F4.
        /// </summary>
        public void AllowClosing()
        {
            if (gameForm == null)
                return;

            gameForm.FormClosing -= GameForm_FormClosing;
            closingPrevented = false;
        }

        public void RemoveControl(XNAControl control)
        {
            Controls.Remove(control);
        }

        /// <summary>
        /// Enables or disables VSync.
        /// </summary>
        /// <param name="value">A boolean that determines whether VSync should be enabled or disabled.</param>
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
            windowWidth = iWidth;
            windowHeight = iHeight;
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
                    RecalculateScaling();
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
                        RecalculateScaling();
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Updates the WindowManager. Do not call manually; MonoGame will call 
        /// this automatically on every game frame.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        public override void Update(GameTime gameTime)
        {
            _hasFocus = Game.IsActive;

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

            SoundPlayer.Update(gameTime);

            for (int i = Controls.Count - 1; i > -1; i--)
            {
                XNAControl control = Controls[i];

                if (_hasFocus && control.Enabled && 
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

        /// <summary>
        /// Draws all the visible controls in the WindowManager.
        /// Do not call manually; MonoGame calls this automatically.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
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

            GraphicsDevice.SetRenderTarget(null);

            if (Keyboard.PressedKeys.Contains(Microsoft.Xna.Framework.Input.Keys.F12))
            {
                FileStream fs = File.Create(Environment.CurrentDirectory + "\\image.png");
                renderTarget.SaveAsPng(fs, renderTarget.Width, renderTarget.Height);
                fs.Close();
            }

            GraphicsDevice.Clear(Color.Black);

            Renderer.BeginDraw();

            Renderer.DrawTexture(renderTarget, new Rectangle(SceneXPosition, SceneYPosition,
                windowWidth - (SceneXPosition * 2), windowHeight - (SceneYPosition * 2)), Color.White);

            if (Cursor.Visible)
                Cursor.Draw(gameTime);

            Renderer.EndDraw();

            base.Draw(gameTime);
        }
    }
}
