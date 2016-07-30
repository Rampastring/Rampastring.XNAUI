using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Media;
using Rampastring.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Rampastring.XNAUI
{
    public static class AudioMaster
    {
        private static float volume { get; set; }

        public static void SetVolume(float volume)
        {
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
            seInstance.Volume = volume;
            seInstance.Play();
        }
    }
}
