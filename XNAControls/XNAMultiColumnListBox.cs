namespace Rampastring.XNAUI.XNAControls;

using System;
using System.Collections.Generic;
using Rampastring.Tools;

/// <summary>
/// A list box with multiple columns.
/// </summary>
public class XNAMultiColumnListBox : XNAPanel
{
    /// <summary>
    /// Creates a new multi-column list box.
    /// </summary>
    /// <param name="windowManager">The WindowManager.</param>
    public XNAMultiColumnListBox(WindowManager windowManager)
        : base(windowManager)
    {
        DrawMode = ControlDrawMode.UNIQUE_RENDER_TARGET;
        ClientRectangleUpdated += XNAMultiColumnListBox_ClientRectangleUpdated;
    }

    /// <summary>
    /// Adjusts the positions and sizes of the columns
    /// when the size of the list box is changed.
    /// </summary>
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

    public event EventHandler HoveredIndexChanged;

    public event EventHandler SelectedIndexChanged;

    public event EventHandler TopIndexChanged;

    public int HeaderFontIndex { get; set; } = 1;

    public int FontIndex { get; set; }

    public int LineHeight { get; set; } = 15;

    public bool DrawListBoxBorders { get; set; }

    private readonly List<XNAListBox> listBoxes = new();
    private readonly List<XNAPanel> headers = new();
    private bool handleSelectedIndexChanged = true;

    /// <summary>
    /// Gets or sets the index of the currently selected list box item.
    /// </summary>
    public int SelectedIndex
    {
        get => listBoxes[0].SelectedIndex;
        set
        {
            if (handleSelectedIndexChanged)
            {
                foreach (XNAListBox lb in listBoxes)
                    lb.SelectedIndex = value;
            }
        }
    }

    private bool hoveredIndexChangeBeingHandled;

    public int HoveredIndex
    {
        get
        {
            foreach (XNAListBox listBox in listBoxes)
            {
                if (listBox.HoveredIndex > -1 && listBox.HoveredIndex < listBox.Items.Count)
                    return listBox.HoveredIndex;
            }

            return -1;
        }
    }

    private bool allowKeyboardInput;

    /// <summary>
    /// If set to true, the user is able to scroll the listbox items
    /// by using keyboard keys.
    /// </summary>
    public bool AllowKeyboardInput
    {
        get => allowKeyboardInput;

        set
        {
            allowKeyboardInput = value;
            foreach (XNAListBox lb in listBoxes)
                lb.AllowKeyboardInput = value;
        }
    }

    private bool topIndexChangeBeingHandled;

    /// <summary>
    /// Gets or sets the index of the first visible item in the list box.
    /// </summary>
    public int TopIndex
    {
        get => listBoxes[0].TopIndex;

        set => listBoxes[0].TopIndex = value;
    }

    /// <summary>
    /// Gets the index of the last visible item in the list box.
    /// </summary>
    public int LastIndex => listBoxes[0].LastIndex;

    /// <summary>
    /// Gets the number of items on the list box.
    /// </summary>
    public int ItemCount => listBoxes.Count == 0 ? 0 : listBoxes[0].Items.Count;

    private bool allowRightClickUnselect = true;

    /// <summary>
    /// Gets or sets a bool that determines whether the user is able to un-select
    /// the currently selected listbox item by right-clicking on the list box.
    /// </summary>
    public bool AllowRightClickUnselect
    {
        get => allowRightClickUnselect;

        set
        {
            allowRightClickUnselect = value;
            foreach (XNAListBox lb in listBoxes)
            {
                lb.AllowRightClickUnselect = allowRightClickUnselect;
            }
        }
    }

    /// <summary>
    /// Controls whether the highlighted background of the selected item should
    /// be drawn under the scrollbar area.
    /// </summary>
    public bool DrawSelectionUnderScrollbar
    {
        get => listBoxes[listBoxes.Count - 1].DrawSelectionUnderScrollbar;
        set => listBoxes[listBoxes.Count - 1].DrawSelectionUnderScrollbar = value;
    }

    protected override void ParseControlINIAttribute(IniFile iniFile, string key, string value)
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
        if (key.StartsWith(columnWidthKeyStart, StringComparison.InvariantCulture))
        {
            int headerIndex = Conversions.IntFromString(key.SafeSubstring(columnWidthKeyStart.Length), -1);
            if (headerIndex == -1 || headerIndex >= headers.Count)
                return;

            ChangeColumnWidth(headerIndex, Conversions.IntFromString(value, headers[headerIndex].Width));
        }

        if (key.StartsWith("Column", StringComparison.InvariantCulture))
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
        if (key.StartsWith("ListBox", StringComparison.InvariantCulture) && key.Length > "ListBoxYAttribute:".Length)
        {
            int listBoxId = Conversions.IntFromString(key.SafeSubstring("ListBox".Length, 1), -1);
            if (listBoxId == -1)
                return;

            if (listBoxId >= listBoxes.Count)
                return;

            if (key.SafeSubstring("ListBoxY".Length, ":Attribute".Length) != ":Attribute")
                return;

            string attrName = key.SafeSubstring("ListBoxYAttribute:".Length);
            listBoxes[listBoxId].ParseINIAttribute(iniFile, attrName, value);
        }

        base.ParseControlINIAttribute(iniFile, key, value);
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
    public void AddColumn(string headerText, int width)
    {
        var headerLabel = new XNALabel(WindowManager)
        {
            FontIndex = HeaderFontIndex,
            X = 3,
            Y = 2,
            Text = headerText
        };

        var headerPanel = new XNAPanel(WindowManager)
        {
            Height = headerLabel.Height + 3,
            Width = DrawListBoxBorders ? width + 1 : width
        };
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
        var listBox = new XNAListBox(WindowManager)
        {
            FontIndex = FontIndex,
            TextBorderDistance = 5
        };

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

        header.ClientRectangle = new(width, 0, header.Width, header.Height);

        headers.Add(header);

        listBox.Name = Name + "_lb" + listBoxes.Count;
        listBox.ClientRectangle = new(
            width, header.Bottom - 1, header.Width, Height - header.Bottom + 1);
        listBox.DrawBorders = DrawListBoxBorders;
        listBox.LineHeight = LineHeight;
        listBox.TopIndexChanged += ListBox_TopIndexChanged;
        listBox.SelectedIndexChanged += ListBox_SelectedIndexChanged;
        listBox.HoveredIndexChanged += ListBox_HoveredIndexChanged;
        listBox.AllowMultiLineItems = false;
        listBox.AllowKeyboardInput = AllowKeyboardInput;
        listBox.AllowRightClickUnselect = AllowRightClickUnselect;
        listBox.RightClick += ListBox_RightClick;

        listBoxes.Add(listBox);
        AddChild(listBox);
        AddChild(header);
    }

    private void ListBox_RightClick(object sender, EventArgs e) => OnRightClick();

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

    private void ListBox_HoveredIndexChanged(object sender, EventArgs e)
    {
        if (hoveredIndexChangeBeingHandled)
            return;

        hoveredIndexChangeBeingHandled = true;

        foreach (XNAListBox lb in listBoxes)
            lb.HoveredIndex = ((XNAListBox)sender).HoveredIndex;

        HoveredIndexChanged?.Invoke(this, EventArgs.Empty);

        hoveredIndexChangeBeingHandled = false;
    }

    private void ListBox_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (!handleSelectedIndexChanged)
            return;

        handleSelectedIndexChanged = false;

        var xnaListBox = (XNAListBox)sender;

        foreach (XNAListBox lb in listBoxes)
        {
            lb.SelectedIndex = xnaListBox.SelectedIndex;
        }

        SelectedIndex = xnaListBox.SelectedIndex;

        SelectedIndexChanged?.Invoke(this, EventArgs.Empty);

        handleSelectedIndexChanged = true;
    }

    private void ListBox_TopIndexChanged(object sender, EventArgs e)
    {
        if (topIndexChangeBeingHandled)
            return;

        topIndexChangeBeingHandled = true;

        foreach (XNAListBox lb in listBoxes)
            lb.ViewTop = ((XNAListBox)sender).ViewTop;

        TopIndexChanged?.Invoke(this, EventArgs.Empty);

        topIndexChangeBeingHandled = false;
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

    public void AddItem(List<string> info, bool selectable) => AddItem(info.ToArray(), selectable);

    public void AddItem(string[] info, bool selectable)
    {
        if (info.Length != listBoxes.Count)
            throw new InvalidOperationException("XNAMultiColumnListBox.AddItem: Invalid amount of info for added item!");

        for (int i = 0; i < info.Length; i++)
        {
            listBoxes[i].AddItem(info[i], selectable);
        }
    }

    public void AddItem(List<XNAListBoxItem> items) => AddItem(items.ToArray());

    public void AddItem(XNAListBoxItem[] items)
    {
        if (items.Length != listBoxes.Count)
            throw new InvalidOperationException("XNAMultiColumnListBox.AddItem: Invalid amount of list box items for added item!");

        for (int i = 0; i < items.Length; i++)
            listBoxes[i].AddItem(items[i]);
    }

    public XNAListBoxItem GetItem(int columnIndex, int itemIndex) => listBoxes[columnIndex].Items[itemIndex];
}