using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;
using System.Globalization;
using System.Linq;
using Rampastring.Tools;

namespace Rampastring.XNAUI.XNAControls;

/// <summary>
/// A panel that allows for scrolling.
/// </summary>
public class XNAScrollPanel : XNAPanel
{
    private const double SCROLL_REPEAT_TIME = 0.03;
    private const double FAST_SCROLL_TRIGGER_TIME = 0.4;
    
    protected XNAHorizontalScrollBar HorizontalScrollBar;
    protected XNAScrollBar VerticalScrollBar;
    protected XNAPanel ContentPanel;
    protected XNAPanel CornerPanel;
    
    private TimeSpan _scrollKeyTime = TimeSpan.Zero;
    private TimeSpan _timeSinceLastScroll = TimeSpan.Zero;
    private bool _isScrollingQuickly = false;

    /// <summary>
    /// Raised when the scroll panel content is scrolled.
    /// </summary>
    public event EventHandler ViewPositionChanged;
    
    #region Properties
    
    #region Configurable properties
    
    /// <summary>
    /// Whether to allow scrolling with arrow keys.
    /// </summary>
    public bool AllowKeyboardInput { get; set; } = true;
    
    /// <summary>
    /// How fast this control is scrolled?
    /// </summary>
    public int ScrollStep
    {
        get => HorizontalScrollBar.ScrollStep;
        set => HorizontalScrollBar.ScrollStep 
            = VerticalScrollBar.ScrollStep 
                = value;
    }
    
    private Point _overscrollMargin;
    
    /// <summary>
    /// How much can the content be "overscrolled", used for the purposes of inner panel size calculation.
    /// </summary>
    public Point OverscrollMargin
    {
        get => _overscrollMargin;
        set
        {
            value = new(Math.Max(0, value.X), Math.Max(0, value.Y));
            
            if (_overscrollMargin == value)
                return;
            
            ContentSize += value - _overscrollMargin;
            _overscrollMargin = value;
        }
    }
    
    // TODO switching off scrolling and scrollbars
    // TODO automatic scrollbar switching off
    // TODO check for graceful handling of controls bigger than viewfinder
    
    #endregion
    
    /// <summary>
    /// Size of the inner panel that contains all the content.
    /// </summary>
    public Point ContentSize
    {
        get => ContentPanel.ClientRectangle.Size;
        private set
        {
            ContentPanel.ClientRectangle = ContentPanel.ClientRectangle with { Size = value };
            RecalculateScrollbars();
        }
    }
    
    /// <summary>
    /// The physical offset of the <see cref="ContentPanel"/>.
    /// </summary>
    private Point CurrentContentPanelPosition
    {
        get => ContentPanel.ClientRectangle.Location;
        set
        {
            value = new Point
            {
                X = Math.Clamp(value.X, Math.Min(0, -(ContentSize.X - ViewSize.X)), 0),
                Y = Math.Clamp(value.Y, Math.Min(0, -(ContentSize.Y - ViewSize.Y)), 0),
            };

            if (value == ContentPanel.ClientRectangle.Location)
                return;

            ContentPanel.ClientRectangle =  ContentPanel.ClientRectangle with { Location = value };
            ViewPositionChanged?.Invoke(this, EventArgs.Empty);
            VerticalScrollBar.RefreshButtonY(-value.Y);
            HorizontalScrollBar.RefreshButtonX(-value.X);
        }
    }
    
    /// <summary>
    /// The space that can be displayed by the control at once.
    /// </summary>
    public Point ViewSize => new()
        {
            X = VerticalScrollBar.Visible ? VerticalScrollBar.X : Width,
            Y = HorizontalScrollBar.Visible ? HorizontalScrollBar.Y : Height,
        };
    
    /// <summary>
    /// Location of the "viewfinder" over the <see cref="ContentPanel"/>.
    /// </summary>
    public Point CurrentViewPosition
    {
        get => new(-CurrentContentPanelPosition.X, -CurrentContentPanelPosition.Y);
        set => CurrentContentPanelPosition = new(-value.X, -value.Y);
    }
    
    protected Rectangle ViewWindowRectangle
    {
        get
        {
            var size = ViewSize;
            var factor = GetTotalScalingRecursive();
            
            return new(GetWindowPoint(),
                new(size.X * factor, size.Y * factor));
        }
    }

    /// <summary>
    /// The viewport area over the <see cref="ContentPanel"/>.
    /// </summary>
    public Rectangle CurrentViewRectangle => new(CurrentViewPosition, ViewSize);
    
    /// <summary>
    /// Whether there is something to scroll.
    /// </summary>
    public bool IsOverflowing => IsOverflowingHorizontally || IsOverflowingVertically;
    
    /// <summary>
    /// Whether there is something to scroll horizontally.
    /// </summary>
    public bool IsOverflowingHorizontally => ViewSize.X > ContentSize.X;
    
    /// <summary>
    /// Whether there is something to scroll vertically.
    /// </summary>
    public bool IsOverflowingVertically => ViewSize.Y > ContentSize.Y;
    
    #endregion
    
    public XNAScrollPanel(WindowManager windowManager) : base(windowManager)
    {
        DrawMode = ControlDrawMode.UNIQUE_RENDER_TARGET; //so controls can be clipped
        HorizontalScrollBar = new XNAHorizontalScrollBar(WindowManager);
        VerticalScrollBar = new XNAScrollBar(WindowManager);
        ContentPanel = new XNAPanel(WindowManager) { DrawBorders = false };
        CornerPanel = new XNAPanel(WindowManager) { DrawBorders = false };

        NameChanged += XNAScrollPanel_NameChanged;
        
        ClientRectangleUpdated += XNAScrollPanel_ClientRectangleUpdated;
    }

    public override void Initialize()
    {
        base.Initialize();
        
        ContentPanel.ChildAdded += ContentPanel_ChildAddedRemoved;
        ContentPanel.ChildRemoved += ContentPanel_ChildAddedRemoved;
        
        HorizontalScrollBar.Scrolled += HorizontalScrollBar_Scrolled;
        HorizontalScrollBar.MouseScrolledHorizontally += HorizontalScrollBar_MouseScrolledHorizontally;
        // additional handler for users without horizontal scroll
        HorizontalScrollBar.MouseScrolled += HorizontalScrollBar_MouseScrolled;
        
        VerticalScrollBar.Scrolled += VerticalScrollBar_Scrolled;
        VerticalScrollBar.MouseScrolled += VerticalScrollBar_MouseScrolled;
        
        ComposeControls();

        if (Parent != null)
            Parent.ClientRectangleUpdated += Parent_ClientRectangleUpdated;

        ParentChanging += XNAScrollPanel_ParentChanging;
        
        ParentChanged += XNAScrollPanel_ParentChanged;
    }

    protected virtual void ComposeControls()
    {
        AddChild(ContentPanel);
        AddChild(HorizontalScrollBar);
        AddChild(VerticalScrollBar);
        AddChild(CornerPanel);
    }

    public override void Kill()
    {
        ContentPanel.ChildAdded -= ContentPanel_ChildAddedRemoved;
        ContentPanel.ChildRemoved -= ContentPanel_ChildAddedRemoved;
        
        ParentChanged -= Parent_ClientRectangleUpdated;

        if (Parent != null)
            Parent.ClientRectangleUpdated -= Parent_ClientRectangleUpdated;
        
        base.Kill();
    }

    protected override void ParseControlINIAttribute(IniFile iniFile, string key, string value)
    {
        switch (key)
        {
            case "AllowKeyboardInput":
                AllowKeyboardInput = Conversions.BooleanFromString(value, true);
                return;
            case "ScrollStep":
                ScrollStep = int.Parse(value);
                return;
            case "OverscrollMarginSize":
                string[] size = value.Split(',');
                OverscrollMargin = new(
                    int.Parse(size[0], CultureInfo.InvariantCulture),
                    int.Parse(size[1], CultureInfo.InvariantCulture));
                return;
            case "OverscrollMarginWidth":
                OverscrollMargin = OverscrollMargin with { X = int.Parse(value, CultureInfo.InvariantCulture) };
                return;
            case "OverscrollMarginHeight":
                OverscrollMargin = OverscrollMargin with { Y = int.Parse(value, CultureInfo.InvariantCulture) };
                return;
            case "DrawBorders":  // overtaking this one since behavior needs to be adjusted
                DrawBorders = Conversions.BooleanFromString(value, true);
                RecalculateScrollbars();
                return;
            case "Padding":
                // padding is invalid for this control
                return;
        }

        base.ParseControlINIAttribute(iniFile, key, value);
    }

    #region Control behavior / Handlers
    
    private void XNAScrollPanel_NameChanged(object sender, EventArgs e)
    {
        HorizontalScrollBar.Name = $"{Name}.HorizontalScrollBar";
        VerticalScrollBar.Name = $"{Name}.VerticalScrollBar";
        ContentPanel.Name = $"{Name}.ContentPanel";
        CornerPanel.Name = $"{Name}.CornerPanel";
    }
    
    #region Recalculation handlers
    
    private void XNAScrollPanel_ParentChanging(object o, EventArgs eventArgs)
    {
        if (Parent != null)
            Parent.ClientRectangleUpdated -= Parent_ClientRectangleUpdated;
    }
    
    private void XNAScrollPanel_ClientRectangleUpdated(object sender, EventArgs e)
        => RecalculateScrollbars();

    private void ContentPanel_ChildAddedRemoved(object o, ControlEventArgs controlEventArgs)
    {
        RecalculateContentSize();
        RecalculateScrollbars();
    }
    
    private void XNAScrollPanel_ParentChanged(object sender, EventArgs e)
    {
        if (Parent != null)
            Parent.ClientRectangleUpdated += Parent_ClientRectangleUpdated;
        
        RecalculateScrollbars();
    }
    
    private void Parent_ClientRectangleUpdated(object sender, EventArgs e)
        => RecalculateScrollbars();

    #endregion

    #region Scroll handlers

    private void HorizontalScrollBar_Scrolled(object sender, EventArgs e)
        => CurrentViewPosition = CurrentViewPosition with { X = HorizontalScrollBar.ViewLeft };

    private void VerticalScrollBar_Scrolled(object sender, EventArgs e)
        => CurrentViewPosition = CurrentViewPosition with { Y = VerticalScrollBar.ViewTop };
    
    private void HorizontalScrollBar_MouseScrolledHorizontally(object sender, EventArgs e)
        => CurrentViewPosition = CurrentViewPosition with { X = CurrentViewPosition.X - Cursor.HorizontalScrollWheelValue * ScrollStep };

    private void HorizontalScrollBar_MouseScrolled(object sender, EventArgs e)
        => CurrentViewPosition = CurrentViewPosition with { X = CurrentViewPosition.X - Cursor.ScrollWheelValue * ScrollStep };

    private void VerticalScrollBar_MouseScrolled(object sender, EventArgs e)
        => CurrentViewPosition = CurrentViewPosition with { Y = CurrentViewPosition.Y - Cursor.ScrollWheelValue * ScrollStep };
    
    public override void OnMouseScrolled()
    {
        if (!ViewWindowRectangle.Contains(Cursor.Location))
            return;
        
        // scroll horizontally if no vertical scroll needed to ease the life of users without horizontal scroll
        if (!IsOverflowingVertically)
            CurrentViewPosition = CurrentViewPosition with { X = CurrentViewPosition.X - Cursor.ScrollWheelValue * ScrollStep };
        else
            CurrentViewPosition = CurrentViewPosition with { Y = CurrentViewPosition.Y - Cursor.ScrollWheelValue * ScrollStep };
        
        base.OnMouseScrolled();
    }
    
    public override void OnMouseScrolledHorizontally()
    {
        if (!ViewWindowRectangle.Contains(Cursor.Location))
            return;
        
        CurrentViewPosition = CurrentViewPosition with { X = CurrentViewPosition.X - Cursor.HorizontalScrollWheelValue * ScrollStep };
        
        base.OnMouseScrolled();
    }

    #endregion
    
    public override void Update(GameTime gameTime)
    {
        if (IsActive && AllowKeyboardInput)
        {
            bool up = Keyboard.IsKeyHeldDown(Keys.Up);
            bool down = Keyboard.IsKeyHeldDown(Keys.Down);
            bool left = Keyboard.IsKeyHeldDown(Keys.Left);
            bool right = Keyboard.IsKeyHeldDown(Keys.Right);
            
            bool scrollingHorizontally = left != right;
            bool scrollingVertically = up != down;
            bool anyValidInput = scrollingHorizontally || scrollingVertically;
            
            if (scrollingVertically)
                HandleScrollKeyDown(gameTime, up ? ScrollUp : ScrollDown);
            
            if (scrollingHorizontally)
                HandleScrollKeyDown(gameTime, left ? ScrollLeft : ScrollRight);

            if (!anyValidInput)
            {
                _isScrollingQuickly = false;
                _timeSinceLastScroll = TimeSpan.Zero;
                _scrollKeyTime = TimeSpan.Zero;
            }
        }

        base.Update(gameTime);
    }

    private void HandleScrollKeyDown(GameTime gameTime, Action action)
    {
        if (_scrollKeyTime.Equals(TimeSpan.Zero))
            action();

        WindowManager.SelectedControl = this;

        _scrollKeyTime += gameTime.ElapsedGameTime;

        if (_isScrollingQuickly)
        {
            _timeSinceLastScroll += gameTime.ElapsedGameTime;

            if (_timeSinceLastScroll > TimeSpan.FromSeconds(SCROLL_REPEAT_TIME))
            {
                _timeSinceLastScroll = TimeSpan.Zero;
                action();
            }
        }

        if (_scrollKeyTime > TimeSpan.FromSeconds(FAST_SCROLL_TRIGGER_TIME) && !_isScrollingQuickly)
        {
            _isScrollingQuickly = true;
            _timeSinceLastScroll = TimeSpan.Zero;
        }
    }
    
    #endregion

    #region Recalculation methods
    
    protected void RecalculateContentSize()
    {
        Point contentSize = ContentPanel.Children
            .Select(c => new Point(c.Right, c.Bottom))
            .Aggregate(Point.Zero, (accumulated, next) 
                => new Point(Math.Max(accumulated.X, next.X), Math.Max(accumulated.Y, next.Y)));

        ContentSize = contentSize + OverscrollMargin;
    }

    /// <summary>
    /// Readjusts the scrollbar sizes, max values etc.
    /// </summary>
    protected void RecalculateScrollbars()
    {
        HorizontalScrollBar.ClientRectangle = new()
        {
            X = DrawBorders ? 1 : 0,
            Y = Height
                - HorizontalScrollBar.ScrollHeight 
                - (DrawBorders ? 1 : 0),
            Width = Width
                - (DrawBorders ? 2 : 0)
                - (VerticalScrollBar.Visible ? VerticalScrollBar.X : 0),
            Height = HorizontalScrollBar.ScrollHeight,
        };

        VerticalScrollBar.ClientRectangle = new()
        {
            X = Width
                - VerticalScrollBar.ScrollWidth 
                - (DrawBorders ? 1 : 0),
            Y = DrawBorders ? 1 : 0,
            Width = VerticalScrollBar.ScrollWidth,
            Height = Height 
                - (DrawBorders ? 1 : 0)
                - (HorizontalScrollBar.Visible ? HorizontalScrollBar.Y : 0),
        };

        CornerPanel.ClientRectangle = new()
        {
            Location = ViewSize,
            Size = ClientRectangle.Size - ViewSize - (DrawBorders ? new Point(1) : Point.Zero),
        };
        CornerPanel.Visible = HorizontalScrollBar.Visible && VerticalScrollBar.Visible;
        
        HorizontalScrollBar.Length = ContentSize.X;
        VerticalScrollBar.Length = ContentSize.Y;
        HorizontalScrollBar.DisplayedPixelCount = ViewSize.X;
        VerticalScrollBar.DisplayedPixelCount = ViewSize.Y;
        
        HorizontalScrollBar.Refresh();
        VerticalScrollBar.Refresh();
    }
    
    #endregion

    #region Scroll methods

    public void ScrollTo(Rectangle rect)
    {
        CurrentViewPosition = new Point
        {
            X = Math.Clamp(value: CurrentViewRectangle.X,
                min: rect.X + rect.Width - CurrentViewRectangle.X,
                max: rect.X),
            Y = Math.Clamp(value: CurrentViewRectangle.Y,
                min: rect.Y + rect.Height - CurrentViewRectangle.Y,
                max: rect.Y),
        };
    }

    public void ScrollTo(Point point) => ScrollTo(new Rectangle(point, size: Point.Zero));
    
    public void ScrollToChildControl(XNAControl control) => ScrollTo(control.ClientRectangle);
    
    private void ScrollUp() => CurrentViewPosition = CurrentViewPosition with { Y = CurrentViewPosition.Y - ScrollStep };

    private void ScrollDown() => CurrentViewPosition = CurrentViewPosition with { Y = CurrentViewPosition.Y + ScrollStep };

    private void ScrollLeft() => CurrentViewPosition = CurrentViewPosition with { X = CurrentViewPosition.X - ScrollStep };

    private void ScrollRight() => CurrentViewPosition = CurrentViewPosition with { X = CurrentViewPosition.X + ScrollStep };
    
    public void ScrollToTop() => CurrentViewPosition = CurrentViewPosition with { Y = 0 };

    public void ScrollToBottom() => CurrentViewPosition = CurrentViewPosition with { Y = ContentSize.Y - ViewSize.Y };

    public void ScrollToLeft() => CurrentViewPosition = CurrentViewPosition with { X = 0 };

    public void ScrollToRight() => CurrentViewPosition = CurrentViewPosition with { X = ContentSize.X - ViewSize.X };

    public void ScrollToBegin()
    {
        ScrollToTop();
        ScrollToLeft();
    }
    
    public void ScrollToEnd()
    {
        ScrollToBottom();
        ScrollToRight();
    }
    
    #endregion
}