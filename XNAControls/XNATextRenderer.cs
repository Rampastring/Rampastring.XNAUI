using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rampastring.XNAUI.XNAControls
{
    /// <summary>
    /// A part of a potentially long line of text.
    /// </summary>
    public struct XNATextPart
    {
        public XNATextPart(string text, int fontIndex, float scale, Color? color, bool underlined)
        {
            _text = text;
            _fontIndex = fontIndex;
            _scale = scale;
            _color = color;
            Underlined = underlined;
            Size = Point.Zero;
            UpdateSize();
        }

        public XNATextPart(string text) : this(text, 0, 1.0f, null, false) { }

        public XNATextPart(string text, int fontIndex, Color? color) : this(text, fontIndex, 1.0f, color, false) { }

        private string _text;

        public string Text
        {
            get => _text;
            set { _text = value; UpdateSize(); }
        }

        private int _fontIndex;

        public int FontIndex
        {
            get => _fontIndex;
            set { _fontIndex = value; UpdateSize(); }
        }

        private float _scale;

        public float Scale
        {
            get => _scale;
            set { _scale = value; UpdateSize(); }
        }

        private Color? _color;

        public Color Color
        {
            get => _color ?? UISettings.ActiveSettings.TextColor;
            set => _color = value;
        }

        public bool Underlined { get; set; }

        public Point Size { get; private set; }

        private void UpdateSize()
        {
            Vector2 size = Renderer.GetTextDimensions(_text, FontIndex) * Scale;
            Size = new Point((int)size.X, (int)size.Y);
        }

        public int Width => (int)(Size.X * Scale);

        public int Height => (int)(Size.Y * Scale);

        public void Draw(Point point)
        {
            Renderer.DrawStringWithShadow(Text, FontIndex, new Vector2(point.X, point.Y), Color, Scale);
            if (Underlined)
            {
                Renderer.DrawRectangle(new Rectangle(point.X, point.Y, (int)(Size.X * Scale), 1), Color, 1);
            }
        }
    }

    /// <summary>
    /// A text line.
    /// </summary>
    internal struct XNATextLine
    {
        public XNATextLine(List<XNATextPart> parts)
        {
            Parts = parts;
        }

        public List<XNATextPart> Parts { get; private set; }

        public int Width
        {
            get
            {
                int width = 0;
                Parts.ForEach(p => width += p.Width);
                return width;
            }
        }

        public int Height
        {
            get
            {
                int height = 0;
                Parts.ForEach(p => height = p.Height > height ? p.Height : height);
                return height;
            }
        }

        public void AddPart(XNATextPart part)
        {
            Parts.Add(part);
        }
    }

    /// <summary>
    /// A text renderer, practically an enhanced label control.
    /// Takes <see cref="XNATextPart"/>s, automatically formats them, applies line
    /// breaks if necessary and renders them.
    /// </summary>
    public class XNATextRenderer : XNAControl
    {
        public XNATextRenderer(WindowManager windowManager) : base(windowManager)
        {
        }

        public int Padding { get; set; } = 3;
        public int SpaceBetweenLines { get; set; } = 0;

        private List<XNATextPart> originalTextParts = new List<XNATextPart>();

        private List<XNATextLine> renderedTextLines = new List<XNATextLine>();

        public void AddTextPart(XNATextPart text)
        {
            originalTextParts.Add(text);
        }

        public void AddTextLine(XNATextPart text)
        {
            originalTextParts.Add(new XNATextPart(Environment.NewLine + text.Text,
                text.FontIndex, text.Scale, text.Color, text.Underlined));
        }

        public void ClearTextParts()
        {
            originalTextParts.Clear();
            renderedTextLines.Clear();
        }

        public int GetTextPartCount() => originalTextParts.Count;

        public XNATextPart GetTextPart(int index) => originalTextParts[index];

        public void PrepareTextParts()
        {
            renderedTextLines.Clear();

            XNATextLine line = new XNATextLine(new List<XNATextPart>());
            renderedTextLines.Add(line);

            int remainingWidth = Width - (Padding * 2);

            foreach (XNATextPart textPart in originalTextParts)
            {
                string remainingText = textPart.Text;
                XNATextPart currentOutputPart = new XNATextPart("", textPart.FontIndex, textPart.Scale, textPart.Color, textPart.Underlined);

                while (true)
                {
                    if (remainingText.StartsWith(Environment.NewLine))
                    {
                        string newLineText = "";
                        if (remainingText.Substring(Environment.NewLine.Length).StartsWith(Environment.NewLine))
                            newLineText = " ";
                        line = new XNATextLine(new List<XNATextPart>() { new XNATextPart(newLineText, textPart.FontIndex, textPart.Scale, textPart.Color, textPart.Underlined) });
                        renderedTextLines.Add(line);
                        remainingText = remainingText.Substring(Environment.NewLine.Length);
                        remainingWidth = Width - (Padding * 2);
                        currentOutputPart = new XNATextPart("", textPart.FontIndex, textPart.Scale, textPart.Color, textPart.Underlined);
                        continue;
                    }

                    var words = remainingText.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (string word in words)
                    {
                        string wordWithSpace = word + " ";
                        int wordWidth = (int)Renderer.GetTextDimensions(word, textPart.FontIndex).X;
                        int wordWidthWithSpace = (int)Renderer.GetTextDimensions(wordWithSpace, textPart.FontIndex).X;
                        if (wordWidth < remainingWidth)
                        {
                            remainingWidth -= wordWidthWithSpace;
                            currentOutputPart.Text += wordWithSpace;
                        }
                        else
                        {
                            line.Parts.Add(currentOutputPart);

                            remainingWidth = Width - (Padding * 2) - wordWidthWithSpace;
                            currentOutputPart = new XNATextPart(wordWithSpace, textPart.FontIndex, textPart.Scale, textPart.Color, textPart.Underlined);
                            line = new XNATextLine(new List<XNATextPart>());
                            renderedTextLines.Add(line);
                        }
                    }

                    line.Parts.Add(currentOutputPart);
                    break;
                }

            }

            ClientRectangleUpdated -= XNATextRenderer_ClientRectangleUpdated;
            Height = renderedTextLines.Sum(l => l.Height);
            if (renderedTextLines.Count > 1)
                Height += (renderedTextLines.Count - 1) * SpaceBetweenLines;

            ClientRectangleUpdated += XNATextRenderer_ClientRectangleUpdated;
        }

        private void XNATextRenderer_ClientRectangleUpdated(object sender, EventArgs e)
        {
            PrepareTextParts();
        }

        public override void Draw(GameTime gameTime)
        {
            int y = 0;

            foreach (XNATextLine line in renderedTextLines)
            {
                int x = 0;

                foreach (XNATextPart part in line.Parts)
                {
                    DrawStringWithShadow(part.Text, part.FontIndex, new Vector2(x, y), part.Color, 1f);
                    x += part.Width + 1;
                }

                y += line.Height + SpaceBetweenLines;
            }

            base.Draw(gameTime);
        }
    }
}
