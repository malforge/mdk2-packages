using VRageMath;

namespace Lumel.Images
{
    /// <summary>Computes one side of a picture from the other, keeping the shape it was made in.</summary>
    public static class AspectRatio
    {
        /// <summary>
        ///     Returns the height of a picture of the given <paramref name="size" /> when it is
        ///     <paramref name="width" /> wide, or a height equal to the width if its size is not known.
        /// </summary>
        public static float HeightFor(Vector2 size, float width)
        {
            return size.X > 0f && size.Y > 0f ? width * size.Y / size.X : width;
        }

        /// <summary>
        ///     Returns the width of a picture of the given <paramref name="size" /> when it is
        ///     <paramref name="height" /> tall, or a width equal to the height if its size is not known.
        /// </summary>
        public static float WidthFor(Vector2 size, float height)
        {
            return size.X > 0f && size.Y > 0f ? height * size.X / size.Y : height;
        }
    }
}
