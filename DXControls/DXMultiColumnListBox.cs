using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;

namespace Rampastring.XNAUI.DXControls
{
    /// <summary>
    /// A list box with multiple columns.
    /// </summary>
    public class DXMultiColumnListBox : DXPanel
    {
        public DXMultiColumnListBox(WindowManager windowManager) : base(windowManager)
        {

        }

        public delegate void SelectedIndexChangedEventHandler(object sender, EventArgs e);
        public event SelectedIndexChangedEventHandler SelectedIndexChanged;

        int _headerFontIndex = 1;
        public int HeaderFontIndex
        {
            get { return _headerFontIndex; }
            set { _headerFontIndex = value; }
        }

        public int FontIndex { get; set; }

        int _lineHeight = 15;
        public int LineHeight
        {
            get { return _lineHeight; }
            set { _lineHeight = value; }
        }

        public bool DrawListBoxBorders { get; set; }

        List<ListBoxColumn> columns = new List<ListBoxColumn>();
        List<DXListBox> listBoxes = new List<DXListBox>();
        List<DXPanel> headers = new List<DXPanel>();

        bool handleSelectedIndexChanged = true;

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
                    foreach (DXListBox lb in listBoxes)
                        lb.SelectedIndex = value;
                }
            }

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

                if (listBoxes.Count == 0)
                    return;

                // Adjust size of columns

                foreach (DXListBox lb in listBoxes)
                {
                    lb.ClientRectangle = new Rectangle(lb.ClientRectangle.X,
                        lb.ClientRectangle.Y,
                        lb.ClientRectangle.Width, 
                        base.ClientRectangle.Height - lb.ClientRectangle.Y);
                }

                DXListBox lastListBox = listBoxes[listBoxes.Count - 1];
                DXPanel lastHeader = headers[headers.Count - 1];

                int lastColumnWidth = base.ClientRectangle.Width -
                    listBoxes[listBoxes.Count - 1].ClientRectangle.X;

                lastListBox.ClientRectangle = new Rectangle(lastListBox.ClientRectangle.X,
                    lastListBox.ClientRectangle.Y,
                    lastColumnWidth, lastListBox.ClientRectangle.Height);

                lastHeader.ClientRectangle = new Rectangle(lastHeader.ClientRectangle.X,
                    lastHeader.ClientRectangle.Y,
                    lastColumnWidth, lastHeader.ClientRectangle.Height);
            }
        }

        public void AddColumn(string header, int width)
        {
            columns.Add(new ListBoxColumn(header, width));
        }

        public void AddColumn(DXPanel header)
        {
            DXListBox listBox = new DXListBox(WindowManager);
            listBox.FontIndex = FontIndex;
            listBox.TextBorderDistance = 5;

            AddColumn(header, listBox);
        }

        public void AddColumn(DXPanel header, DXListBox listBox)
        {
            int width = 0;

            foreach (DXPanel headerPanel in headers)
                width += headerPanel.ClientRectangle.Width;

            header.ClientRectangle = new Rectangle(width, 0, header.ClientRectangle.Width, header.ClientRectangle.Height);

            headers.Add(header);
            AddChild(header);

            listBox.ClientRectangle = new Rectangle(width, header.ClientRectangle.Bottom - 1,
                header.ClientRectangle.Width, this.ClientRectangle.Height - header.ClientRectangle.Bottom + 1);
            listBox.DrawBorders = DrawListBoxBorders;
            listBox.LineHeight = _lineHeight;
            listBox.TopIndexChanged += ListBox_TopIndexChanged;
            listBox.SelectedIndexChanged += ListBox_SelectedIndexChanged;

            listBoxes.Add(listBox);
            AddChild(listBox);
        }

        public override void Initialize()
        {
            base.Initialize();

            int width = 0;

            foreach (DXPanel header in headers)
                width += header.ClientRectangle.Width;

            foreach (ListBoxColumn column in columns)
            {
                DXLabel header = new DXLabel(WindowManager);
                header.FontIndex = HeaderFontIndex;
                header.ClientRectangle = new Rectangle(3, 2, 0, 0);
                header.Text = column.Header;

                DXPanel headerPanel = new DXPanel(WindowManager);

                AddChild(headerPanel);
                headerPanel.AddChild(header);

                if (DrawListBoxBorders)
                    headerPanel.ClientRectangle = new Rectangle(width - 1, 0, column.Width + 1, header.ClientRectangle.Height + 3);
                else
                    headerPanel.ClientRectangle = new Rectangle(width, 0, column.Width, header.ClientRectangle.Height + 3);

                headers.Add(headerPanel);

                DXListBox listBox = new DXListBox(WindowManager);
                listBox.FontIndex = FontIndex;
                if (DrawListBoxBorders)
                {
                    listBox.ClientRectangle = new Rectangle(width - 1, headerPanel.ClientRectangle.Bottom - 1,
                        column.Width + 1, this.ClientRectangle.Height - headerPanel.ClientRectangle.Bottom + 1);
                }
                else
                {
                    listBox.ClientRectangle = new Rectangle(width, headerPanel.ClientRectangle.Bottom - 1,
                        column.Width + 2, this.ClientRectangle.Height - headerPanel.ClientRectangle.Bottom + 1);
                }
                listBox.DrawBorders = DrawListBoxBorders;
                listBox.TopIndexChanged += ListBox_TopIndexChanged;
                listBox.SelectedIndexChanged += ListBox_SelectedIndexChanged;
                listBox.TextBorderDistance = 5;
                listBox.LineHeight = _lineHeight;

                listBoxes.Add(listBox);

                AddChild(listBox);

                width += column.Width;
            }

            DXListBox lb = listBoxes[listBoxes.Count - 1];

            if (DrawListBoxBorders)
            {
                lb.ClientRectangle = new Rectangle(lb.ClientRectangle.X, lb.ClientRectangle.Y,
                    lb.ClientRectangle.Width - 1, lb.ClientRectangle.Height);
                DXPanel headerPanel = headers[headers.Count - 1];
                headerPanel.ClientRectangle = new Rectangle(headerPanel.ClientRectangle.X,
                    headerPanel.ClientRectangle.Y, headerPanel.ClientRectangle.Width - 1,
                    headerPanel.ClientRectangle.Height);
            }
            else
            {
                lb.ClientRectangle = new Rectangle(lb.ClientRectangle.X, lb.ClientRectangle.Y,
                    lb.ClientRectangle.Width - 2, lb.ClientRectangle.Height);
            }
        }

        private void ListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!handleSelectedIndexChanged)
                return;

            handleSelectedIndexChanged = false;

            DXListBox lbSender = (DXListBox)sender;

            foreach (DXListBox lb in listBoxes)
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
            foreach (DXListBox lb in listBoxes)
                lb.TopIndex = ((DXListBox)sender).TopIndex;
        }

        public void ClearItems()
        {
            foreach (DXListBox lb in listBoxes)
                lb.Clear();
        }

        public void SetTopIndex(int topIndex)
        {
            listBoxes[0].TopIndex = topIndex;
        }

        public void AddItem(List<string> info, bool selectable)
        {
            if (info.Count != listBoxes.Count)
                throw new Exception("DXMultiColumnListBox.AddItem: Invalid amount of info for added item!");

            for (int i = 0; i < info.Count; i++)
            {
                listBoxes[i].AddItem(info[i], selectable);
            }
        }

        public void AddItem(string[] info, bool selectable)
        {
            if (info.Length != listBoxes.Count)
                throw new Exception("DXMultiColumnListBox.AddItem: Invalid amount of info for added item!");

            for (int i = 0; i < info.Length; i++)
            {
                listBoxes[i].AddItem(info[i], selectable);
            }
        }

        public void AddItem(DXListBoxItem[] items)
        {
            if (items.Length != listBoxes.Count)
                throw new Exception("DXMultiColumnListBox.AddItem: Invalid amount of list box items for added item!");

            for (int i = 0; i < items.Length; i++)
                listBoxes[i].AddItem(items[i]);
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
