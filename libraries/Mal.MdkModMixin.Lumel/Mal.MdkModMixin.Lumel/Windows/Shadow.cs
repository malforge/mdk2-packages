using Lumel.Drawing;
using VRageMath;

namespace Lumel.Windows
{
    /// <summary>
    ///     The shadow a window casts on what lies beneath it: a soft picture extending past its edge and offset from
    ///     it.
    /// </summary>
    /// <remarks>Never changes once made, so one shadow is shared by every window style that casts it.</remarks>
    public sealed class Shadow
    {
        readonly NineSlice _picture;
        readonly float _spread;
        readonly Vector2 _offset;
        readonly Color _color;

        /// <summary>
        ///     Creates a shadow drawn with <paramref name="picture" />, extending <paramref name="spread" /> past the
        ///     window's edge, offset by <paramref name="offset" />, and tinted <paramref name="color" />.
        /// </summary>
        public Shadow(NineSlice picture, float spread, Vector2 offset, Color color)
        {
            _picture = picture;
            _spread = spread;
            _offset = offset;
            _color = color;
        }

        /// <summary>Draws the shadow under a window at <paramref name="bounds" />.</summary>
        public void Draw(IGraphics g, RectangleF bounds)
        {
            if (_spread <= 0f)
                return;

            _picture.Draw(g, new RectangleF(bounds.X + _offset.X - _spread, bounds.Y + _offset.Y - _spread,
                bounds.Width + 2f * _spread, bounds.Height + 2f * _spread), _spread, _color);
        }
    }
}
