using VRageMath;

namespace Lumel.Drawing
{
    /// <summary>Arrow heads drawn as two bars, as on a control that opens a list or a panel.</summary>
    public static class Chevrons
    {
        /// <summary>
        ///     Draws an arrow head pointing down, <paramref name="size" /> wide and high, centered on
        ///     <paramref name="center" />, in <paramref name="color" />.
        /// </summary>
        /// <remarks>Its bars are a quarter of its size thick, and at least one unit.</remarks>
        public static void Down(IGraphics g, Vector2 center, float size, Color color)
        {
            var stroke = MathHelper.Max(1f, size * 0.25f);
            var length = size * 0.7071f + stroke * 0.5f;
            var y = center.Y - stroke * 0.5f;

            g.Fill(new RectangleF(center.X - size * 0.25f - length * 0.5f, y, length, stroke), color, MathHelper.PiOver4);
            g.Fill(new RectangleF(center.X + size * 0.25f - length * 0.5f, y, length, stroke), color, -MathHelper.PiOver4);
        }
    }
}
