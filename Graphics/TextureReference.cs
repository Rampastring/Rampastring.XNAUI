using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace Rampastring.XNAUI.Graphics;

/// <summary>
/// A reference to a whole texture or a region of a texture. The owner of the
/// backing texture must keep it alive while the reference to it is used.
/// </summary>
public readonly struct TextureReference : IEquatable<TextureReference>
{
    /// <summary>References an entire texture. A null texture represents an empty reference.</summary>
    public TextureReference(Texture2D texture)
    {
        Texture = texture;
    }

    /// <summary>References a rectangle in the backing texture's coordinates.</summary>
    public TextureReference(Texture2D texture, Rectangle sourceRectangle)
    {
        if (texture == null)
            throw new ArgumentNullException(nameof(texture));

        if (sourceRectangle.Width <= 0 || sourceRectangle.Height <= 0 ||
            sourceRectangle.X < 0 || sourceRectangle.Y < 0 ||
            sourceRectangle.X > texture.Width - sourceRectangle.Width ||
            sourceRectangle.Y > texture.Height - sourceRectangle.Height)
        {
            throw new ArgumentOutOfRangeException(nameof(sourceRectangle));
        }

        Texture = texture;
        SourceRectangle = sourceRectangle;
    }

    public Texture2D Texture { get; }
    public Rectangle? SourceRectangle { get; }
    public bool IsEmpty => Texture == null;
    public int Width => SourceRectangle?.Width ?? Texture?.Width ?? 0;
    public int Height => SourceRectangle?.Height ?? Texture?.Height ?? 0;

    /// <summary>Converts a sprite-local crop to a rectangle in the backing texture.</summary>
    public Rectangle? GetSourceRectangle(Rectangle? crop = null)
    {
        if (SourceRectangle is not Rectangle region)
            return crop;

        if (crop is not Rectangle source)
            return region;

        if (source.Width < 0 || source.Height < 0 || source.X < 0 || source.Y < 0 ||
            source.X > region.Width - source.Width || source.Y > region.Height - source.Height)
        {
            throw new ArgumentOutOfRangeException(nameof(crop));
        }

        return new Rectangle(region.X + source.X, region.Y + source.Y, source.Width, source.Height);
    }

    public bool Equals(TextureReference other) =>
        ReferenceEquals(Texture, other.Texture) && SourceRectangle == other.SourceRectangle;

    public override bool Equals(object obj) => obj is TextureReference other && Equals(other);

    public override int GetHashCode() => unchecked((Texture?.GetHashCode() ?? 0) * 397 ^ SourceRectangle.GetHashCode());

    public static bool operator ==(TextureReference left, TextureReference right) => left.Equals(right);
    public static bool operator !=(TextureReference left, TextureReference right) => !left.Equals(right);

    public static implicit operator TextureReference(Texture2D texture) => new TextureReference(texture);
}
