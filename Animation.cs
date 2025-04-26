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
    private List<AnimationFrame> Frames;
    private int currentFrameId = 0;
    private int currentDelay = 0;
    private int totalElapsedTime = 0;

    public Texture2D CurrentFrame { get; private set; }
    public int Height { get; private set; }
    public int Width { get; private set; }

    public Animation(List<AnimationFrame> frames)
    {
        Frames = frames;
        
        if (Frames == null) return;
        if (Frames.Count == 0) return;

        currentDelay = (int)frames[0].Delay.TotalMilliseconds;
    }

    public Animation(Image image, IImageFormat format)
    {
        switch(format)
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
        
        currentDelay = gif.Frames[0].Metadata.GetGifMetadata().FrameDelay * 10;

        Height = gif.Height;
        Width = gif.Width;

        int len = gif.Frames.Count;

        for (int i = 0; i < len; i++)
        {
            // ImageSharp returns not milliseconds, but decisecond
            var delay = gif.Frames[i].Metadata.GetGifMetadata().FrameDelay;

            // When delay is equal to 1 or less, browsers use a delay equivalent to 100ms.
            // Example: https://source.chromium.org/chromium/chromium/src/+/main:third_party/blink/renderer/platform/graphics/deferred_image_decoder.cc;drc=e15f3c2e15ee7570159406526444fa1ffb35150f;l=351
            var currentDelay = delay > 1? delay * 10 : 100;
            var currentFrame = gif.Frames.CloneFrame(i);

            Frames.Add(new AnimationFrame { Texture = AssetLoader.TextureFromImage(currentFrame), Delay = TimeSpan.FromMilliseconds(currentDelay) });
        }

        CurrentFrame = Frames[0].Texture;
    }

    public void Update(GameTime gameTime)
    {
        totalElapsedTime += Convert.ToInt32(gameTime.ElapsedGameTime.TotalMilliseconds);

        if (totalElapsedTime > currentDelay)
        {
            totalElapsedTime = 0;
            Update();
        }
    }

    public void Update()
    {
        currentFrameId = (currentFrameId + 1) % Frames.Count;
        currentDelay = Frames[currentFrameId].Delay.Milliseconds;
        CurrentFrame = Frames[currentFrameId].Texture;
    }
}
