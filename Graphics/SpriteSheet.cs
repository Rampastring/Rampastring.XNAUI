using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Rampastring.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
#if DX
using SharpDX.Direct3D;
#endif

namespace Rampastring.XNAUI.Graphics;

/// <summary>
/// A keyed collection of individual texture regions in a sprite sheet.
/// </summary>
public interface ISpriteSheet<in TKey> : IDisposable
{
    TextureReference TextureReferenceFromKey(TKey key);
    TextureReference this[TKey key] { get; }
}

/// <summary>
/// Builds a sprite sheet with as many texture pages as needed.
/// Register images, call <see cref="Process"/> on the graphics thread, and keep the sheet
/// alive as long as any returned reference is in use.
/// </summary>
public sealed class SpriteSheet<TKey> : ISpriteSheet<TKey>
{
    private const int DEFAULT_APPEND_PAGE_SIZE = 1024;

    private readonly int maxPageWidth;
    private readonly int maxPageHeight;
    private readonly int padding;
    private readonly Dictionary<TKey, int> indices;
    private List<SpriteSheetImage> images = new();
    private SpriteSheetPageCollection pageCollection;
    private List<TextureReference> references;
    private int? appendPageSize;
    private int nextPendingImage;
    private bool disposed;

    /// <summary>
    /// Creates a new sprite sheet.
    /// </summary>
    /// <param name="graphicsDevice">The graphics device instance.</param>
    /// <param name="surfaceFormat">The surface format to use. Supports <see cref="SurfaceFormat.Color"/> and <see cref="SurfaceFormat.Alpha8"/>.</param>
    /// <param name="maxPageWidth">Maximum width of a single sprite-sheet page. If left empty, a default value is used based on what the platform supports.</param>
    /// <param name="maxPageHeight">Maximum height of a single sprite-sheet page. If left empty, a default value is used based on what the platform supports.</param>
    /// <param name="padding">How many pixels of empty space to leave between images. Should be above zero if linear scaling is used when rendering the graphics.</param>
    /// <param name="comparer">
    /// The equality comparer for image keys. If omitted or <see langword="null"/>, uses the default comparer for <typeparamref name="TKey"/>.
    /// </param>
    public SpriteSheet(GraphicsDevice graphicsDevice, SurfaceFormat surfaceFormat, int? maxPageWidth = null, int? maxPageHeight = null,
        int padding = 0, IEqualityComparer<TKey> comparer = null)
    {
        if (surfaceFormat != SurfaceFormat.Color && surfaceFormat != SurfaceFormat.Alpha8)
            throw new ArgumentOutOfRangeException(nameof(surfaceFormat), "Use Color or Alpha8 for sprite sheets.");

        if (maxPageWidth is <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxPageWidth));

        if (maxPageHeight is <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxPageHeight));

        if (padding < 0 || padding > (Math.Min(maxPageWidth ?? int.MaxValue, maxPageHeight ?? int.MaxValue) - 1) / 2)
            throw new ArgumentOutOfRangeException(nameof(padding));

        SurfaceFormat = surfaceFormat;

        this.maxPageWidth = maxPageWidth.GetValueOrDefault(GetDefaultPageSize(graphicsDevice));
        this.maxPageHeight = maxPageHeight.GetValueOrDefault(GetDefaultPageSize(graphicsDevice));
        this.padding = padding;
        indices = new Dictionary<TKey, int>(comparer);
    }

    public SurfaceFormat SurfaceFormat { get; }

    /// <summary>Gets the number of registered images, including pending additions.</summary>
    public int Count => indices.Count;

    public int PageCount => pageCollection?.Count ?? 0;

    public TextureReference this[TKey key] => TextureReferenceFromKey(key);

    public TextureReference TextureReferenceFromKey(TKey key) => GetReferences()[indices[key]];

    /// <summary>Gets a region by the zero-based index returned by <c>AddImage</c>.</summary>
    public TextureReference TextureReferenceFromIndex(int index) => GetReferences()[index];

    private static int GetDefaultPageSize(GraphicsDevice graphicsDevice)
    {
#if DX
        if (graphicsDevice.Handle is SharpDX.Direct3D11.Device device)
        {
            FeatureLevel featureLevel = device.FeatureLevel;
            if (featureLevel >= FeatureLevel.Level_11_0)
                return 16384;

            if (featureLevel >= FeatureLevel.Level_10_0)
                return 8192;

            if (featureLevel >= FeatureLevel.Level_9_3)
                return 4096;

            return 2048;
        }
#endif

        // Use portable profile limits when the backend does not expose a texture-size capability.
        return graphicsDevice.GraphicsProfile == GraphicsProfile.HiDef ? 4096 : 2048;
    }

    /// <summary>
    /// Queues a texture's to be copied into the sprite sheet during <see cref="Process"/>.
    /// </summary>
    /// <param name="texture">The source texture. Its format must match the sprite sheet's format. Keep it alive until the image is processed.</param>
    /// <param name="key">A unique key used to look up the image after processing.</param>
    /// <returns>The zero-based index to pass to <see cref="TextureReferenceFromIndex"/> after processing.</returns>
    public int AddImage(Texture2D texture, TKey key)
    {
        if (texture == null)
            throw new ArgumentNullException(nameof(texture));

        if (texture.Format != SurfaceFormat)
            throw new ArgumentException("Texture format must match the sprite sheet format.", nameof(texture));

        return Register(new SpriteSheetImage(texture.Width, texture.Height, texture), key);
    }

    /// <summary>
    /// Queues an image supplied as raw pixel bytes for <see cref="Process"/>.
    /// </summary>
    /// <param name="data">
    /// Pixel bytes in row order with no extra bytes between rows. Use one byte per pixel for <see cref="SurfaceFormat.Alpha8"/>
    /// or four bytes per pixel in red, green, blue, alpha order for <see cref="SurfaceFormat.Color"/>.
    /// The array is read during processing.
    /// </param>
    /// <param name="width">The image width in pixels.</param>
    /// <param name="height">The image height in pixels.</param>
    /// <param name="key">A unique key used to look up the image after processing.</param>
    /// <returns>The zero-based index to pass to <see cref="TextureReferenceFromIndex"/> after processing.</returns>
    public int AddImage(byte[] data, int width, int height, TKey key)
    {
        if (data == null)
            throw new ArgumentNullException(nameof(data));

        ValidateSize(width, height, maxPageWidth, maxPageHeight);
        int bytesPerPixel = SurfaceFormat == SurfaceFormat.Alpha8 ? 1 : 4;
        if (data.LongLength % bytesPerPixel != 0 || data.LongLength / bytesPerPixel != (long)width * height)
            throw new ArgumentException("Pixel data must match the image dimensions and format.", nameof(data));

        return Register(new SpriteSheetImage(width, height, data), key);
    }

    /// <summary>
    /// Queues an image supplied as pixel colors for <see cref="Process"/>.
    /// Requires a sprite sheet that uses <see cref="SurfaceFormat.Color"/>.
    /// </summary>
    /// <param name="data">One <see cref="Color"/> per pixel, in row order. The array is read during processing.</param>
    /// <param name="width">The image width in pixels.</param>
    /// <param name="height">The image height in pixels.</param>
    /// <param name="key">A unique key used to look up the image after processing.</param>
    /// <returns>The zero-based index to pass to <see cref="TextureReferenceFromIndex"/> after processing.</returns>
    public int AddImage(Color[] data, int width, int height, TKey key)
    {
        if (data == null)
            throw new ArgumentNullException(nameof(data));

        if (SurfaceFormat != SurfaceFormat.Color)
            throw new InvalidOperationException("Color pixels require a Color sprite sheet.");

        ValidateSize(width, height, maxPageWidth, maxPageHeight);

        if (data.LongLength != (long)width * height)
            throw new ArgumentException("Pixel data must match the image dimensions.", nameof(data));

        return Register(new SpriteSheetImage(width, height, data), key);
    }

    /// <summary>
    /// Enables append mode for the next call to <see cref="Process"/> and allows more images to the registered
    /// after the sprite sheet has been baked. Published regions keep their texture and coordinates.
    /// </summary>
    /// <param name="preferredPageSize">
    /// Preferred size for new pages, subject to the sheet's page limits. Omit to reuse the
    /// previous preference or start at 1024 pixels. New pages may be larger to fit an image.
    /// </param>
    public void BeginAppend(int? preferredPageSize = null)
    {
        if (disposed)
            throw new ObjectDisposedException(nameof(SpriteSheet<TKey>));

        if (preferredPageSize is <= 0)
            throw new ArgumentOutOfRangeException(nameof(preferredPageSize));

        appendPageSize = preferredPageSize ?? appendPageSize ?? DEFAULT_APPEND_PAGE_SIZE;
        images ??= new List<SpriteSheetImage>();
    }

    /// <summary>
    /// Processes registered images and releases sprite-sheet preparation data. Call
    /// <see cref="BeginAppend"/> before registering additional images after a successful build.
    /// </summary>
    public void Process(GraphicsDevice graphicsDevice)
    {
        if (graphicsDevice == null)
            throw new ArgumentNullException(nameof(graphicsDevice));

        EnsurePreparing();

        if (pageCollection != null && !ReferenceEquals(pageCollection.GraphicsDevice, graphicsDevice))
            throw new ArgumentException("Use the same graphics device for every build of a sprite sheet.", nameof(graphicsDevice));

        if (appendPageSize.HasValue)
            ProcessAppendedImages(graphicsDevice);
        else
            ProcessInitialImages(graphicsDevice);

        images = null;
        nextPendingImage = 0;
    }

    private void ProcessInitialImages(GraphicsDevice graphicsDevice)
    {
        List<SpriteSheetPage> layouts = PackImages();
        var newPages = new SpriteSheetPageCollection(graphicsDevice, SurfaceFormat, maxPageWidth, maxPageHeight, padding);
        var newReferences = new List<TextureReference>(images.Count);

        for (int i = 0; i < images.Count; i++)
            newReferences.Add(default);

        try
        {
            foreach (SpriteSheetPage page in layouts)
            {
                var texture = new Texture2D(graphicsDevice, page.Layout.Width, page.Layout.Height, false, SurfaceFormat);
                newPages.AddPackedPage(texture, page.Layout);

                if (SurfaceFormat == SurfaceFormat.Alpha8)
                    ProcessPage(page, texture, GetSingleChannelPixels);
                else
                    ProcessPage(page, texture, GetColorPixels);

                foreach (var (imageIndex, bounds) in page.Entries)
                {
                    var region = new Rectangle(bounds.X + padding, bounds.Y + padding,
                        bounds.Width - padding * 2, bounds.Height - padding * 2);

                    newReferences[imageIndex] = new TextureReference(texture, region);
                }
            }
        }
        catch (Exception ex)
        {
            newPages.Dispose();
            Logger.Log($"SpriteSheet.Process: Failed to generate sprite sheet. Error: {ex.Message}");

            throw;
        }

        pageCollection = newPages;
        references = newReferences;
    }

    private void ProcessAppendedImages(GraphicsDevice graphicsDevice)
    {
        pageCollection ??= new SpriteSheetPageCollection(graphicsDevice, SurfaceFormat, maxPageWidth, maxPageHeight, padding);
        references ??= new List<TextureReference>(images.Count);

        while (nextPendingImage < images.Count)
        {
            SpriteSheetImage image = images[nextPendingImage];
            TextureReference reference = SurfaceFormat == SurfaceFormat.Alpha8
                ? pageCollection.Add(GetSingleChannelPixels(image), image.Width, image.Height, appendPageSize.Value)
                : pageCollection.Add(GetColorPixels(image), image.Width, image.Height, appendPageSize.Value);

            references.Add(reference);
            images[nextPendingImage] = default;
            nextPendingImage++;
        }
    }

    public void Dispose()
    {
        if (disposed)
            return;

        disposed = true;

        pageCollection?.Dispose();
        pageCollection = null;
        references = null;
        images = null;
        indices.Clear();
    }

    private int Register(SpriteSheetImage image, TKey key)
    {
        EnsurePreparing();
        ValidateSize(image.Width, image.Height, maxPageWidth, maxPageHeight);

        int index = indices.Count;
        indices.Add(key, index);
        images.Add(image);

        return index;
    }

    private void ValidateSize(int width, int height, int? pageWidth, int? pageHeight)
    {
        if (width <= 0 || width > (pageWidth ?? int.MaxValue) - padding * 2)
        {
            throw new ArgumentOutOfRangeException(nameof(width), width,
                $"Image and padding must fit within the page width of {pageWidth ?? int.MaxValue}.");
        }

        if (height <= 0 || height > (pageHeight ?? int.MaxValue) - padding * 2)
        {
            throw new ArgumentOutOfRangeException(nameof(height), height,
                $"Image and padding must fit within the page height of {pageHeight ?? int.MaxValue}.");
        }
    }

    private void EnsurePreparing()
    {
        if (disposed)
            throw new ObjectDisposedException(nameof(SpriteSheet<TKey>));

        if (images == null)
            throw new InvalidOperationException("Call BeginAppend to reopen a processed sprite sheet.");
    }

    private List<TextureReference> GetReferences()
    {
        if (disposed)
            throw new ObjectDisposedException(nameof(SpriteSheet<TKey>));

        return references ?? throw new InvalidOperationException("Process the sprite sheet before looking up its images.");
    }

    private List<SpriteSheetPage> PackImages()
    {
        var layouts = new List<SpriteSheetPage>();
        var packingOrder = Enumerable.Range(0, images.Count)
            .OrderByDescending(index => images[index].Height)
            .ThenByDescending(index => images[index].Width);

        foreach (int index in packingOrder)
        {
            SpriteSheetImage image = images[index];
            ValidateSize(image.Width, image.Height, maxPageWidth, maxPageHeight);
            int width = image.Width + padding * 2;
            int height = image.Height + padding * 2;
            Rectangle bounds = default;
            SpriteSheetPage targetPage = null;

            foreach (SpriteSheetPage page in layouts)
            {
                if (page.Layout.TryAdd(width, height, out bounds))
                {
                    targetPage = page;
                    break;
                }
            }

            if (targetPage == null)
            {
                targetPage = new SpriteSheetPage(new SpriteSheetLayout(maxPageWidth, maxPageHeight));
                targetPage.Layout.TryAdd(width, height, out bounds);
                layouts.Add(targetPage);
            }

            targetPage.Entries.Add((index, bounds));
        }

        return layouts;
    }

    private void ProcessPage<TPixel>(SpriteSheetPage page, Texture2D texture, Func<SpriteSheetImage, TPixel[]> readPixels) where TPixel : struct
    {
        var pixels = new TPixel[checked(texture.Width * texture.Height)];

        foreach (var (imageIndex, bounds) in page.Entries)
        {
            SpriteSheetImage image = images[imageIndex];
            SpriteSheetPixels.Copy(readPixels(image), image.Width, image.Height, pixels, texture.Width, bounds, padding);
        }

        texture.SetData(pixels);
    }

    private static byte[] GetSingleChannelPixels(SpriteSheetImage image) => image.Source is byte[] bytes ? bytes : image.ReadTexture<byte>();

    private static Color[] GetColorPixels(SpriteSheetImage image)
    {
        if (image.Source is Color[] colors)
            return colors;

        if (image.Source is byte[] bytes)
        {
            colors = new Color[bytes.Length / 4];

            for (int i = 0; i < colors.Length; i++)
            {
                int offset = i * 4;
                colors[i] = new Color(bytes[offset], bytes[offset + 1], bytes[offset + 2], bytes[offset + 3]);
            }

            return colors;
        }

        return image.ReadTexture<Color>();
    }

    private sealed class SpriteSheetPage(SpriteSheetLayout layout)
    {
        internal SpriteSheetLayout Layout { get; } = layout;
        internal List<(int ImageIndex, Rectangle Bounds)> Entries { get; } = new();
    }

    private readonly struct SpriteSheetImage(int width, int height, object source)
    {
        internal int Width { get; } = width;
        internal int Height { get; } = height;
        internal object Source { get; } = source;

        internal TPixel[] ReadTexture<TPixel>() where TPixel : struct
        {
            var pixels = new TPixel[checked(Width * Height)];
            ((Texture2D)Source).GetData(pixels);
            return pixels;
        }
    }
}
