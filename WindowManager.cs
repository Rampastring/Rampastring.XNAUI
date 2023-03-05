namespace Rampastring.XNAUI;

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Rampastring.Tools;
using Rampastring.XNAUI.Input;
using Rampastring.XNAUI.PlatformSpecific;
using Rampastring.XNAUI.XNAControls;
using Color = Microsoft.Xna.Framework.Color;
#if !XNA
using System.Diagnostics;
#if NETFRAMEWORK
using System.Reflection;
#endif
#endif
#if WINFORMS
using System.Windows.Forms;
#endif

/// <summary>
/// Manages the game window and all of the game's controls
/// inside the game window.
/// </summary>
public class WindowManager : DrawableGameComponent
{
#if XNA
    private const int XNA_MAX_TEXTURE_SIZE = 2048;

#endif
    private bool isDisposed;

    /// <summary>
    /// Creates a new WindowManager.
    /// </summary>
    /// <param name="game">The game.</param>
    /// <param name="graphics">The game's GraphicsDeviceManager.</param>
    public WindowManager(Game game, GraphicsDeviceManager graphics)
        : base(game)
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

    private List<XNAControl> controls = new();

    private List<Callback> callbacks = new();

    private readonly object locker = new();

    /// <summary>
    /// Returns the width of the game window.
    /// </summary>
    public int WindowWidth { get; private set; } = 800;

    /// <summary>
    /// Returns the height of the game window.
    /// </summary>
    public int WindowHeight { get; private set; } = 600;

    /// <summary>
    /// Returns the width of the back buffer.
    /// </summary>
    public int RenderResolutionX { get; private set; } = 800;

    /// <summary>
    /// Returns the height of the back buffer.
    /// </summary>
    public int RenderResolutionY { get; private set; } = 600;

    /// <summary>
    /// Gets a boolean that determines whether the game window currently has input focus.
    /// </summary>
    public bool HasFocus { get; private set; } = true;

    public double ScaleRatio { get; private set; } = 1.0;

    public int SceneXPosition { get; private set; }

    public int SceneYPosition { get; private set; }

    private XNAControl _selectedControl;

    /// <summary>
    /// Gets or sets the control that is currently selected.
    /// Usually used for controls that need input focus, like text boxes.
    /// </summary>
    public XNAControl SelectedControl
    {
        get => _selectedControl;

        set
        {
            XNAControl oldSelectedControl = _selectedControl;
            _selectedControl = value;
            if (oldSelectedControl != _selectedControl)
            {
                _selectedControl?.OnSelectedChanged();

                oldSelectedControl?.OnSelectedChanged();
            }
        }
    }

    /// <summary>
    /// Returns a bool that determines whether input is
    /// currently exclusively captured by the selected control.
    /// </summary>
    public bool IsInputExclusivelyCaptured => SelectedControl is { ExclusiveInputCapture: true };

    /// <summary>
    /// A list of custom control INI attribute parsers.
    /// Allows extending the control INI attribute parsing
    /// system with custom INI keys.
    /// </summary>
    public List<IControlINIAttributeParser> ControlINIAttributeParsers { get; } = new();

    private readonly GraphicsDeviceManager graphics;

    private IGameWindowManager gameWindowManager;
    private RenderTarget2D renderTarget;
    private RenderTarget2D doubledRenderTarget;

    /// <summary>
    /// Sets the rendering (back buffer) resolution of the game.
    /// Does not affect the size of the actual game window.
    /// </summary>
    /// <param name="x">The width of the back buffer.</param>
    /// <param name="y">The height of the back buffer.</param>
    public void SetRenderResolution(int x, int y)
    {
#if XNA
        x = Math.Min(x, XNA_MAX_TEXTURE_SIZE);
        y = Math.Min(y, XNA_MAX_TEXTURE_SIZE);
#endif

        RenderResolutionX = x;
        RenderResolutionY = y;

        RecalculateScaling();
    }

    /// <summary>
    /// Re-calculates the scaling of the rendered screen to fill the window.
    /// </summary>
    private void RecalculateScaling()
    {
        double horizontalRatio = WindowWidth / (double)RenderResolutionX;
        double verticalRatio = WindowHeight / (double)RenderResolutionY;

        double ratio;

        int texturePositionX = 0;
        int texturePositionY = 0;

        if (horizontalRatio > verticalRatio)
        {
            ratio = verticalRatio;
            int textureWidth = (int)(RenderResolutionX * ratio);
            texturePositionX = (WindowWidth - textureWidth) / 2;
        }
        else
        {
            ratio = horizontalRatio;
            int textureHeight = (int)(RenderResolutionY * ratio);
            texturePositionY = (WindowHeight - textureHeight) / 2;
        }

        ScaleRatio = ratio;
        SceneXPosition = texturePositionX;
        SceneYPosition = texturePositionY;

        if (renderTarget is { IsDisposed: false })
            renderTarget.Dispose();

        if (doubledRenderTarget is { IsDisposed: false })
            doubledRenderTarget.Dispose();

        renderTarget = new(
            GraphicsDevice,
            RenderResolutionX,
            RenderResolutionY,
            false,
            SurfaceFormat.Color,
            DepthFormat.None,
            0,
            RenderTargetUsage.PreserveContents);

        RenderTargetStack.Initialize(renderTarget, GraphicsDevice);
        RenderTargetStack.InitDetachedScaledControlRenderTarget(RenderResolutionX, RenderResolutionY);

        if (ScaleRatio > 1.5 && ScaleRatio % 1.0 == 0)
        {
#if XNA
            if (RenderResolutionX * 2 > XNA_MAX_TEXTURE_SIZE || RenderResolutionY * 2 > XNA_MAX_TEXTURE_SIZE)
            {
                doubledRenderTarget = null;
                return;
            }
#endif

            // Enable sharper scaling method
            doubledRenderTarget = new(
                GraphicsDevice,
                RenderResolutionX * 2,
                RenderResolutionY * 2,
                false,
                SurfaceFormat.Color,
                DepthFormat.None,
                0,
                RenderTargetUsage.PreserveContents);
        }
        else
        {
            doubledRenderTarget = null;
        }
    }

    /// <summary>
    /// Closes the game.
    /// </summary>
    public void CloseGame()
    {
#if !WINFORMS
        // When using UniversalGL both GameClosing and Game.Exiting trigger GameWindowManager_GameWindowClosing().
        // To avoid executing shutdown code twice we unsubscribe here from Game.Exiting.
        // The default double subscription needs to stay to handle the case of a forceful shutdown e.g. alt+F4.
        Game.Exiting -= GameWindowManager_GameWindowClosing;
#endif
        GameClosing?.Invoke(this, EventArgs.Empty);
        Game.Exit();
    }

    /// <summary>
    /// Restarts the game.
    /// </summary>
#pragma warning disable CA1822 // Mark members as static
    public void RestartGame()
#pragma warning restore CA1822 // Mark members as static
    {
        Logger.Log("Restarting game.");

#if !XNA
        // MonoGame takes ages to unload assets compared to XNA; sometimes MonoGame
        // can take over 8 seconds while XNA takes only 1 second
        // This is a bit dirty, but at least it makes the MonoGame build exit quicker
        GameClosing?.Invoke(this, EventArgs.Empty);

        // TODO move Windows-specific functionality
#if WINFORMS
        Application.DoEvents();
#endif
#if NETFRAMEWORK
        using var process = Process.Start(Assembly.GetEntryAssembly().Location);
#else
        using var process = Process.Start(new ProcessStartInfo
        {
            FileName = Environment.ProcessPath,
            Arguments = Environment.CommandLine
        });
#endif

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
        Initialize();

        content.RootDirectory = SafePath.GetDirectory(contentPath).FullName;

        Cursor = new(this);
        Cursor.Initialize();
        Keyboard = new(Game);
        if (!AssetLoader.IsInitialized)
            AssetLoader.Initialize(graphics.GraphicsDevice, content);
        Renderer.Initialize(GraphicsDevice, content);
        SoundPlayer = new(Game);

        gameWindowManager = new WindowsGameWindowManager(Game);
#if WINFORMS
        gameWindowManager.GameWindowClosing += GameWindowManager_GameWindowClosing;
#else
        Game.Exiting += GameWindowManager_GameWindowClosing;
#endif

        UISettings.ActiveSettings ??= new();
#if XNA

        KeyboardEventInput.Initialize(Game.Window);
#endif
    }

    private void GameWindowManager_GameWindowClosing(object sender, EventArgs e)
        => GameClosing?.Invoke(this, EventArgs.Empty);

    /// <summary>
    /// Schedules a delegate to be executed on the next game loop frame,
    /// on the main game thread.
    /// </summary>
    /// <param name="d">The delegate.</param>
    /// <param name="args">The arguments to be passed on to the delegate.</param>
    public void AddCallback(Delegate d, params object[] args)
    {
        lock (locker)
            callbacks.Add(new(d, args));
    }

    /// <summary>
    /// Adds a control into the WindowManager, on the last place
    /// in the list of controls.
    /// </summary>
    /// <param name="control">The control to add.</param>
    public void AddAndInitializeControl(XNAControl control)
    {
        if (controls.Contains(control))
        {
            throw new InvalidOperationException("WindowManager.AddAndInitializeControl: Control " + control.Name + " already exists!");
        }

        control.Initialize();
        controls.Add(control);
        ReorderControls();
    }

    /// <summary>
    /// Adds a control to the WindowManager, on the last place
    /// in the list of controls. Does not call the control's
    /// Initialize() method.
    /// </summary>
    /// <param name="control">The control to add.</param>
    public void AddControl(XNAControl control)
    {
        if (controls.Contains(control))
        {
            throw new InvalidOperationException("WindowManager.AddControl: Control " + control.Name + " already exists!");
        }

        controls.Add(control);
    }

    /// <summary>
    /// Inserts a control into the WindowManager on the first place
    /// in the list of controls.
    /// </summary>
    /// <param name="control">The control to insert.</param>
    public void InsertAndInitializeControl(XNAControl control)
    {
        if (controls.Contains(control))
        {
            throw new("WindowManager.InsertAndInitializeControl: Control " + control.Name + " already exists!");
        }

        controls.Insert(0, control);
    }

    /// <summary>
    /// Centers a control on the game window.
    /// </summary>
    /// <param name="control">The control to center.</param>
    public void CenterControlOnScreen(XNAControl control)
    {
        control.ClientRectangle = new(
            (RenderResolutionX - control.Width) / 2,
            (RenderResolutionY - control.Height) / 2,
            control.Width,
            control.Height);
    }

    /// <summary>
    /// Centers the game window on the screen.
    /// </summary>
    public void CenterOnScreen() => gameWindowManager.CenterOnScreen();

    /// <summary>
    /// Enables or disables borderless windowed mode.
    /// </summary>
    /// <param name="value">A boolean that determines whether borderless
    /// windowed mode should be enabled.</param>
    public void SetBorderlessMode(bool value) => gameWindowManager.SetBorderlessMode(value);
#if WINFORMS

    public void MinimizeWindow() => gameWindowManager.MinimizeWindow();

    public void MaximizeWindow() => gameWindowManager.MaximizeWindow();

    public void HideWindow() => gameWindowManager.HideWindow();

    public void ShowWindow() => gameWindowManager.ShowWindow();

    /// <summary>
    /// Flashes the game window on the taskbar.
    /// </summary>
    public void FlashWindow() => gameWindowManager.FlashWindow();

    /// <summary>
    /// Sets the icon of the game window to an icon that exists on a specific
    /// file path.
    /// </summary>
    /// <param name="path">The path to the icon file.</param>
    public void SetIcon(string path) => gameWindowManager.SetIcon(path);

    /// <summary>
    /// Returns the IntPtr handle of the game window on Windows.
    /// On other platforms, returns IntPtr.Zero.
    /// </summary>
    public IntPtr GetWindowHandle() => gameWindowManager.GetWindowHandle();

    /// <summary>
    /// Enables or disables the "control box" (minimize/maximize/close buttons) for the game form.
    /// </summary>
    /// <param name="value">True to enable the control box, false to disable it.</param>
    public void SetControlBox(bool value) => gameWindowManager.SetControlBox(value);

    /// <summary>
    /// Prevents the user from closing the game form by Alt-F4.
    /// </summary>
    public void PreventClosing() => gameWindowManager.PreventClosing();

    /// <summary>
    /// Allows the user to close the game form by Alt-F4.
    /// </summary>
    public void AllowClosing() => gameWindowManager.AllowClosing();
#endif

    /// <summary>
    /// Removes a control from the window manager.
    /// </summary>
    /// <param name="control">The control to remove.</param>
    public void RemoveControl(XNAControl control) => controls.Remove(control);

    /// <summary>
    /// Enables or disables VSync.
    /// </summary>
    /// <param name="value">A boolean that determines whether VSync should be enabled or disabled.</param>
    public void SetVSync(bool value) => graphics.SynchronizeWithVerticalRetrace = value;

    public void SetFinalRenderTarget() => GraphicsDevice.SetRenderTarget(renderTarget);

    public RenderTarget2D GetFinalRenderTarget() => renderTarget;

    /// <summary>
    /// Re-orders controls by their update order.
    /// </summary>
    public void ReorderControls() => controls = controls.OrderBy(control => control.Detached).ThenBy(control => control.UpdateOrder).ToList();

    /// <summary>
    /// Attempt to set the display mode to the desired resolution. Iterates through the display
    /// capabilities of the default graphics adapter to determine if the graphics adapter supports the
    /// requested resolution.  If so, the resolution is set and the function returns true.  If not,
    /// no change is made and the function returns false.
    /// </summary>
    /// <param name="width">Desired screen width.</param>
    /// <param name="height">Desired screen height.</param>
    /// <param name="fullScreen">True if you wish to go to Full Screen, false for Windowed Mode.</param>
    public bool InitGraphicsMode(int width, int height, bool fullScreen)
    {
        Logger.Log("InitGraphicsMode: " + width + "x" + height);
        WindowWidth = width;
        WindowHeight = height;

        // If we aren't using a full screen mode, the height and width of the window can
        // be set to anything equal to or smaller than the actual screen size.
        if (!fullScreen)
        {
            if ((width <= GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width)
                && (height <= GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height))
            {
                graphics.PreferredBackBufferWidth = width;
                graphics.PreferredBackBufferHeight = height;
                graphics.IsFullScreen = false;
                graphics.ApplyChanges();
                RecalculateScaling();
                return true;
            }
        }
        else
        {
            // If we are using full screen mode, we should check to make sure that the display
            // adapter can handle the video mode we are trying to set.  To do this, we will
            // iterate through the display modes supported by the adapter and check them against
            // the mode we want to set.
            foreach (DisplayMode dm in GraphicsAdapter.DefaultAdapter.SupportedDisplayModes)
            {
                // Check the width and height of each mode against the passed values
                if ((dm.Width == width) && (dm.Height == height))
                {
                    // The mode is supported, so set the buffer formats, apply changes and return
                    graphics.PreferredBackBufferWidth = width;
                    graphics.PreferredBackBufferHeight = height;
                    graphics.IsFullScreen = true;
                    graphics.ApplyChanges();
                    RecalculateScaling();
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Returns whether the game is running in fullscreen mode.
    /// </summary>
    public bool IsFullscreen => graphics.IsFullScreen;

    /// <summary>
    /// Updates the WindowManager. Do not call manually; MonoGame will call
    /// this automatically on every game frame.
    /// </summary>
    /// <param name="gameTime">Provides a snapshot of timing values.</param>
    public override void Update(GameTime gameTime)
    {
        HasFocus = gameWindowManager.HasFocus();

        lock (locker)
        {
            if (callbacks.Count > 0)
            {
                List<Callback> callbacksCopy = callbacks;
                callbacks = new();

                foreach (Callback c in callbacksCopy)
                    c.Invoke();
            }
        }

        XNAControl activeControl = null;
        activeControlName = null;

        if (HasFocus)
            Keyboard.Update(gameTime);

        Cursor.Update(gameTime);

        SoundPlayer.Update(gameTime);

        for (int i = controls.Count - 1; i > -1; i--)
        {
            XNAControl control = controls[i];

            if (HasFocus && control.InputEnabled && control.Enabled &&
                ((activeControl == null && control.GetWindowRectangle().Contains(Cursor.Location)) || control.Focused))
            {
                control.IsActive = true;
                activeControl = control;
                activeControlName = control.Name;
            }
            else
            {
                control.IsActive = false;
            }

            if (control.Enabled)
            {
                control.Update(gameTime);

                if (control.InputPassthrough && activeControl == control && !control.ChildHandledInput)
                {
                    control.IsActive = false;
                    activeControl = null;
                    activeControlName = null;
                }
            }
        }

        // Make sure that, if input is exclusively captured:
        // 1) a mouse button is held down
        // 2) the control that is capturing the input is visible and enabled
        // If either of these conditions is not true, then release the exclusively captured input.
        if (SelectedControl is { ExclusiveInputCapture: true })
        {
            if ((!Cursor.RightDown && !Cursor.LeftDown) ||
                !SelectedControl.AppliesToSelfAndAllParents(p => p.Enabled && p.InputEnabled))
            {
                SelectedControl = null;
            }
        }

        base.Update(gameTime);
    }

    public string activeControlName;

    /// <summary>
    /// Draws all the visible controls in the WindowManager.
    /// Do not call manually; MonoGame calls this automatically.
    /// </summary>
    /// <param name="gameTime">Provides a snapshot of timing values.</param>
    public override void Draw(GameTime gameTime)
    {
        GraphicsDevice.SetRenderTarget(renderTarget);

        GraphicsDevice.Clear(Color.Black);

        Renderer.ClearStack();
        Renderer.CurrentSettings = new(
            SpriteSortMode.Deferred, BlendState.AlphaBlend, null, null, null, null);
        Renderer.BeginDraw();

        foreach (XNAControl control in controls)
        {
            if (control.Visible)
                control.DrawInternal(gameTime);
        }

        Renderer.EndDraw();

        if (doubledRenderTarget != null)
        {
            GraphicsDevice.SetRenderTarget(doubledRenderTarget);
            GraphicsDevice.Clear(Color.Black);
            Renderer.CurrentSettings = new(
                SpriteSortMode.Deferred,
                BlendState.NonPremultiplied,
                SamplerState.PointWrap,
                null,
                null,
                null);
            Renderer.BeginDraw();
            Renderer.DrawTexture(
                renderTarget, new(0, 0, RenderResolutionX * 2, RenderResolutionY * 2), Color.White);
            Renderer.EndDraw();
        }

        GraphicsDevice.SetRenderTarget(null);
        GraphicsDevice.Clear(Color.Black);

        SamplerState scalingSamplerState = SamplerState.LinearClamp;
        if (ScaleRatio % 1.0 == 0)
            scalingSamplerState = SamplerState.PointClamp;

        Renderer.CurrentSettings = new(
            SpriteSortMode.Deferred, BlendState.NonPremultiplied, scalingSamplerState, null, null, null);
        Renderer.BeginDraw();

        RenderTarget2D renderTargetToDraw = doubledRenderTarget ?? renderTarget;

        Renderer.DrawTexture(
            renderTargetToDraw,
            new(
                SceneXPosition,
                SceneYPosition,
                WindowWidth - (SceneXPosition * 2),
                WindowHeight - (SceneYPosition * 2)),
            Color.White);

#if DEBUG
        Renderer.DrawString("Active control " + activeControlName, 0, Vector2.Zero, Color.Red);

#endif
        if (Cursor.Visible)
            Cursor.Draw(gameTime);

        Renderer.EndDraw();

        base.Draw(gameTime);
    }

    protected override void Dispose(bool disposing)
    {
        if (!isDisposed)
        {
            if (disposing)
            {
                renderTarget?.Dispose();
                doubledRenderTarget?.Dispose();
            }

            isDisposed = true;
        }

        base.Dispose(disposing);
    }
}