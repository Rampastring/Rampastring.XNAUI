namespace Rampastring.XNAUI.XNAControls;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Rampastring.Tools;
using Rampastring.XNAUI.Input;

/// <summary>
/// The base class for a XNA-based UI control.
/// </summary>
public class XNAControl : DrawableGameComponent
{
    private const double DOUBLE_CLICK_TIME = 1.0;

    /// <summary>
    /// Creates a new control instance.
    /// </summary>
    /// <param name="windowManager">The WindowManager associated with this control.</param>
    public XNAControl(WindowManager windowManager)
        : base(windowManager.Game)
    {
        WindowManager = windowManager;
    }

    /// <summary>
    /// Gets the window manager associated with this control.
    /// </summary>
    public WindowManager WindowManager { get; }

    #region Events

    /// <summary>
    /// Raised when the mouse cursor enters the control's area.
    /// </summary>
    public event EventHandler MouseEnter;

    /// <summary>
    /// Raised once when the left mouse button is pressed down while the
    /// cursor is inside the control's area.
    /// </summary>
    public event EventHandler MouseLeftDown;

    /// <summary>
    /// Raised once when the right mouse button is pressed down while the
    /// cursor is inside the control's area.
    /// </summary>
    public event EventHandler MouseRightDown;

    /// <summary>
    /// Raised when the mouse cursor leaves the control's area.
    /// </summary>
    public event EventHandler MouseLeave;

    /// <summary>
    /// Raised when the mouse cusor moves while inside the control's area.
    /// </summary>
    public event EventHandler MouseMove;

    /// <summary>
    /// Raised each frame when the mouse cursor is inside the control's area.
    /// </summary>
    public event EventHandler MouseOnControl;

    /// <summary>
    /// Raised when the scroll wheel is used while the cursor is inside
    /// the control.
    /// </summary>
    public event EventHandler MouseScrolled;

    /// <summary>
    /// Raised when the left mouse button is clicked (pressed and released)
    /// while the cursor is inside the control's area.
    /// </summary>
    public event EventHandler LeftClick;

    /// <summary>
    /// Raised when the left mouse button is clicked twice in a short
    /// time-frame while the cursor is inside the control's area.
    /// </summary>
    public event EventHandler DoubleLeftClick;

    /// <summary>
    /// Raised when the right mouse button is clicked (pressed and released)
    /// while the cursor is inside the control's area.
    /// </summary>
    public event EventHandler RightClick;

    /// <summary>
    /// Raised when the control's client rectangle is changed.
    /// </summary>
    public event EventHandler ClientRectangleUpdated;

    /// <summary>
    /// Raised when the control is selected or un-selected.
    /// </summary>
    public event EventHandler SelectedChanged;

    /// <summary>
    /// Raised when the control's parent is changed.
    /// </summary>
    public event EventHandler ParentChanged;

    #endregion

    private XNAControl parent;

    /// <summary>
    /// Gets or sets the parent of this control.
    /// </summary>
    public XNAControl Parent
    {
        get => parent;

        set
        {
            parent = value;
            ParentChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public XNAControl GetRootParent()
    {
        if (Parent == null)
            return null;

        XNAControl rootParent = Parent;

        while (rootParent.Parent != null)
        {
            rootParent = rootParent.Parent;
        }

        return rootParent;
    }

    /// <summary>
    /// Set if the control is detached from its parent.
    /// A detached control's mouse input is handled independently
    /// from its parent, ie. it can grow beyond its parent's area
    /// rectangle and still handle input correctly.
    /// </summary>
    public bool Detached { get; private set; }

    /// <summary>
    /// Holds a reference to the cursor.
    /// </summary>
    protected Cursor Cursor => WindowManager.Cursor;

    /// <summary>
    /// Holds a reference to the keyboard.
    /// </summary>
    protected RKeyboard Keyboard => WindowManager.Keyboard;

    /// <summary>
    /// A list of the control's children. Don't add children to this list directly;
    /// call the AddChild method instead.
    /// </summary>
    private readonly List<XNAControl> children = new();

    private List<XNAControl> updateList = new();
    private List<XNAControl> drawList = new();

    private readonly List<XNAControl> childAddQueue = new();
    private readonly List<XNAControl> childRemoveQueue = new();

    /// <summary>
    /// A read-only list of the control's children.
    /// Call the AddChild method to add children to the control.
    /// </summary>
    public ReadOnlyCollection<XNAControl> Children => new(children);

    #region Location and size

    private int x;
    private int y;
    private int width;
    private int height;
    private int scaling = 1;
    private int initScaling;

    /// <summary>
    /// The non-scaled display rectangle of the control inside its parent.
    /// </summary>
    public Rectangle ClientRectangle
    {
        get => new(x, y, width, height);
        set
        {
            x = value.X;
            y = value.Y;
            bool isSizeChanged = value.Width != width || value.Height != height;
            if (isSizeChanged)
            {
                width = value.Width;
                height = value.Height;
                OnSizeChanged();
            }

            OnClientRectangleUpdated();
        }
    }

    /// <summary>
    /// Called when the control's size is changed.
    /// </summary>
    protected virtual void OnSizeChanged()
    {
        if (!IsChangingSize && Initialized && DrawMode == ControlDrawMode.UNIQUE_RENDER_TARGET)
        {
            RefreshRenderTarget();
        }
    }

    /// <summary>
    /// Called when the control's client rectangle is changed.
    /// </summary>
    protected virtual void OnClientRectangleUpdated() => ClientRectangleUpdated?.Invoke(this, EventArgs.Empty);

    private void CheckForRenderAreaChange()
    {
        if (DrawMode != ControlDrawMode.UNIQUE_RENDER_TARGET)
            return;

        if (RenderTarget == null || RenderTarget.Width != Width || RenderTarget.Height != Height)
        {
            RefreshRenderTarget();
        }

        children.ForEach(c => c.CheckForRenderAreaChange());
    }

    /// <summary>
    /// The X-coordinate of the control relative to its parent's location.
    /// </summary>
    public int X
    {
        get => x;
        set
        {
            x = value;
            OnClientRectangleUpdated();
        }
    }

    /// <summary>
    /// The Y-coordinate of the control relative to its parent's location.
    /// </summary>
    public int Y
    {
        get => y;
        set
        {
            y = value;
            OnClientRectangleUpdated();
        }
    }

    /// <summary>
    /// The width of the control.
    /// </summary>
    public int Width
    {
        get => width;
        set
        {
            width = value;
            OnSizeChanged();
            OnClientRectangleUpdated();
        }
    }

    public int ScaledWidth => Width * Scaling;

    /// <summary>
    /// The height of the control.
    /// </summary>
    public int Height
    {
        get => height;
        set
        {
            height = value;
            OnSizeChanged();
            OnClientRectangleUpdated();
        }
    }

    public int ScaledHeight => Height * Scaling;

    /// <summary>
    /// Shortcut for accessing ClientRectangle.Bottom.
    /// </summary>
    public int Bottom => ClientRectangle.Bottom;

    /// <summary>
    /// Shortcut for accessing ClientRectangle.Right.
    /// </summary>
    public int Right => ClientRectangle.Right;

    #endregion

    #region Public members

    /// <summary>
    /// Gets or sets the name of this control. The name is only an identifier
    /// and does not affect functionality.
    /// </summary>
    public string Name { get; set; }

    public Color RemapColor { get; set; } = Color.White;

    /// <summary>
    /// Determines whether this control should exclusively capture input when it's the selected control,
    /// preventing any other control from being the input focus. An example is scroll bars; when the user
    /// starts dragging a scroll bar, no other control should get focus as long as the scroll bar is being scrolled,
    /// even if the cursor left the scroll bar's area.
    /// NOTE: controls using this should give up their selected control status when they've figured out they don't need it anymore.
    /// </summary>
    public bool ExclusiveInputCapture { get; protected set; }

    private bool cursorOnControl;
    private float alpha = 1.0f;

    public virtual float Alpha
    {
        get => alpha;
        set => alpha = value > 1.0f ? 1.0f : value < 0.0 ? 0.0f : value;
    }

    public int CursorTextureIndex;

    public virtual string Text { get; set; }

    public object Tag { get; set; }

    public bool Killed { get; set; }

    /// <summary>
    /// Determines whether the control should block other controls on the screen
    /// from being interacted with.
    /// </summary>
    public bool Focused { get; set; }

    /// <summary>
    /// Determines whether this control is able to handle input.
    /// If set to false, input management will ignore this control.
    /// </summary>
    public bool InputEnabled { get; set; } = true;

    private bool isActive;

    /// <summary>
    /// Gets or sets a bool that determines whether this control is the current focus of the mouse cursor.
    /// </summary>
    public bool IsActive
    {
        get => Parent != null && !Detached ? Parent.IsActive && isActive : isActive;

        set => isActive = value;
    }

    private bool ignoreInputOnFrame;

    public bool IgnoreInputOnFrame
    {
        get => Parent == null ? ignoreInputOnFrame : ignoreInputOnFrame || Parent.IgnoreInputOnFrame;
        set => ignoreInputOnFrame = value;
    }

    private ControlDrawMode drawMode = ControlDrawMode.NORMAL;

    /// <summary>
    /// The draw mode of the control.
    /// Cannot be changed after the control's <see cref="Initialize"/>
    /// method has been run.
    /// </summary>
    public ControlDrawMode DrawMode
    {
        get => drawMode;

        set
        {
            if (Initialized)
            {
                throw new InvalidOperationException("DrawMode cannot be " +
                    "changed after a control has been initialized.");
            }

            drawMode = value;
        }
    }

    private bool isChangingSize;

    /// <summary>
    /// If set to true and the control has
    /// <see cref="DrawMode"/> == <see cref="ControlDrawMode.UNIQUE_RENDER_TARGET"/>,
    /// the control won't try to update its render target when its size is changed.
    /// </summary>
    public bool IsChangingSize
    {
        get => isChangingSize || Parent is { IsChangingSize: true };
        set
        {
            isChangingSize = value;
            if (!isChangingSize)
                CheckForRenderAreaChange();
        }
    }

    public int Scaling
    {
        get => scaling;
        set
        {
            if (DrawMode != ControlDrawMode.UNIQUE_RENDER_TARGET)
            {
                throw new InvalidOperationException("Scaling cannot be " +
                    "used when the control has no unique render target.");
            }

            if (Initialized && value < initScaling)
            {
                throw new InvalidOperationException("Scaling cannot be " +
                    "lowered below the initial scaling multiplier after control initialization.");
            }

            if (value < 1)
            {
                throw new InvalidOperationException("Scale factor cannot be below one.");
            }

            scaling = value;
        }
    }

    /// <summary>
    /// Whether this control should allow input to pass through to controls
    /// that come after this in the control hierarchy when the control
    /// itself is the focus of input, but none of its children are.
    /// Useful for controls that act as composite for other controls.
    /// </summary>
    public bool InputPassthrough { get; protected set; }

    #endregion

    private TimeSpan timeSinceLastLeftClick = TimeSpan.Zero;
    private bool isLeftPressedOn;
    private bool isRightPressedOn;

    private bool isIteratingChildren;

    /// <summary>
    /// Whether a child of this control handled input during the ongoing frame.
    /// Used for input pass-through.
    /// </summary>
    internal bool ChildHandledInput;

    /// <summary>
    /// Gets a value that can be used to check whether a child of this control is active on the current frame.
    /// See <see cref="IsActive"/>.
    /// </summary>
    public bool IsChildActive { get; private set; }

    /// <summary>
    /// Determines whether the control will automatically update the order of children
    /// when their DrawOrder or UpdateOrder is changed.
    ///
    /// It is recommended to set this to false prior to performing large Update/DrawOrder changes
    /// for children in performance-critical points, and set it back to true afterwards.
    /// </summary>
    public bool AutoUpdateChildOrder { get; protected set; } = true;

    /// <summary>
    /// The render target of the control
    /// in unique render target mode.
    /// </summary>
    protected RenderTarget2D RenderTarget { get; set; }

    /// <summary>
    /// Determines whether the control's <see cref="Initialize"/> method
    /// has been called yet.
    /// </summary>
    protected bool Initialized { get; private set; }

    /// <summary>
    /// Checks if the last parent of this control is active.
    /// </summary>
    public bool IsLastParentActive() => Parent?.IsLastParentActive() ?? isActive;

    /// <summary>
    /// Checks whether a condition applies to this control and all of its parents.
    /// </summary>
    /// <param name="func">The condition.</param>
    public bool AppliesToSelfAndAllParents(Func<XNAControl, bool> func) => func(this) && (Parent == null || Parent.AppliesToSelfAndAllParents(func));

    /// <summary>
    /// Gets the cursor's location relative to this control's location.
    /// </summary>
    /// <returns>A point that represents the cursor's location relative to this control's location.</returns>
    public Point GetCursorPoint()
    {
        Point windowPoint = GetWindowPoint();
        int totalScaling = GetTotalScalingRecursive();
        return new((Cursor.Location.X - windowPoint.X) / totalScaling, (Cursor.Location.Y - windowPoint.Y) / totalScaling);
    }

    /// <summary>
    /// Gets the location of the control's top-left corner within the game window.
    /// Use for input handling; for rendering, use <see cref="GetRenderPoint"/> instead.
    /// </summary>
    public Point GetWindowPoint()
    {
        var p = new Point(X, Y);

        if (Parent != null)
        {
            int parentTotalScaling = Parent.GetTotalScalingRecursive();
            p = new(p.X * parentTotalScaling, p.Y * parentTotalScaling);

#if XNA
            return XNAControl.SumPoints(p, parent.GetWindowPoint());
#else
            return p + parent.GetWindowPoint();
#endif
        }

        return p;
    }

#if XNA
    // XNA's Point is too dumb to know the plus operator
    private static Point SumPoints(Point p1, Point p2) => new(p1.X + p2.X, p1.Y + p2.Y);

#endif
    public Point GetSizePoint()
    {
        int totalScaling = GetTotalScalingRecursive();
        return new(Width * totalScaling, Height * totalScaling);
    }

    public int GetTotalScalingRecursive() => Parent != null ? Scaling * Parent.GetTotalScalingRecursive() : Scaling;

    /// <summary>
    /// Gets the control's client area within the game window.
    /// Use for input handling; for rendering, use <see cref="RenderRectangle"/> instead.
    /// </summary>
    public Rectangle GetWindowRectangle()
    {
        Point p = GetWindowPoint();
        Point size = GetSizePoint();
        return new(p.X, p.Y, size.X, size.Y);
    }

    /// <summary>
    /// Returns the draw area of the control relative to the used render target.
    /// </summary>
    public Rectangle RenderRectangle()
    {
        Point p = GetRenderPoint();
        return new(p.X, p.Y, Width, Height);
    }

    /// <summary>
    /// Gets the location of the control's top-left corner within the current render target.
    /// </summary>
    public Point GetRenderPoint()
    {
        var p = new Point(X, Y);

        if (Parent != null)
        {
            if (Detached)
                return GetWindowPoint();

            if (Parent.DrawMode == ControlDrawMode.UNIQUE_RENDER_TARGET)
                return p;

#if XNA
            return XNAControl.SumPoints(p, Parent.GetRenderPoint());
#else
            return p + Parent.GetRenderPoint();
#endif
        }

        return p;
    }

    /// <summary>
    /// Centers the control on the middle of its parent's client rectangle.
    /// </summary>
    public void CenterOnParent()
    {
        if (Parent == null)
        {
            WindowManager.CenterControlOnScreen(this);
            return;
        }

        ClientRectangle = new(
            (Parent.Width - ScaledWidth) / 2,
            (Parent.Height - ScaledHeight) / 2,
            Width,
            Height);
    }

    /// <summary>
    /// Centers the control horizontally on the middle of its parent's client rectangle.
    /// </summary>
    public void CenterOnParentHorizontally()
    {
        if (Parent == null)
        {
            // TODO WindowManager.CenterControlOnScreenHorizontally();
            return;
        }

        ClientRectangle = new(
            (Parent.Width - ScaledWidth) / 2, Y, Width, Height);
    }

    /// <summary>
    /// Centers the control vertically in proportion to another control.
    /// Assumes that this control and the other control share the same parent control.
    /// </summary>
    /// <param name="control">The other control.</param>
    public void CenterOnControlVertically(XNAControl control) => Y = control.Y - ((Height - control.Height) / 2);

    /// <summary>
    /// Detaches the control from its parent.
    /// See <see cref="Detached"/>.
    /// </summary>
    public void Detach()
    {
        if (Detached)
            throw new InvalidOperationException("The control is already detached!");

        Detached = true;
        WindowManager.AddControl(this);
    }

    /// <summary>
    /// Attaches the control to its parent.
    /// </summary>
    public void Attach()
    {
        Detached = false;
        WindowManager.RemoveControl(this);
    }

    private readonly object locker = new();

    private readonly List<Callback> callbacks = new();

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

    #region Child control management

    /// <summary>
    /// Adds a child to the control.
    /// In case the control is currently being updated, schedules the child
    /// to be added at the end of the current frame.
    /// </summary>
    /// <param name="child">The child control.</param>
    public virtual void AddChild(XNAControl child)
    {
        if (child == null)
            throw new ArgumentNullException(nameof(child));

        if (isIteratingChildren)
            childAddQueue.Add(child);
        else
            AddChildImmediate(child);
    }

    /// <summary>
    /// Adds a child control to the control without calling the child's Initialize method.
    /// In case the control is currently being updated, schedules the child
    /// to be added at the end of the current frame.
    /// </summary>
    /// <param name="child">The child control.</param>
    public void AddChildWithoutInitialize(XNAControl child)
    {
        if (child == null)
            throw new ArgumentNullException(nameof(child));

        if (isIteratingChildren)
        {
            throw new NotImplementedException("AddChildWithoutInitialize cannot currently be called" +
                " while the control is iterating through its children.");
        }
        else
        {
            AddChildImmediateWithoutInitialize(child);
        }
    }

    /// <summary>
    /// Immediately adds a child control to the control.
    /// </summary>
    /// <param name="child">The child control.</param>
    private void AddChildImmediate(XNAControl child)
    {
        InitChild(child);
        child.Initialize();
        children.Add(child);
        ReorderControls();
    }

    /// <summary>
    /// Immediately adds a child control to the control without calling the child's Initialize method.
    /// </summary>
    /// <param name="child">The child control.</param>
    private void AddChildImmediateWithoutInitialize(XNAControl child)
    {
        InitChild(child);
        children.Add(child);
        ReorderControls();
    }

    private void InitChild(XNAControl child)
    {
        if (child.Parent != null)
            throw new InvalidOperationException("Child controls cannot be shared between controls. Child control name: " + child.Name);

        child.Parent = this;
        child.UpdateOrderChanged += Child_UpdateOrderChanged;
        child.DrawOrderChanged += Child_DrawOrderChanged;
    }

    private void Child_DrawOrderChanged(object sender, EventArgs e)
    {
        if (AutoUpdateChildOrder)
            UpdateChildDrawList();
    }

    private void Child_UpdateOrderChanged(object sender, EventArgs e)
    {
        if (AutoUpdateChildOrder)
            UpdateChildUpdateList();
    }

    private void UpdateChildDrawList() =>

        // Controls that are updated first should be drawn last
        // (on top of the other controls).
        // It's weird for the updateorder and draworder to behave differently,
        // but at this point we don't have a choice because of backwards compatibility.
        drawList = children.OrderBy(c => c.DrawOrder).ToList();

    private void UpdateChildUpdateList() => updateList = children.OrderBy(c => c.UpdateOrder).Reverse().ToList();

    /// <summary>
    /// Removes a child from the control.
    /// </summary>
    /// <param name="child">The child control to remove.</param>
    public void RemoveChild(XNAControl child)
    {
        if (isIteratingChildren)
            childRemoveQueue.Add(child);
        else
            RemoveChildImmediate(child);
    }

    /// <summary>
    /// Immediately removes a child from the control.
    /// </summary>
    /// <param name="child">The child control to remove.</param>
    private void RemoveChildImmediate(XNAControl child)
    {
        if (children.Remove(child))
        {
            child.UpdateOrderChanged -= Child_UpdateOrderChanged;
            child.DrawOrderChanged -= Child_DrawOrderChanged;
            child.Parent = null;
            ReorderControls();
        }
    }

    /// <summary>
    /// Re-orders the internal list of children of this control based on the
    /// DrawOrder and UpdateOrder values of the children. Does not need to be called
    /// manually unless you've made use of disabling <see cref="AutoUpdateChildOrder"/>.
    /// </summary>
    public void ReorderControls()
    {
        UpdateChildUpdateList();
        UpdateChildDrawList();
    }

    #endregion

    /// <summary>
    /// Initializes the control.
    /// </summary>
    public override void Initialize()
    {
        base.Initialize();

        Initialized = true;
        initScaling = scaling;
    }

    protected override void OnVisibleChanged(object sender, EventArgs args)
    {
        if (Visible)
        {
            if (DrawMode == ControlDrawMode.UNIQUE_RENDER_TARGET && RenderTarget == null)
                RenderTarget = GetRenderTarget();
        }
        else
        {
            if (DrawMode == ControlDrawMode.UNIQUE_RENDER_TARGET && RenderTarget != null && FreeRenderTarget())
                RenderTarget = null;
        }

        base.OnVisibleChanged(sender, args);
    }

    /// <summary>
    /// Called for a control with an unique render target when its Visible= is set to false.
    /// Can be used to free up the render target in derived classes.
    /// Returns true if the render target should be cleared after this call, false otherwise.
    /// </summary>
    protected virtual bool FreeRenderTarget() => false;

    private void RefreshRenderTarget()
    {
        if (RenderTarget != null)
        {
            if (!FreeRenderTarget())
            {
                RenderTarget.Dispose();
            }
        }

        RenderTarget = GetRenderTarget();
        if (RenderTarget == null)
            throw new InvalidOperationException("GetRenderTarget did not return a render target.");
    }

    protected virtual RenderTarget2D GetRenderTarget()
    {
        return new(
            GraphicsDevice,
            GetRenderTargetWidth(),
            GetRenderTargetHeight(),
            false,
            SurfaceFormat.Color,
            DepthFormat.None,
            0,
            RenderTargetUsage.PreserveContents);
    }

    protected virtual int GetRenderTargetWidth() => Width <= 0 ? 2 : Width;

    protected virtual int GetRenderTargetHeight() => Height <= 0 ? 2 : Height;

    public virtual void GetAttributes(IniFile iniFile)
    {
        IsChangingSize = true;

        foreach (XNAControl child in Children)
            child.GetAttributes(iniFile);

        List<string> keys = iniFile.GetSectionKeys(Name);

        if (keys != null)
        {
            foreach (string key in keys)
                ParseINIAttribute(iniFile, key, iniFile.GetStringValue(Name, key, string.Empty));
        }

        IsChangingSize = false;
    }

    /// <summary>
    /// Attempts to parse given key's value using custom parsers and control's own parsing method (see
    /// <see cref="ParseControlINIAttribute(IniFile, string, string)"/>) and sets the parameter value for the control.
    /// </summary>
    /// <param name="iniFile">The INI file that is being read from.</param>
    /// <param name="key">The key that is being read.</param>
    /// <param name="value">The key's value.</param>
    public void ParseINIAttribute(IniFile iniFile, string key, string value)
    {
        // Attempt to parse the key through custom parsers
        foreach (IControlINIAttributeParser parser in WindowManager.ControlINIAttributeParsers)
        {
            if (parser.ParseINIAttribute(this, iniFile, key, value))
                return;
        }

        ParseControlINIAttribute(iniFile, key, value);
    }

    /// <summary>
    /// Attempts to parse given key's value and sets the parameter value for the control.
    /// </summary>
    /// <remarks>
    /// The control-specific INI attributes should be defined in an override of this method,
    /// which should also call the base method for parsing the inherited attributes.
    /// </remarks>
    /// <param name="iniFile">The INI file that is being read from.</param>
    /// <param name="key">The key that is being read.</param>
    /// <param name="value">The key's value.</param>
    protected virtual void ParseControlINIAttribute(IniFile iniFile, string key, string value)
    {
        switch (key)
        {
            case "DrawOrder":
                DrawOrder = int.Parse(value, CultureInfo.InvariantCulture);
                return;
            case "UpdateOrder":
                UpdateOrder = int.Parse(value, CultureInfo.InvariantCulture);
                return;
            case "Size":
                string[] size = value.Split(',');
                ClientRectangle = new(
                    X,
                    Y,
                    int.Parse(size[0], CultureInfo.InvariantCulture),
                    int.Parse(size[1], CultureInfo.InvariantCulture));
                return;
            case "Width":
                Width = int.Parse(value, CultureInfo.InvariantCulture);
                return;
            case "Height":
                Height = int.Parse(value, CultureInfo.InvariantCulture);
                return;
            case "Location":
                string[] location = value.Split(',');
                ClientRectangle = new(
                    int.Parse(location[0], CultureInfo.InvariantCulture),
                    int.Parse(location[1], CultureInfo.InvariantCulture),
                    Width,
                    Height);
                return;
            case "X":
                X = int.Parse(value, CultureInfo.InvariantCulture);
                return;
            case "Y":
                Y = int.Parse(value, CultureInfo.InvariantCulture);
                return;
            case "RemapColor":
                RemapColor = AssetLoader.GetColorFromString(value);
                return;
            case "Text":
                Text = value.SafeReplace("@", Environment.NewLine);
                return;
            case "Visible":
                Visible = Conversions.BooleanFromString(value, true);
                Enabled = Visible;
                return;
            case "Enabled":
                Enabled = Conversions.BooleanFromString(value, true);
                return;
            case "DistanceFromRightBorder":
                if (Parent != null)
                {
                    ClientRectangle = new(
                        Parent.Width - Width - Conversions.IntFromString(value, 0),
                        Y,
                        Width,
                        Height);
                }

                return;
            case "DistanceFromBottomBorder":
                if (Parent != null)
                {
                    ClientRectangle = new(
                        X,
                        Parent.Height - Height - Conversions.IntFromString(value, 0),
                        Width,
                        Height);
                }

                return;
            case "FillWidth":
                ClientRectangle = Parent != null
                    ? new(
                        X,
                        Y,
                        Parent.Width - X - Conversions.IntFromString(value, 0),
                        Height)
                    : new(
                        X,
                        Y,
                        WindowManager.RenderResolutionX - X - Conversions.IntFromString(value, 0),
                        Height);

                return;
            case "FillHeight":
                ClientRectangle = Parent != null
                    ? new(
                        X,
                        Y,
                        Width,
                        Parent.Height - Y - Conversions.IntFromString(value, 0))
                    : new(
                        X,
                        Y,
                        Width,
                        WindowManager.RenderResolutionY - Y - Conversions.IntFromString(value, 0));

                return;
            case "ControlDrawMode":
                if (value == "UniqueRenderTarget")
                    DrawMode = ControlDrawMode.UNIQUE_RENDER_TARGET;
                return;
        }
    }

    /// <summary>
    /// Disables and hides the control.
    /// </summary>
    public void Disable()
    {
        Enabled = false;
        Visible = false;
    }

    /// <summary>
    /// Enables and shows the control.
    /// </summary>
    public void Enable()
    {
        Enabled = true;
        Visible = true;
    }

    /// <summary>
    /// Destroys the control and all child controls to free up resources.
    /// </summary>
    public virtual void Kill()
    {
        if (Detached)
            Attach();

        var childrenCopy = new List<XNAControl>(Children);
        childrenCopy.ForEach(c => c.Kill());
        childrenCopy.ForEach(RemoveChild);

        Killed = true;
    }

    public virtual void RefreshSize()
    {
        foreach (XNAControl child in Children)
            child.RefreshSize();
    }

    /// <summary>
    /// Updates the control's logic and handles input.
    /// </summary>
    /// <param name="gameTime">Provides a snapshot of timing values.</param>
    public override void Update(GameTime gameTime)
    {
        Rectangle rectangle = GetWindowRectangle();

        timeSinceLastLeftClick += gameTime.ElapsedGameTime;

        lock (locker)
        {
            int callbackCount = callbacks.Count;

            if (callbackCount > 0)
            {
                for (int i = 0; i < callbackCount; i++)
                    callbacks[i].Invoke();

                // Do not clear the list; another thread could theoretically add an
                // item after we get the callback count, but before we lock
                callbacks.RemoveRange(0, callbackCount);
            }
        }

        if (IgnoreInputOnFrame)
        {
            ignoreInputOnFrame = false;
            return;
        }

        XNAControl activeChild = null;

        bool isInputCaptured = WindowManager.IsInputExclusivelyCaptured && WindowManager.SelectedControl != this;

        if (Cursor.IsOnScreen && IsActive && rectangle.Contains(Cursor.Location))
        {
            if (!cursorOnControl)
            {
                cursorOnControl = true;
                OnMouseEnter();
            }

            isIteratingChildren = true;

            using List<XNAControl>.Enumerator activeChildEnumerator = updateList.GetEnumerator();

            while (activeChildEnumerator.MoveNext())
            {
                XNAControl child = activeChildEnumerator.Current;

                if (child.Visible && !child.Detached && (child.Focused || (child.InputEnabled &&
                    child.GetWindowRectangle().Contains(Cursor.Location))))
                {
                    child.IsActive = true;
                    activeChild = child;
                    WindowManager.activeControlName = child.Name;
                    break;
                }
            }

            isIteratingChildren = false;

            Cursor.TextureIndex = CursorTextureIndex;

            bool handleClick = false;

            if (!isInputCaptured)
            {
                OnMouseOnControl();

                if (Cursor.HasMoved)
                    OnMouseMove();

                handleClick = activeChild == null;
            }

            if (!isLeftPressedOn && Cursor.LeftPressedDown)
            {
                isLeftPressedOn = true;

                if (!isInputCaptured)
                    OnMouseLeftDown();
            }
            else if (isLeftPressedOn && Cursor.LeftClicked)
            {
                if (handleClick)
                    OnLeftClick();

                isLeftPressedOn = false;
            }

            if (!isRightPressedOn && Cursor.RightPressedDown)
            {
                isRightPressedOn = true;

                if (!isInputCaptured)
                    OnMouseRightDown();
            }
            else if (isRightPressedOn && Cursor.RightClicked)
            {
                if (handleClick)
                    OnRightClick();

                isRightPressedOn = false;
            }

            if (Cursor.ScrollWheelValue != 0)
            {
                if (!isInputCaptured)
                    OnMouseScrolled();
            }
        }
        else if (cursorOnControl)
        {
            if (!isInputCaptured)
                OnMouseLeave();

            cursorOnControl = false;
            isRightPressedOn = false;
        }
        else
        {
            if (isLeftPressedOn && Cursor.LeftClicked)
                isLeftPressedOn = false;

            if (isRightPressedOn && Cursor.RightClicked)
                isRightPressedOn = false;
        }

        isIteratingChildren = true;

        using List<XNAControl>.Enumerator enumerator = updateList.GetEnumerator();

        while (enumerator.MoveNext())
        {
            XNAControl child = enumerator.Current;

            if (child != activeChild && !child.Detached)
                child.IsActive = false;

            if (child.Enabled)
            {
                child.Update(gameTime);
            }
        }

        isIteratingChildren = false;

        foreach (XNAControl child in childAddQueue)
            AddChildImmediate(child);

        childAddQueue.Clear();

        foreach (XNAControl child in childRemoveQueue)
            RemoveChildImmediate(child);

        childRemoveQueue.Clear();

        ChildHandledInput = activeChild != null;
        IsChildActive = activeChild != null;
    }

    /// <summary>
    /// Draws the control.
    /// DO NOT call manually unless you know what you're doing.
    /// </summary>
    internal void DrawInternal(GameTime gameTime)
    {
        if (!Visible)
            return;

        if (DrawMode == ControlDrawMode.UNIQUE_RENDER_TARGET)
        {
            DrawInternal_UniqueRenderTarget(gameTime);
        }
        else
        {
            drawPoint = GetRenderPoint();

            if (Detached)
            {
                DrawInternal_Detached(gameTime);
            }
            else
            {
                Draw(gameTime);
            }
        }
    }

    private void DrawInternal_UniqueRenderTarget(GameTime gameTime)
    {
        if (RenderTarget == null)
            RefreshRenderTarget();

        drawPoint = Point.Zero;
        Renderer.PushRenderTarget(RenderTarget);
        GraphicsDevice.Clear(Color.Transparent);
        Draw(gameTime);
        Renderer.PopRenderTarget();
        Rectangle rect = RenderRectangle();
        if (Scaling > 1 && Renderer.CurrentSettings.SamplerState != SamplerState.PointClamp)
        {
            Renderer.PushSettings(new(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null));
            DrawUniqueRenderTarget(rect);
            Renderer.PopSettings();
        }
        else
        {
            DrawUniqueRenderTarget(rect);
        }
    }

    /// <summary>
    /// Draws the control when it is detached from its parent.
    /// </summary>
    private void DrawInternal_Detached(GameTime gameTime)
    {
        int totalScaling = GetTotalScalingRecursive();
        if (totalScaling > 1)
        {
            // We have to use an unique render target for scaling
            Renderer.PushRenderTarget(RenderTargetStack.DetachedScaledControlRenderTarget);
            Draw(gameTime);
            Renderer.PopRenderTarget();
            Rectangle renderRectangle = RenderRectangle();
            if (Renderer.CurrentSettings.SamplerState != SamplerState.PointClamp)
            {
                Renderer.PushSettings(new(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null));
                DrawDetachedScaledTexture(renderRectangle, totalScaling);
                Renderer.PopSettings();
            }
            else
            {
                DrawDetachedScaledTexture(renderRectangle, totalScaling);
            }

            return;
        }

        Draw(gameTime);
    }

    private void DrawUniqueRenderTarget(Rectangle renderRectangle)
    {
        Renderer.DrawTexture(
            RenderTarget,
            new(0, 0, Width, Height),
            new(renderRectangle.X, renderRectangle.Y, ScaledWidth, ScaledHeight),
            Color.White * Alpha);
    }

    private void DrawDetachedScaledTexture(Rectangle renderRectangle, int totalScaling)
    {
        Renderer.DrawTexture(
            RenderTargetStack.DetachedScaledControlRenderTarget,
            renderRectangle,
            new(renderRectangle.X, renderRectangle.Y, Width * totalScaling, Height * totalScaling),
            Color.White * Alpha);
    }

    /// <summary>
    /// Draws the control and its child controls.
    /// </summary>
    /// <param name="gameTime">Provides a snapshot of timing values.</param>
    public override void Draw(GameTime gameTime) => DrawChildren(gameTime);

    /// <summary>
    /// Draws the control's child controls.
    /// </summary>
    protected void DrawChildren(GameTime gameTime)
    {
        using List<XNAControl>.Enumerator enumerator = drawList.GetEnumerator();

        while (enumerator.MoveNext())
        {
            XNAControl current = enumerator.Current;

            if (current.Visible && !current.Detached)
                current.DrawInternal(gameTime);
        }
    }

    #region Draw helpers

    private Point drawPoint;

    /// <summary>
    /// Draws a texture relative to the control's location.
    /// </summary>
    /// <param name="texture">The texture.</param>
    /// <param name="rectangle">The rectangle where to draw the texture
    /// relative to the control.</param>
    /// <param name="color">The remap color.</param>
    public void DrawTexture(Texture2D texture, Rectangle rectangle, Color color)
    {
        Renderer.DrawTexture(
            texture,
            new(
                drawPoint.X + rectangle.X,
                drawPoint.Y + rectangle.Y,
                rectangle.Width,
                rectangle.Height),
            color);
    }

    /// <summary>
    /// Draws a texture relative to the control's location.
    /// </summary>
    /// <param name="texture">The texture.</param>
    /// <param name="point">The point where to draw the texture
    /// relative to the control.</param>
    /// <param name="color">The remap color.</param>
    public void DrawTexture(Texture2D texture, Point point, Color color) =>
        Renderer.DrawTexture(texture, new(drawPoint.X + point.X, drawPoint.Y + point.Y, texture.Width, texture.Height), color);

    /// <summary>
    /// Draws a texture relative to the control's location
    /// within the used render target.
    /// </summary>
    public void DrawTexture(Texture2D texture, Rectangle sourceRectangle, Rectangle destinationRectangle, Color color)
    {
        var destRect = new Rectangle(
            drawPoint.X + destinationRectangle.X,
            drawPoint.Y + destinationRectangle.Y,
            destinationRectangle.Width,
            destinationRectangle.Height);

        Renderer.DrawTexture(texture, sourceRectangle, destRect, color);
    }

    /// <summary>
    /// Draws a texture relative to the control's location.
    /// </summary>
    public void DrawTexture(Texture2D texture, Vector2 location, float rotation, Vector2 origin, Vector2 scale, Color color, float layerDepth)
        => Renderer.DrawTexture(texture, new(location.X + drawPoint.X, location.Y + drawPoint.Y), rotation, origin, scale, color, layerDepth);

    /// <summary>
    /// Draws a texture relative to the control's location.
    /// </summary>
    public void DrawTexture(Texture2D texture, Rectangle destinationRectangle, Rectangle? sourceRectangle, Color color, float rotation, Vector2 origin, SpriteEffects effects, float layerDepth)
    {
        var destRect = new Rectangle(
            drawPoint.X + destinationRectangle.X,
            drawPoint.Y + destinationRectangle.Y,
            destinationRectangle.Width,
            destinationRectangle.Height);

        Renderer.DrawTexture(texture, destRect, sourceRectangle, color, rotation, origin, effects, layerDepth);
    }

    /// <summary>
    /// Draws a string relative to the control's location.
    /// </summary>
    public void DrawString(string text, int fontIndex, Vector2 location, Color color, float scale = 1.0f)
        => Renderer.DrawString(text, fontIndex, new(location.X + drawPoint.X, location.Y + drawPoint.Y), color, scale);

    /// <summary>
    /// Draws a string with a shadow, relative to the control's location.
    /// </summary>
    /// <param name="text">The string to draw.</param>
    /// <param name="fontIndex">The index of the font to use for drawing the string.</param>
    /// <param name="location">The location of the text to draw, relative to the control's location.</param>
    /// <param name="color">The color of the text.</param>
    /// <param name="scale">The scale of the text.</param>
    /// <param name="shadowDistance">How many distance units (typically pixels) the text shadow is offset from the text.</param>
    public void DrawStringWithShadow(string text, int fontIndex, Vector2 location, Color color, float scale = 1.0f, float shadowDistance = 1.0f)
        => Renderer.DrawStringWithShadow(text, fontIndex, new(location.X + drawPoint.X, location.Y + drawPoint.Y), color, scale, shadowDistance);

    /// <summary>
    /// Draws a rectangle's borders relative to the control's location
    /// with the given color and thickness.
    /// </summary>
    /// <param name="rect">The rectangle.</param>
    /// <param name="color">The color.</param>
    /// <param name="thickness">The thickness of the rectangle's borders.</param>
    public void DrawRectangle(Rectangle rect, Color color, int thickness = 1)
        => Renderer.DrawRectangle(new(rect.X + drawPoint.X, rect.Y + drawPoint.Y, rect.Width, rect.Height), color, thickness);

    /// <summary>
    /// Fills the control's drawing area with the given color.
    /// </summary>
    /// <param name="color">The color to fill the area with.</param>
    public void FillControlArea(Color color) => FillRectangle(new(0, 0, Width, Height), color);

    /// <summary>
    /// Fills a rectangle relative to the control's location with the given color.
    /// </summary>
    /// <param name="rect">The rectangle.</param>
    /// <param name="color">The color to fill the rectangle with.</param>
    public void FillRectangle(Rectangle rect, Color color)
        => Renderer.FillRectangle(new(rect.X + drawPoint.X, rect.Y + drawPoint.Y, rect.Width, rect.Height), color);

    /// <summary>
    /// Draws a line relative to the control's location.
    /// </summary>
    /// <param name="start">The start point of the line.</param>
    /// <param name="end">The end point of the line.</param>
    /// <param name="color">The color of the line.</param>
    /// <param name="thickness">The thickness of the line.</param>
    /// <param name="depth">The depth of the line for the depth buffer.</param>
    public void DrawLine(Vector2 start, Vector2 end, Color color, int thickness = 1, float depth = 0f)
        => Renderer.DrawLine(new(start.X + drawPoint.X, start.Y + drawPoint.Y), new(end.X + drawPoint.X, end.Y + drawPoint.Y), color, thickness, depth);

    #endregion

    /// <summary>
    /// Called when the mouse cursor enters the control's client rectangle.
    /// </summary>
    public virtual void OnMouseEnter() => MouseEnter?.Invoke(this, EventArgs.Empty);

    /// <summary>
    /// Called when the mouse cursor leaves the control's client rectangle.
    /// </summary>
    public virtual void OnMouseLeave() => MouseLeave?.Invoke(this, EventArgs.Empty);

    /// <summary>
    /// Called once when the left mouse button is pressed down while the cursor
    /// is on the control.
    /// </summary>
    public virtual void OnMouseLeftDown() => MouseLeftDown?.Invoke(this, EventArgs.Empty);

    /// <summary>
    /// Called once when the right mouse button is pressed down while the cursor
    /// is on the control.
    /// </summary>
    public virtual void OnMouseRightDown() => MouseRightDown?.Invoke(this, EventArgs.Empty);

    /// <summary>
    /// Called when the left mouse button has been
    /// clicked on the control's client rectangle.
    /// </summary>
    public virtual void OnLeftClick()
    {
        WindowManager.SelectedControl = this;

        LeftClick?.Invoke(this, EventArgs.Empty);

        if (timeSinceLastLeftClick < TimeSpan.FromSeconds(DOUBLE_CLICK_TIME))
        {
            OnDoubleLeftClick();
            return;
        }

        timeSinceLastLeftClick = TimeSpan.Zero;
    }

    /// <summary>
    /// Called when the left mouse button has been
    /// clicked twice on the control's client rectangle.
    /// </summary>
    public virtual void OnDoubleLeftClick() => DoubleLeftClick?.Invoke(this, EventArgs.Empty);

    /// <summary>
    /// Called when the right mouse button has been
    /// clicked on the control's client rectangle.
    /// </summary>
    public virtual void OnRightClick() => RightClick?.Invoke(this, EventArgs.Empty);

    /// <summary>
    /// Called when the mouse moves on the control's client rectangle.
    /// </summary>
    public virtual void OnMouseMove() => MouseMove?.Invoke(this, EventArgs.Empty);

    /// <summary>
    /// Called on each frame while the mouse is on the control's
    /// client rectangle.
    /// </summary>
    public virtual void OnMouseOnControl() => MouseOnControl?.Invoke(this, EventArgs.Empty);

    /// <summary>
    /// Called when the scroll wheel has been scrolled on the
    /// control's client rectangle.
    /// </summary>
    public virtual void OnMouseScrolled() => MouseScrolled?.Invoke(this, EventArgs.Empty);

    /// <summary>
    /// Called when the control's status as the selected (last-clicked)
    /// control has been changed.
    /// </summary>
    public virtual void OnSelectedChanged() => SelectedChanged?.Invoke(this, EventArgs.Empty);
}