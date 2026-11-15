using System;
using Lumel.Drawing;

namespace Lumel.Motion
{
    /// <summary>
    ///     A change drawn over several ticks that tracks how far it has run, eased, from 0 at the start to 1 at the
    ///     end.
    /// </summary>
    /// <remarks>
    ///     Started at any time, it runs from the next frame drawn, which is when it first reads the clock; while it runs, it
    ///     asks the host for every frame. One never started is at rest at 1.
    /// </remarks>
    public sealed class Transition
    {
        readonly Func<float, float> _ease;

        int _delay;
        int _began;
        bool _waiting;
        bool _running;

        /// <summary>Creates a transition lasting <paramref name="durationTicks" /> ticks, run evenly.</summary>
        public Transition(int durationTicks) : this(durationTicks, null)
        {
        }

        /// <summary>
        ///     Creates a transition lasting <paramref name="durationTicks" /> ticks, eased by <paramref name="ease" />,
        ///     or run evenly if it is null.
        /// </summary>
        public Transition(int durationTicks, Func<float, float> ease)
        {
            DurationTicks = durationTicks;
            _ease = ease;
        }

        /// <summary>Gets how many ticks the transition takes to run once it has begun.</summary>
        public int DurationTicks { get; }

        /// <summary>
        ///     Whether the transition has started and not yet reached its end, as of the last frame it was drawn in.
        /// </summary>
        public bool IsRunning => _running;

        /// <summary>Restarts the transition from 0 at the next frame drawn.</summary>
        public void Start()
        {
            Start(0);
        }

        /// <summary>
        ///     Restarts the transition from 0 <paramref name="delay" /> ticks after the next frame drawn, staying at 0
        ///     until then.
        /// </summary>
        public void Start(int delay)
        {
            _delay = delay;
            _waiting = true;
            _running = true;
        }

        /// <summary>Moves the transition to its end at once.</summary>
        public void Finish()
        {
            _waiting = false;
            _running = false;
        }

        /// <summary>
        ///     Returns how far the transition has run at the frame <paramref name="g" /> is drawing, eased, and asks
        ///     <paramref name="g" /> for the next frame while it runs.
        /// </summary>
        public float Progress(IGraphics g)
        {
            if (!_running)
                return 1f;

            var now = g.Now;
            if (_waiting)
            {
                _began = now;
                _waiting = false;
            }

            // A clock that has gone back, as a new session's does, ends the transition.
            var done = DurationTicks > 0 ? (now - _began - _delay) / (float)DurationTicks : 1f;
            if (done >= 1f || now < _began)
            {
                _running = false;
                return 1f;
            }

            g.RequestFrame();
            if (done <= 0f)
                return 0f;

            return _ease == null ? done : _ease(done);
        }
    }
}
