using Lumel.Drawing;
using VRageMath;

namespace Lumel.Images
{
    /// <summary>An image that reports its size and draws itself.</summary>
    /// <remarks>Never changes once made, so one can be kept and shared for as long as the picture exists.</remarks>
    public interface IImageSource
    {
        /// <summary>
        ///     Gets the size the image was made at, which gives its aspect ratio; zero when unknown, in which case it
        ///     is treated as square.
        /// </summary>
        Vector2 Size { get; }

        /// <summary>
        ///     Draws the image into <paramref name="destination" />, multiplied by <paramref name="tint" /> and rotated
        ///     clockwise about its center by <paramref name="rotation" /> radians; a white tint draws it unchanged.
        /// </summary>
        void Draw(IGraphics g, RectangleF destination, Color tint, float rotation);
    }
}
