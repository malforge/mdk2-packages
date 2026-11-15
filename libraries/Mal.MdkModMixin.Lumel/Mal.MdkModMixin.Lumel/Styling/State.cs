using System;
using System.Collections.Generic;
using System.Threading;

namespace Lumel.Styling
{
    /// <summary>A state an element can be in, such as hovered or disabled, or several combined with |.</summary>
    /// <remarks>
    ///     Any element can carry any declared state; a control's states are only the ones it comes with. Where looks for
    ///     two states set the same thing, the state of greater weight wins, and a joining of states beats the states it is
    ///     made of.
    /// </remarks>
    public struct State : IEquatable<State>
    {
        // At most 64 states, one bit each, so a set of them is one ulong and a change of state costs no allocation.
        const int Most = 64;

        // Declared states are kept for the whole of a mod's life, as their bits are what every look is filed under.
        static readonly string[] Names = new string[Most];
        static readonly int[] Weights = new int[Most];
        static int _declared;

        /// <summary>The pointer is resting on the element, and a press would reach it.</summary>
        /// <remarks>An element that cannot be used is never in it.</remarks>
        public static readonly State PointerOver = Declare("PointerOver", 100);

        /// <summary>The element holds the keyboard.</summary>
        public static readonly State Focused = Declare("Focused", 200);

        /// <summary>
        ///     The element holds the keyboard and shows it: the keys or the gamepad gave it the keyboard or have been used
        ///     since, or it is focusable from a press, as a box that takes typing is.
        /// </summary>
        /// <remarks>
        ///     A control pressed with the pointer holds the keyboard without showing it, as browsers keep <c>:focus-visible</c>
        ///     apart from <c>:focus</c>.
        /// </remarks>
        public static readonly State FocusVisible = Declare("FocusVisible", 250);

        /// <summary>The element is disabled.</summary>
        public static readonly State Disabled = Declare("Disabled", 1000);

        readonly ulong _bits;

        State(ulong bits)
        {
            _bits = bits;
        }

        /// <summary>Gets no state at all, the state an element's base look is filed under.</summary>
        public static State None => default(State);

        /// <summary>Gets the states this holds, one bit each.</summary>
        public ulong Bits => _bits;

        /// <summary>Whether this holds no state at all.</summary>
        public bool IsNone => _bits == 0UL;

        /// <summary>
        ///     Gets how much a look for this state weighs against looks for other states: the weight of its heaviest
        ///     state.
        /// </summary>
        public int Weight
        {
            get
            {
                var weight = 0;
                for (var i = 0; i < Most; i++)
                {
                    if ((_bits & (1UL << i)) != 0UL && Weights[i] > weight)
                        weight = Weights[i];
                }

                return weight;
            }
        }

        /// <summary>Gets how many states this holds.</summary>
        public int Count
        {
            get
            {
                var count = 0;
                for (var bits = _bits; bits != 0UL; bits &= bits - 1UL)
                    count++;

                return count;
            }
        }

        /// <summary>
        ///     Declares a new state named <paramref name="name" />, whose looks weigh <paramref name="weight" />
        ///     against other states'.
        /// </summary>
        /// <remarks>
        ///     Declared once, into a static field. Throws past the 64th state a mod declares. Safe to call from several
        ///     threads at once.
        /// </remarks>
        public static State Declare(string name, int weight)
        {
            // Taken atomically: the game sets up blocks on several threads at once, and a control's states are declared
            // the first time its type is used.
            var at = Interlocked.Increment(ref _declared) - 1;
            if (at >= Most)
                throw new InvalidOperationException("No more than " + Most + " states can be declared; " + name + " is one too many.");

            Names[at] = name;
            Weights[at] = weight;
            return new State(1UL << at);
        }

        /// <summary>Returns true if this holds every state that <paramref name="other" /> holds.</summary>
        public bool Holds(State other)
        {
            return (_bits & other._bits) == other._bits;
        }

        /// <summary>Returns the states this holds that <paramref name="other" /> does not.</summary>
        public State Except(State other)
        {
            return new State(_bits & ~other._bits);
        }

        /// <summary>
        ///     Returns true if looks for this state are laid before looks for <paramref name="other" />, so that
        ///     <paramref name="other" /> wins where both set something.
        /// </summary>
        /// <remarks>Lighter before heavier; at one weight, the fewer states before the more; then the earlier declared first.</remarks>
        public bool LaidBefore(State other)
        {
            var weight = Weight;
            var otherWeight = other.Weight;
            if (weight != otherWeight)
                return weight < otherWeight;

            var count = Count;
            var otherCount = other.Count;
            if (count != otherCount)
                return count < otherCount;

            return _bits < other._bits;
        }

        /// <summary>
        ///     Sorts <paramref name="states" /> into the order their looks are laid in, the one that wins last.
        /// </summary>
        public static void Order(List<State> states)
        {
            for (var i = 1; i < states.Count; i++)
            {
                var state = states[i];
                var at = i - 1;
                while (at >= 0 && state.LaidBefore(states[at]))
                {
                    states[at + 1] = states[at];
                    at--;
                }

                states[at + 1] = state;
            }
        }

        /// <summary>Combines two states.</summary>
        public static State operator |(State a, State b)
        {
            return new State(a._bits | b._bits);
        }

        /// <summary>Returns true if both hold the same states.</summary>
        public static bool operator ==(State a, State b)
        {
            return a._bits == b._bits;
        }

        /// <summary>Returns true if the two hold different states.</summary>
        public static bool operator !=(State a, State b)
        {
            return a._bits != b._bits;
        }

        /// <inheritdoc />
        public bool Equals(State other)
        {
            return _bits == other._bits;
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            return obj is State && Equals((State)obj);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return _bits.GetHashCode();
        }

        /// <inheritdoc />
        public override string ToString()
        {
            if (_bits == 0UL)
                return "None";

            var names = string.Empty;
            for (var i = 0; i < Most; i++)
            {
                if ((_bits & (1UL << i)) == 0UL)
                    continue;

                names = names.Length == 0 ? Names[i] : names + " | " + Names[i];
            }

            return names;
        }
    }
}
