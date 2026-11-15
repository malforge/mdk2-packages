using System;
using System.Collections.Generic;
using VRage.Utils;
using VRageMath;

namespace Lumel.Images
{
    /// <summary>
    ///     An <see cref="IImageCatalog" /> a host can use that reads every locator as the name of a game material and
    ///     caches what it finds.
    /// </summary>
    /// <remarks>Each locator's answer is kept for the life of the resolver, unknown ones included.</remarks>
    public class MaterialImages : IImageCatalog
    {
        readonly Func<MyStringId, Vector2> _textureSize;
        readonly Dictionary<string, IImageSource> _found = new Dictionary<string, IImageSource>();

        /// <summary>
        ///     Creates a catalog of the game's materials whose pixel sizes come from <paramref name="textureSize" />,
        ///     which returns zero for a material the host cannot draw.
        /// </summary>
        /// <remarks>On the screen space that is <c>screen.TextureSize</c>; a text panel measures through the game's screen space, as <c>new GameScreenSpace().TextureSize</c>.</remarks>
        public MaterialImages(Func<MyStringId, Vector2> textureSize)
        {
            if (textureSize == null)
                throw new ArgumentNullException(nameof(textureSize));

            _textureSize = textureSize;
        }

        /// <inheritdoc />
        public IImageSource Find(string locator)
        {
            if (string.IsNullOrEmpty(locator))
                return null;

            IImageSource image;
            if (_found.TryGetValue(locator, out image))
                return image;

            var material = MyStringId.GetOrCompute(locator);
            var size = _textureSize(material);
            image = size.X > 0f && size.Y > 0f ? new Sprite(material, size) : null;
            _found.Add(locator, image);
            return image;
        }
    }
}
