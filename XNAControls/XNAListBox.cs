using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Rampastring.XNAUI.Input;
using System;
using System.Collections.Generic;
using Rampastring.Tools;
using System.Globalization;
using System.Collections.ObjectModel;

namespace Rampastring.XNAUI.XNAControls
{
    /// <summary>
    /// A list box.
    /// </summary>
    public class XNAListBox : XNAPanel
    {
        private const int MARGIN = 2;
        private const int ITEM_TEXT_TEXTURE_MARGIN = 2;
        private const double SCROLL_REPEAT_TIME = 0.03;
        private const double FAST_SCROLL_TRIGGER_TIME = 0.4;

        /// <summary>
        /// Creates a new list box instance.
        /// </summary>
        /// <param name="windowManager"></param>
        public XNAListBox(WindowManager windowManager) : base(windowManager)
        {
            DrawMode = ControlDrawMode.UNIQUE_RENDER_TARGET;
            ScrollBar = new XNAScrollBar(WindowManager);
            ScrollBar.ScrollStep = LineHeight;
            ClientRectangleUpdated += XNAListBox_ClientRectangleUpdated;
        }

        private void XNAListBox_ClientRectangleUpdated(object sender, EventArgs e)
        {
            if (ScrollBar != null)
            {
                ScrollBar.ClientRectangle = new Rectangle(Width - ScrollBar.ScrollWidth - 1,
                    1, ScrollBar.ScrollWidth, Height - 2);
                ScrollBar.DisplayedPixelCount = Height - MARGIN * 2;
                ScrollBar.Refresh();
            }
        }

        public delegate void HoveredIndexChangedEventHandler(object sender, EventArgs e);
        public event HoveredIndexChangedEventHandler HoveredIndexChanged;

        public delegate void SelectedIndexChangedEventHandler(object sender, EventArgs e);
        public event SelectedIndexChangedEventHandler SelectedIndexChanged;

        public delegate void TopIndexChangedEventHandler(object sender, EventArgs e);
        public event TopIndexChangedEventHandler TopIndexChanged;

        #region Public members

        /// <summary>
        /// Returns the list of items in the list box.
        /// If you manipulate the list directly, call
        /// RefreshScrollbar afterwards.
        /// TODO change to ObservableCollection?
        /// </summary>
        public List<XNAListBoxItem> Items = new List<XNAListBoxItem>();

        private Color? _focusColor;

        public Color FocusColor
        {
            get => _focusColor ?? UISettings.ActiveSettings.FocusColor;
            set { _focusColor = value; }
        }

        private Color? _defaultItemColor;

        public Color DefaultItemColor
        {
            get => _defaultItemColor ?? UISettings.ActiveSettings.AltColor;
            set { _defaultItemColor = value; }
        }

        private int _lineHeight = 15;

        /// <summary>
        /// Gets or sets the height of a single line of text in the list box.
        /// </summary>
        public int LineHeight
        {
            get => _lineHeight;
            set { _lineHeight = value; ScrollBar.ScrollStep = value; }
        }

        public int FontIndex { get; set; }

        /// <summary>
        /// If set to false, only the first line will be displayed from items
        /// that are long enough to cover more than one line. Changing this
        /// only affects new items in the list box; existing items are not
        /// truncated!
        /// </summary>
        public bool AllowMultiLineItems { get; set; } = true;

        /// <summary>
        /// If set to true, the user is able to scroll the listbox items
        /// by using keyboard keys.
        /// </summary>
        public bool AllowKeyboardInput { get; set; } = true;

        /// <summary>
        /// Gets or sets the distance between the text of a list box item
        /// and the list box border in pixels.
        /// </summary>
        public int TextBorderDistance { get; set; } = 3;

        private int _viewTop;

        public int ViewTop
        {
            get => _viewTop;
            set
            {
                if (value != _viewTop)
                {
                    _viewTop = value;
                    TopIndexChanged?.Invoke(this, EventArgs.Empty);
                    ScrollBar.RefreshButtonY(_viewTop);
                }
            }
        }

        public int TopIndex
        {
            get
            {
                int h = 0;
                for (int i = 0; i < Items.Count; i++)
                {
                    h += Items[i].TextLines.Count * LineHeight;
                    if (h > ViewTop)
                        return i;
                }
                return 0;
            }
            set
            {
                int h = 0;
                for (int i = 0; i < value; i++)
                {
                    h += Items[i].TextLines.Count * LineHeight;
                }
                ViewTop = h;
            }
        }

        public int LastIndex
        {
            get
            {
                int height = 2 - ViewTop % LineHeight;

                Rectangle windowRectangle = RenderRectangle();

                for (int i = TopIndex; i < Items.Count; i++)
                {
                    XNAListBoxItem lbItem = Items[i];

                    height += lbItem.TextLines.Count * LineHeight;

                    if (height > Height)
                        return i - 1;
                }

                return Items.Count - 1;
            }
        }

        float itemAlphaRate = 0.01f;
        public float ItemAlphaRate
        { get { return itemAlphaRate; } set { itemAlphaRate = value; } }

        int selectedIndex = -1;
        public int SelectedIndex
        {
            get { return selectedIndex; }
            set
            {
                int oldSelectedIndex = selectedIndex;

                selectedIndex = value;

                if (value != oldSelectedIndex && SelectedIndexChanged != null)
                    SelectedIndexChanged(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Gets the currently selected list box item.
        /// </summary>
        public XNAListBoxItem SelectedItem
        {
            get
            {
                if (SelectedIndex < 0 || SelectedIndex >= Items.Count)
                    return null;

                return Items[SelectedIndex];
            }
        }

        int hoveredIndex = -1;
        public int HoveredIndex
        {
            get
            {
                return hoveredIndex;
            }
            set
            {
                int oldHoveredIndex = hoveredIndex;

                hoveredIndex = value;

                if (value != oldHoveredIndex && HoveredIndexChanged != null)
                    HoveredIndexChanged(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Returns the number of text lines that can fit on the list box at a time.
        /// </summary>
        public int NumberOfLinesOnList
        {
            get { return (ClientRectangle.Height - 4) / LineHeight; }
        }

        private bool _enableScrollbar = true;

        /// <summary>
        /// Controls whether the integrated listbox scrollbar is used.
        /// </summary>
        public bool EnableScrollbar
        {
            get { return _enableScrollbar; }
            set
            {
                _enableScrollbar = value;

                ScrollBar.Visible = _enableScrollbar;
                ScrollBar.Enabled = _enableScrollbar;
            }
        }

        private bool _allowRightClickUnselect = true;

        /// <summary>
        /// Gets or sets a bool that determines whether the user is able to un-select
        /// the currently selected listbox item by right-clicking on the list box.
        /// </summary>
        public bool AllowRightClickUnselect
        {
            get { return _allowRightClickUnselect; }
            set { _allowRightClickUnselect = value; }
        }

        private bool _drawSelectionUnderScrollbar = false;

        /// <summary>
        /// Controls whether the highlighted background of the selected item should
        /// be drawn under the scrollbar area.
        /// </summary>
        public bool DrawSelectionUnderScrollbar
        {
            get { return _drawSelectionUnderScrollbar; }
            set { _drawSelectionUnderScrollbar = value; }
        }

        #endregion

        protected XNAScrollBar ScrollBar;

        private TimeSpan scrollKeyTime = TimeSpan.Zero;
        private TimeSpan timeSinceLastScroll = TimeSpan.Zero;
        private bool isScrollingQuickly = false;
        private bool selectedIndexChanged = false;

        protected override void ParseAttributeFromINI(IniFile iniFile, string key, string value)
        {
            switch (key)
            {
                case "EnableScrollbar":
                    EnableScrollbar = Conversions.BooleanFromString(value, true);
                    return;
                case "DrawSelectionUnderScrollbar":
                    DrawSelectionUnderScrollbar = Conversions.BooleanFromString(value, true);
                    return;
            }

            base.ParseAttributeFromINI(iniFile, key, value);
        }

        public void Clear()
        {
            //foreach (DXListBoxItem item in Items)
            //{
            //    if (item.Texture != null)
            //        item.Texture.Dispose();
            //}

            Items.Clear();
            RefreshScrollbar();
        }

        /// <summary>
        /// Adds a selectable item to the list box with the default item color.
        /// </summary>
        /// <param name="text">The text of the item.</param>
        public void AddItem(string text)
        {
            AddItem(text, null, true);
        }

        public void AddItem(string text, bool selectable)
        {
            AddItem(text, null, selectable);
        }

        public void AddItem(string text, Texture2D texture)
        {
            AddItem(text, texture, true, null);
        }

        public void AddItem(string text, Color textColor)
        {
            AddItem(text, textColor, true);
        }

        public void AddItem(string text, Color? textColor, bool selectable)
        {
            AddItem(text, null, selectable, textColor);
        }

        public void AddItem(string text, Texture2D texture, bool selectable, Color? textColor = null)
        {
            XNAListBoxItem item = new XNAListBoxItem();
            if (textColor.HasValue)
                item.TextColor = textColor.Value;
            item.Text = text;
            item.Texture = texture;
            item.Selectable = selectable;
            AddItem(item);
        }

        /// <summary>
        /// Adds an item into the list box.
        /// </summary>
        /// <param name="listBoxItem">The item to add.</param>
        public void AddItem(XNAListBoxItem listBoxItem)
        {
            int width = Width - TextBorderDistance * 2;
            if (EnableScrollbar)
            {
                width -= ScrollBar.Width;
            }

            if (listBoxItem.Texture != null)
            {
                int textureHeight = listBoxItem.Texture.Height;
                int textureWidth = listBoxItem.Texture.Width;

                if (listBoxItem.Texture.Height > LineHeight)
                {
                    double scaleRatio = textureHeight / (double)LineHeight;
                    textureHeight = LineHeight;
                    textureWidth = (int)(textureWidth / scaleRatio);
                }

                width -= textureWidth + ITEM_TEXT_TEXTURE_MARGIN;
            }

            // Apply word wrap if needed
            List<string> textLines = Renderer.GetFixedTextLines(listBoxItem.Text, FontIndex, width);
            if (textLines.Count == 0)
                textLines.Add(string.Empty);
            listBoxItem.TextLines = textLines;

            if (textLines.Count > 1 && !AllowMultiLineItems)
                textLines.RemoveRange(1, textLines.Count - 1);

            if (textLines.Count == 1)
            {
                Vector2 textSize = Renderer.GetTextDimensions(textLines[0], FontIndex);
                listBoxItem.TextYPadding = (LineHeight - (int)textSize.Y) / 2;

                if (listBoxItem.IsHeader)
                {
                    listBoxItem.TextXPadding = (width - (int)textSize.X) / 2;
                }
            }

            Items.Add(listBoxItem);
            RefreshScrollbar();
        }

        /// <summary>
        /// Refreshes the scroll bar's status. Call when list box items are modified.
        /// </summary>
        public void RefreshScrollbar()
        {
            int length = 0;
            foreach (var item in Items)
                length += item.TextLines.Count * LineHeight;
            ScrollBar.Length = length;
            ScrollBar.DisplayedPixelCount = Height - MARGIN * 2;
            ScrollBar.Refresh();
        }

        /// <summary>
        /// Returns the total amount of lines in all list box items combined.
        /// </summary>
        private int GetTotalLineCount()
        {
            int lineCount = 0;

            foreach (XNAListBoxItem item in Items)
                lineCount += item.TextLines.Count;

            return lineCount;
        }

        /// <summary>
        /// Scrolls the list box so that the last item is entirely visible.
        /// </summary>
        public void ScrollToBottom()
        {
            int displayedLineCount = NumberOfLinesOnList;
            TopIndex = Items.Count;
            ViewTop -= Height - MARGIN * 2;
        }

        /// <summary>
        /// Initializes the list box.
        /// </summary>
        public override void Initialize()
        {
            base.Initialize();

            Keyboard.OnKeyPressed += Keyboard_OnKeyPressed;

#if !XNA
            Game.Window.TextInput += Window_TextInput;
#else
            KeyboardEventInput.CharEntered += KeyboardEventInput_CharEntered;
#endif

            ScrollBar.ClientRectangle = new Rectangle(Width - ScrollBar.ScrollWidth - 1,
                1, ScrollBar.ScrollWidth, Height - 2);
            ScrollBar.Scrolled += ScrollBar_Scrolled;
            AddChild(ScrollBar);
            ScrollBar.Refresh();

            ParentChanged += Parent_ClientRectangleUpdated;

            if (Parent != null)
                Parent.ClientRectangleUpdated += Parent_ClientRectangleUpdated;
        }

        private void Parent_ClientRectangleUpdated(object sender, EventArgs e)
        {
            ScrollBar.Refresh();
        }

        /// <summary>
        /// Returns the width of the list box's scroll bar.
        /// </summary>
        public int GetScrollBarWidth()
        {
            return ScrollBar.Width;
        }

        private void ScrollBar_Scrolled(object sender, EventArgs e)
        {
            ViewTop = ScrollBar.ViewTop;
        }

        /// <summary>
        /// Allows copying items to the clipboard using Ctrl + C.
        /// </summary>
        private void Keyboard_OnKeyPressed(object sender, KeyPressEventArgs e)
        {
            if (!IsActive || !Enabled || SelectedItem == null)
                return;

            if (e.PressedKey == Keys.C && Keyboard.IsCtrlHeldDown())
                System.Windows.Forms.Clipboard.SetText(SelectedItem.Text);
        }

#if XNA
        private void KeyboardEventInput_CharEntered(object sender, KeyboardEventArgs e)
        {
            HandleCharInput(e.Character);
        }
#else
        private void Window_TextInput(object sender, TextInputEventArgs e)
        {
            HandleCharInput(e.Character);
        }
#endif

        /// <summary>
        /// Allows the user to select items by selecting the list box and then
        /// pressing the first letter of the item's text.
        /// </summary>
        /// <param name="character">The entered character.</param>
        private void HandleCharInput(char character)
        {
            if (WindowManager.SelectedControl != this || !Enabled || !Parent.Enabled || !WindowManager.HasFocus || !AllowKeyboardInput)
                return;

            string charString = character.ToString();

            for (int i = SelectedIndex + 1; i < Items.Count; i++)
            {
                var item = Items[i];

                if (!item.Selectable)
                    return;

                if (item.TextLines.Count == 0)
                    return;

                if (item.TextLines[0].StartsWith(charString, true, CultureInfo.CurrentCulture))
                {
                    SelectedIndex = i;

                    int lastIndex = LastIndex;

                    if (lastIndex < SelectedIndex)
                        TopIndex += SelectedIndex - lastIndex;

                    break;
                }
            }
        }

        public override void Update(GameTime gameTime)
        {
            foreach (XNAListBoxItem lbItem in Items)
            {
                if (lbItem.Alpha < 1.0f)
                    lbItem.Alpha += ItemAlphaRate;
            }

            if (IsActive && AllowKeyboardInput)
            {
                if (Keyboard.IsKeyHeldDown(Keys.Up))
                {
                    HandleScrollKeyDown(gameTime, ScrollUp);
                }
                else if (Keyboard.IsKeyHeldDown(Keys.Down))
                {
                    HandleScrollKeyDown(gameTime, ScrollDown);
                }
                else
                {
                    isScrollingQuickly = false;
                    timeSinceLastScroll = TimeSpan.Zero;
                    scrollKeyTime = TimeSpan.Zero;
                }
            }

            base.Update(gameTime);
        }

        /// <summary>
        /// Handles input from scrolling keys.
        /// </summary>
        /// <param name="gameTime">GameTime.</param>
        /// <param name="action">The action to execute when scrolling.</param>
        private void HandleScrollKeyDown(GameTime gameTime, Action action)
        {
            if (scrollKeyTime.Equals(TimeSpan.Zero))
                action();

            scrollKeyTime += gameTime.ElapsedGameTime;

            if (isScrollingQuickly)
            {
                timeSinceLastScroll += gameTime.ElapsedGameTime;

                if (timeSinceLastScroll > TimeSpan.FromSeconds(SCROLL_REPEAT_TIME))
                {
                    timeSinceLastScroll = TimeSpan.Zero;
                    action();
                }
            }

            if (scrollKeyTime > TimeSpan.FromSeconds(FAST_SCROLL_TRIGGER_TIME) && !isScrollingQuickly)
            {
                isScrollingQuickly = true;
                timeSinceLastScroll = TimeSpan.Zero;
            }
        }

        /// <summary>
        /// Handles scrolling by holding down the up arrow key.
        /// </summary>
        private void ScrollUp()
        {
            for (int i = SelectedIndex - 1; i > -1; i--)
            {
                if (Items[i].Selectable)
                {
                    SelectedIndex = i;
                    if (TopIndex > i)
                        TopIndex = i;

                    ScrollBar.RefreshButtonY(ViewTop);
                    return;
                }
            }

            ScrollBar.RefreshButtonY(ViewTop);
        }

        /// <summary>
        /// Handles scrolling by holding down the down arrow key.
        /// </summary>
        private void ScrollDown()
        {
            int scrollLineCount = 1;
            for (int i = SelectedIndex + 1; i < Items.Count; i++)
            {
                if (Items[i].Selectable)
                {
                    SelectedIndex = i;
                    while (LastIndex < i)
                        TopIndex++;

                    ScrollBar.RefreshButtonY(ViewTop);
                    return;
                }
                scrollLineCount++;
            }

            ScrollBar.RefreshButtonY(ViewTop);
        }

        /// <summary>
        /// Handles input from a scroll wheel.
        /// </summary>
        public override void OnMouseScrolled()
        {
            if (GetTotalLineCount() <= NumberOfLinesOnList)
            {
                TopIndex = 0;
                return;
            }

            ViewTop -= Cursor.ScrollWheelValue * ScrollBar.ScrollStep;

            if (ViewTop < 0)
            {
                TopIndex = 0;
                ScrollBar.RefreshButtonY(ViewTop);
                return;
            }

            int lastIndex = LastIndex;

            if (lastIndex == Items.Count - 1)
            {
                while (LastIndex == lastIndex && TopIndex > 0)
                {
                    TopIndex--;
                }

                TopIndex++;
            }

            ScrollBar.RefreshButtonY(ViewTop);

            base.OnMouseScrolled();
        }

        /// <summary>
        /// Updates the hovered item index while the cursor is on this control.
        /// </summary>
        public override void OnMouseOnControl(MouseEventArgs eventArgs)
        {
            base.OnMouseOnControl(eventArgs);

            int itemIndex = GetItemIndexOnCursor(eventArgs.RelativeLocation);
            HoveredIndex = itemIndex;
        }

        /// <summary>
        /// Clears the selection on right-click.
        /// </summary>
        public override void OnRightClick()
        {
            if (AllowRightClickUnselect)
                SelectedIndex = -1;

            base.OnRightClick();
        }

        /// <summary>
        /// Selects an item when the user left-clicks on this control.
        /// </summary>
        public override void OnMouseLeftDown()
        {
            int itemIndex = GetItemIndexOnCursor(GetCursorPoint());

            if (itemIndex == -1)
                return;

            selectedIndexChanged = false;

            if (Items[itemIndex].Selectable && itemIndex != SelectedIndex)
            {
                selectedIndexChanged = true;
                SelectedIndex = itemIndex;
            }

            base.OnMouseLeftDown();
        }

        public override void OnDoubleLeftClick()
        {
            // We don't want to send a "double left click" message if the user
            // is just quickly changing the selected index
            if (!selectedIndexChanged)
                base.OnDoubleLeftClick();
        }

        /// <summary>
        /// Updates the hovered index when the mouse cursor leaves this control's client rectangle.
        /// </summary>
        public override void OnMouseLeave()
        {
            base.OnMouseLeave();

            HoveredIndex = -1;
        }

        /// <summary>
        /// Returns the index of the list box item that the cursor is
        /// currently pointing at, or -1 if the cursor doesn't point at any item
        /// of this list box.
        /// </summary>
        /// <param name="mouseLocation">The location of the cursor relative 
        /// to this control.</param>
        private int GetItemIndexOnCursor(Point mouseLocation)
        {
            int height = 2 - (ViewTop % LineHeight);

            if (mouseLocation.X < 0)
                return -1;

            if (EnableScrollbar)
            {
                if (mouseLocation.X > Width - ScrollBar.ScrollWidth)
                    return -1;
            }
            else if (mouseLocation.X > Width)
                return -1;

            for (int i = TopIndex; i < Items.Count; i++)
            {
                XNAListBoxItem lbItem = Items[i];

                height += lbItem.TextLines.Count * LineHeight;

                if (height > mouseLocation.Y)
                {
                    return i;
                }

                if (height > Height)
                {
                    return -1;
                }
            }

            return -1;
        }

        /// <summary>
        /// Draws the list box and its items.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        public override void Draw(GameTime gameTime)
        {
            DrawPanel();

            int height = 2 - (ViewTop % LineHeight);

            for (int i = TopIndex; i < Items.Count; i++)
            { 
                XNAListBoxItem lbItem = Items[i];

                if (height > Height)
                    break;

                int x = TextBorderDistance;

                if (i == SelectedIndex)
                {
                    int drawnWidth;

                    if (DrawSelectionUnderScrollbar || !ScrollBar.IsDrawn() || !EnableScrollbar)
                    {
                        drawnWidth = Width - 2;
                    }
                    else
                    {
                        drawnWidth = Width - 2 - ScrollBar.Width;
                    }

                    FillRectangle(new Rectangle(1, height, drawnWidth,
                        lbItem.TextLines.Count * LineHeight),
                        FocusColor);
                }

                if (lbItem.Texture != null)
                {
                    int textureHeight = lbItem.Texture.Height;
                    int textureWidth = lbItem.Texture.Width;
                    int textureYPosition = 0;

                    if (lbItem.Texture.Height > LineHeight)
                    {
                        double scaleRatio = textureHeight / (double)LineHeight;
                        textureHeight = LineHeight;
                        textureWidth = (int)(textureWidth / scaleRatio);
                    }
                    else
                        textureYPosition = (LineHeight - textureHeight) / 2;

                    DrawTexture(lbItem.Texture,
                        new Rectangle(x, height + textureYPosition, 
                        textureWidth, textureHeight), Color.White);

                    x += textureWidth + ITEM_TEXT_TEXTURE_MARGIN;
                }

                x += lbItem.TextXPadding;

                for (int j = 0; j < lbItem.TextLines.Count; j++)
                {
                    DrawStringWithShadow(lbItem.TextLines[j], FontIndex, 
                        new Vector2(x, height + j * LineHeight + lbItem.TextYPadding),
                        lbItem.TextColor);
                }

                height += lbItem.TextLines.Count * LineHeight;
            }

            if (DrawBorders)
                DrawPanelBorders();

            DrawChildren(gameTime);
        }
    }
}
