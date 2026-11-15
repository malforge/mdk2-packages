using Lumel.Drawing;
using Lumel.Layout;
using VRageMath;

namespace Lumel
{
    /// <summary>
    ///     A container that holds one piece of content, laid out to fill the space inside its padding unless a derived
    ///     control says otherwise.
    /// </summary>
    public abstract class ContentControl : Container
    {
        readonly Collection _held;
        Element _content;

        /// <summary>Creates a control that holds no content.</summary>
        protected ContentControl()
        {
            _held = CreateChildren();
        }

        /// <summary>Gets or sets the content of this control, or null for none.</summary>
        public Element Content
        {
            get { return _content; }
            set
            {
                if (_content == value)
                    return;

                if (_content != null)
                    _held.Remove(_content);

                if (value != null)
                    _held.Add(value);

                _content = value;
                InvalidateMeasure();
            }
        }

        /// <summary>
        ///     Adds <paramref name="part" />, an element of the control's own beside its content, which the control
        ///     measures, arranges and draws itself.
        /// </summary>
        protected void AddPart(Element part)
        {
            _held.Add(part);
        }

        /// <summary>Gets the alignment of the content, which fills the available space.</summary>
        protected override Align Aligns => Align.Fill;

        /// <inheritdoc />
        protected override Vector2 MeasureContent(IGraphics g, Vector2 available)
        {
            if (_content == null || !_content.IsVisible)
                return Vector2.Zero;

            MeasureHeld(g, available);
            return HeldSize;
        }

        /// <inheritdoc />
        protected override void ArrangeContent(RectangleF bounds)
        {
            if (_content == null || !_content.IsVisible)
                return;

            float x;
            float wide;
            float y;
            float tall;
            var size = HeldSize;
            Span(_content, true, bounds.X, bounds.Width, Fixed(true), size.X, out x, out wide);
            Span(_content, false, bounds.Y, bounds.Height, Fixed(false), size.Y, out y, out tall);
            _content.Arrange(new RectangleF(x, y, wide, tall));
        }

        /// <summary>
        ///     Gets the size of the content: the size its placement fixes, or the size it requested in its last measure
        ///     where its placement fixes none.
        /// </summary>
        protected Vector2 HeldSize
        {
            get
            {
                var sized = _content.Placement as ISized;
                return new Vector2(Given(sized, true, _content.DesiredSize.X), Given(sized, false, _content.DesiredSize.Y));
            }
        }

        /// <summary>
        ///     Measures the content within <paramref name="available" /> using <paramref name="g" />, substituting the
        ///     lengths its placement fixes for the available space on those axes.
        /// </summary>
        protected void MeasureHeld(IGraphics g, Vector2 available)
        {
            var sized = _content.Placement as ISized;
            _content.Measure(g, new Vector2(Given(sized, true, available.X), Given(sized, false, available.Y)));
        }

        /// <summary>
        ///     Returns the length the content's placement fixes horizontally, or vertically if
        ///     <paramref name="across" /> is false, or NaN if it fixes none.
        /// </summary>
        float Fixed(bool across)
        {
            return Given(_content.Placement as ISized, across, float.NaN);
        }

        /// <summary>Rejects content whose placement is not <see cref="ISized" />.</summary>
        protected override void Admit(Element child, IPlacement placement)
        {
            AdmitSized(placement);
        }

        /// <inheritdoc />
        protected override void DrawContent(IGraphics g)
        {
            if (_content != null)
                _content.Draw(g);
        }

        /// <inheritdoc />
        protected override Element HitTestContent(Vector2 point)
        {
            return _content?.HitTest(point);
        }
    }
}
