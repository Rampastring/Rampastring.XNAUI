using Microsoft.Xna.Framework.Graphics;
using SixLabors.ImageSharp;
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Rampastring.XNAUI;

public class Animation
{
    private List<Texture2D> Frames;
    private List<int> Delays;
    private int currentFrameId = 0;
    private int currentDelay = 0;
    private int totalElapsedTime = 0;

    public Texture2D CurrentFrame { get; private set; }
    public int Height { get; }
    public int Width { get; }

    public Animation(string value) 
    {
        Frames = new List<Texture2D>();
        Delays = new List<int>();
        
        var gif = AssetLoader.LoadAnimation(value);
        currentDelay = gif.Frames[0].Metadata.GetGifMetadata().FrameDelay * 10;

        Height = gif.Height;
        Width = gif.Width;

        try
        {
            // ImageSharper have a bug that gives half of all frame count
            for (;;)
            {
                // ImageSharper returns not milliseconds, but decisecond
                var delay = gif.Frames[0].Metadata.GetGifMetadata().FrameDelay * 10;
                var currentFrame = gif.Frames.ExportFrame(0);

                Delays.Add(delay);
                Frames.Add(AssetLoader.TextureFromImage(currentFrame));
            }
        }
        catch
        {
        }

        CurrentFrame = Frames[0];
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
        currentFrameId = ++currentFrameId % Delays.Count;
        currentDelay = Delays[currentFrameId];
        CurrentFrame = Frames[currentFrameId];
        return CurrentFrame;
    }
}
