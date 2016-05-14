using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Rampastring.Tools;

namespace Rampastring.XNAUI
{
    public static class AssetLoader
    {
        static GraphicsDevice GraphicsDevice;

        public static List<string> AssetSearchPaths;

        static List<Texture2D> TextureCache;
        static List<SoundEffect> SoundCache;

        public static void Initialize(GraphicsDevice gd)
        {
            GraphicsDevice = gd;
            AssetSearchPaths = new List<string>();
            TextureCache = new List<Texture2D>();
            SoundCache = new List<SoundEffect>();
        }

        /// <summary>
        /// Loads a texture with the specific name. If the texture isn't found from any
        /// asset search path, returns a dummy texture.
        /// </summary>
        /// <param name="name">The name of the texture.</param>
        /// <returns>The texture if it was found and could be loaded, otherwise a dummy texture.</returns>
        public static Texture2D LoadTexture(string name)
        {
            Texture2D cachedTexture = TextureCache.Find(t => t.Name == name);

            if (cachedTexture != null)
                return cachedTexture;

            foreach (string searchPath in AssetSearchPaths)
            {
                if (File.Exists(searchPath + name))
                {
                    using (FileStream fs = File.OpenRead(searchPath + name))
                    {
                        Texture2D texture = Texture2D.FromStream(GraphicsDevice, fs);
                        TextureCache.Add(texture);
                        texture.Name = name;
                        return texture;
                    }
                }
            }

            using (MemoryStream ms = new MemoryStream())
            {
                Properties.Resources.hotbutton.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                return Texture2D.FromStream(GraphicsDevice, ms);
            }
        }

        public static Texture2D LoadTextureUncached(string name)
        {
            foreach (string searchPath in AssetSearchPaths)
            {
                if (File.Exists(searchPath + name))
                {
                    using (FileStream fs = File.OpenRead(searchPath + name))
                    {
                        Texture2D texture = Texture2D.FromStream(GraphicsDevice, fs);
                        TextureCache.Add(texture);
                        texture.Name = name;
                        return texture;
                    }
                }
            }

            using (MemoryStream ms = new MemoryStream())
            {
                Properties.Resources.hotbutton.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                return Texture2D.FromStream(GraphicsDevice, ms);
            }
        }

        /// <summary>
        /// Creates a one-colored texture.
        /// </summary>
        /// <param name="color">The color of the texture.</param>
        /// <param name="width">The width of the texture in pixels.</param>
        /// <param name="height">The height of the texture in pixels.</param>
        /// <returns>A texture.</returns>
        public static Texture2D CreateTexture(Color color, int width, int height)
        {
            Texture2D texture = new Texture2D(GraphicsDevice, width, height, false, SurfaceFormat.Color);

            Color[] colorArray = new Color[width * height];

            for (int i = 0; i < colorArray.Length; i++)
                colorArray[i] = color;

            texture.SetData(colorArray);

            return texture;
        }

        public static SoundEffect LoadSound(string name)
        {
            SoundEffect cachedSound = SoundCache.Find(se => se.Name == name);

            if (cachedSound != null)
                return cachedSound;

            foreach (string searchPath in AssetSearchPaths)
            {
                if (File.Exists(searchPath + name))
                {
                    using (FileStream fs = File.OpenRead(searchPath + name))
                    {
                        SoundEffect se = SoundEffect.FromStream(fs);
                        se.Name = name;
                        SoundCache.Add(se);
                        return se;
                    }
                }
            }

            Logger.Log("AssetLoader.LoadSound: Sound not found! " + name);

            return null;
        }

        /// <summary>
        /// Creates a color based on a color string in the form "R,G,B". All values must be between 0 and 255.
        /// </summary>
        /// <param name="colorString">The color string in the form "R,G,B". All values must be between 0 and 255.</param>
        /// <returns>A XNA Color struct based on the given string.</returns>
        public static Color GetColorFromString(string colorString)
        {
            try
            {
                string[] colorArray = colorString.Split(',');
                Color color = new Color(Convert.ToByte(colorArray[0]), Convert.ToByte(colorArray[1]), Convert.ToByte(colorArray[2]));
                return color;
            }
            catch
            {
                throw new Exception("AssetLoader.GetColorFromString: Failed to convert " + colorString + " to a valid color!");
            }
        }

        /// <summary>
        /// Creates a color based on a color string in the form "R,G,B,A". All values must be between 0 and 255.
        /// </summary>
        public static Color GetARGBColorFromString(string colorString)
        {
            try
            {
                string[] colorArray = colorString.Split(',');
                Color color = new Color(Convert.ToByte(colorArray[0]), 
                    Convert.ToByte(colorArray[1]), 
                    Convert.ToByte(colorArray[2]),
                    Convert.ToByte(colorArray[3]));
                return color;
            }
            catch
            {
                throw new Exception("AssetLoader.GetColorFromString: Failed to convert " + colorString + " to a valid color!");
            }
        }
    }
}
