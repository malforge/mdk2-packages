using Lumel.Styling;

namespace Lumel.Images
{
    /// <summary>The style for an image: its box and its tint.</summary>
    public class ImageStyle : Style<Image, ImageLook>
    {
        /// <summary>Creates a style that sets nothing, leaving the image at its neutral values.</summary>
        public ImageStyle()
        {
        }

        /// <summary>
        ///     Creates a style that includes everything in <paramref name="basedOn" />, with whatever this style sets
        ///     applied on top.
        /// </summary>
        public ImageStyle(IStyle<Image> basedOn) : base(basedOn)
        {
        }
    }
}
