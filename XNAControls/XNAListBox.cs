using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Rampastring.XNAUI.XNAControls
{
    public class XNAListBox : XNAPanel
    {
        public XNAListBox(WindowManager windowManager) : base(windowManager)
        {
            FocusColor = UISettings.FocusColor;
            DefaultItemColor = UISettings.AltColor;
        }

        const double SCROLL_REPEAT_TIME = 0.03;
        const double FAST_SCROLL_TRIGGER_TIME = 0.4;

        public delegate void HoveredIndexChangedEventHandler(object sender, EventArgs e);
        public event HoveredIndexChangedEventHandler HoveredIndexChanged;

        public delegate void SelectedIndexChangedEventHandler(object sender, EventArgs e);
        public event SelectedIndexChangedEventHandler SelectedIndexChanged;

        public delegate void TopIndexChangedEventHandler(object sender, EventArgs e);
        public event TopIndexChangedEventHandler TopIndexChanged;

        #region Public members

        public List<XNAListBoxItem> Items = new List<XNAListBoxItem>();

        public Texture2D BorderTexture { get; set; }

        public Color FocusColor { get; set; }

        public Color DefaultItemColor { get; set; }

        public int LineHeight = 15;

        public int FontIndex { get; set; }

        bool _allowMultiLineItems = true;
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

        #endregion

        float itemAlphaRate = 0.01f;
        public float ItemAlphaRate
        { get { return itemAlphaRate; } set { itemAlphaRate = value; } }

        TimeSpan scrollKeyTime = TimeSpan.Zero;
        TimeSpan timeSinceLastScroll = TimeSpan.Zero;
        bool isScrollingQuickly = false;

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

        public void Clear()
        {
            //foreach (DXListBoxItem item in Items)
            //{
            //    if (item.Texture != null)
            //        item.Texture.Dispose();
            //}

            Items.Clear();
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

        public void AddItem(string text, Color textColor)
        {
            AddItem(text, textColor, true);
        }

        public void AddItem(string text, Color textColor, bool selectable)
        {
            XNAListBoxItem item = new XNAListBoxItem();
            item.TextColor = textColor;
            item.Text = text;
            item.Selectable = selectable;
            AddItem(item);
        }

        public void AddItem(XNAListBoxItem listBoxItem)
        {
            //if (LastIndex == Items.Count - 1 && GetTotalLineCount() > GetNumberOfLinesOnList())
            //{
            //    int scrolledLineCount = 0;
            //    while (true)
            //    {
            //        DXListBoxItem topItem = Items[TopIndex];
            //        TopIndex++;
            //        scrolledLineCount += topItem.TextLines.Count;

            //        if (scrolledLineCount >= listBoxItem.TextLines.Count || TopIndex == Items.Count - 1)
            //            break;
            //    }
            //}

            int width = ClientRectangle.Width - 4;
            if (listBoxItem.Texture != null)
                width -= listBoxItem.Texture.Width + 2;
            List<string> textLines = Renderer.GetFixedTextLines(listBoxItem.Text, FontIndex, width);
            if (textLines.Count == 0)
                textLines.Add(String.Empty);
            listBoxItem.TextLines = textLines;

            // Split too long lines
            for (int i = 0; i < textLines.Count; i++)
            {
                string line = textLines[i];

                StringBuilder sb = new StringBuilder(line);

                while (Renderer.GetTextDimensions(sb.ToString(), FontIndex).X >
                    ClientRectangle.Width - TextBorderDistance * 2)
                {
                    sb.Remove(sb.Length - 1, 1);
                }

                if (sb.ToString() != line)
                {
                    textLines[i] = sb.ToString();
                    textLines.Insert(i + 1, line.Substring(sb.Length));
                }
            }

            if (textLines.Count > 1 && !AllowMultiLineItems)
                textLines.RemoveRange(1, textLines.Count - 1);

            if (textLines.Count == 1)
            {
                Vector2 textSize = Renderer.GetTextDimensions(textLines[0], FontIndex);
                listBoxItem.TextYPadding = (LineHeight - (int)textSize.Y) / 2;

                if (listBoxItem.IsHeader)
                {
                    listBoxItem.TextXPadding = (ClientRectangle.Width - (int)textSize.X) / 2;
                }
            }

            Items.Add(listBoxItem);
        }

        int GetTotalLineCount()
        {
            int lineCount = 0;

            foreach (XNAListBoxItem item in Items)
                lineCount += item.TextLines.Count;

            return lineCount;
        }

        int GetNumberOfLinesOnList()
        {
            return (ClientRectangle.Height - 4) / LineHeight;
        }

        public void ScrollToBottom()
        {
            // Calculate the number of lines we need to scroll
            int lastDisplayedItemIndex = LastIndex;

            int linesToScroll = 0;

            for (int i = lastDisplayedItemIndex + 1; i < Items.Count; i++)
                linesToScroll += Items[i].TextLines.Count;

            // Now let's scroll by the necessary number of lines
            int scrolledLines = 0;

            while (scrolledLines < linesToScroll)
            {
                scrolledLines += Items[TopIndex].TextLines.Count;
                TopIndex++;
            }
        }

        public override void Initialize()
        {
            base.Initialize();

            BorderTexture = AssetLoader.CreateTexture(Color.White, 1, 1);
        }

        void ScrollUp()
        {
            for (int i = SelectedIndex - 1; i > -1; i--)
            {
                if (Items[i].Selectable)
                {
                    SelectedIndex = i;
                    if (TopIndex > i)
                        TopIndex = i;
                    return;
                }
            }
        }

        void ScrollDown()
        {
            int scrollLineCount = 1;
            for (int i = SelectedIndex + 1; i < Items.Count; i++)
            {
                if (Items[i].Selectable)
                {
                    SelectedIndex = i;
                    while (LastIndex < i)
                        TopIndex++;
                    return;
                }
                scrollLineCount++;
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

        void HandleScrollKeyDown(GameTime gameTime, Action action)
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

        public override void OnMouseScrolled()
        {
            if (GetTotalLineCount() <= GetNumberOfLinesOnList())
            {
                TopIndex = 0;
                return;
            }

            TopIndex -= Cursor.ScrollWheelValue;

            if (TopIndex < 0)
                TopIndex = 0;

            int lastIndex = LastIndex;

            if (lastIndex == Items.Count - 1)
            {
                while (LastIndex == lastIndex)
                {
                    TopIndex--;
                }

                TopIndex++;
            }

            base.OnMouseScrolled();
        }

        public override void OnMouseOnControl(MouseEventArgs eventArgs)
        {
            base.OnMouseOnControl(eventArgs);

            int itemIndex = GetItemIndexOnCursor(eventArgs.RelativeLocation);
            HoveredIndex = itemIndex;
        }

        public override void OnRightClick()
        {
            base.OnRightClick();

            SelectedIndex = -1;
        }

        public override void OnLeftClick()
        {
            base.OnLeftClick();

            int itemIndex = GetItemIndexOnCursor(GetCursorPoint());

            if (itemIndex == -1)
                return;

            if (Items[itemIndex].Selectable)
                SelectedIndex = itemIndex;
        }

        public override void OnMouseLeave()
        {
            base.OnMouseLeave();

            HoveredIndex = -1;
        }

        int GetItemIndexOnCursor(Point mouseLocation)
        {
            int height = 2;

            Rectangle windowRectangle = WindowRectangle();

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

        public override void Draw(GameTime gameTime)
        {
            base.Draw(gameTime);

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
                    Renderer.DrawTexture(BorderTexture, 
                        new Rectangle(windowRectangle.X + 1, windowRectangle.Y + height, windowRectangle.Width - 2, lbItem.TextLines.Count * LineHeight),
                        GetColorWithAlpha(FocusColor));
                }

                if (lbItem.Texture != null)
                {
                    int textureHeight = lbItem.Texture.Height;
                    int textureYPosition = 0;

                    if (lbItem.Texture.Height > LineHeight)
                        textureHeight = LineHeight;
                    else
                        textureYPosition = (LineHeight - textureHeight) / 2;

                    Renderer.DrawTexture(lbItem.Texture,
                        new Rectangle(windowRectangle.X + x, windowRectangle.Y + height + textureYPosition, 
                        lbItem.Texture.Width, textureHeight), Color.White);

                    x += lbItem.Texture.Width + 2;
                }

                x += lbItem.TextXPadding;

                for (int j = 0; j < lbItem.TextLines.Count; j++)
                {
                    Renderer.DrawStringWithShadow(lbItem.TextLines[j], FontIndex, 
                        new Vector2(windowRectangle.X + x, windowRectangle.Y + height + j * LineHeight + lbItem.TextYPadding),
                        GetColorWithAlpha(lbItem.TextColor));
                }

                height += lbItem.TextLines.Count * LineHeight;
            }
        }
    }
}
