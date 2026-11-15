using System;
using System.Collections.Generic;

namespace Lumel.Styling
{
    /// <summary>
    ///     How a <typeparamref name="TControl" /> looks: a base look, and a look for each state or combination of
    ///     states it names.
    /// </summary>
    /// <remarks>
    ///     Written once, in its initializer, and handed out as an <see cref="IStyle{TControl}" />, which has nothing to write.
    ///     A look for a set of states is worked out the first time an element is in it, and kept.
    /// </remarks>
    public class Style<TControl, TLook> : IStyle<TControl> where TControl : Element where TLook : ElementLook, new()
    {
        readonly IStyle _basedOn;
        readonly List<State> _keys = new List<State>();
        readonly List<TLook> _looks = new List<TLook>();
        Dictionary<ulong, TLook> _laid;

        /// <summary>Creates a style that sets nothing, leaving its control at its neutral values.</summary>
        public Style()
        {
            Base = this[State.None];
        }

        /// <summary>
        ///     Creates a style that includes everything <paramref name="basedOn" /> sets, with this style's own
        ///     settings laid over it.
        /// </summary>
        /// <remarks>Throws for a null base, which is what a static style declared above the one it is based on receives.</remarks>
        protected Style(IStyle basedOn) : this()
        {
            if (basedOn == null)
                throw new ArgumentNullException("basedOn", "A style's base must be declared before it.");

            _basedOn = basedOn;
        }

        /// <summary>Gets how the control looks whatever state it is in.</summary>
        public TLook Base { get; }

        /// <summary>Gets what changes while a press would reach the control (on hover).</summary>
        public TLook PointerOver => this[State.PointerOver];

        /// <summary>Gets what changes while the control is disabled.</summary>
        public TLook Disabled => this[State.Disabled];

        /// <summary>
        ///     Gets what changes while the control is in <paramref name="state" />, or in every state it combines.
        /// </summary>
        public TLook this[State state]
        {
            get
            {
                var at = _keys.IndexOf(state);
                if (at >= 0)
                    return _looks[at];

                var look = new TLook();
                _keys.Add(state);
                _looks.Add(look);
                return look;
            }
        }

        /// <inheritdoc />
        public void Lay(ElementLook look, State states)
        {
            look.Merge(Laid(states));
        }

        /// <inheritdoc />
        public void FindLooks(State states, List<State> keys)
        {
            foreach (var key in _keys)
            {
                if (states.Holds(key) && !keys.Contains(key))
                    keys.Add(key);
            }

            _basedOn?.FindLooks(states, keys);
        }

        /// <inheritdoc />
        public void LayOne(ElementLook look, State key)
        {
            _basedOn?.LayOne(look, key);

            var at = _keys.IndexOf(key);
            if (at >= 0)
                look.Merge(_looks[at]);
        }

        /// <inheritdoc />
        public bool IsFor(Element element)
        {
            return element is TControl;
        }

        /// <inheritdoc />
        public bool Includes(IStyle other)
        {
            return other is IStyle<TControl>;
        }

        /// <summary>Returns everything this sets for <paramref name="states" />, computed once and kept.</summary>
        /// <remarks>Safe to call from several threads at once.</remarks>
        TLook Laid(State states)
        {
            // Copied on write: a dictionary once published is never changed, so a thread reading it while another adds
            // reads a whole one. Two threads adding at once may each work out a look, and the one whose copy is published
            // second keeps its own; the other's is worked out again when next asked for.
            var known = _laid;
            TLook laid;
            if (known != null && known.TryGetValue(states.Bits, out laid))
                return laid;

            var keys = new List<State>();
            FindLooks(states, keys);
            State.Order(keys);

            laid = new TLook();
            foreach (var key in keys)
                LayOne(laid, key);

            var grown = known == null ? new Dictionary<ulong, TLook>() : new Dictionary<ulong, TLook>(known);
            grown[states.Bits] = laid;
            _laid = grown;
            return laid;
        }
    }
}
