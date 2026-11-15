using Lumel.Drawing;
using VRageMath;

namespace Lumel.Layout
{
    /// <summary>A panel that stacks its children in one place like a deck of cards, showing only the top one.</summary>
    /// <remarks>
    /// The deck is as large as its largest card, so turning to another card never changes its size. Only the card on top
    /// is drawn, pressed or reached by the keyboard.
    /// </remarks>
    public class Deck : OverlayPanel
    {
        Element _top;

        /// <summary>
        ///     Gets or sets the child that is shown; the first child if none is chosen or the chosen one has been
        ///     removed.
        /// </summary>
        public Element Top
        {
            get { return _top != null && _top.Parent == this ? _top : Children.Count > 0 ? Children[0] : null; }
            set
            {
                if (ReferenceEquals(_top, value))
                    return;

                _top = value;
                if (value != null)
                    value.Restyle();
            }
        }

        /// <inheritdoc />
        public override int VisualChildCount => Top == null ? 0 : 1;

        /// <inheritdoc />
        public override Element GetVisualChild(int index)
        {
            return Top;
        }

        /// <inheritdoc />
        protected override void DrawContent(IGraphics g)
        {
            var top = Top;
            if (top != null)
                top.Draw(g);
        }

        /// <inheritdoc />
        protected override Element HitTestContent(Vector2 point)
        {
            var top = Top;
            return top?.HitTest(point);
        }
    }
}
