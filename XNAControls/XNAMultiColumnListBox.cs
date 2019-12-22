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

        List<ListBoxColumn> columns = new List<ListBoxColumn>();
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
                case "DrawSelectionUnderScrollbar":
                    DrawSelectionUnderScrollbar = Conversions.BooleanFromString(value, true);
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

        public void AddColumn(string header, int width)
        {
            columns.Add(new ListBoxColumn(header, width));
        }

        public void AddColumn(XNAPanel header)
        {
            XNAListBox listBox = new XNAListBox(WindowManager);
            listBox.FontIndex = FontIndex;
            listBox.TextBorderDistance = 5;

            AddColumn(header, listBox);
        }

        public void AddColumn(XNAPanel header, XNAListBox listBox)
        {
            int width = 0;
            headers.ForEach(h => width += h.Width);

            header.ClientRectangle = new Rectangle(width, 0, header.Width, header.Height);

            headers.Add(header);

            listBox.Name = Name + "_lb" + listBoxes.Count;
            listBox.ClientRectangle = new Rectangle(width, header.Bottom - 1,
                header.Width, this.Height - header.Bottom + 1);
            listBox.DrawBorders = DrawListBoxBorders;
            listBox.LineHeight = LineHeight;
            listBox.TopIndexChanged += ListBox_TopIndexChanged;
            listBox.SelectedIndexChanged += ListBox_SelectedIndexChanged;
            listBox.AllowMultiLineItems = false;
            listBox.AllowKeyboardInput = this.AllowKeyboardInput;
            listBox.AllowRightClickUnselect = this.AllowRightClickUnselect;
            //listBox.DrawMode = ControlDrawMode.NORMAL;

            listBoxes.Add(listBox);
            AddChild(listBox);
            AddChild(header);
        }

        public override void Initialize()
        {
            base.Initialize();

            int width = 0;

            foreach (XNAPanel header in headers)
                width += header.ClientRectangle.Width;

            foreach (ListBoxColumn column in columns)
            {
                XNALabel header = new XNALabel(WindowManager);
                header.FontIndex = HeaderFontIndex;
                header.ClientRectangle = new Rectangle(3, 2, 0, 0);
                header.Text = column.Header;

                XNAPanel headerPanel = new XNAPanel(WindowManager);

                if (DrawListBoxBorders)
                    headerPanel.ClientRectangle = new Rectangle(width - 1, 0, column.Width + 1, header.Height + 3);
                else
                    headerPanel.ClientRectangle = new Rectangle(width, 0, column.Width, header.Height + 3);

                headers.Add(headerPanel);

                XNAListBox listBox = new XNAListBox(WindowManager);
                listBox.FontIndex = FontIndex;

                if (DrawListBoxBorders)
                {
                    listBox.ClientRectangle = new Rectangle(width - 1, headerPanel.Bottom - 1,
                        column.Width + 1, this.Height - headerPanel.Bottom + 1);
                }
                else
                {
                    listBox.ClientRectangle = new Rectangle(width, headerPanel.Bottom - 1,
                        column.Width + 2, this.Height - headerPanel.Bottom + 1);
                }

                listBox.Name = Name + "_lb" + listBoxes.Count;
                listBox.DrawBorders = DrawListBoxBorders;
                listBox.TopIndexChanged += ListBox_TopIndexChanged;
                listBox.SelectedIndexChanged += ListBox_SelectedIndexChanged;
                listBox.TextBorderDistance = 5;
                listBox.LineHeight = LineHeight;
                listBox.AllowMultiLineItems = false;
                listBox.AllowKeyboardInput = this.AllowKeyboardInput;
                listBox.AllowRightClickUnselect = this.AllowRightClickUnselect;
                listBox.RightClick += ListBox_RightClick;
                //listBox.DrawMode = ControlDrawMode.NORMAL;

                listBoxes.Add(listBox);

                AddChild(listBox);
                AddChild(headerPanel);
                headerPanel.AddChild(header);

                width += column.Width;
            }

            XNAListBox lb = listBoxes[listBoxes.Count - 1];

            if (DrawListBoxBorders)
            {
                lb.ClientRectangle = new Rectangle(lb.X, lb.Y,
                    lb.Width - 1, lb.Height);
                XNAPanel headerPanel = headers[headers.Count - 1];
                headerPanel.ClientRectangle = new Rectangle(headerPanel.X,
                    headerPanel.Y, headerPanel.Width - 1,
                    headerPanel.Height);
            }
            else
            {
                lb.ClientRectangle = new Rectangle(lb.X, lb.Y,
                    lb.Width - 2, lb.Height);
            }

            for (int i = 0; i < listBoxes.Count - 1; i++)
                listBoxes[i].EnableScrollbar = false;
        }

        private void ListBox_RightClick(object sender, EventArgs e)
        {
            OnRightClick();
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

    class ListBoxColumn
    {
        public ListBoxColumn(string header, int width)
        {
            Header = header;
            Width = width;
        }

        public string Header { get; set; }

        public int Width { get; set; }
    }
}
