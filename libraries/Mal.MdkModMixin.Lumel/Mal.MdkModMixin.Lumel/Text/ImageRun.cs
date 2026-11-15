namespace Lumel.Text
{
    /// <summary>A run that shows an image, with its description as its text.</summary>
    /// <remarks>Where the picture is not to be found, the description is said in its place.</remarks>
    public sealed class ImageRun : Run
    {
        /// <summary>
        ///     Creates an image run that shows <paramref name="picture" />, is described by <paramref name="words" />,
        ///     is formatted as <paramref name="kind" />, and belongs to <paramref name="link" /> if it is given.
        /// </summary>
        public ImageRun(Picture picture, string words, RunKind kind, Link link = null)
            : base(words, kind)
        {
            Picture = picture;
            Link = link;
        }

        /// <summary>Gets the picture the image shows.</summary>
        public Picture Picture { get; }

        /// <summary>Gets the link the image is inside, whose words' color it is drawn in, or null if it is in none.</summary>
        public override Link Link { get; }
    }
}
