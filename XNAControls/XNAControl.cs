using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Rampastring.Tools;
using Rampastring.XNAUI.Input;
using System.Collections.ObjectModel;
using System.Linq;

namespace Rampastring.XNAUI.XNAControls
{
    /// <summary>
    /// The base class for a XNA-based UI control.
    /// </summary>
    public class XNAControl : DrawableGameComponent
    {
        const double DOUBLE_CLICK_TIME = 1.0;

        /// <summary>
        /// Creates a new control instance.
        /// </summary>
        /// <param name="windowManager">The WindowManager associated with this control.</param>
        public XNAControl(WindowManager windowManager) : base(windowManager.Game)
        {
            _windowManager = windowManager ?? throw new ArgumentNullException("windowManager");
        }

        #region Events

        public event EventHandler MouseEnter;

        public event EventHandler MouseLeftDown;
        public event EventHandler MouseRightDown;

        public event EventHandler MouseLeave;

        public event EventHandler MouseMove;

        public delegate void MouseOnControlEventHandler(object sender, MouseEventArgs e);
        public event MouseOnControlEventHandler MouseOnControl;

        public event EventHandler MouseScrolled;

        public event EventHandler LeftClick;
        public event EventHandler DoubleLeftClick;
        public event EventHandler RightClick;

        public event EventHandler ClientRectangleUpdated;

        public event EventHandler SelectedChanged;

        public event EventHandler ParentChanged;

        #endregion

        WindowManager _windowManager;

        /// <summary>
        /// Gets the window manager associated with this control.
        /// </summary>
        public WindowManager WindowManager
        {
            get { return _windowManager; }
        }

        #region Public members

        private XNAControl parent;

        /// <summary>
        /// Gets or sets the parent of this control.
        /// </summary>
        public XNAControl Parent
        {
            get { return parent; }
            set
            {
                parent = value;
                ParentChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Holds a reference to the cursor.
        /// </summary>
        protected Cursor Cursor
        {
            get { return WindowManager.Cursor; }
        }

        /// <summary>
        /// Holds a reference to the keyboard.
        /// </summary>
        protected RKeyboard Keyboard
        {
            get { return WindowManager.Keyboard; }
        }

        /// <summary>
        /// A list of the control's children. Don't add children to this list directly;
        /// call the AddChild method instead.
        /// </summary>
        private List<XNAControl> _children = new List<XNAControl>();

        private List<XNAControl> updateList = new List<XNAControl>();
        private List<XNAControl> drawList = new List<XNAControl>();

        private List<XNAControl> childAddQueue = new List<XNAControl>();
        private List<XNAControl> childRemoveQueue = new List<XNAControl>();

        /// <summary>
        /// A read-only list of the control's children. 
        /// Call the AddChild method to add children to the control.
        /// </summary>
        public ReadOnlyCollection<XNAControl> Children
        {
            get { return new ReadOnlyCollection<XNAControl>(_children); }
        }

        /// <summary>
        /// Gets or sets the name of this control. The name is only an identifier
        /// and does not affect functionality.
        /// </summary>
        public string Name { get; set; }

        private Rectangle _clientRectangle;

        /// <summary>
        /// The display rectangle of the control inside its parent.
        /// </summary>
        public virtual Rectangle ClientRectangle
        {
            get
            {
                return _clientRectangle;
            }
            set
            {
                _clientRectangle = value;

                ClientRectangleUpdated?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Shortcut for accessing and changing ClientRectangle.X.
        /// </summary>
        public int X
        {
            get { return ClientRectangle.X; }
            set { ClientRectangle = new Rectangle(value, ClientRectangle.Y, ClientRectangle.Width, ClientRectangle.Height); }
        }

        /// <summary>
        /// Shortcut for accessing and changing ClientRectangle.Y.
        /// </summary>
        public int Y
        {
            get { return ClientRectangle.Y; }
            set { ClientRectangle = new Rectangle(ClientRectangle.X, value, ClientRectangle.Width, ClientRectangle.Height); }
        }

        /// <summary>
        /// Shortcut for accessing and changing ClientRectangle.Width.
        /// </summary>
        public int Width
        {
            get { return ClientRectangle.Width; }
            set { ClientRectangle = new Rectangle(ClientRectangle.X, ClientRectangle.Y, value, ClientRectangle.Height); }
        }

        /// <summary>
        /// Shortcut for accessing and changing ClientRectangle.Height.
        /// </summary>
        public int Height
        {
            get { return ClientRectangle.Height; }
            set { ClientRectangle = new Rectangle(ClientRectangle.X, ClientRectangle.Y, ClientRectangle.Width, value); }
        }

        /// <summary>
        /// Shortcut for accessing ClientRectangle.Bottom.
        /// </summary>
        public int Bottom
        {
            get { return ClientRectangle.Bottom; }
        }

        /// <summary>
        /// Shortcut for accessing ClientRectangle.Right.
        /// </summary>
        public int Right
        {
            get { return ClientRectangle.Right; }
        }

        Color remapColor = Color.White;
        public Color RemapColor
        {
            get { return remapColor; }
            set { remapColor = value; }
        }

        bool CursorOnControl = false;

        float alpha = 1.0f;
        public virtual float Alpha
        {
            get
            { 
                if (Parent != null)
                    return alpha * Parent.Alpha;

                return alpha;
            }
            set
            {
                if (value > 1.0f)
                    alpha = 1.0f;
                else if (value < 0.0)
                    alpha = 0.0f;
                else
                    alpha = value;
            }
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

        bool _inputEnabled = true;
        
        /// <summary>
        /// Determines whether this control is able to handle input.
        /// If set to false, input management will ignore this control.
        /// </summary>
        public bool InputEnabled
        {
            get { return _inputEnabled; }
            set { _inputEnabled = value; }
        }

        bool isActive = false;

        /// <summary>
        /// Gets or sets a bool that determines whether this control is the current focus of the mouse cursor.
        /// </summary>
        public bool IsActive
        { 
            get
            {
                if (Parent != null)
                    return Parent.IsActive && isActive;

                return isActive;
            }
            set { isActive = value; }
        }

        bool _ignoreInputOnFrame = false;

        public bool IgnoreInputOnFrame
        {
            get
            {
                if (Parent == null)
                    return _ignoreInputOnFrame;
                else
                    return _ignoreInputOnFrame || Parent.IgnoreInputOnFrame;
            }
            set
            {
                _ignoreInputOnFrame = true;
            }
        }

        #endregion

        private TimeSpan timeSinceLastLeftClick = TimeSpan.Zero;
        private bool isLeftPressedOn = false;
        private bool isRightPressedOn = false;

        private bool isIteratingChildren = false;

        /// <summary>
        /// Checks if the last parent of this control is active.
        /// </summary>
        public bool IsLastParentActive()
        {
            if (Parent != null)
                return Parent.IsLastParentActive();

            return isActive;
        }

        public Color GetRemapColor()
        {
            return GetColorWithAlpha(RemapColor);
        }

        public virtual Color GetColorWithAlpha(Color baseColor)
        {
            return new Color(baseColor.R, baseColor.G, baseColor.B, (int)(Alpha * 255));
        }

        /// <summary>
        /// Returns the client rectangle of the control inside the game window.
        /// </summary>
        /// <returns>The client rectangle of the control inside the game window.</returns>
        public Rectangle WindowRectangle()
        {
            return new Rectangle(GetLocationX(), GetLocationY(), ClientRectangle.Width, ClientRectangle.Height);
        }

        /// <summary>
        /// Returns the control's absolute X coordinate within the game window.
        /// </summary>
        public int GetLocationX()
        {
            if (Parent != null)
                return ClientRectangle.X + Parent.GetLocationX();

            return ClientRectangle.X;
        }

        /// <summary>
        /// Returns the control's absolute Y coordinate within the game window.
        /// </summary>
        public int GetLocationY()
        {
            if (Parent != null)
                return ClientRectangle.Y + Parent.GetLocationY();

            return ClientRectangle.Y;
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

            Rectangle parentRectangle = Parent.ClientRectangle;

            ClientRectangle = new Rectangle((parentRectangle.Width - ClientRectangle.Width) / 2,
                (parentRectangle.Height - ClientRectangle.Height) / 2, ClientRectangle.Width, ClientRectangle.Height);
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

            Rectangle parentRectangle = Parent.ClientRectangle;

            ClientRectangle = new Rectangle((Parent.Width - Width) / 2,
                Y, Width, Height);
        }

        /// <summary>
        /// Gets the cursor's location relative to this control's location.
        /// </summary>
        /// <returns>A point that represents the cursor's location relative to this control's location.</returns>
        public Point GetCursorPoint()
        {
            return new Point(Cursor.Location.X - WindowRectangle().X, Cursor.Location.Y - WindowRectangle().Y);
        }

        private readonly object locker = new object();

        private List<Callback> Callbacks = new List<Callback>();

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
                throw new ArgumentNullException("child");

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
                throw new ArgumentNullException("child");

            if (isIteratingChildren)
            {
                throw new NotImplementedException("AddChildWithoutInitialize cannot currently be called" +
                    " while the control is iterating through its children.");
            }
            else
                AddChildImmediateWithoutInitialize(child);
        }

        /// <summary>
        /// Immediately adds a child control to the control.
        /// </summary>
        /// <param name="child">The child control.</param>
        private void AddChildImmediate(XNAControl child)
        {
            InitChild(child);
            child.Initialize();
            _children.Add(child);
            ReorderControls();
        }

        /// <summary>
        /// Immediately adds a child control to the control without calling the child's Initialize method.
        /// </summary>
        /// <param name="child">The child control.</param>
        private void AddChildImmediateWithoutInitialize(XNAControl child)
        {
            InitChild(child);
            _children.Add(child);
            ReorderControls();
        }

        /// <summary>
        /// Adds a child control to the control, making the added child
        /// the "first child" of this control.
        /// </summary>
        /// <param name="child">The child control.</param>
        private void AddChildToFirstIndexImmediate(XNAControl child)
        {
            InitChild(child);
            child.Initialize();
            _children.Insert(0, child);
            ReorderControls();
        }

        private void InitChild(XNAControl child)
        {
            child.Parent = this;
            child.UpdateOrderChanged += Child_UpdateOrderChanged;
            child.DrawOrderChanged += Child_DrawOrderChanged;
        }

        private void Child_DrawOrderChanged(object sender, EventArgs e)
        {
            drawList = _children.OrderBy(c => c.DrawOrder).ToList();
        }

        private void Child_UpdateOrderChanged(object sender, EventArgs e)
        {
            updateList = _children.OrderBy(c => c.UpdateOrder).Reverse().ToList();
        }

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
            if (_children.Remove(child))
            {
                child.UpdateOrderChanged -= Child_UpdateOrderChanged;
                child.DrawOrderChanged -= Child_DrawOrderChanged;
                ReorderControls();
            }
        }

        private void ReorderControls()
        {
            // Controls that are updated first should be drawn last
            // (on top of the other controls).
            // It's weird for the updateorder and draworder to behave differently,
            // but at this point we don't have a choice because of backwards compatibility.
            updateList = _children.OrderBy(c => c.UpdateOrder).Reverse().ToList();
            drawList = _children.OrderBy(c => c.DrawOrder).ToList();
        }

        #endregion

        public virtual void GetAttributes(IniFile iniFile)
        {
            foreach (XNAControl child in Children)
                child.GetAttributes(iniFile);

            List<string> keys = iniFile.GetSectionKeys(Name);

            if (keys == null)
                return;

            foreach (string key in keys)
                ParseAttributeFromINI(iniFile, key, iniFile.GetStringValue(Name, key, String.Empty));
        }

        protected virtual void ParseAttributeFromINI(IniFile iniFile, string key, string value)
        {
            switch (key)
            {
                case "DrawOrder":
                    DrawOrder = Int32.Parse(value);
                    return;
                case "UpdateOrder":
                    UpdateOrder = Int32.Parse(value);
                    return;
                case "Size":
                    string[] size = value.Split(',');
                    ClientRectangle = new Rectangle(ClientRectangle.X, ClientRectangle.Y,
                        Int32.Parse(size[0]), Int32.Parse(size[1]));
                    return;
                case "Location":
                    string[] location = value.Split(',');
                    ClientRectangle = new Rectangle(Int32.Parse(location[0]), Int32.Parse(location[1]),
                        ClientRectangle.Width, ClientRectangle.Height);
                    return;
                case "RemapColor":
                    string[] colors = value.Split(',');
                    RemapColor = new Color(Int32.Parse(colors[0]), Int32.Parse(colors[1]), Int32.Parse(colors[2]), 255);
                    return;
                case "Text":
                    Text = value.Replace("@", Environment.NewLine);
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
                        ClientRectangle = new Rectangle(Parent.ClientRectangle.Width - ClientRectangle.Width - Conversions.IntFromString(value, 0),
                            ClientRectangle.Y, ClientRectangle.Width, ClientRectangle.Height);
                    }
                    return;
                case "DistanceFromBottomBorder":
                    if (Parent != null)
                    {
                        ClientRectangle = new Rectangle(ClientRectangle.X, Parent.ClientRectangle.Height - ClientRectangle.Height - Conversions.IntFromString(value, 0),
                            ClientRectangle.Width, ClientRectangle.Height);
                    }
                    return;
                case "FillWidth":
                    if (Parent != null)
                    {
                        ClientRectangle = new Rectangle(ClientRectangle.X, ClientRectangle.Y,
                            Parent.ClientRectangle.Width - ClientRectangle.X - Conversions.IntFromString(value, 0), ClientRectangle.Height);
                    }
                    else
                    {
                        ClientRectangle = new Rectangle(ClientRectangle.X, ClientRectangle.Y,
                            WindowManager.RenderResolutionX - ClientRectangle.X - Conversions.IntFromString(value, 0),
                            ClientRectangle.Height);
                    }
                    return;
                case "FillHeight":
                    if (Parent != null)
                    {
                        ClientRectangle = new Rectangle(ClientRectangle.X, ClientRectangle.Y,
                            ClientRectangle.Width, Parent.ClientRectangle.Height - ClientRectangle.Y - Conversions.IntFromString(value, 0));
                    }
                    else
                    {
                        ClientRectangle = new Rectangle(ClientRectangle.X, ClientRectangle.Y,
                            ClientRectangle.Width,
                            WindowManager.RenderResolutionY - ClientRectangle.Y - Conversions.IntFromString(value, 0));
                    }
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
            foreach (XNAControl child in Children)
                child.Kill();

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
            Rectangle rectangle = WindowRectangle();

            timeSinceLastLeftClick += gameTime.ElapsedGameTime;

            int callbackCount = Callbacks.Count;

            if (callbackCount > 0)
            {
                lock (locker)
                {
                    for (int i = 0; i < callbackCount; i++)
                        Callbacks[i].Invoke();

                    Callbacks.RemoveRange(0, callbackCount);
                }
            }

            if (IgnoreInputOnFrame)
            {
                _ignoreInputOnFrame = false;
                return;
            }

            XNAControl activeChild = null;

            if (Cursor.IsOnScreen && IsActive && rectangle.Contains(Cursor.Location))
            {
                if (!CursorOnControl)
                {
                    CursorOnControl = true;
                    OnMouseEnter();
                }

                isIteratingChildren = true;

                var activeChildEnumerator = updateList.GetEnumerator();

                while (activeChildEnumerator.MoveNext())
                {
                    XNAControl child = activeChildEnumerator.Current;

                    if (child.Visible && (child.Focused || (child.InputEnabled && 
                        child.WindowRectangle().Contains(Cursor.Location) && activeChild == null)))
                    {
                        child.IsActive = true;
                        activeChild = child;
                        break;
                    }
                }

                isIteratingChildren = false;

                Cursor.TextureIndex = CursorTextureIndex;

                MouseEventArgs mouseEventArgs = new MouseEventArgs(
                    new Point(Cursor.Location.X - rectangle.Location.X,
                    Cursor.Location.Y - rectangle.Location.Y));

                OnMouseOnControl(mouseEventArgs);

                if (Cursor.HasMoved)
                    OnMouseMove();

                if (!isLeftPressedOn && Cursor.LeftPressedDown)
                {
                    isLeftPressedOn = true;
                    OnMouseLeftDown();
                }

                if (!isRightPressedOn && Cursor.RightPressedDown)
                {
                    isRightPressedOn = true;
                    OnMouseRightDown();
                }

                if (activeChild == null)
                {
                    if (isLeftPressedOn && Cursor.LeftClicked)
                    {
                        OnLeftClick();
                        isLeftPressedOn = false;
                    }

                    if (isRightPressedOn && Cursor.RightClicked)
                    {
                        OnRightClick();
                        isRightPressedOn = false;
                    }
                }

                if (Cursor.ScrollWheelValue != 0)
                {
                    OnMouseScrolled();
                }
            }
            else if (CursorOnControl)
            {
                OnMouseLeave();

                CursorOnControl = false;
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

            var enumerator = updateList.GetEnumerator();

            while (enumerator.MoveNext())
            {
                var child = enumerator.Current;

                if (child != activeChild)
                    child.IsActive = false;

                if (child.Enabled)
                {
                    child.Update(gameTime);
                }
            }

            isIteratingChildren = false;

            foreach (var child in childAddQueue)
                AddChildImmediate(child);

            childAddQueue.Clear();

            foreach (var child in childRemoveQueue)
                RemoveChildImmediate(child);

            childRemoveQueue.Clear();
        }

        /// <summary>
        /// Draws the control and its child controls.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        public override void Draw(GameTime gameTime)
        {
            DrawChildren(gameTime);
        }

        /// <summary>
        /// Draws the control's child controls.
        /// </summary>
        protected void DrawChildren(GameTime gameTime)
        {
            var enumerator = drawList.GetEnumerator();

            while (enumerator.MoveNext())
            {
                if (enumerator.Current.Visible)
                    enumerator.Current.Draw(gameTime);
            }
        }

        /// <summary>
        /// Called when the mouse cursor enters the control's client rectangle.
        /// </summary>
        public virtual void OnMouseEnter()
        {
            MouseEnter?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Called when the mouse cursor leaves the control's client rectangle.
        /// </summary>
        public virtual void OnMouseLeave()
        {
            MouseLeave?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Called once when the left mouse button is pressed down while the cursor
        /// is on the control.
        /// </summary>
        public virtual void OnMouseLeftDown()
        {
            MouseLeftDown?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Called once when the right mouse button is pressed down while the cursor
        /// is on the control.
        /// </summary>
        public virtual void OnMouseRightDown()
        {
            MouseRightDown?.Invoke(this, EventArgs.Empty);
        }

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
        public virtual void OnDoubleLeftClick()
        {
            DoubleLeftClick?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Called when the right mouse button has been 
        /// clicked on the control's client rectangle.
        /// </summary>
        public virtual void OnRightClick()
        {
            RightClick?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Called when the mouse moves on the control's client rectangle.
        /// </summary>
        public virtual void OnMouseMove()
        {
            MouseMove?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Called on each frame while the mouse is on the control's
        /// client rectangle.
        /// </summary>
        /// <param name="eventArgs">Mouse event arguments.</param>
        public virtual void OnMouseOnControl(MouseEventArgs eventArgs)
        {
            MouseOnControl?.Invoke(this, eventArgs);
        }

        /// <summary>
        /// Called when the scroll wheel has been scrolled on the 
        /// control's client rectangle.
        /// </summary>
        public virtual void OnMouseScrolled()
        {
            MouseScrolled?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Called when the control's status as the selected (last-clicked)
        /// control has been changed.
        /// </summary>
        public virtual void OnSelectedChanged()
        {
            SelectedChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
