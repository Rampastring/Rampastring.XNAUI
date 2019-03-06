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
    public class XNATextPart
    {
        public XNATextPart() { }

        public XNATextPart(string text, int fontIndex = 0, float scale = 1.0f, Color? color = null, bool underlined = false)
        {
            _text = text;
            _fontIndex = fontIndex;
            _scale = scale;
            _color = color;
            Underlined = underlined;
            UpdateSize();
        }

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

        private float _scale = 1.0f;

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
    /// A text renderer.
    /// Takes <see cref="XNATextPart"/>s, automatically formats them, applied line
    /// breaks if necessary and renders them.
    /// </summary>
    public class XNATextRenderer : XNAControl
    {
        public XNATextRenderer(WindowManager windowManager) : base(windowManager)
        {
        }

        public int Padding { get; set; } = 3;

        private List<XNATextPart> originalTextParts = new List<XNATextPart>();

        private List<XNATextLine> renderedTextLines = new List<XNATextLine>();

        public void AddTextPart(XNATextPart text)
        {
            originalTextParts.Add(text);
        }

        protected override void OnClientRectangleUpdated()
        {
            PrepareTextParts();
            base.OnClientRectangleUpdated();
        }

        public void PrepareTextParts()
        {
            renderedTextLines.Clear();

            XNATextLine line = new XNATextLine(new List<XNATextPart>());
            renderedTextLines.Add(line);

            foreach (XNATextPart textPart in originalTextParts)
            {
                string remainingText = textPart.Text;

                while (true)
                {
                    line = renderedTextLines[renderedTextLines.Count - 1];
                    int remainingWidth = (Width - Padding * 2) - line.Width;

                    List<string> textLines = Renderer.GetFixedTextLines(remainingText, textPart.FontIndex, remainingWidth, false);
                    if (Renderer.GetTextDimensions(textLines[0], textPart.FontIndex).X < remainingWidth)
                    {
                        line.AddPart(new XNATextPart(textLines[0], textPart.FontIndex, textPart.Scale, textPart.Color, textPart.Underlined));
                        remainingText = textPart.Text.Substring(textLines[0].Length - 1);
                    }

                    remainingText = remainingText.TrimStart(' ');

                    if (textLines.Count > 1 || remainingText != "")
                        renderedTextLines.Add(new XNATextLine(new List<XNATextPart>()));
                    else
                        break;
                }
            }
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

                y += line.Height;
            }

            base.Draw(gameTime);
        }
    }
}
