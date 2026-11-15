using VRage.Utils;
using VRageMath;

namespace Lumel.Drawing
{
    /// <summary>
    ///     A square picture drawn over a rectangle in nine parts: corners at a fixed size, edges stretched along the
    ///     sides between them, and the middle stretched across the rest.
    /// </summary>
    /// <remarks>Never changes once made.</remarks>
    public sealed class NineSlice
    {
        readonly MyStringId _material;
        readonly int _size;
        readonly int _corner;

        /// <summary>
        ///     Creates a nine-slice from the picture <paramref name="material" />, <paramref name="size" /> pixels
        ///     square, with corners <paramref name="corner" /> pixels square.
        /// </summary>
        public NineSlice(MyStringId material, int size, int corner)
        {
            _material = material;
            _size = size;
            _corner = corner;
        }

        /// <summary>
        ///     Draws the picture over <paramref name="bounds" />, with corners <paramref name="thickness" /> square,
        ///     tinted <paramref name="color" />.
        /// </summary>
        public void Draw(IGraphics g, RectangleF bounds, float thickness, Color color)
        {
            var c = _corner;
            var middle = _size - 2 * c;
            var inner = new Vector2(bounds.Width - 2f * thickness, bounds.Height - 2f * thickness);
            var left = bounds.X;
            var top = bounds.Y;
            var right = bounds.X + bounds.Width - thickness;
            var bottom = bounds.Y + bounds.Height - thickness;

            g.DrawSprite(_material, new RectangleF(left, top, thickness, thickness), new Rectangle(0, 0, c, c), color);
            g.DrawSprite(_material, new RectangleF(right, top, thickness, thickness), new Rectangle(_size - c, 0, c, c), color);
            g.DrawSprite(_material, new RectangleF(left, bottom, thickness, thickness), new Rectangle(0, _size - c, c, c), color);
            g.DrawSprite(_material, new RectangleF(right, bottom, thickness, thickness), new Rectangle(_size - c, _size - c, c, c), color);

            if (inner.X > 0f)
            {
                g.DrawSprite(_material, new RectangleF(left + thickness, top, inner.X, thickness), new Rectangle(c, 0, middle, c), color);
                g.DrawSprite(_material, new RectangleF(left + thickness, bottom, inner.X, thickness), new Rectangle(c, _size - c, middle, c), color);
            }

            if (inner.Y > 0f)
            {
                g.DrawSprite(_material, new RectangleF(left, top + thickness, thickness, inner.Y), new Rectangle(0, c, c, middle), color);
                g.DrawSprite(_material, new RectangleF(right, top + thickness, thickness, inner.Y), new Rectangle(_size - c, c, c, middle), color);
            }

            if (inner.X > 0f && inner.Y > 0f)
                g.DrawSprite(_material, new RectangleF(left + thickness, top + thickness, inner.X, inner.Y), new Rectangle(c, c, middle, middle), color);
        }
    }
}
