namespace Rampastring.XNAUI;

using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;

internal sealed class PrioritizedSoundInstance : IDisposable
{
    public PrioritizedSoundInstance(
        SoundEffectInstance soundInstance,
        double priority,
        double priorityDecayRate)
    {
        SoundInstance = soundInstance;
        Priority = priority;
        PriorityDecayRate = priorityDecayRate;
    }

    public SoundEffectInstance SoundInstance { get; }

    public double Priority { get; private set; }

    public double PriorityDecayRate { get; }

    /// <summary>
    /// Updates the priority of the sound. Returns true if the sound effect is
    /// still playing, otherwise false.
    /// </summary>
    /// <param name="gameTime">Tells how much time has passed since the previous frame.</param>
    /// <returns>True if the sound effect is still playing, otherwise false.</returns>
    public bool Update(GameTime gameTime)
    {
        Priority -= PriorityDecayRate * gameTime.ElapsedGameTime.TotalSeconds;

        return SoundInstance.State == SoundState.Playing;
    }

    public void Dispose() => SoundInstance.Dispose();
}