using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Rampastring.XNAUI
{
    public static class UISettings
    {
        public static Color TextColor { get; set; }

        public static Color SubtleTextColor { get; set; }

        public static Color AltColor { get; set; }

        public static Color ButtonColor { get; set; }

        public static Color DisabledButtonColor { get; set; }

        public static Color ButtonHoverColor { get; set; }

        public static Color BackgroundColor { get; set; }

        public static Color PanelBorderColor { get; set; }

        public static Color FocusColor { get; set; }

        public static Color WindowBorderColor { get; set; }

        public static Texture2D CheckBoxCheckedTexture { get; set; }

        public static Texture2D CheckBoxClearTexture { get; set; }

        public static Texture2D CheckBoxDisabledCheckedTexture { get; set; }

        public static Texture2D CheckBoxDisabledClearTexture { get; set; }

        public static float DefaultAlphaRate = 0.005f;

        public static float CheckBoxAlphaRate = 0.05f;
    }
}
