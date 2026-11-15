using System;
using VRageMath;

namespace Lumel.Drawing
{
    /// <summary>
    ///     Measures the contrast between two colors using the contrast ratio of the Web Content Accessibility
    ///     Guidelines.
    /// </summary>
    /// <remarks>
    /// The ratio runs from 1, two colors alike, to 21, black on white. Text wants 4.5 against what is behind it, and anything
    /// else, or large text, 3.
    /// </remarks>
    public static class Contrast
    {
        /// <summary>The minimum contrast ratio for readable text.</summary>
        public const float MinimumTextRatio = 4.5f;

        /// <summary>The minimum contrast ratio for anything that is not text, and for large text.</summary>
        public const float MinimumNonTextRatio = 3f;

        /// <summary>
        ///     Returns the contrast ratio between <paramref name="first" /> and <paramref name="second" />, from 1 to
        ///     21.
        /// </summary>
        public static float Ratio(Color first, Color second)
        {
            var one = Luminance(first);
            var other = Luminance(second);
            return (Math.Max(one, other) + 0.05f) / (Math.Min(one, other) + 0.05f);
        }

        /// <summary>
        ///     Returns <paramref name="color" /> moved toward <paramref name="toward" /> by the least amount that gives
        ///     it a contrast ratio of <paramref name="ratio" /> against <paramref name="ground" />, or
        ///     <paramref name="toward" /> itself if nothing shorter does.
        /// </summary>
        public static Color EnsureContrast(Color color, Color ground, Color toward, float ratio)
        {
            for (var step = 0; step <= 10; step++)
            {
                var moved = Color.Lerp(color, toward, step / 10f);
                if (Ratio(moved, ground) >= ratio)
                    return moved;
            }

            return toward;
        }

        /// <summary>Returns how bright <paramref name="color" /> appears, from 0 for black to 1 for white.</summary>
        static float Luminance(Color color)
        {
            return 0.2126f * Linear(color.R) + 0.7152f * Linear(color.G) + 0.0722f * Linear(color.B);
        }

        static float Linear(byte channel)
        {
            var value = channel / 255f;
            return value <= 0.04045f ? value / 12.92f : (float)Math.Pow((value + 0.055f) / 1.055f, 2.4f);
        }
    }
}
