using System;
using System.Collections.Generic;
using Lumel.Drawing;
using Lumel.Styling;
using VRageMath;

namespace Lumel.Layout
{
    /// <summary>
    ///     A panel that lays children out in a line and wraps to the next line when it runs out of room, centering each
    ///     child vertically within its line.
    /// </summary>
    /// <remarks>A child wider than the room has a line to itself, and is given its own width.</remarks>
    public class Flow : Panel
    {
        readonly List<Vector2> _sizes = new List<Vector2>();

        float _indent;
        float _lineSpacing;
        float _spacing;

        /// <summary>Gets or sets the style of this flow, or null to use the theme's default style for flows.</summary>
        public new IStyle<Flow> Style
        {
            get { return (IStyle<Flow>)StyleSet; }
            set { SetStyle(value); }
        }

        /// <summary>Gets this flow's own look, applied in every state on top of its style's.</summary>
        public new FlowLook Base => (FlowLook)base.Base;

        /// <summary>Gets this flow's own look for when it is hovered, applied on top of its style's.</summary>
        public new FlowLook PointerOver => (FlowLook)base.PointerOver;

        /// <summary>Gets this flow's own look for when it is disabled, applied on top of its style's.</summary>
        public new FlowLook Disabled => (FlowLook)base.Disabled;

        /// <summary>
        ///     Gets this flow's own look for <paramref name="state" />, or for every state that includes it, applied on
        ///     top of its style's.
        /// </summary>
        public new FlowLook this[State state] => (FlowLook)base[state];

        /// <summary>Gets the blank space between one child and the next along a line.</summary>
        /// <remarks>Set by this flow's looks.</remarks>
        public float Spacing => _spacing;

        /// <summary>Gets the blank space between one line and the next.</summary>
        /// <remarks>Set by this flow's looks.</remarks>
        public float LineSpacing => _lineSpacing;

        /// <summary>Gets the blank space before every line after the first.</summary>
        /// <remarks>Set by this flow's looks.</remarks>
        public float Indent => _indent;

        /// <inheritdoc />
        protected override bool Fits(IStyle style)
        {
            return style is IStyle<Flow>;
        }

        /// <inheritdoc />
        protected override ElementLook NewLook()
        {
            return new FlowLook();
        }

        /// <inheritdoc />
        protected override void Apply(ElementLook look)
        {
            base.Apply(look);

            var flow = (FlowLook)look;
            var spacing = flow.Spacing ?? 0f;
            var lineSpacing = flow.LineSpacing ?? 0f;
            var indent = flow.Indent ?? 0f;
            if (spacing == _spacing && lineSpacing == _lineSpacing && indent == _indent)
                return;

            _spacing = spacing;
            _lineSpacing = lineSpacing;
            _indent = indent;
            InvalidateMeasure();
        }

        /// <summary>Each line vertically centers what it holds.</summary>
        protected override Align Aligns => Align.Center;

        /// <inheritdoc />
        protected override Vector2 MeasureContent(IGraphics g, Vector2 available)
        {
            for (var i = 0; i < Children.Count; i++)
            {
                var size = _sizes[i];
                Children[i].Measure(g, new Vector2(Given(size.X, available.X), Given(size.Y, available.Y)));
            }

            var widest = 0f;
            var height = 0f;
            var from = 0;
            var placed = false;

            while (from < Children.Count)
            {
                float width;
                float tall;
                var inset = placed ? _indent : 0f;
                var to = Line(from, available.X - inset, out width, out tall);

                if (width > 0f || tall > 0f)
                {
                    if (placed)
                        height += _lineSpacing;

                    placed = true;
                    height += tall;
                    widest = Math.Max(widest, inset + width);
                }

                from = to;
            }

            return new Vector2(widest, height);
        }

        /// <inheritdoc />
        protected override void ArrangeContent(RectangleF bounds)
        {
            var y = bounds.Y;
            var from = 0;
            var placed = false;

            while (from < Children.Count)
            {
                float width;
                float tall;
                var inset = placed ? _indent : 0f;
                var to = Line(from, bounds.Width - inset, out width, out tall);

                if (width > 0f || tall > 0f)
                {
                    if (placed)
                        y += _lineSpacing;

                    placed = true;
                    Place(from, to, bounds.X + inset, y, tall);
                    y += tall;
                }

                from = to;
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
            _sizes.Insert(index, Resolve(child.Placement));
        }

        /// <inheritdoc />
        protected override void OnChildRemoved(Element child, int index)
        {
            _sizes.RemoveAt(index);
        }

        /// <inheritdoc />
        protected override void OnChildPlacementChanged(Element child)
        {
            var index = Children.IndexOf(child);
            if (index >= 0)
                _sizes[index] = Resolve(child.Placement);
        }

        int Line(int from, float room, out float width, out float tall)
        {
            // Measuring and arranging both break lines here, so a line measured as fitting cannot break
            // differently when it is placed.
            width = 0f;
            tall = 0f;
            var any = false;
            var at = from;

            for (; at < Children.Count; at++)
            {
                var child = Children[at];
                if (!child.IsVisible)
                    continue;

                var size = Size(at);
                var advance = any ? size.X + _spacing : size.X;
                if (any && width + advance > room)
                    break;

                width += advance;
                tall = Math.Max(tall, size.Y);
                any = true;
            }

            return at;
        }

        void Place(int from, int to, float x, float y, float tall)
        {
            var any = false;

            for (var i = from; i < to; i++)
            {
                var child = Children[i];
                if (!child.IsVisible)
                    continue;

                if (any)
                    x += _spacing;

                any = true;

                var size = Size(i);
                float at;
                float length;
                Span(child, false, y, tall, float.NaN, size.Y, out at, out length);
                child.Arrange(new RectangleF(x, at, size.X, length));
                x += size.X;
            }
        }

        Vector2 Size(int index)
        {
            var size = _sizes[index];
            var desired = Children[index].DesiredSize;
            return new Vector2(Given(size.X, desired.X), Given(size.Y, desired.Y));
        }

        static Vector2 Resolve(IPlacement placement)
        {
            var sized = placement as ISized;
            return sized == null ? new Vector2(float.NaN) : new Vector2(sized.Width, sized.Height);
        }

        static float Given(float size, float otherwise)
        {
            return float.IsNaN(size) ? otherwise : size;
        }
    }
}
