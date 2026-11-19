using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace Rampastring.XNAUI.Graphics;

/// <summary>
/// A collection of sprite-sheet "pages".
/// One sprite sheet might grow too big for the graphics device, making it necessary to split the sheet
/// into multiple smaller sheets, called pages. 
/// 
/// This class is the layer that handles the creation of new pages as necessary.
/// </summary>
internal sealed class SpriteSheetPageCollection(GraphicsDevice graphicsDevice, SurfaceFormat surfaceFormat, int maxPageWidth, int maxPageHeight, int padding)
    : IDisposable
{
    private readonly List<Page> pages = new();

    internal GraphicsDevice GraphicsDevice { get; } = graphicsDevice;
    internal int Count => pages.Count;

    internal void AddPackedPage(Texture2D texture, SpriteSheetLayout layout)
    {
        layout.TrimToUsedArea();
        pages.Add(new Page(texture, layout));
    }

    internal TextureReference Add<TPixel>(TPixel[] pixels, int width, int height, int preferredPageSize) where TPixel : struct
    {
        int paddedWidth = width + padding * 2;
        int paddedHeight = height + padding * 2;

        foreach (Page page in pages)
        {
            if (page.Layout.TryFindSpace(paddedWidth, paddedHeight, out Rectangle bounds, out int rowIndex))
                return Upload(page, bounds, rowIndex, pixels, width, height);
        }

        int pageWidth = GetPageSize(paddedWidth, preferredPageSize, maxPageWidth);
        int pageHeight = GetPageSize(paddedHeight, preferredPageSize, maxPageHeight);
        var layout = new SpriteSheetLayout(pageWidth, pageHeight);
        var texture = new Texture2D(GraphicsDevice, pageWidth, pageHeight, false, surfaceFormat);
        var newPage = new Page(texture, layout);

        try
        {
            var result = Upload(newPage, new Rectangle(0, 0, paddedWidth, paddedHeight), 0, pixels, width, height);
            pages.Add(newPage);

            return result;
        }
        catch
        {
            texture.Dispose();
            throw;
        }
    }

    public void Dispose()
    {
        foreach (Page page in pages)
            page.Texture.Dispose();

        pages.Clear();
    }

    private static int GetPageSize(int requiredSize, int preferredSize, int limit)
    {
        int size = Math.Min(preferredSize, limit);

        while (size < requiredSize && size < limit)
            size = size <= limit / 2 ? size * 2 : limit;

        return size;
    }

    private TextureReference Upload<TPixel>(Page page, Rectangle bounds, int rowIndex, TPixel[] pixels, int width, int height) where TPixel : struct
    {
        TPixel[] uploadPixels = pixels;

        if (padding > 0)
        {
            uploadPixels = new TPixel[checked(bounds.Width * bounds.Height)];
            var uploadBounds = new Rectangle(0, 0, bounds.Width, bounds.Height);
            SpriteSheetPixels.Copy(pixels, width, height, uploadPixels, bounds.Width, uploadBounds, padding);
        }

        page.Texture.SetData(0, bounds, uploadPixels, 0, uploadPixels.Length);
        page.Layout.Reserve(bounds, rowIndex);

        return new TextureReference(page.Texture, new Rectangle(bounds.X + padding, bounds.Y + padding, width, height));
    }

    private sealed class Page(Texture2D texture, SpriteSheetLayout layout)
    {
        internal Texture2D Texture { get; } = texture;
        internal SpriteSheetLayout Layout { get; } = layout;
    }
}
