using System;
using Lumel.Drawing;
using VRageMath;

namespace Lumel.Viewing
{
    /// <summary>Scales its content to its space by its <see cref="Stretch" />, keeping the content's aspect ratio.</summary>
    /// <remarks>
    ///     The scale is worked out from the content's own size: the size its placement fixes, or else the size it asks for
    ///     with no bounds, its text unwrapped. In a room unbounded either way the content is drawn at its own size. The scale
    ///     is the content's <see cref="Element.RenderTransform" />, which this sets.
    /// </remarks>
    public class Viewbox : ContentControl
    {
        // How much longer than its own size the content is laid out again where the room at its scale is about that size,
        // so its widest part keeps its line whatever rounding the length went through.
        const float Slack = 0.5f;

        Stretch _stretch = Stretch.FitAndExpand;
        StretchDirection _stretchDirection = StretchDirection.Both;
        float _scale = 1f;
        Vector2 _ownSize;

        /// <summary>Gets or sets how the content is scaled to the space and laid out in it.</summary>
        /// <remarks>Defaults to <see cref="Viewing.Stretch.FitAndExpand" />.</remarks>
        public Stretch Stretch
        {
            get { return _stretch; }
            set
            {
                if (_stretch == value)
                    return;

                _stretch = value;
                InvalidateMeasure();
            }
        }

        /// <summary>
        ///     Gets or sets the directions in which the content is scaled to the space: up, down, both, or neither.
        /// </summary>
        /// <remarks>
        ///     A scale it forbids is taken as 1, after which <see cref="Stretch" /> lays the content out at that scale as at
        ///     any other.
        /// </remarks>
        public StretchDirection StretchDirection
        {
            get { return _stretchDirection; }
            set
            {
                if (_stretchDirection == value)
                    return;

                _stretchDirection = value;
                InvalidateMeasure();
            }
        }

        /// <summary>
        ///     Gets the scale applied to the content when last measured, 1 if it is drawn at its own size.
        /// </summary>
        public float Scale => _scale;

        /// <summary>Gets whether the content keeps its own size, rather than being laid out again at its scale.</summary>
        bool KeepsSize => _stretch == Stretch.Fit || _stretch == Stretch.Fill;

        /// <summary>
        ///     Gets the alignment of the content: centered in the room left over where it keeps its own size, and filling
        ///     the space where it is laid out again.
        /// </summary>
        protected override Align Aligns => KeepsSize ? Align.Center : Align.Fill;

        /// <inheritdoc />
        protected override bool Clips => _stretch == Stretch.Fill || _stretch == Stretch.FillAndContract;

        /// <inheritdoc />
        protected override Vector2 MeasureContent(IGraphics g, Vector2 available)
        {
            _scale = 1f;
            var content = Content;
            if (content == null || !content.IsVisible)
                return Vector2.Zero;

            if (_stretch == Stretch.FitAndExpand)
                return MeasureExpanded(g, content, available);

            MeasureHeld(g, new Vector2(Unbounded, Unbounded));
            _ownSize = HeldSize;
            if (_ownSize.X <= 0f || _ownSize.Y <= 0f)
                return _ownSize;

            _scale = ScaleFor(available);
            if (KeepsSize)
            {
                var drawn = _ownSize * _scale;
                return _stretch == Stretch.Fill ? Vector2.Min(drawn, available) : drawn;
            }

            var room = available / _scale;
            content.Measure(g, new Vector2(Relaid(room.X, _ownSize.X), Relaid(room.Y, _ownSize.Y)));
            var laid = content.DesiredSize * _scale;
            return new Vector2(Math.Min(laid.X, Math.Max(available.X, _ownSize.X * _scale)), Math.Min(laid.Y, Math.Max(available.Y, _ownSize.Y * _scale)));
        }

        /// <summary>
        ///     Measures <paramref name="content" /> for <see cref="Viewing.Stretch.FitAndExpand" />: scaled to fit
        ///     <paramref name="available" />, then laid out again across the room's width at that scale.
        /// </summary>
        Vector2 MeasureExpanded(IGraphics g, Element content, Vector2 available)
        {
            if (_stretchDirection == StretchDirection.None || available.X >= Unbounded || available.Y >= Unbounded)
            {
                content.Measure(g, available);
                return content.DesiredSize;
            }

            content.Measure(g, new Vector2(Unbounded, Unbounded));
            _ownSize = content.DesiredSize;
            if (_ownSize.X <= 0f || _ownSize.Y <= 0f)
                return _ownSize;

            _scale = ScaleFor(available);
            content.Measure(g, new Vector2(Math.Max(available.X / _scale, _ownSize.X + Slack), Unbounded));
            return new Vector2(Math.Min(content.DesiredSize.X * _scale, available.X), _ownSize.Y * _scale);
        }

        /// <inheritdoc />
        protected override void ArrangeContent(RectangleF bounds)
        {
            var content = Content;
            if (content == null || !content.IsVisible)
                return;

            var wide = bounds.Width / _scale;
            var tall = bounds.Height / _scale;

            float x;
            float across;
            float y;
            float down;
            if (KeepsSize)
            {
                across = _ownSize.X;
                down = _ownSize.Y;
                x = bounds.X + (wide - across) * ShareBefore(content, true);
                y = bounds.Y + (tall - down) * ShareBefore(content, false);
            }
            else
            {
                Span(content, true, bounds.X, wide, float.NaN, content.DesiredSize.X, out x, out across);
                Span(content, false, bounds.Y, tall, float.NaN, content.DesiredSize.Y, out y, out down);
            }

            content.Arrange(new RectangleF(x, y, across, down));

            // A render transform scales about the content's own corner, so it is moved by as much as its distance from
            // this one's corner grows, which scales the whole room about this corner.
            var grown = _scale - 1f;
            content.RenderTransform = _scale == 1f
                ? RenderTransform.Identity
                : new RenderTransform(new Vector2((x - bounds.X) * grown, (y - bounds.Y) * grown), _scale);
        }

        /// <summary>
        ///     Returns the scale that brings the content's own size to <paramref name="available" /> by
        ///     <see cref="Stretch" />, held to 1 where <see cref="StretchDirection" /> forbids it or the room is unbounded.
        /// </summary>
        float ScaleFor(Vector2 available)
        {
            if (_stretchDirection == StretchDirection.None || available.X >= Unbounded || available.Y >= Unbounded)
                return 1f;

            var acrossRatio = available.X / _ownSize.X;
            var downRatio = available.Y / _ownSize.Y;
            var scale = _stretch == Stretch.Fit || _stretch == Stretch.FitAndExpand ? Math.Min(acrossRatio, downRatio) : Math.Max(acrossRatio, downRatio);
            if (scale > 1f && _stretchDirection == StretchDirection.DownOnly || scale < 1f && _stretchDirection == StretchDirection.UpOnly)
                return 1f;

            return scale;
        }

        /// <summary>
        ///     Returns the length the content is laid out again in on one axis, given the <paramref name="room" /> at its scale
        ///     and its <paramref name="own" /> length there.
        /// </summary>
        static float Relaid(float room, float own)
        {
            return room >= own - Slack ? Math.Max(room, own + Slack) : room;
        }
    }
}
