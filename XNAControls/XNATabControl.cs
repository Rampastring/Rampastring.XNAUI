using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Rampastring.Tools;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Rampastring.XNAUI.XNAControls;

public class TabEventArgs : EventArgs
{
    public TabEventArgs(Tab tab)
    {
        Tab = tab;
    }

    public Tab Tab { get; }
}

/// <summary>
/// A control that has multiple tabs, of which only one can be selected at a time.
/// </summary>
public class XNATabControl : XNAControl
{
    public XNATabControl(WindowManager windowManager) : base(windowManager)
    {
    }

    public delegate void SelectedIndexChangedEventHandler(object sender, EventArgs e);
    public event EventHandler<TabEventArgs> SelectedTabChanged;
    public event EventHandler SelectedIndexChanged;
    public event EventHandler<TabEventArgs> HoveredTabChanged;

    private int _selectedTabIndex = -1;
    private int _selectedIndexChangeDepth;

    private void FireSelectedIndexChangedEvent()
    {
        _selectedIndexChangeDepth++;
        try
        {
            SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
        }
        finally
        {
            _selectedIndexChangeDepth--;
        }
    }

    public int SelectedTabIndex
    {
        get => _selectedTabIndex;
        set
        {
            if (value >= Tabs.Count || value < -1)
                value = -1; // Normalize "not selected" as -1

            if (value == _selectedTabIndex)
                return;

            Tab previousTab = SelectedTab;
            _selectedTabIndex = value;

            // Properly handle cases where the handler of a tab-selection event changes the tab.
            // Only the "outermost" tab change emits SelectedTabChanged.
            FireSelectedIndexChangedEvent();

            if (_selectedIndexChangeDepth != 0)
                return;

            Tab newTab = SelectedTab;
            if (previousTab != newTab)
                SelectedTabChanged?.Invoke(this, new TabEventArgs(newTab));
        }
    }

    public Tab SelectedTab
    {
        get
        {
            if (_selectedTabIndex < 0 || _selectedTabIndex >= Tabs.Count)
                return null;

            return Tabs[_selectedTabIndex];
        }
        set
        {
            if (value == null)
            {
                SelectedTabIndex = -1;
                return;
            }

            int tabIndex = Tabs.IndexOf(value);
            if (tabIndex < 0)
                throw new InvalidOperationException("Attempted to select a tab that is not part of this TabControl.");

            if (tabIndex == SelectedTabIndex)
                return;

            SelectedTabIndex = tabIndex;
        }
    }

    private Tab _hoveredTab = null;
    public Tab HoveredTab
    {
        get => _hoveredTab;
        set
        {
            if (_hoveredTab == value)
                return;

            _hoveredTab = value;
            HoveredTabChanged?.Invoke(this, new TabEventArgs(_hoveredTab));
        }
    }

    public int FontIndex { get; set; }

    public bool DisposeTexturesOnTabRemove { get; set; }

    private Color? _textColor;

    public Color TextColor
    {
        get => _textColor ?? UISettings.ActiveSettings.AltColor;
        set => _textColor = value;
    }

    private Color? _textColorDisabled;

    public Color TextColorDisabled
    {
        get => _textColorDisabled ?? UISettings.ActiveSettings.DisabledItemColor;
        set => _textColorDisabled = value;
    }

    public List<Tab> Tabs { get; set; } = new List<Tab>();

    public EnhancedSoundEffect HoverSound { get; set; }
    public EnhancedSoundEffect ClickSound { get; set; }

    public override void Initialize()
    {
        base.Initialize();
    }

    public void RemoveTab(int index)
    {
        if (index < 0 || index >= Tabs.Count)
            throw new ArgumentOutOfRangeException($"{nameof(RemoveTab)}: Tab index out of range: {index}");

        Tab tab = Tabs[index];
        RemoveTab(tab);
    }

    public void RemoveTab(string text)
    {
        Tab tab = Tabs.Find(t => t.Text == text);
        if (tab != null)
            RemoveTab(tab);
    }

    private void FireSelectedTabChangedEvent(Tab previousTab)
    {
        if (_selectedIndexChangeDepth == 0)
        {
            if (SelectedTab != previousTab)
                SelectedTabChanged?.Invoke(this, new TabEventArgs(SelectedTab));
        }
    }

    public void RemoveTab(Tab tab)
    {
        int index = Tabs.IndexOf(tab);

        if (tab == null)
            throw new ArgumentNullException(nameof(tab));

        if (index < 0)
            throw new ArgumentException("The given tab does not belong to this tab control!");

        if (DisposeTexturesOnTabRemove)
        {
            tab.DefaultTexture.Dispose();
            tab.PressedTexture.Dispose();
        }

        // Record selection state. Only fire event handlers after the removal has been completed,
        // so there's no order-of-operation issues if an event handler wants to change to another tab.
        bool decrementSelectedTab = false;

        Tab previousTab = SelectedTab;

        if (SelectedTabIndex >= index)
            decrementSelectedTab = true;

        Tabs.RemoveAt(index);

        if (previousTab == tab)
        {
            _selectedTabIndex = -1;
            FireSelectedIndexChangedEvent();
            FireSelectedTabChangedEvent(previousTab);
        }
        else if (decrementSelectedTab)
        {
            _selectedTabIndex--;
            FireSelectedIndexChangedEvent();
            FireSelectedTabChangedEvent(previousTab);
        }

        if (HoveredTab == tab)
            HoveredTab = null;

        Width = Tabs.Sum(t => t.Width);
    }

    public void AddTab(string text, Texture2D defaultTexture, Texture2D pressedTexture)
    {
        AddTab(text, defaultTexture, pressedTexture, true, null);
    }

    public void AddTab(string text, Texture2D defaultTexture, Texture2D pressedTexture, bool selectable, object tag)
    {
        if (defaultTexture == null)
            throw new ArgumentNullException($"{nameof(AddTab)} requires a default texture to be specified.");

        var tab = new Tab(text, defaultTexture, pressedTexture, selectable);
        tab.Tag = tag;
        Tabs.Add(tab);

        Vector2 textSize = Renderer.GetTextDimensions(text, FontIndex);
        tab.TextXPosition = (defaultTexture.Width - (int)textSize.X) / 2;
        tab.TextYPosition = Renderer.GetTextYPadding(text, FontIndex, defaultTexture.Height);

        Width += defaultTexture.Width;
        Height = defaultTexture.Height;
    }

    public void AddTab(Tab tab)
    {
        if (tab == null)
            throw new ArgumentNullException(nameof(tab));

        Tabs.Add(tab);
        Width += tab.Width;
        if (tab.DefaultTexture != null && Height < tab.DefaultTexture.Height)
            Height = tab.DefaultTexture.Height;
    }

    protected override void ParseControlINIAttribute(IniFile iniFile, string key, string value)
    {
        switch (key)
        {
            case "RemapColor":
            case "TextColor":
                TextColor = AssetLoader.GetColorFromString(value);
                return;
            case "TextColorDisabled":
                TextColorDisabled = AssetLoader.GetColorFromString(value);
                return;
        }

        if (key.StartsWith("RemoveTabIndex", StringComparison.InvariantCulture))
        {
            int index = int.Parse(key.Substring(14), CultureInfo.InvariantCulture);

            if (Conversions.BooleanFromString(value, false))
                RemoveTab(index);
        }

        base.ParseControlINIAttribute(iniFile, key, value);
    }

    private Tab GetTabOnCursor()
    {
        Point p = GetCursorPoint();
        if (p.Y < 0 || p.Y >= Height)
            return null;

        int w = 0;
        foreach (Tab tab in Tabs)
        {
            w += tab.Width;

            if (p.X < w)
            {
                return tab;
            }
        }

        return null;
    }

    public override void OnLeftClick(InputEventArgs inputEventArgs)
    {
        inputEventArgs.Handled = true;
        Tab tabOnCursor = GetTabOnCursor();
        if (tabOnCursor != null && tabOnCursor.Selectable)
        {
            SelectedTab = tabOnCursor;
            ClickSound?.Play();
        }

        base.OnLeftClick(inputEventArgs);
    }

    public override void OnMouseMove()
    {
        Tab newHoveredTab = GetTabOnCursor();
        if (newHoveredTab != HoveredTab)
        {
            HoveredTab = newHoveredTab;
            if (HoveredTab != null && HoveredTab.Selectable)
            {
                HoverSound?.Play();
            }
        }

        base.OnMouseMove();
    }

    public override void OnMouseLeave()
    {
        HoveredTab = null;
        base.OnMouseLeave();
    }

    public virtual void DrawTab(GameTime gameTime, int x, Tab tab)
    {
        Texture2D texture = tab == SelectedTab ? tab.PressedTexture : tab.DefaultTexture;

        DrawTexture(texture, new Point(x, 0), RemapColor);

        DrawStringWithShadow(tab.Text, FontIndex,
            new Vector2(x + tab.TextXPosition, tab.TextYPosition),
            tab.Selectable && Enabled ? TextColor : TextColorDisabled);
    }

    public override void Draw(GameTime gameTime)
    {
        int x = 0;

        for (int i = 0; i < Tabs.Count; i++)
        {
            Tab tab = Tabs[i];
            DrawTab(gameTime, x, tab);
            x += tab.Width;
        }
    }
}

public class Tab
{
    public Tab() { }

    public Tab(string text, Texture2D defaultTexture, Texture2D pressedTexture, bool selectable)
    {
        Text = text;
        DefaultTexture = defaultTexture;
        PressedTexture = pressedTexture;
        Selectable = selectable;
    }

    /// <summary>
    /// The width of the tab based on the tab's texture.
    /// </summary>
    public int Width => DefaultTexture.Width;

    public Texture2D DefaultTexture { get; set; }

    public Texture2D PressedTexture { get; set; }

    public string Text { get; set; }

    public bool Selectable { get; set; }

    public int TextXPosition { get; set; }

    public int TextYPosition { get; set; }

    public object Tag { get; set;  }
}
