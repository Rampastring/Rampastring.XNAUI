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
        switch(format.ToString())
        {
            case "SixLabors.ImageSharp.Formats.Gif.GifFormat":
                FromGIF(image);
                break;
            default:
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

        // ImageSharp doesn't return last gif frame
        int len = gif.Frames.Count - 1;

        for (int i = 0; i < len; i++)
        {
            // ImageSharp returns not milliseconds, but decisecond
            var delay = gif.Frames[0].Metadata.GetGifMetadata().FrameDelay * 10;
            var currentFrame = gif.Frames.ExportFrame(0);

            Frames.Add(new AnimationFrame { Texture = AssetLoader.TextureFromImage(currentFrame), Delay = TimeSpan.FromMilliseconds(delay) });
        }

        CurrentFrame = Frames[0].Texture;
    }

    public Texture2D Next(GameTime gameTime)
    {
        totalElapsedTime += Convert.ToInt32(gameTime.ElapsedGameTime.TotalMilliseconds);

        if (totalElapsedTime > currentDelay)
        {
            totalElapsedTime = 0;
            return Next();
        }

        return null;
    }

    public Texture2D Next()
    {
        currentFrameId = (currentFrameId + 1) % Frames.Count;
        currentDelay = Frames[currentFrameId].Delay.Milliseconds;
        CurrentFrame = Frames[currentFrameId].Texture;
        return CurrentFrame;
    }
}
