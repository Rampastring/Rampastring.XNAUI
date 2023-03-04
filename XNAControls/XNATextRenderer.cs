namespace Rampastring.XNAUI.XNAControls;

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Rampastring.Tools;

/// <summary>
/// A text renderer, practically an enhanced label control.
/// Takes <see cref="XNATextPart"/>s, automatically formats them, applies line
/// breaks if necessary and renders them.
/// </summary>
public class XNATextRenderer : XNAControl
{
    public XNATextRenderer(WindowManager windowManager)
        : base(windowManager)
    {
    }

    public int Padding { get; set; } = 3;

    public int SpaceBetweenLines { get; set; }

    private readonly List<XNATextPart> originalTextParts = new();

    private readonly List<XNATextLine> renderedTextLines = new();

    public void AddTextPart(XNATextPart text) => originalTextParts.Add(text);

    public void AddTextLine(XNATextPart text)
    {
        originalTextParts.Add(new(
            Environment.NewLine + text.Text,
            text.FontIndex,
            text.Scale,
            text.Color,
            text.Underlined));
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

        var line = new XNATextLine(new());
        renderedTextLines.Add(line);

        int remainingWidth = Width - (Padding * 2);

        foreach (XNATextPart textPart in originalTextParts)
        {
            string remainingText = textPart.Text;
            var currentOutputPart = new XNATextPart(string.Empty, textPart.FontIndex, textPart.Scale, textPart.Color, textPart.Underlined);

            while (true)
            {
                if (remainingText.StartsWith(Environment.NewLine, StringComparison.InvariantCulture))
                {
                    string newLineText = string.Empty;
                    if (remainingText.SafeSubstring(Environment.NewLine.Length).StartsWith(Environment.NewLine, StringComparison.InvariantCulture))
                        newLineText = " ";
                    line = new(new() { new(newLineText, textPart.FontIndex, textPart.Scale, textPart.Color, textPart.Underlined) });
                    renderedTextLines.Add(line);
                    remainingText = remainingText.SafeSubstring(Environment.NewLine.Length);
                    remainingWidth = Width - (Padding * 2);
                    currentOutputPart = new(string.Empty, textPart.FontIndex, textPart.Scale, textPart.Color, textPart.Underlined);
                    continue;
                }

                string[] words = remainingText.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
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
                        currentOutputPart = new(wordWithSpace, textPart.FontIndex, textPart.Scale, textPart.Color, textPart.Underlined);
                        line = new(new());
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
        => PrepareTextParts();

    public override void Draw(GameTime gameTime)
    {
        int y = 0;

        foreach (XNATextLine line in renderedTextLines)
        {
            int x = 0;

            foreach (XNATextPart part in line.Parts)
            {
                DrawStringWithShadow(part.Text, part.FontIndex, new(x, y), part.Color);
                x += part.Width + 1;
            }

            y += line.Height + SpaceBetweenLines;
        }

        base.Draw(gameTime);
    }
}