using System;
using Lumel.Drawing;
using Lumel.Layout;
using Lumel.Motion;
using VRageMath;

namespace Lumel.Viewing
{
    /// <summary>
    ///     Slides one content in from the right while another slides out to the left, side by side, over several ticks.
    /// </summary>
    /// <remarks>
    /// Both are laid out in the whole of the slide, each by its own <see cref="Element.Align" />, and how far it has gone moves
    /// them only as they are drawn, so a slide in motion is never laid out again. While it moves each is drawn as far outside
    /// the slide as <see cref="Travel" /> takes it, which suits a slide whose sides reach the screen's, or one inside something
    /// that clips.
    /// </remarks>
    public class SlideTransition : Container
    {
        /// <summary>The number of game ticks a slide takes.</summary>
        public const int SlideTicks = 12;

        readonly Collection _held;
        readonly Transition _motion = new Transition(SlideTicks, Easing.EaseOut);
        Element _from;
        Element _to;
        float _progress = 1f;
        float? _travel;

        /// <summary>Creates a slide with no content, at rest.</summary>
        public SlideTransition()
        {
            _held = CreateChildren();
        }

        /// <summary>Gets or sets the content sliding out, or null for none.</summary>
        public Element From
        {
            get { return _from; }
            set { Swap(ref _from, value); }
        }

        /// <summary>
        ///     Gets or sets the content sliding in, which is the one in place when the slide finishes, or null for
        ///     none.
        /// </summary>
        public Element To
        {
            get { return _to; }
            set { Swap(ref _to, value); }
        }

        /// <summary>
        ///     Gets how far the slide has progressed as of the last frame drawn, from 0 with <see cref="From" /> in
        ///     place to 1 with <see cref="To" /> in place.
        /// </summary>
        public float Progress => _progress;

        /// <summary>
        ///     Gets or sets the distance between the two contents while drawn, or null to use the slide's own width.
        /// </summary>
        public float? Travel
        {
            get { return _travel; }
            set
            {
                _travel = value;
                Move();
            }
        }

        /// <summary>Stretches each content over the whole slide.</summary>
        protected override Align Aligns => Align.Fill;

        /// <summary>
        ///     Starts sliding <see cref="From" /> out and <see cref="To" /> in, beginning with the next frame drawn.
        /// </summary>
        public void Start()
        {
            _progress = 0f;
            _motion.Start();
            Move();
        }

        /// <summary>Ends the slide immediately, with <see cref="To" /> in place.</summary>
        public void Finish()
        {
            _progress = 1f;
            _motion.Finish();
            Move();
        }

        /// <inheritdoc />
        protected override Vector2 MeasureContent(IGraphics g, Vector2 available)
        {
            var size = Vector2.Zero;
            foreach (var child in _held)
            {
                var sized = child.Placement as ISized;
                child.Measure(g, new Vector2(Given(sized, true, available.X), Given(sized, false, available.Y)));
                size.X = Math.Max(size.X, Given(sized, true, child.DesiredSize.X));
                size.Y = Math.Max(size.Y, Given(sized, false, child.DesiredSize.Y));
            }

            return size;
        }

        /// <inheritdoc />
        protected override void ArrangeContent(RectangleF bounds)
        {
            foreach (var child in _held)
            {
                var sized = child.Placement as ISized;
                float x;
                float width;
                float y;
                float height;
                Span(child, true, bounds.X, bounds.Width, sized?.Width ?? float.NaN, child.DesiredSize.X, out x, out width);
                Span(child, false, bounds.Y, bounds.Height, sized?.Height ?? float.NaN, child.DesiredSize.Y, out y, out height);
                child.Arrange(new RectangleF(x, y, width, height));
            }

            Move();
        }

        /// <summary>
        ///     Advances the slide to this frame, then draws the outgoing content while any of it is visible, and the
        ///     incoming content.
        /// </summary>
        protected override void DrawContent(IGraphics g)
        {
            if (_motion.IsRunning)
            {
                _progress = _motion.Progress(g);
                Move();
            }

            if (_from != null && _progress < 1f)
                _from.Draw(g);

            _to?.Draw(g);
        }

        /// <summary>Hit tests only the incoming content, and only once it is in place.</summary>
        protected override Element HitTestContent(Vector2 point)
        {
            return _progress >= 1f ? _to?.HitTest(point) : null;
        }

        /// <summary>Rejects a content whose placement is not <see cref="ISized" />.</summary>
        protected override void Admit(Element child, IPlacement placement)
        {
            AdmitSized(placement);
        }

        /// <summary>
        ///     Offsets each content horizontally by the slide's progress, which is where it is drawn and hit tested.
        /// </summary>
        void Move()
        {
            var apart = _travel ?? Bounds.Width;
            if (_from != null)
                _from.RenderTransform = _progress <= 0f ? RenderTransform.Identity : RenderTransform.Moved(new Vector2(-apart * _progress, 0f));

            if (_to != null)
                _to.RenderTransform = _progress >= 1f ? RenderTransform.Identity : RenderTransform.Moved(new Vector2(apart * (1f - _progress), 0f));
        }

        void Swap(ref Element held, Element value)
        {
            if (ReferenceEquals(held, value))
                return;

            if (held != null)
            {
                held.RenderTransform = RenderTransform.Identity;
                _held.Remove(held);
            }

            held = value;
            if (value != null)
                _held.Add(value);

            Move();
        }
    }
}
