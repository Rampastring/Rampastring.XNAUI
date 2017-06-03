using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Rampastring.Tools;
using System;
using System.Collections.Generic;

namespace Rampastring.XNAUI.XNAControls
{
    /// <summary>
    /// A drop-down control.
    /// </summary>
    public class XNADropDown : XNAControl
    {
        /// <summary>
        /// Creates a new drop-down control.
        /// </summary>
        /// <param name="windowManager">The WindowManager associated with this control.</param>
        public XNADropDown(WindowManager windowManager) : base(windowManager)
        {
            BorderColor = UISettings.PanelBorderColor;
            FocusColor = UISettings.FocusColor;
            BackColor = UISettings.BackgroundColor;
            DisabledItemColor = Color.Gray;
            Height = ItemHeight + 2;
        }

        public delegate void SelectedIndexChangedEventHandler(object sender, EventArgs e);
        public event SelectedIndexChangedEventHandler SelectedIndexChanged;

        /// <summary>
        /// Raised when the user re-selects an already selected drop-down item.
        /// </summary>
        public event EventHandler IndexReselected;

        int _itemHeight = 17;

        /// <summary>
        /// The height of drop-down items.
        /// </summary>
        public int ItemHeight
        {
            get { return _itemHeight; }
            set { _itemHeight = value; }
        }

        public List<XNADropDownItem> Items = new List<XNADropDownItem>();

        /// <summary>
        /// Gets or sets the dropped-down status of the drop-down control.
        /// </summary>
        public bool IsDroppedDown { get; set; }

        bool _allowDropDown = true;

        /// <summary>
        /// Controls whether the drop-down control can be dropped down.
        /// </summary>
        public bool AllowDropDown
        {
            get { return _allowDropDown; }
            set
            {
                _allowDropDown = value;
                if (!_allowDropDown && IsDroppedDown)
                {
                    IsDroppedDown = false;
                    ClientRectangle = new Rectangle(ClientRectangle.X, ClientRectangle.Y,
                        ClientRectangle.Width, dropDownTexture.Height);
                }
            }
        }

        int _selectedIndex = -1;

        /// <summary>
        /// Gets or sets the selected index of the drop-down control.
        /// </summary>
        public int SelectedIndex
        {
            get { return _selectedIndex; }
            set
            {
                int oldSelectedIndex = _selectedIndex;

                _selectedIndex = value;

                if (value != oldSelectedIndex)
                    SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
                else
                    IndexReselected?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Gets the currently selected item of the drop-down control.
        /// </summary>
        public XNADropDownItem SelectedItem
        {
            get
            {
                if (SelectedIndex < 0 || SelectedIndex >= Items.Count)
                    return null;

                return Items[SelectedIndex];
            }
        }

        public int FontIndex { get; set; }

        public Color BorderColor { get; set; }

        public Color FocusColor { get; set; }

        public Color BackColor { get; set; }

        public Color DisabledItemColor { get; set; }

        Texture2D dropDownTexture { get; set; }
        Texture2D dropDownOpenTexture { get; set; }

        public SoundEffect ClickSoundEffect { get; set; }
        SoundEffectInstance _clickSoundInstance;

        int hoveredIndex = 0;

        bool clickedAfterOpen = false;

        #region AddItem methods

        /// <summary>
        /// Adds an item into the drop-down.
        /// </summary>
        /// <param name="item">The item.</param>
        public void AddItem(XNADropDownItem item)
        {
            Items.Add(item);
        }

        /// <summary>
        /// Generates and adds an item with the specified text into the drop-down.
        /// </summary>
        /// <param name="text">The text of the item.</param>
        public void AddItem(string text)
        {
            XNADropDownItem item = new XNADropDownItem();
            item.Text = text;
            item.TextColor = UISettings.AltColor;

            Items.Add(item);
        }

        /// <summary>
        /// Generates and adds an item with the specified text and texture
        /// into the drop-down.
        /// </summary>
        /// <param name="text">The text of the item.</param>
        /// <param name="texture">The item's texture.</param>
        public void AddItem(string text, Texture2D texture)
        {
            XNADropDownItem item = new XNADropDownItem();
            item.Text = text;
            item.TextColor = UISettings.AltColor;
            item.Texture = texture;

            Items.Add(item);
        }

        /// <summary>
        /// Generates and adds an item with the specified text
        /// and text color into the drop-down control.
        /// </summary>
        /// <param name="text">The text of the item.</param>
        /// <param name="color">The color of the item's text.</param>
        public void AddItem(string text, Color color)
        {
            XNADropDownItem item = new XNADropDownItem();
            item.Text = text;
            item.TextColor = color;

            Items.Add(item);
        }

        #endregion

        public override void Initialize()
        {
            base.Initialize();

            dropDownTexture = AssetLoader.LoadTexture("comboBoxArrow.png");
            dropDownOpenTexture = AssetLoader.LoadTexture("openedComboBoxArrow.png");

            ClientRectangle = new Rectangle(ClientRectangle.X, ClientRectangle.Y, ClientRectangle.Width, dropDownTexture.Height);

            if (ClickSoundEffect != null)
                _clickSoundInstance = ClickSoundEffect.CreateInstance();
        }

        protected override void ParseAttributeFromINI(IniFile iniFile, string key, string value)
        {
            switch (key)
            {
                case "ItemHeight":
                    ItemHeight = Conversions.IntFromString(value, ItemHeight);
                    return;
                case "ClickSoundEffect":
                    ClickSoundEffect = AssetLoader.LoadSound(value);
                    _clickSoundInstance = ClickSoundEffect.CreateInstance();
                    return;
                case "FontIndex":
                    FontIndex = Conversions.IntFromString(value, FontIndex);
                    return;
                case "BorderColor":
                    BorderColor = AssetLoader.GetRGBAColorFromString(value);
                    return;
                case "FocusColor":
                    FocusColor = AssetLoader.GetRGBAColorFromString(value);
                    return;
                case "BackColor":
                    BackColor = AssetLoader.GetRGBAColorFromString(value);
                    return;
                case "DisabledItemColor":
                    DisabledItemColor = AssetLoader.GetColorFromString(value);
                    return;
            }

            base.ParseAttributeFromINI(iniFile, key, value);
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            if (Cursor.LeftPressedDown)
            {
                if (IsActive)
                {
                    if (!AllowDropDown)
                        return;

                    if (IsDroppedDown)
                        return;

                    if (_clickSoundInstance != null)
                        AudioMaster.PlaySound(_clickSoundInstance);

                    Rectangle wr = WindowRectangle();

                    clickedAfterOpen = false;
                    IsDroppedDown = true;
                    ClientRectangle = new Rectangle(ClientRectangle.X, ClientRectangle.Y,
                        ClientRectangle.Width, dropDownTexture.Height + 1 + ItemHeight * Items.Count);
                    hoveredIndex = -1;
                    return;
                }
                else
                {
                    if (IsDroppedDown)
                    {
                        OnLeftClick();
                    }
                }
            }

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

            if (!IsDroppedDown)
            {
                return;
            }

            int itemIndexOnCursor = GetItemIndexOnCursor();

            if (itemIndexOnCursor == -1 && !clickedAfterOpen)
            {
                clickedAfterOpen = true;
                return;
            }

            if (itemIndexOnCursor > -1)
            {
                if (Items[itemIndexOnCursor].Selectable)
                    SelectedIndex = itemIndexOnCursor;
                else
                    return;
            }

            if (_clickSoundInstance != null)
                AudioMaster.PlaySound(_clickSoundInstance);

            IsDroppedDown = false;
            ClientRectangle = new Rectangle(ClientRectangle.X, ClientRectangle.Y,
                ClientRectangle.Width, dropDownTexture.Height);
        }

        public override void OnMouseScrolled()
        {
            if (!AllowDropDown)
                return;

            if (Cursor.ScrollWheelValue < 0)
            {
                if (SelectedIndex >= Items.Count - 1)
                    return;

                if (Items[SelectedIndex + 1].Selectable)
                    SelectedIndex++;
            }

            if (Cursor.ScrollWheelValue > 0)
            {
                if (SelectedIndex < 1)
                    return;

                if (Items[SelectedIndex - 1].Selectable)
                    SelectedIndex--;
            }

            base.OnMouseScrolled();
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
                return -2;
            }

            if (p.Y < dropDownTexture.Height + 1)
                return -1;

            int y = p.Y - dropDownTexture.Height - 1;
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
            Renderer.DrawRectangle(new Rectangle(wr.X, wr.Y, wr.Width, dropDownTexture.Height), BorderColor);

            if (SelectedIndex > -1 && SelectedIndex < Items.Count)
            {
                XNADropDownItem item = Items[SelectedIndex];

                int textX = 3;
                if (item.Texture != null)
                {
                    Renderer.DrawTexture(item.Texture, new Rectangle(wr.X + 1, wr.Y + 2, item.Texture.Width, item.Texture.Height), Color.White);
                    textX += item.Texture.Width + 1;
                }

                if (item.Text != null)
                    Renderer.DrawStringWithShadow(item.Text, FontIndex, new Vector2(wr.X + textX, wr.Y + 2), item.TextColor);
            }

            if (AllowDropDown)
            {
                Rectangle ddRectangle = new Rectangle(wr.X + wr.Width - dropDownTexture.Width,
                    wr.Y, dropDownTexture.Width, dropDownTexture.Height);

                if (IsDroppedDown)
                {
                    Renderer.DrawTexture(dropDownOpenTexture,
                        ddRectangle, GetColorWithAlpha(RemapColor));

                    Renderer.DrawRectangle(new Rectangle(wr.X, wr.Y + dropDownTexture.Height, wr.Width, wr.Height + 1 - dropDownTexture.Height), BorderColor);

                    for (int i = 0; i < Items.Count; i++)
                    {
                        XNADropDownItem item = Items[i];

                        int y = wr.Y + dropDownTexture.Height + 1 + i * ItemHeight;
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

                        if (item.Text != null)
                            Renderer.DrawStringWithShadow(item.Text, FontIndex, new Vector2(wr.X + textX, y + 1), textColor);
                    }
                }
                else
                    Renderer.DrawTexture(dropDownTexture, ddRectangle, RemapColor);
            }

            base.Draw(gameTime);
        }
    }
}
