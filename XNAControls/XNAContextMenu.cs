using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Rampastring.XNAUI.XNAControls
{
    public class ContextMenuOptionEventArgs : EventArgs
    {
        public ContextMenuOptionEventArgs(int index)
        {
            Index = index;
        }

        public int Index { get; set; }
    }

    public class XNAContextMenu : XNAControl
    {
        public XNAContextMenu(WindowManager windowManager) : base(windowManager)
        {
            BorderColor = UISettings.PanelBorderColor;
            FocusColor = UISettings.FocusColor;
            BackColor = UISettings.BackgroundColor;
            DisabledItemColor = Color.Gray;
        }

        public delegate void OptionSelectedEventHandler(object sender, ContextMenuOptionEventArgs e);
        public event OptionSelectedEventHandler OptionSelected;

        int _itemHeight = 17;
        public int ItemHeight
        {
            get { return _itemHeight; }
            set { _itemHeight = value; }
        }

        public List<XNADropDownItem> Items = new List<XNADropDownItem>();

        public Color BorderColor { get; set; }

        public Color FocusColor { get; set; }

        public Color BackColor { get; set; }

        public Color DisabledItemColor { get; set; }

        public int FontIndex { get; set; }

        int hoveredIndex = -1;

        bool leftClickHandled = false;

        #region AddItem methods

        /// <summary>
        /// Adds an item into the context menu.
        /// </summary>
        /// <param name="item">The item.</param>
        public void AddItem(XNADropDownItem item)
        {
            Items.Add(item);
            ClientRectangle = new Rectangle(ClientRectangle.X, ClientRectangle.Y,
                ClientRectangle.Width, ClientRectangle.Height + ItemHeight);
        }

        /// <summary>
        /// Generates and adds an item with the specified text into the context menu.
        /// </summary>
        /// <param name="text">The text of the item.</param>
        public void AddItem(string text)
        {
            XNADropDownItem item = new XNADropDownItem();
            item.Text = text;
            item.TextColor = UISettings.AltColor;

            AddItem(item);
        }

        /// <summary>
        /// Generates and adds an item with the specified text and texture
        /// into the context menu.
        /// </summary>
        /// <param name="text">The text of the item.</param>
        /// <param name="texture">The item's texture.</param>
        public void AddItem(string text, Texture2D texture)
        {
            XNADropDownItem item = new XNADropDownItem();
            item.Text = text;
            item.TextColor = UISettings.AltColor;
            item.Texture = texture;

            AddItem(item);
        }

        /// <summary>
        /// Generates and adds an item with the specified text
        /// and text color into the context menu.
        /// </summary>
        /// <param name="text">The text of the item.</param>
        /// <param name="color">The color of the item's text.</param>
        public void AddItem(string text, Color color)
        {
            XNADropDownItem item = new XNADropDownItem();
            item.Text = text;
            item.TextColor = color;

            AddItem(item);
        }

        #endregion

        public void ClearItems()
        {
            Items.Clear();
            ClientRectangle = new Rectangle(ClientRectangle.X, ClientRectangle.Y, ClientRectangle.Width, 2);
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            // Hide the drop-down if the left mouse button is clicked while the
            // cursor isn't on this control
            if (Cursor.LeftClicked && !leftClickHandled)
                OnLeftClick();

            leftClickHandled = false;

            // Update hovered index

            int itemIndexOnCursor = GetItemIndexOnCursor();

            if (itemIndexOnCursor > -1 && Items[itemIndexOnCursor].Selectable)
                hoveredIndex = itemIndexOnCursor;
            else
                hoveredIndex = -1;
        }

        public override void OnLeftClick()
        {
            base.OnLeftClick();

            leftClickHandled = true;

            int itemIndexOnCursor = GetItemIndexOnCursor();

            if (itemIndexOnCursor > -1)
            {
                if (Items[itemIndexOnCursor].Selectable)
                {
                    OptionSelected?.Invoke(this, new ContextMenuOptionEventArgs(itemIndexOnCursor));
                    Visible = false;
                    Enabled = false;
                }

                return;
            }

            Visible = false;
            Enabled = false;
        }

        /// <summary>
        /// Returns the index of the item that the cursor currently points to.
        /// </summary>
        private int GetItemIndexOnCursor()
        {
            Point p = GetCursorPoint();

            Rectangle displayRectangle = WindowRectangle();

            if (p.X < 0 || p.X > ClientRectangle.Width ||
                p.Y > ClientRectangle.Height ||
                p.Y < 0)
            {
                return -1;
            }

            int y = p.Y;
            int itemIndex = y / _itemHeight;

            if (itemIndex < Items.Count && itemIndex > -1)
            {
                return itemIndex;
            }

            return -1;
        }

        public override void Draw(GameTime gameTime)
        {
            Rectangle wr = WindowRectangle();

            Renderer.FillRectangle(new Rectangle(wr.X + 1, wr.Y + 1, wr.Width - 2, wr.Height - 2), BackColor);
            Renderer.DrawRectangle(new Rectangle(wr.X, wr.Y, wr.Width, wr.Height), BorderColor);

            for (int i = 0; i < Items.Count; i++)
            {
                XNADropDownItem item = Items[i];

                int y = wr.Y + 1 + i * ItemHeight;
                if (hoveredIndex == i)
                {
                    Renderer.FillRectangle(new Rectangle(wr.X + 1, y, wr.Width - 2, ItemHeight), FocusColor);
                }
                else
                    Renderer.FillRectangle(new Rectangle(wr.X + 1, y, wr.Width - 2, ItemHeight), BackColor);

                int textX = 2;
                if (item.Texture != null)
                {
                    Renderer.DrawTexture(item.Texture, new Rectangle(wr.X + 1, y + 1, item.Texture.Width, item.Texture.Height), Color.White);
                    textX += item.Texture.Width + 1;
                }

                Color textColor;

                if (item.Selectable)
                    textColor = item.TextColor;
                else
                    textColor = DisabledItemColor;

                Renderer.DrawStringWithShadow(item.Text, FontIndex, new Vector2(wr.X + textX, y + 1), textColor);
            }

            base.Draw(gameTime);
        }
    }
}
