namespace Lumel.Text
{
    /// <summary>
    ///     The image shown in a document: its locator, the size it is written to be drawn at, and its rotation.
    /// </summary>
    public sealed class Picture
    {
        /// <summary>
        ///     Creates a picture found by <paramref name="locator" />, drawn <paramref name="width" /> wide and
        ///     <paramref name="height" /> tall when those are numbers, and rotated <paramref name="rotation" /> degrees
        ///     clockwise.
        /// </summary>
        public Picture(string locator, float width, float height, float rotation)
        {
            Locator = locator;
            Width = width;
            Height = height;
            Rotation = rotation;
        }

        /// <summary>Gets the locator as written; the host's resolver decides what it leads to.</summary>
        public string Locator { get; }

        /// <summary>Gets the width it is drawn at, or float.NaN if none is written.</summary>
        public float Width { get; }

        /// <summary>Gets the height it is drawn at, or float.NaN if none is written.</summary>
        public float Height { get; }

        /// <summary>Gets the clockwise rotation in degrees as written, or 0 if no rotation is written.</summary>
        public float Rotation { get; }
    }
}
