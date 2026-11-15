using Lumel.Drawing;
using Lumel.Layout;
using Lumel.Motion;
using VRageMath;

namespace Lumel.Viewing
{
    /// <summary>
    ///     Shows part of its content from the bottom up and clips the rest, opening and closing over several ticks.
    /// </summary>
    /// <remarks>Closed until it is opened, it runs from wherever it stands towards open or closed each time <see cref="IsOpen" /> changes.</remarks>
    public class Reveal : ContentControl
    {
        /// <summary>The number of game ticks it takes to open from closed, or to close from open.</summary>
        public const int OpenTicks = 12;

        readonly Transition _motion = new Transition(OpenTicks, Easing.EaseOut);
        float _progress;
        float _from;
        bool _open;

        /// <summary>Whether it is open or opening, rather than closed or closing.</summary>
        public bool IsOpen
        {
            get { return _open; }
            set
            {
                if (_open == value)
                    return;

                _open = value;
                _from = _progress;
                _motion.Start();
                InvalidateMeasure();
            }
        }

        /// <summary>Gets how much of the content is visible, from none to all, as of the last frame drawn.</summary>
        public float Progress => _progress;

        /// <summary>Whether none of the content is visible, so that it may be removed.</summary>
        public bool IsClosed => _progress <= 0f;

        /// <summary>
        ///     Whether the content is reachable: nothing inside can be reached until all of it is visible.
        /// </summary>
        protected override bool Passes => _progress >= 1f;

        /// <inheritdoc />
        protected override bool Clips => true;

        /// <summary>
        ///     Stretches the content horizontally and aligns it to the bottom edge, where the visible part ends.
        /// </summary>
        protected override Align Aligns => Align.FillWidth | Align.Bottom;

        /// <inheritdoc />
        protected override Vector2 MeasureContent(IGraphics g, Vector2 available)
        {
            if (Content == null || !Content.IsVisible)
                return Vector2.Zero;

            MeasureHeld(g, available);

            var size = HeldSize;
            return new Vector2(size.X, size.Y * _progress);
        }

        /// <inheritdoc />
        protected override void ArrangeContent(RectangleF bounds)
        {
            if (Content == null || !Content.IsVisible)
                return;

            // Full height, bottom-aligned to what is on show, with the hidden part above the top edge.
            var size = HeldSize;
            var height = size.Y;

            float x;
            float wide;
            Span(Content, true, bounds.X, bounds.Width, Given(Content.Placement as ISized, true, float.NaN), size.X, out x, out wide);
            Content.Arrange(new RectangleF(x, bounds.Y + bounds.Height - height, wide, height));
        }

        /// <inheritdoc />
        protected override Element HitTestContent(Vector2 point)
        {
            return Passes && Content != null ? Content.HitTest(point) : null;
        }

        /// <summary>Advances the opening or closing to this frame, then draws the visible content.</summary>
        /// <remarks>A step that changes how much is in view is laid out from the next frame, which it asks for.</remarks>
        protected override void DrawContent(IGraphics g)
        {
            var progress = MathHelper.Lerp(_from, _open ? 1f : 0f, _motion.Progress(g));
            if (progress != _progress)
            {
                _progress = progress;
                InvalidateMeasure();
                g.RequestFrame();
            }

            if (IsClosed || Content == null || !Content.IsVisible)
                return;

            Content.Draw(g);
        }
    }
}
