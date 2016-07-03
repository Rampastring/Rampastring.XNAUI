using Microsoft.Xna.Framework.Audio;

namespace Rampastring.XNAUI
{
    /// <summary>
    /// A wrapper around SoundEffectInstance that allows toggling
    /// the sound on and off.
    /// </summary>
    public class ToggleableSound
    {
        public ToggleableSound(SoundEffectInstance seInstance)
        {
            this.seInstance = seInstance;
            Enabled = true;
        }

        SoundEffectInstance seInstance;

        public bool Enabled { get; set; }

        /// <summary>
        /// Plays the sound if it is enabled. Otherwise does nothing.
        /// </summary>
        public void Play()
        {
            if (Enabled && seInstance != null)
                seInstance.Play();
        }
    }
}
