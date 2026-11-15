using System;
using VRage.Utils;
using VRageMath;

namespace Lumel.Drawing
{
    /// <summary>
    ///     A square picture tiled edge to edge across a rectangle on its own grid and tinted one color: a repeated
    ///     texture.
    /// </summary>
    /// <remarks>Never changes once made, so one pattern is shared by every style that lays it.</remarks>
    public sealed class Pattern
    {
        readonly MyStringId _material;
        readonly int _size;
        readonly float _tile;
        readonly Color _color;

        /// <summary>
        ///     Creates a pattern from the picture <paramref name="material" />, <paramref name="size" /> pixels square,
        ///     tiled <paramref name="tile" /> UI units square and tinted <paramref name="color" />.
        /// </summary>
        public Pattern(MyStringId material, int size, float tile, Color color)
        {
            _material = material;
            _size = size;
            _tile = tile;
            _color = color;
        }

        /// <summary>
        ///     Tiles the picture across <paramref name="bounds" /> on a grid starting at <paramref name="origin" />, so
        ///     rectangles laid from one origin share one unbroken pattern; tiles crossing the edges of the bounds are
        ///     cut to fit.
        /// </summary>
        /// <remarks>
        /// One sprite per tile the bounds reach. Where the graphics draws part of a picture, as the screen does, a cut tile draws
        /// only its part and nothing is clipped; where it draws every picture whole, as a text panel does, whole tiles are laid
        /// inside a clip of the bounds.
        /// </remarks>
        public void Draw(IGraphics g, RectangleF bounds, Vector2 origin)
        {
            if (_tile <= 0f || bounds.Width <= 0f || bounds.Height <= 0f)
                return;

            var right = bounds.X + bounds.Width;
            var bottom = bounds.Y + bounds.Height;
            var left = Start(bounds.X, origin.X);
            var top = Start(bounds.Y, origin.Y);

            if (!g.SupportsSourceRectangles)
            {
                g.PushClip(bounds);
                for (var y = top; y < bottom; y += _tile)
                {
                    for (var x = left; x < right; x += _tile)
                        g.DrawSprite(_material, new RectangleF(x, y, _tile, _tile), _color, 0f);
                }

                g.PopClip();
                return;
            }

            for (var y = top; y < bottom; y += _tile)
            {
                var from = Math.Max(y, bounds.Y);
                var to = Math.Min(y + _tile, bottom);
                for (var x = left; x < right; x += _tile)
                {
                    var start = Math.Max(x, bounds.X);
                    var end = Math.Min(x + _tile, right);
                    var source = new Rectangle(Pixels(start - x), Pixels(from - y), Math.Max(1, Pixels(end - x) - Pixels(start - x)),
                        Math.Max(1, Pixels(to - y) - Pixels(from - y)));
                    g.DrawSprite(_material, new RectangleF(start, from, end - start, to - from), source, _color);
                }
            }
        }

        /// <summary>
        ///     Returns where the first tile reaching <paramref name="edge" /> starts, on the grid laid from
        ///     <paramref name="origin" />.
        /// </summary>
        float Start(float edge, float origin)
        {
            return origin + (float)Math.Floor((edge - origin) / _tile) * _tile;
        }

        /// <summary>
        ///     Returns how many of the picture's pixels a tile of length <paramref name="span" /> covers.
        /// </summary>
        int Pixels(float span)
        {
            return (int)(span / _tile * _size + 0.5f);
        }
    }
}
