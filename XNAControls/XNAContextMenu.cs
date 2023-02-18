namespace Rampastring.XNAUI.XNAControls;

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

/// <summary>
/// A context menu.
/// </summary>
public class XNAContextMenu : XNAControl
{
    protected const int BORDER_WIDTH = 1;
    protected const int TEXTURE_PADDING = 1;

    /// <summary>
    /// Creates a new context menu.
    /// </summary>
    /// <param name="windowManager">The WindowManager associated with this context menu.</param>
    public XNAContextMenu(WindowManager windowManager)
        : base(windowManager)
    {
        Height = BORDER_WIDTH * 2;
        DisabledItemColor = Color.Gray;
        Disable();
    }

    public event EventHandler<ContextMenuItemSelectedEventArgs> OptionSelected;

    public int ItemHeight { get; set; } = 17;

    public List<XNAContextMenuItem> Items = new();

    private Color? borderColor;

    public Color BorderColor
    {
        get => borderColor ?? UISettings.ActiveSettings.PanelBorderColor;
        set => borderColor = value;
    }

    private Color? focusColor;

    public Color FocusColor
    {
        get => focusColor ?? UISettings.ActiveSettings.FocusColor;
        set => focusColor = value;
    }

    private Color? backColor;

    public Color BackColor
    {
        get => backColor ?? UISettings.ActiveSettings.BackgroundColor;
        set => backColor = value;
    }

    private Color? itemColor;

    public Color ItemColor
    {
        get => itemColor ?? UISettings.ActiveSettings.AltColor;
        set => itemColor = value;
    }

    private Color? disabledItemColor;

    public Color DisabledItemColor
    {
        get => disabledItemColor ?? UISettings.ActiveSettings.DisabledItemColor;
        set => disabledItemColor = value;
    }

    public int FontIndex { get; set; }

    public int HintFontIndex { get; set; }

    public int TextHorizontalPadding { get; set; } = 1;

    public int TextVerticalPadding { get; set; } = 1;

    /// <summary>
    /// The index of the context menu item that
    /// the user's cursor is hovering on. -1 for none.
    /// </summary>
    public int HoveredIndex { get; private set; } = -1;

    private bool leftClickHandled;
    private bool openedOnThisFrame;

    #region AddItem methods

    /// <summary>
    /// Adds an item into the context menu.
    /// </summary>
    /// <param name="item">The item.</param>
    public void AddItem(XNAContextMenuItem item) => Items.Add(item);

    /// <summary>
    /// Generates and adds an item with the specified text into the context menu.
    /// </summary>
    /// <param name="text">The text of the item.</param>
    public void AddItem(string text)
    {
        var item = new XNAContextMenuItem
        {
            Text = text
        };

        AddItem(item);
    }

    public void AddItem(string text, Action selectAction, Func<bool> selectableChecker = null, Func<bool> visibilityChecker = null, Texture2D texture = null, string hintText = null)
    {
        var item = new XNAContextMenuItem()
        {
            Text = text,
            SelectAction = selectAction,
            SelectableChecker = selectableChecker,
            VisibilityChecker = visibilityChecker,
            Texture = texture,
            HintText = hintText
        };

        AddItem(item);

        if (Enabled)
        {
            Height += GetItemHeight(item);
        }
    }

    #endregion

    public void Open(Point point)
    {
        X = point.X;
        Y = point.Y;

        int height = BORDER_WIDTH * 2;

        for (int i = 0; i < Items.Count; i++)
        {
            XNAContextMenuItem item = Items[i];
            if (item.VisibilityChecker != null)
                item.Visible = item.VisibilityChecker();

            if (item.Visible)
            {
                int itemHeight = GetItemHeight(item);
                height += itemHeight;
                item.TextY = (itemHeight - Renderer.GetTextDimensions(item.Text, GetItemFontIndex(item)).Y) / 2;
            }

            if (item.SelectableChecker != null)
                item.Selectable = item.SelectableChecker();
        }

        Height = height;

        Enable();

        if (!Detached)
            Detach();

        Point windowPoint = GetWindowPoint();
        if (windowPoint.X + Width > WindowManager.RenderResolutionX)
            X -= Width;

        if (windowPoint.Y + Height > WindowManager.RenderResolutionY)
            Y -= Height;

        openedOnThisFrame = true;
    }

    public void ClearItems()
    {
        Items.Clear();
        Height = 2;
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        if (InputEnabled)
        {
            // Hide the drop-down if the left mouse button is clicked while the
            // cursor isn't on this control
            if (Cursor.LeftClicked && !leftClickHandled && !openedOnThisFrame)
                OnLeftClick();

            leftClickHandled = false;
            openedOnThisFrame = false;

            // Update hovered index
            int itemIndexOnCursor = GetItemIndexOnCursor();

            HoveredIndex = itemIndexOnCursor > -1 && Items[itemIndexOnCursor].Selectable ? itemIndexOnCursor : -1;
        }
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
                Items[itemIndexOnCursor].SelectAction?.Invoke();
                OptionSelected?.Invoke(this, new(itemIndexOnCursor));

                if (Detached)
                    Attach();
                Disable();
            }

            return;
        }

        Attach();
        Disable();
    }

    /// <summary>
    /// Returns the index of the item that the cursor currently points to.
    /// </summary>
    private int GetItemIndexOnCursor()
    {
        Point p = GetCursorPoint();

        if (p.X < 0 || p.X > Width ||
            p.Y > Height ||
            p.Y < 0)
        {
            return -1;
        }

        int y = p.Y;
        int currentHeight = BORDER_WIDTH;

        for (int i = 0; i < Items.Count; i++)
        {
            XNAContextMenuItem item = Items[i];

            if (!item.Visible)
                continue;

            int itemHeight = GetItemHeight(item);
            if (y >= currentHeight && y <= currentHeight + itemHeight)
                return i;

            currentHeight += itemHeight;
        }

        return -1;
    }

    /// <summary>
    /// Gets the height of a context menu item.
    /// </summary>
    /// <param name="item">The item.</param>
    protected int GetItemHeight(XNAContextMenuItem item) =>
         item.Height ?? ItemHeight;

    /// <summary>
    /// Gets the index of an item's font.
    /// </summary>
    /// <param name="item">The item.</param>
    protected int GetItemFontIndex(XNAContextMenuItem item) =>
        item.FontIndex ?? FontIndex;

    protected Color GetItemTextColor(XNAContextMenuItem item) =>
        item.TextColor ?? ItemColor;

    public override void Draw(GameTime gameTime)
    {
        DrawRectangle(new(0, 0, Width, Height), BorderColor);

        int y = BORDER_WIDTH;

        for (int i = 0; i < Items.Count; i++)
        {
            if (Items[i].Visible)
                y += DrawItem(i, new(BORDER_WIDTH, y));
        }

        base.Draw(gameTime);
    }

    /// <summary>
    /// Draws a single context menu item.
    /// Returns the height of the item.
    /// This can be overriden in derived classes to customize the drawing code.
    /// </summary>
    /// <param name="index">The index of the item to draw.</param>
    /// <param name="point">The point (relative to the control) where to draw the item.</param>
    /// <returns>The height of the item that was drawn.</returns>
    protected virtual int DrawItem(int index, Point point)
    {
        XNAContextMenuItem item = Items[index];

        int itemHeight = GetItemHeight(item);

        if (HoveredIndex == index)
        {
            FillRectangle(new(point.X, point.Y, Width - (BORDER_WIDTH * 2), itemHeight), FocusColor);
        }
        else
        {
            FillRectangle(new(point.X, point.Y, Width - (BORDER_WIDTH * 2), itemHeight), BackColor);
        }

        int textX = point.X + TextHorizontalPadding;
        if (item.Texture != null)
        {
            Renderer.DrawTexture(
                item.Texture,
                new(point.X + TEXTURE_PADDING, point.Y + TEXTURE_PADDING, item.Texture.Width, item.Texture.Height),
                Color.White);
            textX += item.Texture.Width + (TEXTURE_PADDING * 2);
        }

        Color textColor = item.Selectable ? GetItemTextColor(item) : DisabledItemColor;

        DrawStringWithShadow(item.Text, FontIndex, new(textX, point.Y + TextVerticalPadding), textColor);
        if (item.HintText != null)
        {
            int hintTextX = Width - TextHorizontalPadding - (int)Renderer.GetTextDimensions(item.HintText, HintFontIndex).X;
            DrawStringWithShadow(item.HintText, HintFontIndex, new(hintTextX, point.Y + TextVerticalPadding), textColor);
        }

        return itemHeight;
    }
}