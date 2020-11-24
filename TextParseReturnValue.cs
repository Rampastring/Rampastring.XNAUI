using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;
using System.Text;

namespace Rampastring.XNAUI
{
    public class TextParseReturnValue
    {
        public int LineCount = 1;
        public string Text;

        public static TextParseReturnValue FixText(SpriteFont spriteFont, int width, string text)
        {
            string line = String.Empty;
            TextParseReturnValue returnValue = new TextParseReturnValue();
            returnValue.Text = String.Empty;
            string[] wordArray = text.Split(' ');

            foreach (string word in wordArray)
            {
                if (spriteFont.MeasureString(line + word).Length() > width)
                {
                    returnValue.Text = returnValue.Text + line + Environment.NewLine;
                    returnValue.LineCount = returnValue.LineCount + 1;
                    line = String.Empty;
                }

                line = line + word + " ";
            }

            returnValue.Text = returnValue.Text + line;
            return returnValue;
        }

        public static List<string> GetFixedTextLines(SpriteFont spriteFont, int width, string text, bool splitWords = true)
        {
            if (string.IsNullOrEmpty(text))
                return new List<string>(0);

            List<string> returnValue = new List<string>();
            string[] lineArray = text.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string originalTextLine in lineArray)
            {
                string line = string.Empty;

                string[] wordArray = originalTextLine.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                foreach (string word in wordArray)
                {
                    if (spriteFont.MeasureString(line + word).X > width)
                    {
                        if (line.Length > 0)
                        {
                            returnValue.Add(line.Remove(line.Length - 1));
                        }

                        // Split individual words that are longer than the allowed width
                        if (splitWords && spriteFont.MeasureString(word).X > width)
                        {
                            StringBuilder sb = new StringBuilder();

                            for (int i = 0; i < word.Length; i++)
                            {
                                if (spriteFont.MeasureString(sb.ToString() + word[i]).X > width)
                                {
                                    returnValue.Add(sb.ToString());
                                    sb.Clear();
                                }

                                sb.Append(word[i]);
                            }

                            if (sb.Length > 0)
                                line = sb.ToString() + " ";

                            continue;
                        }

                        line = word + " ";
                        continue;
                    }

                    line = line + word + " ";
                }

                if (!string.IsNullOrEmpty(line) && line.Length > 1)
                    returnValue.Add(line);
            }

            return returnValue;
        }
    }
}
