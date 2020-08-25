using Microsoft.Xna.Framework;
using Rampastring.Tools;
using System;
using System.Collections.Generic;

namespace Rampastring.XNAUI.XNAControls
{
    /// <summary>
    /// A list box with multiple columns.
    /// </summary>
    public class XNAMultiColumnListBox : XNAPanel
    {
        /// <summary>
        /// Creates a new multi-column list box.
        /// </summary>
        /// <param name="windowManager">The WindowManager.</param>
        public XNAMultiColumnListBox(WindowManager windowManager) : base(windowManager)
        {
            DrawMode = ControlDrawMode.UNIQUE_RENDER_TARGET;
            ClientRectangleUpdated += XNAMultiColumnListBox_ClientRectangleUpdated;
        }

        /// <summary>
        /// Adjusts the positions and sizes of the columns
        /// when the size of the list box is changed.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void XNAMultiColumnListBox_ClientRectangleUpdated(object sender, EventArgs e)
        {
            if (listBoxes.Count == 0)
                return;

            // Adjust size of columns

            foreach (XNAListBox lb in listBoxes)
            {
                lb.Height = Height - lb.Y;
            }

            XNAListBox lastListBox = listBoxes[listBoxes.Count - 1];
            XNAPanel lastHeader = headers[headers.Count - 1];

            int lastColumnWidth = Width -
                listBoxes[listBoxes.Count - 1].X;

            lastListBox.Width = lastColumnWidth;
            lastHeader.Width = lastColumnWidth;
        }

        public delegate void SelectedIndexChangedEventHandler(object sender, EventArgs e);
        public event SelectedIndexChangedEventHandler SelectedIndexChanged;
        public int HeaderFontIndex { get; set; } = 1;

        public int FontIndex { get; set; }
        public int LineHeight { get; set; } = 15;

        public bool DrawListBoxBorders { get; set; }

        List<XNAListBox> listBoxes = new List<XNAListBox>();
        List<XNAPanel> headers = new List<XNAPanel>();

        bool handleSelectedIndexChanged = true;

        /// <summary>
        /// Gets or sets the index of the currently selected list box item.
        /// </summary>
        public int SelectedIndex
        {
            get
            {
                return listBoxes[0].SelectedIndex;
            }
            set
            {
                if (handleSelectedIndexChanged)
                {
                    foreach (XNAListBox lb in listBoxes)
                        lb.SelectedIndex = value;
                }
            }

        }

        public int HoveredIndex
        {
            get
            {
                foreach (var listBox in listBoxes)
                {
                    if (listBox.HoveredIndex > -1 && listBox.HoveredIndex < listBox.Items.Count)
                        return listBox.HoveredIndex;
                }

                return -1;
            }
        }

        private bool _allowKeyboardInput = false;

        /// <summary>
        /// If set to true, the user is able to scroll the listbox items
        /// by using keyboard keys.
        /// </summary>
        public bool AllowKeyboardInput
        {
            get { return _allowKeyboardInput; }
            set
            {
                _allowKeyboardInput = value;

                foreach (XNAListBox lb in listBoxes)
                    lb.AllowKeyboardInput = value;
            }
        }

        /// <summary>
        /// Gets or sets the index of the first visible item in the list box.
        /// </summary>
        public int TopIndex
        {
            get { return listBoxes[0].TopIndex; }
            set
            {
                listBoxes[0].TopIndex = value;
            }
        }

        /// <summary>
        /// Gets the index of the last visible item in the list box.
        /// </summary>
        public int LastIndex
        {
            get { return listBoxes[0].LastIndex; }
        }

        /// <summary>
        /// Gets the number of items on the list box.
        /// </summary>
        public int ItemCount
        {
            get
            {
                if (listBoxes.Count == 0)
                    return 0;

                return listBoxes[0].Items.Count;
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
            set
            {
                _allowRightClickUnselect = value;

                foreach (XNAListBox lb in listBoxes)
                {
                    lb.AllowRightClickUnselect = _allowRightClickUnselect;  
                }
            }
        }

        /// <summary>
        /// Controls whether the highlighted background of the selected item should
        /// be drawn under the scrollbar area.
        /// </summary>
        public bool DrawSelectionUnderScrollbar
        {
            get { return listBoxes[listBoxes.Count - 1].DrawSelectionUnderScrollbar; }
            set { listBoxes[listBoxes.Count - 1].DrawSelectionUnderScrollbar = value; }
        }

        public override void ParseAttributeFromINI(IniFile iniFile, string key, string value)
        {
            switch (key)
            {
                case nameof(DrawSelectionUnderScrollbar):
                    DrawSelectionUnderScrollbar = Conversions.BooleanFromString(value, true);
                    return;
                case nameof(FontIndex):
                    FontIndex = Conversions.IntFromString(value, FontIndex);
                    return;
            }

            const string columnWidthKeyStart = "ColumnWidth";
            if (key.StartsWith(columnWidthKeyStart))
            {
                int headerIndex = Conversions.IntFromString(key.Substring(columnWidthKeyStart.Length), -1);
                if (headerIndex == -1 || headerIndex >= headers.Count)
                    return;

                ChangeColumnWidth(headerIndex, Conversions.IntFromString(value, headers[headerIndex].Width));
            }

            if (key.StartsWith("Column"))
            {
                string[] parts = value.Split(':');
                if (parts.Length != 2)
                    return;

                if (!int.TryParse(parts[1], out int width))
                    return;

                AddColumn(parts[0], width);
            }

            // Usage: ListBoxYAttribute:<AttrName>=<value>
            // Allows setting list box attributes
            if (key.StartsWith("ListBox") && key.Length > "ListBoxYAttribute:".Length)
            {
                int listBoxId = Conversions.IntFromString(key.Substring("ListBox".Length, 1), -1);
                if (listBoxId == -1)
                    return;

                if (listBoxId >= listBoxes.Count)
                    return;

                if (key.Substring("ListBoxY".Length, ":Attribute".Length) != ":Attribute")
                    return;

                string attrName = key.Substring("ListBoxYAttribute:".Length);
                listBoxes[listBoxId].ParseAttributeFromINI(iniFile, attrName, value);
            }

            base.ParseAttributeFromINI(iniFile, key, value);
        }

        /// <summary>
        /// Changes the width of a column and adjusts the positions
        /// of the following columns accordingly.
        /// </summary>
        /// <param name="columnIndex">The index of the column.</param>
        /// <param name="width">The new width of the column.</param>
        public void ChangeColumnWidth(int columnIndex, int width)
        {
            headers[columnIndex].Width = width;
            listBoxes[columnIndex].Width = width;

            int totalWidth = 0;
            for (int i = 0; i <= columnIndex; i++)
            {
                totalWidth += headers[i].Width;
            }

            for (int i = columnIndex + 1; i < headers.Count; i++)
            {
                headers[i].X = totalWidth;
                listBoxes[i].X = totalWidth;

                totalWidth += headers[i].Width;
            }
        }

        /// <summary>
        /// Creates a column with the given header text and width.
        /// </summary>
        /// <param name="headerText"></param>
        /// <param name="width"></param>
        public void AddColumn(string headerText, int width)
        {
            XNALabel headerLabel = new XNALabel(WindowManager);
            headerLabel.FontIndex = HeaderFontIndex;
            headerLabel.X = 3;
            headerLabel.Y = 2;
            headerLabel.Text = headerText;

            XNAPanel headerPanel = new XNAPanel(WindowManager);
            headerPanel.Height = headerLabel.Height + 3;
            if (DrawListBoxBorders)
                headerPanel.Width = width + 1;
            else
                headerPanel.Width = width;
            headerPanel.AddChild(headerLabel);

            AddColumn(headerPanel);
        }

        /// <summary>
        /// Creates a column with the given header and an automatically generated list box.
        /// The width of the header defines the width of the list box.
        /// </summary>
        /// <param name="header">The header panel.</param>
        public void AddColumn(XNAPanel header)
        {
            XNAListBox listBox = new XNAListBox(WindowManager);
            listBox.FontIndex = FontIndex;
            listBox.TextBorderDistance = 5;

            AddColumn(header, listBox);
        }

        /// <summary>
        /// Creates a column with the given header and list box.
        /// </summary>
        /// <param name="header">The header panel.</param>
        /// <param name="listBox">The list box.</param>
        public void AddColumn(XNAPanel header, XNAListBox listBox)
        {
            AdjustExistingListBoxes();

            int width = GetExistingWidth();

            header.ClientRectangle = new Rectangle(width, 0, header.Width, header.Height);

            headers.Add(header);

            listBox.Name = Name + "_lb" + listBoxes.Count;
            listBox.ClientRectangle = new Rectangle(width, header.Bottom - 1,
                header.Width, Height - header.Bottom + 1);
            listBox.DrawBorders = DrawListBoxBorders;
            listBox.LineHeight = LineHeight;
            listBox.TopIndexChanged += ListBox_TopIndexChanged;
            listBox.SelectedIndexChanged += ListBox_SelectedIndexChanged;
            listBox.AllowMultiLineItems = false;
            listBox.AllowKeyboardInput = this.AllowKeyboardInput;
            listBox.AllowRightClickUnselect = AllowRightClickUnselect;
            listBox.RightClick += ListBox_RightClick;

            listBoxes.Add(listBox);
            AddChild(listBox);
            AddChild(header);
        }

        private void ListBox_RightClick(object sender, EventArgs e)
        {
            OnRightClick();
        }

        private void AdjustExistingListBoxes()
        {
            if (listBoxes.Count > 0)
            {
                XNAListBox lb = listBoxes[listBoxes.Count - 1];
                lb.EnableScrollbar = false;

                if (DrawListBoxBorders)
                {
                    lb.Width++;
                    headers[headers.Count - 1].Width++;
                }
                else
                {
                    lb.Width += 2;
                }
            }
        }

        private int GetExistingWidth()
        {
            int width = 0;
            headers.ForEach(h => width += h.Width);
            return width;
        }

        /// <summary>
        /// Checks whether an item is selected in the list box.
        /// </summary>
        public bool IsValidIndexSelected() => SelectedIndex > -1 && SelectedIndex < ItemCount;

        private void ListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!handleSelectedIndexChanged)
                return;

            handleSelectedIndexChanged = false;

            XNAListBox lbSender = (XNAListBox)sender;

            foreach (XNAListBox lb in listBoxes)
            {
                lb.SelectedIndex = lbSender.SelectedIndex;
            }

            SelectedIndex = lbSender.SelectedIndex;

            if (SelectedIndexChanged != null)
                SelectedIndexChanged(this, EventArgs.Empty);

            handleSelectedIndexChanged = true;
        }

        private void ListBox_TopIndexChanged(object sender, EventArgs e)
        {
            foreach (XNAListBox lb in listBoxes)
                lb.ViewTop = ((XNAListBox)sender).ViewTop;
        }

        public void ClearItems()
        {
            foreach (XNAListBox lb in listBoxes)
                lb.Clear();
        }

        public void SetTopIndex(int topIndex)
        {
            if (listBoxes.Count == 0)
                return;

            listBoxes[0].TopIndex = topIndex;
        }

        public void ScrollToBottom()
        {
            if (listBoxes.Count == 0)
                return;

            listBoxes[0].ScrollToBottom();
        }

        public void AddItem(List<string> info, bool selectable)
        {
            AddItem(info.ToArray(), selectable);
        }

        public void AddItem(string[] info, bool selectable)
        {
            if (info.Length != listBoxes.Count)
                throw new InvalidOperationException("XNAMultiColumnListBox.AddItem: Invalid amount of info for added item!");

            for (int i = 0; i < info.Length; i++)
            {
                listBoxes[i].AddItem(info[i], selectable);
            }
        }

        public void AddItem(List<XNAListBoxItem> items)
        {
            AddItem(items.ToArray());
        }

        public void AddItem(XNAListBoxItem[] items)
        {
            if (items.Length != listBoxes.Count)
                throw new InvalidOperationException("XNAMultiColumnListBox.AddItem: Invalid amount of list box items for added item!");

            for (int i = 0; i < items.Length; i++)
                listBoxes[i].AddItem(items[i]);
        }

        public XNAListBoxItem GetItem(int columnIndex, int itemIndex)
        {
            return listBoxes[columnIndex].Items[itemIndex];
        }
    }
}
