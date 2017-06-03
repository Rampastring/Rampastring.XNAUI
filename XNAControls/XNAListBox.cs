using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Rampastring.XNAUI.Input;
using System;
using System.Collections.Generic;
using Rampastring.Tools;
using System.Globalization;

namespace Rampastring.XNAUI.XNAControls
{
    /// <summary>
    /// A list box.
    /// </summary>
    public class XNAListBox : XNAPanel
    {
        private const int ITEM_TEXT_TEXTURE_MARGIN = 2;
        private const double SCROLL_REPEAT_TIME = 0.03;
        private const double FAST_SCROLL_TRIGGER_TIME = 0.4;

        /// <summary>
        /// Creates a new list box instance.
        /// </summary>
        /// <param name="windowManager"></param>
        public XNAListBox(WindowManager windowManager) : base(windowManager)
        {
            FocusColor = UISettings.FocusColor;
            DefaultItemColor = UISettings.AltColor;

            scrollBar = new XNAScrollBar(WindowManager);
        }

        public delegate void HoveredIndexChangedEventHandler(object sender, EventArgs e);
        public event HoveredIndexChangedEventHandler HoveredIndexChanged;

        public delegate void SelectedIndexChangedEventHandler(object sender, EventArgs e);
        public event SelectedIndexChangedEventHandler SelectedIndexChanged;

        public delegate void TopIndexChangedEventHandler(object sender, EventArgs e);
        public event TopIndexChangedEventHandler TopIndexChanged;

        #region Public members

        public List<XNAListBoxItem> Items = new List<XNAListBoxItem>();

        public Color FocusColor { get; set; }

        public Color DefaultItemColor { get; set; }

        public int LineHeight = 15;

        public int FontIndex { get; set; }

        bool _allowMultiLineItems = true;

        /// <summary>
        /// If set to false, only the first line will be displayed from items
        /// that are long enough to cover more than one line. Changing this
        /// only affects new items in the list box; existing items are not
        /// truncated!
        /// </summary>
        public bool AllowMultiLineItems
        {
            get { return _allowMultiLineItems; }
            set { _allowMultiLineItems = value; }
        }

        int _itemBorderDistance = 3;
        public int TextBorderDistance
        {
            get { return _itemBorderDistance; }
            set { _itemBorderDistance = value; }
        }

        int topIndex = 0;
        public int TopIndex
        {
            get { return topIndex; }
            set
            {
                if (value != topIndex)
                {
                    topIndex = value;
                    TopIndexChanged?.Invoke(this, EventArgs.Empty);
                    scrollBar.RefreshButtonY(topIndex);
                }
            }
        }

        public int LastIndex
        {
            get
            {
                int height = 2;

                Rectangle windowRectangle = WindowRectangle();

                for (int i = TopIndex; i < Items.Count; i++)
                {
                    XNAListBoxItem lbItem = Items[i];

                    height += lbItem.TextLines.Count * LineHeight;

                    if (height > ClientRectangle.Height)
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

                scrollBar.Visible = _enableScrollbar;
                scrollBar.Enabled = _enableScrollbar;
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

        public override Rectangle ClientRectangle
        {
            get
            {
                return base.ClientRectangle;
            }

            set
            {
                base.ClientRectangle = value;

                if (scrollBar != null)
                {
                    scrollBar.ClientRectangle = new Rectangle(ClientRectangle.Width - scrollBar.ScrollWidth - 1,
                        1, scrollBar.ScrollWidth, ClientRectangle.Height - 2);
                    scrollBar.DisplayedItemCount = NumberOfLinesOnList;
                    scrollBar.Refresh();
                }
            }
        }

        #endregion

        private XNAScrollBar scrollBar;

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

            scrollBar.ItemCount = 0;
            scrollBar.Refresh();
        }

        /// <summary>
        /// Adds a selectable item to the list box with the default item color.
        /// </summary>
        /// <param name="text">The text of the item.</param>
        public void AddItem(string text)
        {
            AddItem(text, DefaultItemColor, true);
        }

        public void AddItem(string text, bool selectable)
        {
            AddItem(text, DefaultItemColor, selectable);
        }

        public void AddItem(string text, Texture2D texture)
        {
            AddItem(text, DefaultItemColor, texture, true);
        }

        public void AddItem(string text, Color textColor)
        {
            AddItem(text, textColor, true);
        }

        public void AddItem(string text, Color textColor, bool selectable)
        {
            AddItem(text, textColor, null, selectable);
        }

        public void AddItem(string text, Color textColor, Texture2D texture, bool selectable)
        {
            XNAListBoxItem item = new XNAListBoxItem();
            item.TextColor = textColor;
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
            int width = ClientRectangle.Width - TextBorderDistance * 2;
            if (EnableScrollbar)
            {
                width -= scrollBar.ClientRectangle.Width;
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

            scrollBar.ItemCount = Items.Count;
            scrollBar.DisplayedItemCount = NumberOfLinesOnList;
            scrollBar.Refresh();
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
        /// Scrolls the list box so that the last item is visible.
        /// </summary>
        public void ScrollToBottom()
        {
            int displayedLineCount = NumberOfLinesOnList;
            int currentLineCount = 0;
            TopIndex = Items.Count;

            for (int i = Items.Count - 1; i > -1; i--)
            {
                currentLineCount += Items[i].TextLines.Count;

                if (currentLineCount <= displayedLineCount)
                    TopIndex--;
                else
                    break;
            }
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

            scrollBar.ClientRectangle = new Rectangle(ClientRectangle.Width - scrollBar.ScrollWidth - 1,
                1, scrollBar.ScrollWidth, ClientRectangle.Height - 2);
            scrollBar.Scrolled += ScrollBar_Scrolled;
            scrollBar.ScrolledToBottom += ScrollBar_ScrolledToBottom;
            AddChild(scrollBar);
            scrollBar.Refresh();

            ParentChanged += Parent_ClientRectangleUpdated;

            if (Parent != null)
                Parent.ClientRectangleUpdated += Parent_ClientRectangleUpdated;
        }

        private void Parent_ClientRectangleUpdated(object sender, EventArgs e)
        {
            scrollBar.Refresh();
        }

        private void ScrollBar_ScrolledToBottom(object sender, EventArgs e)
        {
            ScrollToBottom();
            scrollBar.RefreshButtonY(TopIndex);
        }

        private void ScrollBar_Scrolled(object sender, EventArgs e)
        {
            TopIndex = scrollBar.TopIndex;
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
            if (WindowManager.SelectedControl != this || !Enabled || !Parent.Enabled || !WindowManager.HasFocus)
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

            if (IsActive)
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

                    scrollBar.RefreshButtonY(TopIndex);
                    return;
                }
            }

            scrollBar.RefreshButtonY(TopIndex);
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

                    scrollBar.RefreshButtonY(TopIndex);
                    return;
                }
                scrollLineCount++;
            }

            scrollBar.RefreshButtonY(TopIndex);
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

            TopIndex -= Cursor.ScrollWheelValue;

            if (TopIndex < 0)
            {
                TopIndex = 0;
                scrollBar.RefreshButtonY(TopIndex);
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

            scrollBar.RefreshButtonY(TopIndex);

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
        public override void OnLeftClick()
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

            base.OnLeftClick();
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
            int height = 2;

            if (mouseLocation.X < 0)
                return -1;

            if (EnableScrollbar)
            {
                if (mouseLocation.X > ClientRectangle.Width - scrollBar.ScrollWidth)
                    return -1;
            }
            else if (mouseLocation.X > ClientRectangle.Width)
                return -1;

            for (int i = TopIndex; i < Items.Count; i++)
            {
                XNAListBoxItem lbItem = Items[i];

                height += lbItem.TextLines.Count * LineHeight;

                if (height > ClientRectangle.Height)
                {
                    return -1;
                }

                if (height > mouseLocation.Y)
                {
                    return i;
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

            Rectangle windowRectangle = WindowRectangle();

            int height = 2;

            for (int i = TopIndex; i < Items.Count; i++)
            { 
                XNAListBoxItem lbItem = Items[i];

                if (height + lbItem.TextLines.Count * LineHeight > ClientRectangle.Height)
                    break;

                int x = TextBorderDistance;

                if (i == SelectedIndex)
                {
                    int drawnWidth;

                    if (DrawSelectionUnderScrollbar || !scrollBar.IsDrawn() || !EnableScrollbar)
                    {
                        drawnWidth = windowRectangle.Width - 2;
                    }
                    else
                    {
                        drawnWidth = windowRectangle.Width - 2 - scrollBar.ClientRectangle.Width;
                    }

                    Renderer.FillRectangle(
                        new Rectangle(windowRectangle.X + 1, windowRectangle.Y + height,
                        drawnWidth, lbItem.TextLines.Count * LineHeight),
                        GetColorWithAlpha(FocusColor));
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

                    Renderer.DrawTexture(lbItem.Texture,
                        new Rectangle(windowRectangle.X + x, windowRectangle.Y + height + textureYPosition, 
                        textureWidth, textureHeight), Color.White);

                    x += textureWidth + ITEM_TEXT_TEXTURE_MARGIN;
                }

                x += lbItem.TextXPadding;

                for (int j = 0; j < lbItem.TextLines.Count; j++)
                {
                    Renderer.DrawStringWithShadow(lbItem.TextLines[j], FontIndex, 
                        new Vector2(windowRectangle.X + x, windowRectangle.Y + height + j * LineHeight + lbItem.TextYPadding),
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
