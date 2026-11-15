using Lumel.Drawing;
using VRage.Utils;
using VRageMath;

namespace Lumel.Images
{
    /// <summary>An image the game draws from a material.</summary>
    public sealed class Sprite : IImageSource
    {
        /// <summary>
        ///     Creates an image that draws <paramref name="material" /> at <paramref name="size" />, where a zero size
        ///     means the size is unknown.
        /// </summary>
        public Sprite(MyStringId material, Vector2 size)
        {
            Material = material;
            Size = size;
        }

        /// <summary>Gets the material the image is drawn from.</summary>
        public MyStringId Material { get; }

        /// <inheritdoc />
        public Vector2 Size { get; }

        /// <inheritdoc />
        public void Draw(IGraphics g, RectangleF destination, Color tint, float rotation)
        {
            g.DrawSprite(Material, destination, tint, rotation);
        }
    }
}
