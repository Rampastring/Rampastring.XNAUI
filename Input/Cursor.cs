using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
#if WINFORMS
using System;
using System.IO;
using Rampastring.Tools;
using Windows.Win32;
#endif

namespace Rampastring.XNAUI.Input;

public class Cursor : DrawableGameComponent
{
    private const int ScrollWheelValueDivisor = 40;

    public Cursor(WindowManager windowManager)
        : base(windowManager.Game)
    {
        previousMouseState = Mouse.GetState();
        previousDownButtons = GetDownButtons(previousMouseState);
        RemapColor = Color.White;
        this.windowManager = windowManager;
    }

    public Point Location { get; set; }
    private Point DrawnLocation { get; set; }

    public bool HasMoved { get; private set; }
    public bool IsOnScreen { get; private set; }

    public Texture2D[] Textures;

    public int TextureIndex { get; set; }

    public bool LeftClicked => IsButtonClicked(MouseButton.Left);

    public bool RightClicked => IsButtonClicked(MouseButton.Right);

    public bool MiddleClicked => IsButtonClicked(MouseButton.Middle);

    /// <summary>
    /// Gets a value that indicates whether the left mouse button is held down
    /// on the current frame.
    /// </summary>
    public bool LeftDown => IsButtonDown(MouseButton.Left);

    /// <summary>
    /// Gets a value that indicates whether the left mouse button was pressed
    /// down on this frame (meaning it's down on the current frame, but wasn't down
    /// on the previous frame).
    /// </summary>
    public bool LeftPressedDown => IsButtonPressedDown(MouseButton.Left);

    /// <summary>
    /// Gets a value that indicates whether the right mouse button is held down
    /// on the current frame.
    /// </summary>
    public bool RightDown => IsButtonDown(MouseButton.Right);

    /// <summary>
    /// Gets a value that indicates whether the right mouse button was pressed
    /// down on this frame (meaning it's down on the current frame, but wasn't down
    /// on the previous frame).
    /// </summary>
    public bool RightPressedDown => IsButtonPressedDown(MouseButton.Right);

    /// <summary>
    /// Gets a value that indicates whether the middle mouse button is held down
    /// on the current frame.
    /// </summary>
    public bool MiddleDown => IsButtonDown(MouseButton.Middle);

    /// <summary>
    /// Gets a value that indicates whether the middle mouse button was pressed
    /// down on this frame (meaning it's down on the current frame, but wasn't down
    /// on the previous frame).
    /// </summary>
    public bool MiddlePressedDown => IsButtonPressedDown(MouseButton.Middle);

    /// <summary>
    /// Returns whether any of the specified mouse buttons are held down on the current frame.
    /// </summary>
    public bool IsButtonDown(MouseButton buttons) => (DownButtons & buttons) != MouseButton.None;

    /// <summary>
    /// Returns whether any of the specified mouse buttons were newly pressed on the current frame.
    /// </summary>
    public bool IsButtonPressedDown(MouseButton buttons) => (PressedDownButtons & buttons) != MouseButton.None;

    /// <summary>
    /// Returns whether any of the specified mouse buttons were released on the current frame.
    /// </summary>
    public bool IsButtonClicked(MouseButton buttons) => (clickedButtons & buttons) != MouseButton.None;

    /// <summary>
    /// True if the cursor has received input on the current frame.
    /// </summary>
    public bool HasInput => !Disabled && (HasMoved || (DownButtons | clickedButtons) != MouseButton.None ||
        ScrollWheelValue != 0 || HorizontalScrollWheelValue != 0);

    public bool Disabled { get; set; }

    public int ScrollWheelValue { get; set; }
    
    public int HorizontalScrollWheelValue { get; set; }

    public Color RemapColor { get; set; }

    internal MouseButton DownButtons { get; private set; }
    internal MouseButton PressedDownButtons { get; private set; }

    private WindowManager windowManager;
    private MouseState previousMouseState;
    private MouseButton previousDownButtons;
    private MouseButton clickedButtons;

#if WINFORMS
    /// <summary>
    /// Attempts to replace the native operating system pointer cursor with
    /// a cursor file from a specific path for the game window. If successful,
    /// the cursor sprite is hidden, otherwise the cursor sprite remains visible.
    /// </summary>
    /// <param name="path">The path to the cursor (.cur) file.</param>
#if NET5_0_OR_GREATER
    [System.Runtime.Versioning.SupportedOSPlatform("windows5.0")]
#endif
    public void LoadNativeCursor(string path)
    {
        FileInfo fileInfo = SafePath.GetFile(path);
        if (!fileInfo.Exists)
            return;

        DestroyCursorSafeHandle cursorPointer = PInvoke.LoadCursorFromFile(fileInfo.FullName);

        var form = (System.Windows.Forms.Form)System.Windows.Forms.Control.FromHandle(Game.Window.Handle);
        bool success = false;

        cursorPointer.DangerousAddRef(ref success);

        if (!success)
            throw new Exception(FormattableString.Invariant($"{nameof(DestroyCursorSafeHandle)}.{nameof(DestroyCursorSafeHandle.DangerousAddRef)}"));

        if (form != null)
        {
            form.Cursor = new System.Windows.Forms.Cursor(cursorPointer.DangerousGetHandle());
            Visible = false;
            Game.IsMouseVisible = true;
        }

        /* We don't call DestroyCursorSafeHandle.DangerousRelease() or DestroyCursorSafeHandle.Dispose():
         * The DestroyCursor function destroys a nonshared cursor.
         * Do not use this function to destroy a shared cursor.
         * A shared cursor is valid as long as the module from which it was loaded remains in memory.
         * LoadCursorFromFile creates a shared cursor */
    }
#endif

    public override void Initialize()
    {
    }

    public override void Update(GameTime gameTime)
    {
        MouseState ms = Mouse.GetState();

        // Calculate HasMoved before taking scaling into account so the sprite cursor
        // is redrawn on pixel level movement while power saving and scaling is active.
        var oldLocation = DrawnLocation;
        DrawnLocation = new Point(ms.X, ms.Y);
        HasMoved = oldLocation != DrawnLocation;

        if (!windowManager.HasFocus || Disabled)
        {
            DownButtons = MouseButton.None;
            PressedDownButtons = MouseButton.None;
            clickedButtons = MouseButton.None;
            ScrollWheelValue = 0;
#if !XNA
            HorizontalScrollWheelValue = 0;
#endif
            return;
        }

        Point location = DrawnLocation;

        IsOnScreen = !(location.X < 0 || location.Y < 0 ||
            location.X > windowManager.WindowWidth ||
            location.Y > windowManager.WindowHeight);

        location = new Point(location.X - windowManager.SceneXPosition, location.Y - windowManager.SceneYPosition);
        location = new Point((int)(location.X / windowManager.ScaleRatio), (int)(location.Y / windowManager.ScaleRatio));

        Location = location;

        ScrollWheelValue = (ms.ScrollWheelValue - previousMouseState.ScrollWheelValue) / ScrollWheelValueDivisor;
#if !XNA
        // there's something unholy going with what is the direction of horizontal scroll
        // https://github.com/wesnoth/wesnoth/issues/2218
        HorizontalScrollWheelValue = -(ms.HorizontalScrollWheelValue - previousMouseState.HorizontalScrollWheelValue) / ScrollWheelValueDivisor;
#endif

        MouseButton currentDownButtons = GetDownButtons(ms);
        DownButtons = currentDownButtons;
        PressedDownButtons = currentDownButtons & ~previousDownButtons;
        clickedButtons = previousDownButtons & ~currentDownButtons;
        previousDownButtons = currentDownButtons;
        previousMouseState = ms;
    }

    private static MouseButton GetDownButtons(MouseState state)
    {
        return (state.LeftButton == ButtonState.Pressed ? MouseButton.Left : MouseButton.None)
            | (state.RightButton == ButtonState.Pressed ? MouseButton.Right : MouseButton.None)
            | (state.MiddleButton == ButtonState.Pressed ? MouseButton.Middle : MouseButton.None)
            | (state.XButton1 == ButtonState.Pressed ? MouseButton.XButton1 : MouseButton.None)
            | (state.XButton2 == ButtonState.Pressed ? MouseButton.XButton2 : MouseButton.None);
    }

    public override void Draw(GameTime gameTime)
    {
        if (Textures == null)
            return;

        Texture2D texture = Textures[TextureIndex];

        Renderer.DrawTexture(texture,
            new Rectangle(DrawnLocation.X, DrawnLocation.Y, texture.Width, texture.Height), RemapColor);
    }
}
