using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Rampastring.XNAUI;

/// <summary>
/// A class that contains UI-related settings, like default UI colors.
/// </summary>
public class UISettings
{
    /// <summary>
    /// The currently active UI settings.
    /// </summary>
    public static UISettings ActiveSettings { get; set; }

    public float TextShadowDistance { get; set; } = 1.0f;
    public Color TextShadowColor { get; set; } = Color.Black;

    public Color TextColor { get; set; } = new Color(196, 196, 196);

    public Color SubtleTextColor { get; set; } = Color.Gray;

    public Color AltColor { get; set; } = Color.White;

    public Color ButtonTextColor { get; set; } = new Color(196, 196, 196);

    public Color DisabledItemColor { get; set; } = Color.Gray;

    public Color ButtonHoverColor { get; set; } = Color.White;

    public Color BackgroundColor { get; set; } = Color.Black;

    private Color? textBoxBackgroundColor;

    /// <summary>
    /// The default background color of text boxes. Falls back to
    /// <see cref="BackgroundColor"/> when it has not been set explicitly.
    /// </summary>
    public Color TextBoxBackgroundColor
    {
        get => textBoxBackgroundColor ?? BackgroundColor;
        set => textBoxBackgroundColor = value;
    }

    private Color? dropDownBackgroundColor;

    /// <summary>
    /// The default background color of drop-downs. Falls back to
    /// <see cref="BackgroundColor"/> when it has not been set explicitly.
    /// </summary>
    public Color DropDownBackgroundColor
    {
        get => dropDownBackgroundColor ?? BackgroundColor;
        set => dropDownBackgroundColor = value;
    }

    public Color SelectionColor { get; set; } = new Color(128, 128, 128);

    public Color FocusColor { get; set; } = new Color(64, 64, 64);

    public Color PanelBackgroundColor { get; set; } = new Color(32, 32, 32);

    public Color PanelBorderColor { get; set; } = new Color(196, 196, 196);

    public Color WindowActiveBorderColor { get; set; } = new Color(222, 222, 222);

    public Color WindowInactiveBorderColor { get; set; } = new Color(64, 64, 64);

    public Texture2D CheckBoxCheckedTexture { get; set; }

    public Texture2D CheckBoxClearTexture { get; set; }

    public Texture2D CheckBoxDisabledCheckedTexture { get; set; }

    public Texture2D CheckBoxDisabledClearTexture { get; set; }
    public int? DropDownDefaultItemHeight { get; set; }
    public int? ListBoxDefaultItemHeight { get; set; }
    public int? ContextMenuDefaultItemHeight { get; set; }
    public int? TextBoxDefaultHeight { get; set; }

    public float DefaultAlphaRate { get; set; } = 0.005f;

    public float CheckBoxAlphaRate { get; set; } = 0.05f;

    public float IndicatorAlphaRate { get; set; } = 0.05f;

    public float WindowAppearingRate { get; set; } = 0.9f;

    public float WindowDisappearingRate { get; set; } = 1.0f;

    public int BorderThickness { get; set; } = 1;
}
