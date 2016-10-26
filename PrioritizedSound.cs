using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Rampastring.XNAUI
{
    /// <summary>
    /// A toggleable sound effect that can also have a defined priority
    /// and a decay rate for the priority.
    /// </summary>
    public class PrioritizedSound
    {
        public PrioritizedSound(string assetName)
        {
            soundEffect = AssetLoader.LoadSound(assetName);
        }

        private bool _enabled = true;

        private SoundEffect soundEffect;

        public bool Enabled
        {
            get { return _enabled; }
            set { _enabled = value; }
        }

        public double Priority { get; set; }

        /// <summary>
        /// The decay rate of this sound's priority.
        /// The sound's priority will be reduced by the specified number each second.
        /// </summary>
        public double PriorityDecayRate { get; set; }

        public void Play()
        {
            if (!Enabled || soundEffect == null)
                return;

            SoundPlayer.Play(this);
        }

        public SoundEffectInstance CreateSoundInstance()
        {
            if (soundEffect == null)
                return null;

            return soundEffect.CreateInstance();
        }
    }

    public class PrioritizedSoundInstance : IDisposable
    {
        public PrioritizedSoundInstance(SoundEffectInstance soundInstance, 
            double priority, double priorityDecayRate)
        {
            SoundInstance = soundInstance;
            Priority = priority;
            PriorityDecayRate = priorityDecayRate;
        }

        public SoundEffectInstance SoundInstance { get; private set; }

        public double Priority { get; private set; }

        public double PriorityDecayRate { get; private set; }

        /// <summary>
        /// Updates the priority of the sound. Returns true if the sound effect has
        /// stopped playing, otherwise false.
        /// </summary>
        /// <param name="gameTime">Tells how much time has passed since the previous frame.</param>
        /// <returns>True if the sound effect has stopped playing, otherwise false.</returns>
        public bool Update(GameTime gameTime)
        {
            Priority = Priority - PriorityDecayRate * gameTime.ElapsedGameTime.TotalSeconds;

            if (SoundInstance.State != SoundState.Playing)
                return false;

            return true;
        }

        public void Dispose()
        {
            SoundInstance.Dispose();
        }
    }
}
