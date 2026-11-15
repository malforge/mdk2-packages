using System;
using System.Collections.Generic;
using Lumel.Drawing;
using Lumel.Styling;
using VRageMath;

namespace Lumel.Layout
{
    /// <summary>
    ///     A panel that lays children out in a line, each at its own size along it, sharing the room left among those
    ///     that grow.
    /// </summary>
    /// <remarks>
    /// A stack asks for what its children need, a child that grows included, and shares the room it is given beyond that
    /// among those that grow. Inside a scroll along the same axis there is nothing to share, and a child that grows is its
    /// natural size.
    /// </remarks>
    public abstract class Stack : Panel
    {
        readonly List<Entry> _entries = new List<Entry>();
        readonly bool _runsAcross;

        bool _sharing;

        float _spacing;

        /// <summary>
        ///     Creates an empty stack that runs across if <paramref name="runsAcross" /> is true and down otherwise.
        /// </summary>
        protected Stack(bool runsAcross)
        {
            _runsAcross = runsAcross;
        }

        /// <summary>
        ///     Gets or sets the style of this stack, or null to use the theme's default style for stacks.
        /// </summary>
        public new IStyle<Stack> Style
        {
            get { return (IStyle<Stack>)StyleSet; }
            set { SetStyle(value); }
        }

        /// <summary>Gets this stack's own look, applied in every state on top of its style's.</summary>
        public new StackLook Base => (StackLook)base.Base;

        /// <summary>Gets this stack's own look for when it is hovered, applied on top of its style's.</summary>
        public new StackLook PointerOver => (StackLook)base.PointerOver;

        /// <summary>Gets this stack's own look for when it is disabled, applied on top of its style's.</summary>
        public new StackLook Disabled => (StackLook)base.Disabled;

        /// <summary>
        ///     Gets this stack's own look for <paramref name="state" />, or for every state that includes it, applied
        ///     on top of its style's.
        /// </summary>
        public new StackLook this[State state] => (StackLook)base[state];

        /// <summary>Gets the blank space between one child and the next.</summary>
        /// <remarks>Set by this stack's looks.</remarks>
        public float Spacing
        {
            get { return _spacing; }
            private set
            {
                if (_spacing == value)
                    return;

                _spacing = value;
                InvalidateMeasure();
            }
        }

        /// <inheritdoc />
        protected override bool Fits(IStyle style)
        {
            return style is IStyle<Stack>;
        }

        /// <inheritdoc />
        protected override ElementLook NewLook()
        {
            return new StackLook();
        }

        /// <inheritdoc />
        protected override void Apply(ElementLook look)
        {
            base.Apply(look);
            Spacing = ((StackLook)look).Spacing ?? 0f;
        }

        /// <inheritdoc />
        protected override Vector2 MeasureContent(IGraphics g, Vector2 available)
        {
            var room = Along(available);
            var roomAcross = Across(available);
            var bounded = room < Unbounded;

            var used = 0f;
            var across = 0f;
            var weight = 0f;
            var placed = false;

            for (var i = 0; i < Children.Count; i++)
            {
                var child = Children[i];
                var entry = _entries[i];
                var grows = bounded && entry.Grow > 0f && entry.Tight;

                if (!grows)
                {
                    var left = room - used - (placed ? Spacing : 0f);
                    child.Measure(g, Make(Given(entry.Along, left > 0f ? left : 0f), Given(entry.Across, roomAcross)));
                }

                if (!child.IsVisible)
                    continue;

                if (placed)
                    used += Spacing;

                placed = true;

                if (grows)
                {
                    weight += entry.Grow;
                    continue;
                }

                used += Given(entry.Along, Along(child.DesiredSize));
                across = Wider(across, Given(entry.Across, Across(child.DesiredSize)));
            }

            _sharing = weight > 0f || Spares();

            if (!(weight > 0f))
                return Make(used, across);

            var spare = room - used;
            if (spare < 0f)
                spare = 0f;

            for (var i = 0; i < Children.Count; i++)
            {
                var entry = _entries[i];
                if (!(entry.Grow > 0f))
                    continue;

                var child = Children[i];
                child.Measure(g, Make(spare * entry.Grow / weight, Given(entry.Across, roomAcross)));

                if (!child.IsVisible)
                    continue;

                used += Along(child.DesiredSize);
                across = Wider(across, Given(entry.Across, Across(child.DesiredSize)));
            }

            return Make(used, across);
        }

        /// <inheritdoc />
        protected override void ArrangeContent(RectangleF bounds)
        {
            var room = _runsAcross ? bounds.Width : bounds.Height;

            var used = 0f;
            var weight = 0f;
            var placed = false;

            for (var i = 0; i < Children.Count; i++)
            {
                var child = Children[i];
                if (!child.IsVisible)
                    continue;

                if (placed)
                    used += Spacing;

                placed = true;

                var entry = _entries[i];
                if (_sharing && entry.Grow > 0f)
                    weight += entry.Grow;
                else
                    used += Given(entry.Along, Along(child.DesiredSize));
            }

            var spare = room - used;
            if (spare < 0f)
                spare = 0f;

            var cursor = _runsAcross ? bounds.X : bounds.Y;
            placed = false;

            for (var i = 0; i < Children.Count; i++)
            {
                var child = Children[i];
                if (!child.IsVisible)
                    continue;

                if (placed)
                    cursor += Spacing;

                placed = true;

                var entry = _entries[i];
                var size = _sharing && entry.Grow > 0f
                    ? spare * entry.Grow / weight
                    : Given(entry.Along, Along(child.DesiredSize));

                Place(child, entry, cursor, size, bounds);
                cursor += size;
            }
        }

        /// <inheritdoc />
        protected override void DrawContent(IGraphics g)
        {
            foreach (var child in Children)
                child.Draw(g);
        }

        /// <inheritdoc />
        protected override Element HitTestContent(Vector2 point)
        {
            return OnTop(Children, point);
        }

        /// <summary>Rejects a child whose placement is not <see cref="ISized" />.</summary>
        protected override void Admit(Element child, IPlacement placement)
        {
            if (placement != null && !(placement is ISized))
                throw new InvalidOperationException(
                    GetType().Name + " cannot place a child by " + placement.GetType().Name + ".");
        }

        /// <inheritdoc />
        protected override void OnChildAdded(Element child, int index)
        {
            _entries.Insert(index, Resolve(child.Placement));
        }

        /// <inheritdoc />
        protected override void OnChildRemoved(Element child, int index)
        {
            _entries.RemoveAt(index);
        }

        /// <inheritdoc />
        protected override void OnChildPlacementChanged(Element child)
        {
            var index = Children.IndexOf(child);
            if (index >= 0)
                _entries[index] = Resolve(child.Placement);
        }

        /// <summary>A column stretches what it holds across its width; a row centers it vertically.</summary>
        protected override Align Aligns => _runsAcross ? Align.Center : Align.FillWidth;

        /// <summary>
        ///     Places <paramref name="child" /> at position <paramref name="at" /> along the line with length
        ///     <paramref name="size" />, and across the line as the child's alignment says.
        /// </summary>
        void Place(Element child, Entry entry, float at, float size, RectangleF bounds)
        {
            float from;
            float length;
            if (!_runsAcross)
            {
                Span(child, true, bounds.X, bounds.Width, entry.Across, child.DesiredSize.X, out from, out length);
                child.Arrange(new RectangleF(from, at, length, size));
                return;
            }

            Span(child, false, bounds.Y, bounds.Height, entry.Across, child.DesiredSize.Y, out from, out length);
            child.Arrange(new RectangleF(at, from, size, length));
        }

        Entry Resolve(IPlacement placement)
        {
            var entry = new Entry { Along = float.NaN, Across = float.NaN };

            var sized = placement as ISized;
            if (sized != null)
            {
                entry.Along = _runsAcross ? sized.Width : sized.Height;
                entry.Across = _runsAcross ? sized.Height : sized.Width;
            }

            var stacked = placement as IStacked;
            if (stacked != null)
            {
                entry.Grow = stacked.Grow;
                entry.Tight = stacked.Fit == FlexFit.Tight;
            }

            return entry;
        }

        float Along(Vector2 size)
        {
            return _runsAcross ? size.X : size.Y;
        }

        float Across(Vector2 size)
        {
            return _runsAcross ? size.Y : size.X;
        }

        Vector2 Make(float along, float across)
        {
            return _runsAcross ? new Vector2(along, across) : new Vector2(across, along);
        }

        /// <summary>
        ///     Returns <paramref name="size" /> unless it is float.NaN, in which case returns
        ///     <paramref name="otherwise" />.
        /// </summary>
        static float Given(float size, float otherwise)
        {
            return float.IsNaN(size) ? otherwise : size;
        }

        static float Wider(float a, float b)
        {
            return a > b ? a : b;
        }

        /// <summary>What a child's placement says, read once when the child joins or changes.</summary>
        struct Entry
        {
            public float Along;
            public float Across;
            public float Grow;
            public bool Tight;
        }

        /// <summary>Returns true if a visible child takes only the room left over when the stack is arranged.</summary>
        bool Spares()
        {
            for (var i = 0; i < Children.Count; i++)
            {
                if (Children[i].IsVisible && _entries[i].Grow > 0f && !_entries[i].Tight)
                    return true;
            }

            return false;
        }
    }
}
