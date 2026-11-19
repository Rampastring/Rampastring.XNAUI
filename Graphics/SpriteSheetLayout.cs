using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;

namespace Rampastring.XNAUI.Graphics;

/// <summary>
/// Packs rectangles into one sprite sheet page without allocating pixel or GPU storage.
/// Callers can reserve space for padding or for a group of sprites in one rectangle.
/// </summary>
public sealed class SpriteSheetLayout
{
    private readonly List<Rectangle> rows = new();

    public SpriteSheetLayout(int maxWidth, int maxHeight)
    {
        if (maxWidth <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxWidth));

        if (maxHeight <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxHeight));

        MaxWidth = maxWidth;
        MaxHeight = maxHeight;
    }

    public int MaxWidth { get; private set; }
    public int MaxHeight { get; private set; }
    public int Width { get; private set; }
    public int Height { get; private set; }

    /// <summary>
    /// Tries to reserve a rectangle without changing the layout on failure.
    /// </summary>
    public bool TryAdd(int width, int height, out Rectangle bounds)
    {
        if (!TryFindSpace(width, height, out bounds, out int rowIndex))
            return false;

        Reserve(bounds, rowIndex);

        return true;
    }

    internal bool TryFindSpace(int width, int height, out Rectangle bounds, out int rowIndex)
    {
        if (width <= 0)
            throw new ArgumentOutOfRangeException(nameof(width));

        if (height <= 0)
            throw new ArgumentOutOfRangeException(nameof(height));

        rowIndex = -1;
        bounds = default;

        if (width > MaxWidth || height > MaxHeight)
            return false;

        for (int i = 0; i < rows.Count; i++)
        {
            Rectangle row = rows[i];
            if (height <= row.Height && width <= MaxWidth - row.Width &&
                (rowIndex < 0 || row.Height < rows[rowIndex].Height))
            {
                rowIndex = i;
            }
        }

        if (rowIndex >= 0)
        {
            Rectangle row = rows[rowIndex];
            bounds = new Rectangle(row.Width, row.Y, width, height);

            return true;
        }

        if (height > MaxHeight - Height)
            return false;

        rowIndex = rows.Count;
        bounds = new Rectangle(0, Height, width, height);

        return true;
    }

    internal void Reserve(Rectangle bounds, int rowIndex)
    {
        if (rowIndex == rows.Count)
        {
            rows.Add(bounds);
            Height = bounds.Bottom;
        }
        else
        {
            Rectangle row = rows[rowIndex];
            row.Width = bounds.Right;
            rows[rowIndex] = row;
        }

        Width = Math.Max(Width, bounds.Right);
    }

    internal void TrimToUsedArea()
    {
        MaxWidth = Width;
        MaxHeight = Height;
    }
}

internal static class SpriteSheetPixels
{
    internal static void Copy<TPixel>(TPixel[] source, int width, int height, TPixel[] destination, int stride, Rectangle allocation, int padding)
        where TPixel : struct
    {
        // Repeat outer texels into the padding so linear filtering behaves like clamp at sprite edges.

        int rowStart = (allocation.Y + padding) * stride + allocation.X;

        for (int y = 0; y < height; y++)
        {
            int sourceStart = y * width;
            int destinationStart = rowStart + y * stride;

            CopyRow(source, sourceStart, destination, destinationStart + padding, width);

            for (int p = 0; p < padding; p++)
            {
                destination[destinationStart + p] = source[sourceStart];
                destination[destinationStart + padding + width + p] = source[sourceStart + width - 1];
            }
        }

        int lastRow = rowStart + (height - 1) * stride;
        for (int p = 1; p <= padding; p++)
        {
            CopyRow(destination, rowStart, destination, rowStart - p * stride, allocation.Width);
            CopyRow(destination, lastRow, destination, lastRow + p * stride, allocation.Width);
        }
    }

    private static void CopyRow<TPixel>(TPixel[] source, int sourceIndex, TPixel[] destination, int destinationIndex, int count) where TPixel : struct
    {
        if (typeof(TPixel) == typeof(byte))
            Buffer.BlockCopy(source, sourceIndex, destination, destinationIndex, count);
        else
            Array.Copy(source, sourceIndex, destination, destinationIndex, count);
    }
}
