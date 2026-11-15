using System.Collections.Generic;

namespace Lumel.Input
{
    /// <summary>
    ///     Turns a held key into repeated presses, the press, pause, repeat pattern every text field has.
    /// </summary>
    /// <remarks>
    ///     The game reports a key as newly pressed for exactly one tick. Typed characters need none of this:
    ///     the game's text buffer has already repeated them.
    /// </remarks>
    public sealed class KeyRepeater
    {
        readonly int _delayTicks;
        readonly int _intervalTicks;

        readonly Dictionary<int, State> _states = new Dictionary<int, State>();

        /// <summary>
        ///     Creates a repeater with a half-second delay and 20 repeats a second, at the game's 60 ticks a second.
        /// </summary>
        public KeyRepeater()
            : this(30, 3)
        {
        }

        /// <summary>
        ///     Creates a repeater that waits <paramref name="delayTicks" />, then repeats every
        ///     <paramref name="intervalTicks" />.
        /// </summary>
        public KeyRepeater(int delayTicks, int intervalTicks)
        {
            _delayTicks = delayTicks;
            _intervalTicks = intervalTicks;
        }

        /// <summary>
        ///     Returns true if <paramref name="key" /> should count as pressed this tick, either by a first press or by
        ///     a repeat.
        /// </summary>
        public bool Fires(int key, bool newPress, bool held, int now)
        {
            if (newPress)
            {
                State fresh;
                fresh.FirstPress = now;
                fresh.LastFire = now;
                _states[key] = fresh;
                return true;
            }

            if (!held)
            {
                _states.Remove(key);
                return false;
            }

            State state;
            if (!_states.TryGetValue(key, out state))
                return false;

            if (now - state.FirstPress < _delayTicks || now - state.LastFire < _intervalTicks)
                return false;

            state.LastFire = now;
            _states[key] = state;
            return true;
        }

        /// <summary>Clears every held key, so none repeats until it is pressed again.</summary>
        public void Clear()
        {
            _states.Clear();
        }

        struct State
        {
            public int FirstPress;
            public int LastFire;
        }
    }
}