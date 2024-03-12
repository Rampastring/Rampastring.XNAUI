using Microsoft.Xna.Framework;

namespace Rampastring.XNAUI.Input.IME.Sdl
{
    /// <summary>
    /// Integrate IME to DesktopGL(SDL2) platform.
    /// </summary>
    internal sealed class SdlIMEHandler(Game game) : IMEHandler
    {
        public override bool Enabled { get => false; protected set => _ = value; }

        public override void StartTextComposition()
        {
        }

        public override void StopTextComposition()
        {
        }
    }
}