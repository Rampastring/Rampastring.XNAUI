using ImeSharp;

using Microsoft.Xna.Framework;

namespace Rampastring.XNAUI.Input.IME.WinForms
{
    /// <summary>
    /// Integrate IME to XNA framework.
    /// </summary>
    internal class WinFormsIMEHandler : IMEHandler
    {
        public WinFormsIMEHandler(Game game)
        {
            InputMethod.Initialize(game.Window.Handle);
            InputMethod.TextInputCallback = OnTextInput;
            InputMethod.TextCompositionCallback = (compositionText, cursorPosition, _, _, _, _) =>
            {
                Composition = compositionText.ToString();
                CompositionCursorPos = cursorPosition;
            };
        }

        public override bool Enabled
        {
            get => InputMethod.Enabled;
            protected set => InputMethod.Enabled = value;
        }

        public override void StartTextComposition()
        {
            Enabled = true;
        }

        public override void StopTextComposition()
        {
            Enabled = false;
        }

        public override void SetTextInputRect(in Rectangle rect)
        {
            InputMethod.SetTextInputRect(rect.X, rect.Y, rect.Width, rect.Height);
        }
    }
}