using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Rampastring.Tools;
using Rampastring.XNAUI.Input;

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
            _windowManager = windowManager;
            Cursor = windowManager.Cursor;
            Keyboard = windowManager.Keyboard;
        }

        #region Events

        public event EventHandler MouseEnter;

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

        public Cursor Cursor;
        public RKeyboard Keyboard;

        /// <summary>
        /// A list of the control's children. Don't add children to this list directly;
        /// call the AddChild method instead.
        /// </summary>
        public List<XNAControl> Children = new List<XNAControl>();

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
        private bool isPressedOn = false;

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

        public int GetLocationX()
        {
            if (Parent != null)
                return ClientRectangle.X + Parent.GetLocationX();

            return ClientRectangle.X;
        }

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

        /// <summary>
        /// Adds a child control to the control.
        /// </summary>
        /// <param name="child">The child control.</param>
        public virtual void AddChild(XNAControl child)
        {
            child.Parent = this;
            child.Initialize();
            Children.Add(child);
        }

        /// <summary>
        /// Adds a child control to the control without calling the child's Initialize method.
        /// </summary>
        /// <param name="child">The child control.</param>
        public void AddChildWithoutInitialize(XNAControl child)
        {
            child.Parent = this;
            Children.Add(child);
        }

        /// <summary>
        /// Adds a child control to the control, making the added child
        /// the "first child" of this control.
        /// </summary>
        /// <param name="child">The child control.</param>
        public void AddChildToFirstIndex(XNAControl child)
        {
            child.Parent = this;
            child.Initialize();
            Children.Insert(0, child);
        }

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
                case "Size":
                    string[] size = value.Split(',');
                    ClientRectangle = new Rectangle(ClientRectangle.X, ClientRectangle.Y,
                        Int32.Parse(size[0]), Int32.Parse(size[1]));
                    break;
                case "Location":
                    string[] location = value.Split(',');
                    ClientRectangle = new Rectangle(Int32.Parse(location[0]), Int32.Parse(location[1]),
                        ClientRectangle.Width, ClientRectangle.Height);
                    break;
                case "RemapColor":
                    string[] colors = value.Split(',');
                    RemapColor = new Color(Int32.Parse(colors[0]), Int32.Parse(colors[1]), Int32.Parse(colors[2]), 255);
                    break;
                case "Text":
                    Text = value.Replace("@", Environment.NewLine);
                    break;
                case "Visible":
                    Visible = Conversions.BooleanFromString(value, true);
                    Enabled = Visible;
                    break;
                case "Enabled":
                    Enabled = Conversions.BooleanFromString(value, true);
                    break;
                case "DistanceFromRightBorder":
                    if (Parent != null)
                    {
                        ClientRectangle = new Rectangle(Parent.ClientRectangle.Width - ClientRectangle.Width - Conversions.IntFromString(value, 0),
                            ClientRectangle.Y, ClientRectangle.Width, ClientRectangle.Height);
                    }
                    break;
                case "DistanceFromBottomBorder":
                    if (Parent != null)
                    {
                        ClientRectangle = new Rectangle(ClientRectangle.X, Parent.ClientRectangle.Height - ClientRectangle.Height - Conversions.IntFromString(value, 0),
                            ClientRectangle.Width, ClientRectangle.Height);
                    }
                    break;
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
                    break;
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
                    break;
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
                    OnMouseEnter();

                for (int i = Children.Count - 1; i > -1; i--)
                {
                    XNAControl child = Children[i];

                    if (child.Visible && (child.Focused || (child.InputEnabled && 
                        child.WindowRectangle().Contains(Cursor.Location) && activeChild == null)))
                    {
                        Children[i].IsActive = true;
                        activeChild = Children[i];
                        break;
                    }
                }

                Cursor.TextureIndex = CursorTextureIndex;

                CursorOnControl = true;

                MouseEventArgs mouseEventArgs = new MouseEventArgs(
                    new Point(Cursor.Location.X - rectangle.Location.X,
                    Cursor.Location.Y - rectangle.Location.Y));

                OnMouseOnControl(mouseEventArgs);

                if (Cursor.HasMoved)
                    OnMouseMove();

                if (Cursor.LeftPressedDown || Cursor.RightPressedDown)
                    isPressedOn = true;

                if (isPressedOn && activeChild == null)
                {
                    if (Cursor.LeftClicked)
                    {
                        OnLeftClick();
                    }

                    if (Cursor.RightClicked)
                    {
                        OnRightClick();
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
            }
            else if (isPressedOn && Cursor.LeftClicked)
            {
                isPressedOn = false;
            }

            for (int i = Children.Count - 1; i > -1; i--)
            {
                var child = Children[i];

                if (child != activeChild)
                    child.IsActive = false;

                if (child.Enabled)
                {
                    child.Update(gameTime);
                }
            }
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
            for (int i = 0; i < Children.Count; i++)
            {
                if (Children[i].Visible)
                {
                    Children[i].Draw(gameTime);
                }
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
