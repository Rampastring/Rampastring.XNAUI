using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Rampastring.Tools;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using System;
using System.Collections.Generic;
using System.IO;

namespace Rampastring.XNAUI;

/// <summary>
/// Basic class of animation texture with delay.
/// </summary>
public class AnimationFrame
{
    public Texture2D Texture { get; set; }
    public TimeSpan Delay { get; set; }
}

/// <summary>
/// General purpose animation class.
/// </summary>
public class Animation
{
    private List<AnimationFrame> Frames = null;
    private Image image = null;
    private IImageFormat format = null;
    private int currentFrameId = 0;
    private int currentDelay = 0;
    private int totalElapsedTime = 0;
    private int repeatCount = 0;

    public Texture2D CurrentFrame { get; private set; }
    public int Height { get; private set; }
    public int Width { get; private set; }

    /// <summary>
    /// Generic constructor.
    /// </summary>
    /// <param name="frames">List of frames.</param>
    public Animation(List<AnimationFrame> frames)
    {
        Frames = frames;
        
        if (Frames != null && Frames.Count > 0)
        {
            CurrentFrame = frames[0].Texture;
            currentDelay = (int)frames[0].Delay.TotalMilliseconds;
        }
    }

    /// <summary>
    /// Loads animation from ImageSharp format.
    /// </summary>
    /// <param name="image">The actual animation parsed by ImageSharp.</param>
    /// <param name="format">The animation format details.</param>
    public Animation(Image image, IImageFormat format)
    {
        this.image = image;
        this.format = format;

        switch (format)
        {
            case SixLabors.ImageSharp.Formats.Gif.GifFormat:
                FromGIF(image);
                break;
            default:
                throw new NotSupportedException("Unsupported image format for animation: " + format.Name);
                break;
        }
    }

    /// <summary>
    /// Loads GIF format animation.
    /// </summary>
    /// <param name="gif">The actual animation parsed by ImageSharp.</param>
    public Animation(Image gif) => FromGIF(gif);

    private void FromGIF(Image gif)
    {
        Frames = new List<AnimationFrame>();

        repeatCount = gif.Metadata.GetGifMetadata().RepeatCount;
        currentDelay = gif.Frames[0].Metadata.GetGifMetadata().FrameDelay * 10;

        Height = gif.Height;
        Width = gif.Width;

        int len = gif.Frames.Count;

        int estimatedSize = Height * Width * len * 4 / 1024 / 1024;
        if (estimatedSize >= 50)
            Logger.Log("Loading animation with estimated size more than 50MiB in raw format.");

        for (int i = 0; i < len; i++)
        {
            // ImageSharp returns not milliseconds, but decisecond as documentation says.
            var delay = gif.Frames[i].Metadata.GetGifMetadata().FrameDelay;

            // When delay is equal to 1 or less, browsers use a delay equivalent to 100ms.
            // Example: https://source.chromium.org/chromium/chromium/src/+/main:third_party/blink/renderer/platform/graphics/deferred_image_decoder.cc;drc=e15f3c2e15ee7570159406526444fa1ffb35150f;l=351
            var currentDelay = delay > 1? delay * 10 : 100;
            var currentFrame = gif.Frames.CloneFrame(i);

            // Fill Frames only with delays because ImageSharp image class is not fast enough when it gets frame metadata and cause play lag.
            Frames.Add(new AnimationFrame { Texture = null, Delay = TimeSpan.FromMilliseconds(currentDelay) });
        }

        CurrentFrame = AssetLoader.TextureFromImage(gif.Frames.CloneFrame(0));
    }

    /// <summary>
    /// Draw next frame when frame delay expires.
    /// </summary>
    /// <param name="gameTime"></param>
    public void Update(GameTime gameTime)
    {
        totalElapsedTime += Convert.ToInt32(gameTime.ElapsedGameTime.TotalMilliseconds);

        if (totalElapsedTime > currentDelay)
        {
            totalElapsedTime = 0;
            NextFrame();
        }
    }

    /// <summary>
    /// Draw next frame of animation.
    /// </summary>
    public void NextFrame()
    {
        if (repeatCount == 1 && currentFrameId == Frames.Count - 1)
            return;

        if (currentFrameId == Frames.Count - 1)
            repeatCount -= 1;

        currentFrameId = (currentFrameId + 1) % Frames.Count;
        currentDelay = Frames[currentFrameId].Delay.Milliseconds;
        
        if (image == null)
        {
            // If the class is allocated from the generic constructor, then image is null and we should use Animation.Frame.
            CurrentFrame = Frames[currentFrameId].Texture;
        }
        else
        {
            // Otherwise we have the source image and would be better off saving memory.
            var clonedFrame = image.Frames.CloneFrame(currentFrameId);
            CurrentFrame.Dispose();
            CurrentFrame = AssetLoader.TextureFromImage(clonedFrame);
            clonedFrame.Dispose();
        }
    }
}

