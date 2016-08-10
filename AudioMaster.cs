using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Media;
using Rampastring.Tools;
using System;

namespace Rampastring.XNAUI
{
    public static class AudioMaster
    {
        public static bool DisableSounds = false;

        private static float volume { get; set; }

        public static void SetVolume(float volume)
        {
            if (DisableSounds)
                return;

            AudioMaster.volume = volume;
            try
            {
                MediaPlayer.Volume = volume;
            }
            catch (Exception ex)
            {
                Logger.Log("AudioMaster exception: " + ex.Message);
            }
        }

        public static float GetVolume()
        {
            return volume;
        }

        public static void PlaySound(SoundEffectInstance seInstance)
        {
            if (DisableSounds)
                return;

#if !LINUX
            _PlaySound(seInstance);
#endif
        }

        private static void _PlaySound(SoundEffectInstance seInstance)
        {
            seInstance.Volume = volume;

            seInstance.Play();
        }
    }
}
