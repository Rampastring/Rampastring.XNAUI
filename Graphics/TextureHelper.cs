using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Rampastring.XNAUI.Graphics;

internal static class TextureHelper
{
    internal static void PremultiplyAlpha(Texture2D texture)
    {
        var pixels = new Color[texture.Width * texture.Height];
        texture.GetData(pixels);

        if (PremultiplyAlpha(pixels))
            texture.SetData(pixels);
    }

    internal static bool PremultiplyAlpha(Color[] pixels)
    {
        bool changed = false;

        for (int i = 0; i < pixels.Length; i++)
        {
            Color color = pixels[i];
            byte alpha = color.A;

            if (alpha == 255)
                continue;

            changed = true;

            if (alpha == 0)
            {
                pixels[i] = Color.Transparent;
                continue;
            }

            color.R = (byte)(color.R * alpha / 255);
            color.G = (byte)(color.G * alpha / 255);
            color.B = (byte)(color.B * alpha / 255);
            pixels[i] = color;
        }

        return changed;
    }
}
