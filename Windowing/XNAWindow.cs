using Microsoft.Xna.Framework;
using Rampastring.Tools;
using Rampastring.XNAUI.XNAControls;
using System;
using System.Linq;

namespace Rampastring.XNAUI.Windowing;

/// <summary>
/// Window that can be drag-moved, reports interactions to an optional window controller,
/// and supports smooth open and close animations.
/// </summary>
public class XNAWindow : XNAPanel, IWindow
{
    public event EventHandler Closed;
    public event EventHandler InteractedWith;

    /// <summary>
    /// This is stored here for the purposes of being able
    /// to clean up event handlers when a window is removed from the window controller.
    /// </summary>
    public EventHandler<InputEventArgs> FocusSwitchEventHandler { get; set; }

    public XNAWindow(WindowManager windowManager) : base(windowManager)
    {
        DrawMode = ControlDrawMode.UNIQUE_RENDER_TARGET;
    }

    public float? AppearingRate
    {
        get => field ?? UISettings.ActiveSettings.WindowAppearingRate;
        set;
    }

    public float? DisappearingRate
    {
        get => field ?? UISettings.ActiveSettings.WindowDisappearingRate;
        set;
    }

    /// <summary>
    /// Gets or sets the window controller that manages this window.
    /// </summary>
    public IWindowController WindowController
    {
        get => field;
        set
        {
            if (field != null)
                throw new InvalidOperationException("A window's window controller can only be set once.");

            field = value;
        }
    }

    public bool IsForeground => WindowController == null ? true : WindowController.IsWindowForeground(this);


    /// <summary>
    /// Determines whether this window should be centered on the screen by default.
    /// </summary>
    public bool CenterByDefault { get; set; } = true;

    /// <summary>
    /// Determines whether this window can be moved by dragging it with the mouse.
    /// </summary>
    public bool AllowDragging { get; set; } = true;

    /// <summary>
    /// Whether the window handles resolution changes by shared logic of the <see cref="XNAWindow"/> class.
    /// </summary>
    protected bool HandleResolutionChanges { get; set; } = true;

    protected bool IsDragged;
    private Point lastCursorPoint;


    protected override void ParseControlINIAttribute(IniFile iniFile, string key, string value)
    {
        if (key == nameof(AllowDragging))
        {
            AllowDragging = Conversions.BooleanFromString(value, AllowDragging);
            return;
        }
        else if (key == nameof(CenterByDefault))
        {
            CenterByDefault = Conversions.BooleanFromString(value, CenterByDefault);
        }

        base.ParseControlINIAttribute(iniFile, key, value);
    }

    public override void Initialize()
    {
        base.Initialize();

        WindowManager.RenderResolutionChanged += WindowManager_RenderResolutionChanged;
    }

    public override void Kill()
    {
        WindowManager.RenderResolutionChanged -= WindowManager_RenderResolutionChanged;

        base.Kill();
    }

    private void WindowManager_RenderResolutionChanged(object sender, EventArgs e)
    {
        if (!HandleResolutionChanges)
            return;

        if (CenterByDefault)
            CenterOnParent();
        else
            ConstrainPosition();
    }

    public void Hide()
    {
        AlphaRate = -DisappearingRate.Value;
    }

    protected virtual void Show()
    {
        AlphaRate = AppearingRate.Value;
        Alpha = 0f;
        Enable();
        IsDragged = false;

        ConstrainPosition();

        AddCallback(() => InteractedWith?.Invoke(this, EventArgs.Empty));
    }

    protected virtual void DrawWindowBackground()
    {
        DrawPanel();
    }

    protected override void DrawPanelBorders()
    {
        Color borderColor = IsForeground ? UISettings.ActiveSettings.WindowActiveBorderColor : UISettings.ActiveSettings.WindowInactiveBorderColor;

        DrawRectangle(new Rectangle(0, 0, Width, Height), borderColor, BorderThickness.Value);
    }

    public override void Draw(GameTime gameTime)
    {
        DrawWindowBackground();
        DrawChildren(gameTime);

        if (DrawBorders)
            DrawPanelBorders();
    }

    protected virtual void ConstrainPosition()
    {
        if (ScaledWidth > WindowManager.RenderResolutionX)
            X = (WindowManager.RenderResolutionX - ScaledWidth) / 2;
        else if (X + ScaledWidth > WindowManager.RenderResolutionX)
            X = WindowManager.RenderResolutionX - ScaledWidth;
        else if (X < 0)
            X = 0;

        if (ScaledHeight > WindowManager.RenderResolutionY)
            Y = (WindowManager.RenderResolutionY - ScaledHeight) / 2;
        else if (Y + ScaledHeight > WindowManager.RenderResolutionY)
            Y = WindowManager.RenderResolutionY - ScaledHeight;
        else if (Y < 0)
            Y = 0;
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        if (Alpha <= 0f && AlphaRate < 0.0f)
        {
            Disable();
            Closed?.Invoke(this, EventArgs.Empty);
        }

        if (IsDragged)
        {
            Point newCursorPoint = GetCursorPoint();
            X = X + (newCursorPoint.X - lastCursorPoint.X) * Scaling;
            Y = Y + (newCursorPoint.Y - lastCursorPoint.Y) * Scaling;

            ConstrainPosition();
            lastCursorPoint = GetCursorPoint();
            IsDragged = Cursor.LeftDown;
        }

        if (IsActive && AllowDragging && Cursor.LeftPressedDown)
        {
            var activeChild = Children.FirstOrDefault(c => c.IsActive);

            if (activeChild != null)
            {
                // Find the last active child from the control hierarchy
                while (true)
                {
                    var childOfChild = activeChild.Children.FirstOrDefault(c => c.IsActive);
                    if (childOfChild == null)
                        break;

                    activeChild = childOfChild;
                }
            }

            // Only allow moving window if the active child is not a control that is used by dragging
            // TODO this could be made more object-oriented with a property at XNAControl level
            if (activeChild == null || !activeChild.HandlesDragging)
            {
                InteractedWith?.Invoke(this, EventArgs.Empty);
                IsDragged = true;
                lastCursorPoint = GetCursorPoint();
            }
        }
    }
}
