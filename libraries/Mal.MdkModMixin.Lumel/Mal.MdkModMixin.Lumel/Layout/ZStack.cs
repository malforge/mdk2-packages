using Lumel.Drawing;
using VRageMath;

namespace Lumel.Layout
{
    /// <summary>A panel that stacks its children in one place, each drawn over the one before.</summary>
    /// <remarks>
    /// Sized by its largest child, with each child placed in the whole of it by its own <see cref="Element.Align" />. A press
    /// reaches the uppermost child under it.
    /// </remarks>
    public class ZStack : OverlayPanel
    {
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
    }
}
