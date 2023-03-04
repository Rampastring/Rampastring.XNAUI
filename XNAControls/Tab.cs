namespace Rampastring.XNAUI.XNAControls;

using Microsoft.Xna.Framework.Graphics;

internal sealed class Tab
{
    public Tab(string text, Texture2D defaultTexture, Texture2D pressedTexture, bool selectable)
    {
        Text = text;
        DefaultTexture = defaultTexture;
        PressedTexture = pressedTexture;
        Selectable = selectable;
    }

    public Texture2D DefaultTexture { get; }

    public Texture2D PressedTexture { get; }

    public string Text { get; }

    public bool Selectable { get; set; }

    public int TextXPosition { get; set; }

    public int TextYPosition { get; set; }
}