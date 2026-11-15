using System;
using VRageMath;

namespace Lumel.Drawing
{
    /// <summary>A line of round dots as wide as the line is thick, a dot apart, drawn from one strip of dots.</summary>
    public static class DottedLine
    {
        // The strip's size, from Content/Textures/Sprites/Lumel_Dots.dds. The game clamps a sprite's texture rather than
        // repeating it, so a line draws a slice of a strip long enough for nearly any line, at one sprite.
        const int DotPixels = 8;
        const int DotsInStrip = 128;

        /// <summary>
        ///     Draws dots in <paramref name="color" /> along <paramref name="line" />, as wide as it is tall and ending on a
        ///     whole dot.
        /// </summary>
        /// <remarks>
        ///     One sprite per 128 dots. Where the graphics cannot draw part of a picture, as a text panel cannot, the line is
        ///     drawn solid.
        /// </remarks>
        public static void Draw(IGraphics g, RectangleF line, Color color)
        {
            if (line.Width <= 0f || line.Height <= 0f)
                return;

            if (!g.SupportsSourceRectangles)
            {
                g.Fill(line, color);
                return;
            }

            // Dots and gaps are a thickness each, so a line holding n dots is 2n - 1 thicknesses long.
            var dots = Math.Max(1, (int)Math.Round((line.Width / line.Height + 1f) * 0.5f));
            var perDot = line.Width / (dots * 2 - 1) * 2f;
            var x = line.X;
            while (dots > 0)
            {
                var here = Math.Min(dots, DotsInStrip);
                dots -= here;
                var pixels = here * DotPixels * 2 - (dots == 0 ? DotPixels : 0);
                var wide = dots == 0 ? line.X + line.Width - x : here * perDot;
                g.DrawSprite(LumelMaterials.Dots, new RectangleF(x, line.Y, wide, line.Height), new Rectangle(0, 0, pixels, DotPixels), color);
                x += wide;
            }
        }
    }
}
