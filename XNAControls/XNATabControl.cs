using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Rampastring.Tools;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace Rampastring.XNAUI.XNAControls;

/// <summary>
/// A control that has multiple tabs, of which only one can be selected at a time.
/// </summary>
public class XNATabControl : XNAControl
{
    public XNATabControl(WindowManager windowManager) : base(windowManager)
    {
    }

    public delegate void SelectedIndexChangedEventHandler(object sender, EventArgs e);
    public event SelectedIndexChangedEventHandler SelectedIndexChanged;

    private int _selectedTab = -1;

    public int SelectedTab
    {
        get => _selectedTab;
        set
        {
            int resolvedPublicIndex = ResolvePublicIndex(value);

            if (_selectedTab == resolvedPublicIndex)
                return;

            _selectedTab = resolvedPublicIndex;

            SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
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

    private List<Tab> Tabs = new List<Tab>();

    /// <summary>
    /// Maps a public tab index to the current internal index in <see cref="Tabs"/>.
    /// </summary>
    private readonly Dictionary<int, int> _publicToInternalIndex = new Dictionary<int, int>();

    /// <summary>
    /// Maps an internal tab index in <see cref="Tabs"/> to the public tab index.
    /// </summary>
    private readonly Dictionary<int, int> _internalToPublicIndex = new Dictionary<int, int>();

    private int _nextPublicIndex = 0;

    public EnhancedSoundEffect ClickSound { get; set; }

    public override void Initialize()
    {
        base.Initialize();
    }

    public void MakeSelectable(int index)
    {
        int internalIndex = GetInternalIndex(index);
        if (internalIndex < 0)
            return;

        Tabs[internalIndex].Selectable = true;
    }

    public void MakeUnselectable(int index)
    {
        int internalIndex = GetInternalIndex(index);
        if (internalIndex < 0)
            return;

        Tabs[internalIndex].Selectable = false;
    }

    /// <summary>
    /// Removes the tab identified by the given tab index.
    /// </summary>
    public void RemoveTab(int index)
    {
        int internalIndex = GetInternalIndex(index);
        if (internalIndex < 0)
            return;

        RemoveTabAtInternalIndex(internalIndex);
    }

    public void RemoveTab(string text)
    {
        int internalIndex = Tabs.FindIndex(t => t.Text == text);
        if (internalIndex < 0)
            return;

        RemoveTabAtInternalIndex(internalIndex);
    }

    public void AddTab(string text, Texture2D defaultTexture, Texture2D pressedTexture)
    {
        AddTab(text, defaultTexture, pressedTexture, true);
    }

    public void AddTab(string text, Texture2D defaultTexture, Texture2D pressedTexture, bool selectable)
    {
        var tab = new Tab(text, defaultTexture, pressedTexture, selectable);
        tab.Index = _nextPublicIndex++;
        Tabs.Add(tab);
        RebuildIndexMaps();

        Vector2 textSize = Renderer.GetTextDimensions(text, FontIndex);
        tab.TextXPosition = (defaultTexture.Width - (int)textSize.X) / 2;
        tab.TextYPosition = Renderer.GetTextYPadding(text, FontIndex, defaultTexture.Height);

        Width += defaultTexture.Width;
        Height = defaultTexture.Height;

        if (_selectedTab < 0)
            SelectedTab = tab.Index;
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

    public override void OnLeftClick(InputEventArgs inputEventArgs)
    {
        base.OnLeftClick(inputEventArgs);
        inputEventArgs.Handled = true;

        Point p = GetCursorPoint();

        int w = 0;
        int internalIndex = 0;
        foreach (Tab tab in Tabs)
        {
            w += tab.DefaultTexture.Width;

            if (p.X < w)
            {
                if (tab.Selectable)
                {
                    ClickSound?.Play();

                    SelectedTab = _internalToPublicIndex[internalIndex];
                }

                return;
            }

            internalIndex++;
        }
    }

    public override void Draw(GameTime gameTime)
    {
        int x = 0;

        for (int i = 0; i < Tabs.Count; i++)
        {
            Tab tab = Tabs[i];

            Texture2D texture = _internalToPublicIndex[i] == SelectedTab ? tab.PressedTexture : tab.DefaultTexture;

            DrawTexture(texture, new Point(x, 0), RemapColor);

            DrawStringWithShadow(tab.Text, FontIndex,
                new Vector2(x + tab.TextXPosition, tab.TextYPosition),
                tab.Selectable && Enabled ? TextColor : TextColorDisabled);

            x += tab.DefaultTexture.Width;
        }
    }

    private void RemoveTabAtInternalIndex(int internalIndex)
    {
        int removedPublicIndex = Tabs[internalIndex].Index;
        bool removedSelectedTab = _selectedTab == removedPublicIndex;

        if (DisposeTexturesOnTabRemove)
        {
            Tabs[internalIndex].DefaultTexture.Dispose();
            Tabs[internalIndex].PressedTexture.Dispose();
        }

        Tabs.RemoveAt(internalIndex);
        RebuildIndexMaps();

        if (removedSelectedTab)
            SelectedTab = GetFirstAvailablePublicIndex();
    }

    private void RebuildIndexMaps()
    {
        _publicToInternalIndex.Clear();
        _internalToPublicIndex.Clear();

        for (int internalIndex = 0; internalIndex < Tabs.Count; internalIndex++)
        {
            int publicIndex = Tabs[internalIndex].Index;
            _publicToInternalIndex[publicIndex] = internalIndex;
            _internalToPublicIndex[internalIndex] = publicIndex;
        }
    }

    /// <summary>
    /// Resolves a requested public index to a currently available public index.
    /// A removed public index maps to the first remaining tab, or -1 if none remain.
    /// </summary>
    private int ResolvePublicIndex(int publicIndex)
    {
        if (publicIndex >= 0 && _publicToInternalIndex.ContainsKey(publicIndex))
            return publicIndex;

        return GetFirstAvailablePublicIndex();
    }

    private int GetFirstAvailablePublicIndex()
    {
        if (Tabs.Count == 0)
            return -1;

        return Tabs[0].Index;
    }

    /// <summary>
    /// Returns the internal list index for a tab index, or -1 if the
    /// index has been removed or was never assigned.
    /// </summary>
    private int GetInternalIndex(int publicIndex)
    {
        if (_publicToInternalIndex.TryGetValue(publicIndex, out int internalIndex))
            return internalIndex;

        return -1;
    }
}

internal class Tab
{
    public Tab() { }

    public Tab(string text, Texture2D defaultTexture, Texture2D pressedTexture, bool selectable)
    {
        Text = text;
        DefaultTexture = defaultTexture;
        PressedTexture = pressedTexture;
        Selectable = selectable;
    }

    public Texture2D DefaultTexture { get; set; }

    public Texture2D PressedTexture { get; set; }

    public string Text { get; set; }

    public bool Selectable { get; set; }

    public int TextXPosition { get; set; }

    public int TextYPosition { get; set; }

    /// <summary>
    /// The index assigned when the tab is added, i.e. the public index.
    /// </summary>
    public int Index { get; set; }
}
