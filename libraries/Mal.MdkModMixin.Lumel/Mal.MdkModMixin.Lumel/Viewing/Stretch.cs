namespace Lumel.Viewing
{
    /// <summary>How a <see cref="Viewbox" /> scales its content to its space, always keeping the content's aspect ratio.</summary>
    public enum Stretch
    {
        /// <summary>
        ///     The content is scaled until it meets the space on one axis, and placed in the room left over by its
        ///     alignment.
        /// </summary>
        Fit,

        /// <summary>The content is scaled until it covers the space on both axes, and what overflows is clipped.</summary>
        Fill,

        /// <summary>
        ///     The content is scaled as by <see cref="Fit" />, then laid out again across the whole space at that scale, so it
        ///     grows into the room left over.
        /// </summary>
        FitAndExpand,

        /// <summary>
        ///     The content is scaled as by <see cref="Fill" />, then laid out again in the space at that scale, so it shrinks
        ///     on the axis it would overflow; what still overflows is clipped.
        /// </summary>
        FillAndContract
    }
}
