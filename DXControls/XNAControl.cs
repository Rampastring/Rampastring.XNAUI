using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Rampastring.Tools;
using Rampastring.XNAUI.Input;

namespace Rampastring.XNAUI.DXControls
{
    /// <summary>
    /// The base class for a XNA-based UI control.
    /// </summary>
    public class XNAControl : DrawableGameComponent
    {
        const double DOUBLE_CLICK_TIME = 1.0;

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
        public XNAControl Parent { get; set; }

        public Cursor Cursor;
        public RKeyboard Keyboard;

        public List<XNAControl> Children = new List<XNAControl>();

        public string Name { get; set; }

        /// <summary>
        /// The display rectangle of the control inside its parent.
        /// </summary>
        public virtual Rectangle ClientRectangle { get; set; }

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
        public bool InputEnabled
        {
            get { return _inputEnabled; }
            set { _inputEnabled = value; }
        }

        bool isActive = false;

        /// <summary>
        /// Gets or sets a bool that determines whether this control is the current focus of input.
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

        TimeSpan timeSinceLastLeftClick = TimeSpan.Zero;

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
                // Logger.Log("Error: CenterOnParent called for a control which has no parent!");
                return;
            }

            Rectangle parentRectangle = Parent.ClientRectangle;

            ClientRectangle = new Rectangle((parentRectangle.Width - ClientRectangle.Width) / 2,
                (parentRectangle.Height - ClientRectangle.Height) / 2, ClientRectangle.Width, ClientRectangle.Height);
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

        List<Callback> Callbacks = new List<Callback>();

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
        public void AddChild(XNAControl child)
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
                    Text = value;
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
                    break;
                case "FillHeight":
                    if (Parent != null)
                    {
                        ClientRectangle = new Rectangle(ClientRectangle.X, ClientRectangle.Y,
                            ClientRectangle.Width, Parent.ClientRectangle.Height - ClientRectangle.Y - Conversions.IntFromString(value, 0));
                    }
                    break;
            }
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

            lock (locker)
            {
                foreach (Callback c in Callbacks)
                    c.Invoke();

                Callbacks.Clear();
            }

            if (IgnoreInputOnFrame)
            {
                _ignoreInputOnFrame = false;
                return;
            }

            if (IsActive && rectangle.Contains(Cursor.Location))
            {
                if (!CursorOnControl)
                    OnMouseEnter();

                bool activeChildFound = false;

                for (int i = Children.Count - 1; i > -1; i--)
                {
                    XNAControl child = Children[i];

                    if (child.Visible && (child.Focused || (child.InputEnabled && 
                        child.WindowRectangle().Contains(Cursor.Location) && !activeChildFound)))
                    {
                        Children[i].IsActive = true;
                        activeChildFound = true;
                    }
                    else
                        Children[i].IsActive = false;
                }

                Cursor.TextureIndex = CursorTextureIndex;

                CursorOnControl = true;

                MouseEventArgs mouseEventArgs = new MouseEventArgs(Cursor.Location - rectangle.Location);

                OnMouseOnControl(mouseEventArgs);

                if (Cursor.HasMoved)
                    OnMouseMove();

                if (Cursor.LeftClicked)
                {
                    OnLeftClick();
                }

                if (Cursor.RightClicked)
                {
                    OnRightClick();
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

            for (int i = Children.Count - 1; i > -1; i--)
            {
                if (Children[i].Enabled)
                {
                    Children[i].Update(gameTime);
                }
            }
        }

        /// <summary>
        /// Draws the control and its child controls.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        public override void Draw(GameTime gameTime)
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
            if (timeSinceLastLeftClick < TimeSpan.FromSeconds(DOUBLE_CLICK_TIME))
            {
                OnDoubleLeftClick();
                return;
            }

            timeSinceLastLeftClick = TimeSpan.Zero;

            LeftClick?.Invoke(this, EventArgs.Empty);
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
    }
}
