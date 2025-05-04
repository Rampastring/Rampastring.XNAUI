using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Rampastring.Tools;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using System;
using System.Collections.Generic;
using System.IO;

namespace Rampastring.XNAUI;

public class AnimationFrame
{
    public Texture2D Texture { get; set; }
    public TimeSpan Delay { get; set; }
}

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

    public Animation(List<AnimationFrame> frames)
    {
        Frames = frames;
        
        if (Frames != null && Frames.Count > 0)
        {
            CurrentFrame = frames[0].Texture;
            currentDelay = (int)frames[0].Delay.TotalMilliseconds;
        }
    }

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

    public Animation(Image gif) => FromGIF(gif);

    private void FromGIF(Image gif)
    {
        Frames = new List<AnimationFrame>();

        repeatCount = gif.Metadata.GetGifMetadata().RepeatCount;
        currentDelay = gif.Frames[0].Metadata.GetGifMetadata().FrameDelay * 10;

        Height = gif.Height;
        Width = gif.Width;

        int len = gif.Frames.Count;

        for (int i = 0; i < len; i++)
        {
            // ImageSharp returns not milliseconds, but decisecond as documentation says.
            var delay = gif.Frames[i].Metadata.GetGifMetadata().FrameDelay;

            // When delay is equal to 1 or less, browsers use a delay equivalent to 100ms.
            // Example: https://source.chromium.org/chromium/chromium/src/+/main:third_party/blink/renderer/platform/graphics/deferred_image_decoder.cc;drc=e15f3c2e15ee7570159406526444fa1ffb35150f;l=351
            var currentDelay = delay > 1? delay * 10 : 100;
            var currentFrame = gif.Frames.CloneFrame(i);

            // ImageSharp image class is not fast enough to draw images in the expected time.
            Frames.Add(new AnimationFrame { Texture = null, Delay = TimeSpan.FromMilliseconds(currentDelay) });
        }

        CurrentFrame = AssetLoader.TextureFromImage(gif.Frames.CloneFrame(0));
    }

    public void Update(GameTime gameTime)
    {
        totalElapsedTime += Convert.ToInt32(gameTime.ElapsedGameTime.TotalMilliseconds);

        if (totalElapsedTime > currentDelay)
        {
            totalElapsedTime = 0;
            NextFrame();
        }
    }

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
            CurrentFrame.Dispose();
            CurrentFrame = AssetLoader.TextureFromImage(image.Frames.CloneFrame(currentFrameId));
        }
    }
}

