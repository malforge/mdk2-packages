using System;
using Lumel.Drawing;
using VRageMath;

namespace Lumel.Layout
{
    /// <summary>
    ///     A panel that stacks its children in one place, sized to the largest of them, with each child placed within
    ///     the whole by its own <see cref="Align" />.
    /// </summary>
    /// <remarks>Which children are drawn and pressed is up to the kind of panel.</remarks>
    public abstract class OverlayPanel : Panel
    {
        /// <summary>Each child stretches over the whole panel.</summary>
        protected override Align Aligns => Align.Fill;

        /// <inheritdoc />
        protected override Vector2 MeasureContent(IGraphics g, Vector2 available)
        {
            var size = Vector2.Zero;
            foreach (var child in Children)
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
            foreach (var child in Children)
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
        }

        /// <summary>Rejects a child whose placement is not <see cref="ISized" />.</summary>
        protected override void Admit(Element child, IPlacement placement)
        {
            AdmitSized(placement);
        }
    }
}
