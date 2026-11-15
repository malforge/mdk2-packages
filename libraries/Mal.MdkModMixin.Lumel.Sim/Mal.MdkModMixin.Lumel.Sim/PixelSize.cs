
namespace Lumel.Sim
{
    /// <summary>
    /// A size in whole pixels.
    /// </summary>
    /// <remarks>
    /// Whole numbers, because this is the resolution a frame is built at and half a pixel of screen is
    /// not a thing the game has. The desktop framework's own sizes are all doubles.
    /// </remarks>
    public struct PixelSize
    {
        /// <summary>Creates a size of <paramref name="width" /> by <paramref name="height" /> pixels.</summary>
        public PixelSize(int width, int height)
        {
            Width = width;
            Height = height;
        }

        /// <summary>Gets the width in pixels.</summary>
        public int Width { get; private set; }

        /// <summary>Gets the height in pixels.</summary>
        public int Height { get; private set; }
    }
}
