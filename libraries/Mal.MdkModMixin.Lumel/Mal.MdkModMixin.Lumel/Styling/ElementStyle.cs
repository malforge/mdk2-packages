namespace Lumel.Styling
{
    /// <summary>
    ///     How any element looks: its background, pattern, border and padding, at rest, on hover and when disabled.
    /// </summary>
    public class ElementStyle : Style<Element, ElementLook>
    {
        /// <summary>Creates a style that sets nothing, leaving its element at its neutral values.</summary>
        public ElementStyle()
        {
        }

        /// <summary>
        ///     Creates a style that includes everything <paramref name="basedOn" /> sets, with this style's own
        ///     settings laid over it.
        /// </summary>
        public ElementStyle(IStyle<Element> basedOn) : base(basedOn)
        {
        }
    }
}
