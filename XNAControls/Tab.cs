namespace Rampastring.XNAUI.XNAControls;

using Microsoft.Xna.Framework.Graphics;

internal sealed class Tab
{
    public Tab()
    {
    }

    public Tab(string text, Texture2D defaultTexture, Texture2D pressedTexture, bool selectable)
    {
        Text = text;
        DefaultTexture = defaultTexture;
        PressedTexture = pressedTexture;
        Selectable = selectable;
    }

    public Texture2D DefaultTexture { get; set; }

    public Texture2D PressedTexture { get; set; }

    public string Text { get; set; }

    public bool Selectable { get; set; }

    public int TextXPosition { get; set; }

    public int TextYPosition { get; set; }
}