using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace Rampastring.XNAUI.XNAControls
{
    /// <summary>
    /// A control that acts as a timer.
    /// </summary>
    public class XNATimerControl : XNAControl
    {
        /// <summary>
        /// Raised after the time specified in Interval has passed.
        /// </summary>
        public event EventHandler TimeElapsed;

        /// <summary>
        /// Creates a new timer.
        /// </summary>
        /// <param name="windowManager">The WindowManager associated with this control.</param>
        public XNATimerControl(WindowManager windowManager) : base(windowManager)
        {
            Visible = false;
        }

        private TimeSpan _interval;

        /// <summary>
        /// The interval after which the TimeElapsed event is raised.
        /// </summary>
        public TimeSpan Interval
        {
            get { return _interval; }
            set { _interval = value; }
        }

        /// <summary>
        /// If set, the timer will automatically restart after calling
        /// the TimeElapsed event.
        /// </summary>
        public bool AutoReset { get; set; }

        private TimeSpan currentInterval;
        private bool timerEnabled;

        /// <summary>
        /// Updates the timer.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            if (!timerEnabled)
                return;

            currentInterval -= gameTime.ElapsedGameTime;

            if (currentInterval < TimeSpan.Zero)
            {
                TimeElapsed?.Invoke(this, EventArgs.Empty);

                if (AutoReset)
                    currentInterval = Interval;
                else
                    timerEnabled = false;
            }
        }

        /// <summary>
        /// Pauses the timer.
        /// </summary>
        public void Pause()
        {
            timerEnabled = false;
        }

        /// <summary>
        /// Resumes a previously paused (with Pause()) timer.
        /// </summary>
        public void Resume()
        {
            timerEnabled = true;
        }

        /// <summary>
        /// Sets the timer to trigger after the time specified in the timer's Interval 
        /// and starts the timer.
        /// </summary>
        public void Start()
        {
            currentInterval = Interval;
            timerEnabled = true;
        }

        /// <summary>
        /// Lowers the time until the next event by the specified amount of time.
        /// </summary>
        /// <param name="timeSpan">The time.</param>
        public void Accelerate(TimeSpan timeSpan)
        {
            currentInterval -= timeSpan;
        }

        /// <summary>
        /// Sets the time until the next TimeElapsed event.
        /// </summary>
        /// <param name="timeSpan">The time.</param>
        public void SetTime(TimeSpan timeSpan)
        {
            currentInterval = timeSpan;
        }

        /// <summary>
        /// Returns the current time until the next TimeElapsed event.
        /// </summary>
        /// <returns>The time until the next TimeElapsed event.</returns>
        public TimeSpan GetTime()
        {
            return currentInterval;
        }

        /// <summary>
        /// A dummy draw function. Doesn't actually do anything.
        /// </summary>
        public override void Draw(GameTime gameTime)
        {
            // Do nothing
        }
    }
}
