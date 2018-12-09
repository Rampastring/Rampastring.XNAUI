using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using System.IO;
using System.Text;

namespace Rampastring.XNAUI
{
    public static class Renderer
    {
        static SpriteBatch SpriteBatch;

        static List<SpriteFont> Fonts;

        static Texture2D whitePixelTexture;

        public static void Initialize(GraphicsDevice gd, ContentManager content, string contentPath)
        {
            SpriteBatch = new SpriteBatch(gd);
            Fonts = new List<SpriteFont>();

            content.RootDirectory = contentPath;

            int i = 0;
            while (true)
            {
                string sfName = string.Format("SpriteFont{0}", i);

                if (File.Exists(contentPath + sfName + ".xnb"))
                {
                    Fonts.Add(content.Load<SpriteFont>(sfName));
                    i++;
                    continue;
                }

                break;
            }

            whitePixelTexture = AssetLoader.CreateTexture(Color.White, 1, 1);
        }

        /// <summary>
        /// Returns a version of the given string where all characters that don't
        /// appear in the given font have been replaced with question marks.
        /// </summary>
        /// <param name="str">The string.</param>
        /// <param name="fontIndex">The index of the font.</param>
        public static string GetSafeString(string str, int fontIndex)
        {
            SpriteFont sf = Fonts[fontIndex];

            StringBuilder sb = new StringBuilder(str);

            for (int i = 0; i < str.Length; i++)
            {
                char c = str[i];

                if (c != '\r' && c != '\n' && !sf.Characters.Contains(c))
                    sb.Replace(c, '?');
            }

            return sb.ToString();
        }

        /// <summary>
        /// Returns a that has had its width limited to a specific number.
        /// Characters that'd cross over the width have been cut.
        /// </summary>
        /// <param name="str">The string to limit.</param>
        /// <param name="fontIndex">The index of the font to use.</param>
        /// <param name="maxWidth">The maximum width of the string.</param>
        /// <returns></returns>
        public static string GetStringWithLimitedWidth(string str, int fontIndex, int maxWidth)
        {
            var sb = new StringBuilder(str);
            var spriteFont = Fonts[fontIndex];

            while (spriteFont.MeasureString(sb.ToString()).X > maxWidth)
            {
                sb.Remove(sb.Length - 1, 1);
            }

            return sb.ToString();
        }

        public static TextParseReturnValue FixText(string text, int fontIndex, int width)
        {
            return TextParseReturnValue.FixText(Fonts[fontIndex], width, text);
        }

        public static List<string> GetFixedTextLines(string text, int fontIndex, int width)
        {
            return TextParseReturnValue.GetFixedTextLines(Fonts[fontIndex], width, text);
        }

        public static void BeginDraw()
        {
            BeginDraw(SamplerState.LinearClamp);
        }

        public static void BeginDraw(SpriteSortMode ssm, BlendState bs)
        {
            SpriteBatch.Begin(ssm, bs);
        }

        public static void BeginDraw(SamplerState ss)
        {
            BlendState bs = new BlendState();

#if XNA
            //bs.AlphaDestinationBlend = Blend.DestinationAlpha;
            //bs.ColorDestinationBlend = Blend.DestinationAlpha;

            SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, ss, DepthStencilState.Default, RasterizerState.CullNone);
#else
            bs.AlphaDestinationBlend = Blend.One;
            bs.ColorDestinationBlend = Blend.InverseSourceAlpha;
            bs.AlphaSourceBlend = Blend.SourceAlpha;
            bs.ColorSourceBlend = Blend.SourceAlpha;

            SpriteBatch.Begin(SpriteSortMode.Deferred, bs, ss, 
                DepthStencilState.None, RasterizerState.CullCounterClockwise);
#endif


        }

        public static void DrawTexture(Texture2D texture, Rectangle rectangle, Color color)
        {
            SpriteBatch.Draw(texture, rectangle, color);
        }

        public static void DrawTexture(Texture2D texture, Rectangle sourceRectangle, Rectangle destinationRectangle, Color color)
        {
            SpriteBatch.Draw(texture, destinationRectangle, sourceRectangle, color);
        }


        public static void DrawTexture(Texture2D texture, Vector2 location, float rotation, Vector2 origin, Vector2 scale, Color color)
        {
#if !XNA
            SpriteBatch.Draw(texture, location, null, null, origin, rotation, scale, color, SpriteEffects.None, 0f);
#else
            SpriteBatch.Draw(texture, location, null, color, rotation, origin, scale, SpriteEffects.None, 0f);
#endif
        }

        public static void DrawString(string text, int fontIndex, float scale, Vector2 location, Color color)
        {
            if (fontIndex >= Fonts.Count)
                throw new Exception("Invalid font index: " + fontIndex);

            SpriteBatch.DrawString(Fonts[fontIndex], text, location, color, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
        }

        public static void DrawStringWithShadow(string text, int fontIndex, Vector2 location, Color color)
        {
            if (fontIndex >= Fonts.Count)
                throw new Exception("Invalid font index: " + fontIndex);

            SpriteBatch.DrawString(Fonts[fontIndex], text, new Vector2(location.X + 1f, location.Y + 1f), new Color(0, 0, 0, color.A));
            SpriteBatch.DrawString(Fonts[fontIndex], text, location, color);
        }

        public static void DrawRectangle(Rectangle rect, Color color, int thickness = 1)
        {
            SpriteBatch.Draw(whitePixelTexture, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
            SpriteBatch.Draw(whitePixelTexture, new Rectangle(rect.X, rect.Y + thickness, thickness, rect.Height - thickness), color);
            SpriteBatch.Draw(whitePixelTexture, new Rectangle(rect.X + rect.Width - thickness, rect.Y, thickness, rect.Height), color);
            SpriteBatch.Draw(whitePixelTexture, new Rectangle(rect.X, rect.Y + rect.Height - thickness, rect.Width, thickness), color);
        }

        public static void FillRectangle(Rectangle rect, Color color)
        {
            SpriteBatch.Draw(whitePixelTexture, rect, color);
        }

        public static Vector2 GetTextDimensions(string text, int fontIndex)
        {
            if (fontIndex >= Fonts.Count)
                throw new Exception("Invalid font index: " + fontIndex);

            return Fonts[fontIndex].MeasureString(text);
        }

        public static void DrawLine(Vector2 start, Vector2 end, Color color, int thickness = 1)
        {
            Vector2 line = end - start;
            SpriteBatch.Draw(whitePixelTexture,
                new Rectangle((int)start.X, (int)start.Y, (int)line.Length(), thickness),
                null, color, (float)Math.Atan2(line.Y, line.X), new Vector2(0, 0), SpriteEffects.None, 0f);
        }

        public static void EndDraw()
        {
            SpriteBatch.End();
        }
    }
}
