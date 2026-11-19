using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace Rampastring.XNAUI.Graphics;

internal sealed class UITextureCache(GraphicsDevice graphicsDevice, Func<string, Texture2D> loadTexture)
{
    private const int DEFAULT_PAGE_SIZE = 1024;
    private const int MAX_PAGE_SIZE = 2048;
    private const int PADDING = 1;

    private readonly Dictionary<string, TextureReference> images = new(StringComparer.Ordinal);
    private readonly Dictionary<Color, TextureReference> solidColors = new();
    private readonly List<Texture2D> standaloneTextures = new();
    private SpriteSheetPageCollection atlas;
    private TextureReference whitePixel;
    private TextureReference missingImage;

    internal TextureReference Load(string name, bool useAtlas)
    {
        if (string.IsNullOrEmpty(name))
            return GetMissingImage(useAtlas);

        if (images.TryGetValue(name, out TextureReference reference))
            return reference;

        Texture2D texture = loadTexture(name);
        if (texture == null)
            return GetMissingImage(useAtlas);

        try
        {
            if (useAtlas && texture.Format == SurfaceFormat.Color && CanFit(texture.Width, texture.Height))
            {
                var pixels = new Color[texture.Width * texture.Height];
                texture.GetData(pixels);
                TextureHelper.PremultiplyAlpha(pixels);
                reference = AddToAtlas(pixels, texture.Width, texture.Height);
                texture.Dispose();
            }
            else
            {
                TextureHelper.PremultiplyAlpha(texture);
                reference = OwnStandaloneTexture(texture);
            }
        }
        catch
        {
            texture.Dispose();
            throw;
        }

        images.Add(name, reference);

        return reference;
    }

    internal void Clear()
    {
        images.Clear();
        solidColors.Clear();
        whitePixel = default;
        missingImage = default;
        atlas?.Dispose();
        atlas = null;

        foreach (Texture2D texture in standaloneTextures)
            texture.Dispose();

        standaloneTextures.Clear();
    }

    internal TextureReference GetWhitePixel(bool useAtlas) => whitePixel.IsEmpty
        ? whitePixel = CreateSolidTexture(Color.White, 1, 1, useAtlas)
        : whitePixel;

    internal TextureReference GetSolidTexture(Color color, bool useAtlas)
    {
        if (!solidColors.TryGetValue(color, out TextureReference reference))
        {
            reference = CreateSolidTexture(color, 2, 2, useAtlas);
            solidColors.Add(color, reference);
        }

        return reference;
    }

    private TextureReference GetMissingImage(bool useAtlas) => missingImage.IsEmpty
        ? missingImage = CreateSolidTexture(new Color(255, 54, 244), 100, 100, useAtlas)
        : missingImage;

    private TextureReference CreateSolidTexture(Color color, int width, int height, bool useAtlas)
    {
        var pixels = new Color[width * height];
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = color;

        TextureHelper.PremultiplyAlpha(pixels);

        return AddPixels(pixels, width, height, useAtlas);
    }

    internal TextureReference AddPixels(Color[] pixels, int width, int height, bool useAtlas)
    {
        if (useAtlas && CanFit(width, height))
            return AddToAtlas(pixels, width, height);

        var texture = new Texture2D(graphicsDevice, width, height, false, SurfaceFormat.Color);

        try
        {
            texture.SetData(pixels);
            return OwnStandaloneTexture(texture);
        }
        catch
        {
            texture.Dispose();
            throw;
        }
    }

    private static bool CanFit(int width, int height) =>
        width <= MAX_PAGE_SIZE - PADDING * 2 && height <= MAX_PAGE_SIZE - PADDING * 2;

    private TextureReference AddToAtlas(Color[] pixels, int width, int height)
    {
        atlas ??= new SpriteSheetPageCollection(graphicsDevice, SurfaceFormat.Color, MAX_PAGE_SIZE, MAX_PAGE_SIZE, PADDING);

        return atlas.Add(pixels, width, height, DEFAULT_PAGE_SIZE);
    }

    private TextureReference OwnStandaloneTexture(Texture2D texture)
    {
        // A region reference is borrowed even when it covers the whole backing texture.
        var reference = new TextureReference(texture, texture.Bounds);
        standaloneTextures.Add(texture);

        return reference;
    }
}
