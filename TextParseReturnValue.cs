using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;
using System.Text;

namespace Rampastring.XNAUI
{
    public class TextParseReturnValue
    {
        public int lineAmount = 1;
        public string text;

        public static TextParseReturnValue FixText(SpriteFont spriteFont, int width, string text)
        {
            string line = String.Empty;
            TextParseReturnValue returnValue = new TextParseReturnValue();
            returnValue.text = String.Empty;
            string[] wordArray = text.Split(' ');

            foreach (string word in wordArray)
            {
                if (spriteFont.MeasureString(line + word).Length() > width)
                {
                    returnValue.text = returnValue.text + line + '\n';
                    returnValue.lineAmount = returnValue.lineAmount + 1;
                    line = String.Empty;
                }

                line = line + word + " ";
            }

            returnValue.text = returnValue.text + line;
            return returnValue;
        }

        public static List<string> GetFixedTextLines(SpriteFont spriteFont, int width, string text)
        {
            if (string.IsNullOrEmpty(text))
                return new List<string>();

            string line = string.Empty;
            List<string> returnValue = new List<string>();
            string[] wordArray = text.Split(' ');

            foreach (string word in wordArray)
            {
                if (spriteFont.MeasureString(line + word).X > width)
                {
                    if (line.Length > 0)
                    {
                        returnValue.Add(line.Remove(line.Length - 1));
                    }

                    // Split individual words that are longer than the allowed width
                    if (spriteFont.MeasureString(word).X > width)
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

            return returnValue;
        }
    }
}
