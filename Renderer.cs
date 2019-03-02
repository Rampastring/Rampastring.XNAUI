using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using System.IO;
using System.Text;

namespace Rampastring.XNAUI
{
    public struct SpriteBatchSettings
    {
        public SpriteBatchSettings(SpriteSortMode ssm, BlendState bs, SamplerState ss)
        {
            SpriteSortMode = ssm;
            BlendState = bs;
            SamplerState = ss;
        }

        public SpriteSortMode SpriteSortMode { get; }
        public SamplerState SamplerState { get; }
        public BlendState BlendState { get; }
    }

    /// <summary>
    /// Provides static methods for drawing.
    /// </summary>
    public static class Renderer
    {
        private static SpriteBatch spriteBatch;

        private static List<SpriteFont> fonts;

        private static Texture2D whitePixelTexture;

        private static readonly LinkedList<SpriteBatchSettings> settingStack = new LinkedList<SpriteBatchSettings>();

        internal static SpriteBatchSettings CurrentSettings;

        public static void Initialize(GraphicsDevice gd, ContentManager content, string contentPath)
        {
            spriteBatch = new SpriteBatch(gd);
            fonts = new List<SpriteFont>();

            content.RootDirectory = contentPath;

            int i = 0;
            while (true)
            {
                string sfName = string.Format("SpriteFont{0}", i);

                if (File.Exists(contentPath + sfName + ".xnb"))
                {
                    fonts.Add(content.Load<SpriteFont>(sfName));
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
            SpriteFont sf = fonts[fontIndex];

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
            var spriteFont = fonts[fontIndex];

            while (spriteFont.MeasureString(sb.ToString()).X > maxWidth)
            {
                sb.Remove(sb.Length - 1, 1);
            }

            return sb.ToString();
        }

        public static TextParseReturnValue FixText(string text, int fontIndex, int width)
        {
            return TextParseReturnValue.FixText(fonts[fontIndex], width, text);
        }

        public static List<string> GetFixedTextLines(string text, int fontIndex, int width)
        {
            return TextParseReturnValue.GetFixedTextLines(fonts[fontIndex], width, text);
        }

        /// <summary>
        /// Pushes new settings into the renderer's internal stack and applies them.
        /// A call to <see cref="PushSettings(SpriteBatchSettings)"/> should always
        /// be followed by <see cref="PopSettings"/> once drawing with the new settings is done.
        /// </summary>
        /// <param name="settings">The sprite batch settings.</param>
        public static void PushSettings(SpriteBatchSettings settings)
        {
            EndDraw();
            PushSettingsInternal();
            CurrentSettings = settings;
            BeginDrawInternal(CurrentSettings);
        }

        /// <summary>
        /// Pops previous settings from the renderer's internal stack and applies them.
        /// </summary>
        public static void PopSettings()
        {
            EndDraw();
            PopSettingsInternal();
            BeginDrawInternal(CurrentSettings);
        }

        /// <summary>
        /// Changes current rendering settings. This can be called between 
        /// <see cref="PushSettings(SpriteBatchSettings)"/> and <see cref="PopSettings"/>
        /// when you want to draw something with new settings, but there's no reason 
        /// to save those settings.
        /// </summary>
        /// <param name="settings">The sprite batch settings.</param>
        public static void ChangeSettings(SpriteBatchSettings settings)
        {
            EndDraw();
            CurrentSettings = settings;
            BeginDrawInternal(CurrentSettings);
        }

        /// <summary>
        /// Prepares the renderer for drawing a batch of sprites.
        /// </summary>
        public static void BeginDraw()
        {
            BeginDrawInternal(CurrentSettings);
        }

        /// <summary>
        /// Draws the currently queued batch of sprites.
        /// </summary>
        public static void EndDraw()
        {
            spriteBatch.End();
        }

        //BlendState blendState = new BlendState();
        //blendState.AlphaDestinationBlend = Blend.One;
        //blendState.ColorDestinationBlend = Blend.InverseSourceAlpha;
        //blendState.AlphaSourceBlend = Blend.SourceAlpha;
        //blendState.ColorSourceBlend = Blend.SourceAlpha;

        internal static void BeginDrawInternal(SpriteBatchSettings settings) =>
            BeginDrawInternal(settings.SpriteSortMode, settings.BlendState, settings.SamplerState);

        internal static void BeginDrawInternal(SpriteSortMode ssm, BlendState bs, SamplerState ss)
        {
#if XNA
            SpriteBatch.Begin(ssm, bs, ss, DepthStencilState.Default, RasterizerState.CullNone);
#else
            spriteBatch.Begin(ssm, bs, ss,
                DepthStencilState.None, RasterizerState.CullCounterClockwise);
#endif
        }

        internal static void PushSettingsInternal()
        {
            settingStack.AddFirst(CurrentSettings);
        }

        internal static void PopSettingsInternal()
        {
            CurrentSettings = settingStack.First.Value;
            settingStack.RemoveFirst();
        }

        internal static void ClearStack()
        {
            settingStack.Clear();
        }

        #region Rendering code

        public static void DrawTexture(Texture2D texture, Rectangle rectangle, Color color)
        {
            spriteBatch.Draw(texture, rectangle, color);
        }

        public static void DrawTexture(Texture2D texture, Rectangle sourceRectangle, Rectangle destinationRectangle, Color color)
        {
            spriteBatch.Draw(texture, destinationRectangle, sourceRectangle, color);
        }


        public static void DrawTexture(Texture2D texture, Vector2 location, float rotation, Vector2 origin, Vector2 scale, Color color)
        {
#if !XNA
            spriteBatch.Draw(texture, location, null, null, origin, rotation, scale, color, SpriteEffects.None, 0f);
#else
            SpriteBatch.Draw(texture, location, null, color, rotation, origin, scale, SpriteEffects.None, 0f);
#endif
        }

        public static void DrawString(string text, int fontIndex, Vector2 location, Color color, float scale = 1.0f)
        {
            if (fontIndex >= fonts.Count)
                throw new Exception("Invalid font index: " + fontIndex);

            spriteBatch.DrawString(fonts[fontIndex], text, location, color, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
        }

        public static void DrawStringWithShadow(string text, int fontIndex, Vector2 location, Color color, float scale = 1.0f)
        {
            if (fontIndex >= fonts.Count)
                throw new Exception("Invalid font index: " + fontIndex);

#if XNA
            SpriteBatch.DrawString(Fonts[fontIndex], text, new Vector2(location.X + 1f, location.Y + 1f), new Color(0, 0, 0, color.A));
#else
            spriteBatch.DrawString(fonts[fontIndex], text,
                new Vector2(location.X + 1f, location.Y + 1f), new Color((byte)0, (byte)0, (byte)0, color.A),
                0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
#endif
            spriteBatch.DrawString(fonts[fontIndex], text, location, color, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
        }

        public static void DrawRectangle(Rectangle rect, Color color, int thickness = 1)
        {
            spriteBatch.Draw(whitePixelTexture, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
            spriteBatch.Draw(whitePixelTexture, new Rectangle(rect.X, rect.Y + thickness, thickness, rect.Height - thickness), color);
            spriteBatch.Draw(whitePixelTexture, new Rectangle(rect.X + rect.Width - thickness, rect.Y, thickness, rect.Height), color);
            spriteBatch.Draw(whitePixelTexture, new Rectangle(rect.X, rect.Y + rect.Height - thickness, rect.Width, thickness), color);
        }

        public static void FillRectangle(Rectangle rect, Color color)
        {
            spriteBatch.Draw(whitePixelTexture, rect, color);
        }

        public static Vector2 GetTextDimensions(string text, int fontIndex)
        {
            if (fontIndex >= fonts.Count)
                throw new Exception("Invalid font index: " + fontIndex);

            return fonts[fontIndex].MeasureString(text);
        }

        public static void DrawLine(Vector2 start, Vector2 end, Color color, int thickness = 1)
        {
            Vector2 line = end - start;
            if (thickness > 1)
            {
                Vector2 offset = RMath.VectorFromLengthAndAngle(thickness / 2, RMath.AngleFromVector(line) - (float)Math.PI / 2.0f);
                end += offset;
                start += offset;
            }
            spriteBatch.Draw(whitePixelTexture,
                new Rectangle((int)start.X, (int)start.Y, (int)line.Length(), thickness),
                null, color, (float)Math.Atan2(line.Y, line.X), new Vector2(0, 0), SpriteEffects.None, 0f);
        }

        #endregion
    }
}
