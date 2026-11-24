using System;
using System.Collections.Generic;
using Lumel.Drawing;
using Lumel.Input;
using Lumel.Styling;
using VRageMath;

namespace Lumel
{
    public abstract partial class Element : IThemed
    {
        bool _applied;
        ElementLook _look;
        Action _ownChanged;
        List<State> _ownKeys;
        List<ElementLook> _ownLooks;
        State _states;
        IStyle _style;
        ITheme _theme;
        IStyle _themed;
        bool _themeFound;

        // The looks an element of this tree lays in turn, gathered afresh on each apply, kept by the tree's top element:
        // one list per tree, so an apply allocates nothing, and trees laid out on different threads at once, as the game
        // does with text panels while a world loads, share nothing.
        List<State> _layKeys;

        /// <summary>The color painted across <see cref="Bounds" /> before anything else.</summary>
        /// <remarks>Set by this element's looks.</remarks>
        public Color Background { get; private set; }

        /// <summary>
        ///     Whether the <see cref="Background" /> is painted at the <see cref="Lumel.Windows.WindowLayer.UserBackgroundOpacity" /> of the
        ///     layer this is in.
        /// </summary>
        /// <remarks>Set by this element's looks.</remarks>
        public bool FollowsUserBackgroundOpacity { get; private set; }

        /// <summary>
        ///     The pattern laid across <see cref="Bounds" /> over the <see cref="Background" /> before anything else,
        ///     or null for none.
        /// </summary>
        /// <remarks>Set by this element's looks.</remarks>
        public Pattern Pattern { get; private set; }

        /// <summary>The color of the line drawn around <see cref="Bounds" />, inside it.</summary>
        /// <remarks>Set by this element's looks.</remarks>
        public Color Border { get; private set; }

        /// <summary>The thickness of the <see cref="Border" />, in UI units.</summary>
        /// <remarks>Set by this element's looks.</remarks>
        public float BorderThickness { get; private set; }

        /// <summary>The sound played as the pointer comes onto this while a press would run its command, or null for none.</summary>
        /// <remarks>Set by this element's looks.</remarks>
        public Sound HoverSound { get; private set; }

        /// <summary>The sound played as a press runs this element's command, or null for none.</summary>
        /// <remarks>Set by this element's looks.</remarks>
        public Sound ClickSound { get; private set; }

        /// <summary>
        ///     Gets or sets the logic that picks the sound this plays for each control sound, returning null for the one
        ///     its looks set or <see cref="Sound.None" /> for silence.
        /// </summary>
        /// <remarks>Asked as it happens: for a click, before the command runs.</remarks>
        public Func<ControlSound, Sound> FnSound { get; set; }

        /// <summary>
        ///     Returns the sound this plays for <paramref name="sound" />, from <see cref="FnSound" /> or else its looks,
        ///     or null for none.
        /// </summary>
        public Sound SoundFor(ControlSound sound)
        {
            var chosen = FnSound?.Invoke(sound) ?? (sound == ControlSound.Hover ? HoverSound : ClickSound);
            return chosen == Sound.None ? null : chosen;
        }

        /// <summary>
        ///     Gets or sets the style of this element, or null to use the default its host's theme holds for its kind.
        /// </summary>
        public IStyle<Element> Style
        {
            get { return (IStyle<Element>)StyleSet; }
            set { SetStyle(value); }
        }

        /// <summary>The theme of the host this is in, or null for Lumel's game look.</summary>
        protected ITheme HostTheme => Top._theme;

        /// <summary>The element at the top of the tree this is in, which is this element where it has no parent.</summary>
        Element Top
        {
            get
            {
                var top = this;
                while (top._parent != null)
                    top = top._parent;

                return top;
            }
        }

        /// <summary>The style this was given, or null if it uses its host theme's.</summary>
        protected IStyle StyleSet => _style;

        /// <summary>The look this element sets itself for every state, applied over its style's.</summary>
        public ElementLook Base => Own(State.None);

        /// <summary>
        ///     The look this element sets itself for when the pointer is over it, applied over its style's.
        /// </summary>
        public ElementLook PointerOver => Own(State.PointerOver);

        /// <summary>The look this element sets itself for when it is disabled, applied over its style's.</summary>
        public ElementLook Disabled => Own(State.Disabled);

        /// <summary>
        ///     The look this element sets itself for <paramref name="state" />, or for every state that includes it,
        ///     applied over its style's.
        /// </summary>
        public ElementLook this[State state] => Own(state);

        /// <summary>The states this is in right now.</summary>
        /// <remarks>An override joins its own states to what this answers.</remarks>
        public virtual State States
        {
            get
            {
                var states = _states;
                if (_pointerOver && IsEnabled)
                    states |= State.PointerOver;

                if (!IsEnabled)
                    states |= State.Disabled;

                if (_focused)
                    states |= State.Focused;

                if (IsFocusVisible)
                    states |= State.FocusVisible;

                return states;
            }
        }

        /// <summary>Returns true if this is in every state that <paramref name="state" /> includes.</summary>
        public bool Is(State state)
        {
            return States.Holds(state);
        }

        /// <summary>
        ///     Puts this into <paramref name="state" /> if <paramref name="on" /> is true, or takes it out otherwise,
        ///     and shows the look that goes with it.
        /// </summary>
        /// <remarks>Pointer over, disabled and focused come from the element itself.</remarks>
        public void Set(State state, bool on)
        {
            var states = on ? _states | state : _states.Except(state);
            if (states == _states)
                return;

            _states = states;
            Reapply();
        }

        /// <summary>Applies the looks of this element and everything inside it now, using its host's theme.</summary>
        /// <remarks>For reading a look before the element's first measure.</remarks>
        public void Restyle()
        {
            _themeFound = false;
            ApplyLook();

            for (var i = 0; i < VisualChildCount; i++)
                GetVisualChild(i).Restyle();
        }

        /// <summary>
        ///     Gives this element the style <paramref name="style" />, for a control's own <c>Style</c> property to
        ///     call.
        /// </summary>
        /// <remarks>Throws where the style is not for this kind of control.</remarks>
        protected void SetStyle(IStyle style)
        {
            if (style != null && !Fits(style))
                throw new ArgumentException("A " + style.GetType().Name + " is not a style for a " + GetType().Name + ".", "style");

            if (_style == style)
                return;

            _style = style;
            Reapply();
        }

        /// <summary>Returns true if <paramref name="style" /> is one this kind of control accepts.</summary>
        /// <remarks>A control that narrows its <c>Style</c> property narrows this to match.</remarks>
        protected virtual bool Fits(IStyle style)
        {
            return style.IsFor(this);
        }

        /// <summary>Creates a look of the kind this control reads, setting nothing.</summary>
        protected virtual ElementLook NewLook()
        {
            return new ElementLook();
        }

        /// <summary>
        ///     Applies <paramref name="look" />, which holds everything this element's style and own looks set for the
        ///     states it is in.
        /// </summary>
        /// <remarks>
        ///     Called from the element's first measure on. An override calls this first, then reads its own properties from the
        ///     look, which is always of the kind <see cref="NewLook" /> makes.
        /// </remarks>
        protected virtual void Apply(ElementLook look)
        {
            Background = look.Background ?? Color.Transparent;
            FollowsUserBackgroundOpacity = look.FollowsUserBackgroundOpacity ?? false;
            Pattern = look.Pattern;
            Border = look.Border ?? Color.Transparent;
            BorderThickness = look.BorderThickness ?? 1f;
            HoverSound = look.HoverSound;
            ClickSound = look.ClickSound;

            var padded = this as IPadded;
            padded?.Pad(look.Padding ?? default(Thickness));
        }

        /// <summary>
        ///     Replaces the own looks of <paramref name="other" /> with this element's own looks, state by state.
        /// </summary>
        protected void CopyOwnLooksTo(Element other)
        {
            if (other._ownKeys != null)
            {
                foreach (var look in other._ownLooks)
                    look.Clear();
            }

            if (_ownKeys != null)
            {
                for (var i = 0; i < _ownKeys.Count; i++)
                    other.Own(_ownKeys[i]).Merge(_ownLooks[i]);
            }

            other.Reapply();
        }

        /// <summary>
        ///     Returns this element's own look for <paramref name="state" />, creating it the first time it is
        ///     requested.
        /// </summary>
        ElementLook Own(State state)
        {
            if (_ownKeys == null)
            {
                _ownKeys = new List<State>();
                _ownLooks = new List<ElementLook>();
                _ownChanged = Reapply;
            }

            var at = _ownKeys.IndexOf(state);
            if (at >= 0)
                return _ownLooks[at];

            var look = NewLook();
            look.Changed += _ownChanged;
            _ownKeys.Add(state);
            _ownLooks.Add(look);
            Reapply();
            return look;
        }

        /// <summary>
        ///     Reapplies the look if it has been applied before; until then, the first measure applies it.
        /// </summary>
        void Reapply()
        {
            if (_applied)
                ApplyLook();
        }

        void ApplyLook()
        {
            _applied = true;

            var look = _look ?? (_look = NewLook());
            Lay(look, States);
            Apply(look);
        }

        /// <summary>
        ///     Clears <paramref name="look" /> and fills it with what this element's style and own looks set for
        ///     <paramref name="states" />.
        /// </summary>
        protected void Lay(ElementLook look, State states)
        {
            look.Clear();

            var style = _style ?? ThemeStyle();
            if (_ownKeys == null)
                style?.Lay(look, states);
            else
                LayWithOwn(look, style, states);
        }

        /// <summary>
        ///     Lays the looks of <paramref name="style" /> and this element's own looks for <paramref name="states" />
        ///     into <paramref name="look" />, each own look straight after the style's look for the same states.
        /// </summary>
        void LayWithOwn(ElementLook look, IStyle style, State states)
        {
            var top = Top;
            var keys = top._layKeys ?? (top._layKeys = new List<State>());
            keys.Clear();
            style?.FindLooks(states, keys);

            foreach (var key in _ownKeys)
            {
                if (states.Holds(key) && !keys.Contains(key))
                    keys.Add(key);
            }

            State.Order(keys);

            foreach (var key in keys)
            {
                style?.LayOne(look, key);

                var at = _ownKeys.IndexOf(key);
                if (at >= 0)
                    look.Merge(_ownLooks[at]);
            }
        }

        /// <summary>
        ///     Returns the style that the theme of the host this is in holds for its kind, found once and kept until it
        ///     moves.
        /// </summary>
        IStyle ThemeStyle()
        {
            if (_themeFound)
                return _themed;

            _themed = (HostTheme ?? GameTheme.Theme).StyleFor(this);
            _themeFound = true;
            return _themed;
        }

        /// <inheritdoc />
        void IThemed.UseTheme(ITheme theme)
        {
            if (_theme == theme)
                return;

            _theme = theme;
            Rehost();
        }

        /// <summary>Marks this element and everything inside it to reapply their looks for their current host at the next measure.</summary>
        void Rehost()
        {
            if (!_themeFound && !_applied)
                return;

            _themeFound = false;
            _applied = false;
            _measureDirty = true;
            _arrangeDirty = true;

            if (_contents != null)
                for (var i = 0; i < _contents.Count; i++)
                    _contents[i].Rehost();

            _parent?.InvalidateMeasure();
        }
    }
}
