namespace Lumel.Windows
{
    /// <summary>The side of its anchor on which a popup opens.</summary>
    public enum PopupSide
    {
        /// <summary>
        ///     Below the anchor if all of the popup fits there, otherwise above if all of it fits, otherwise on the
        ///     roomier side, cut to fit.
        /// </summary>
        Bottom,

        /// <summary>
        ///     To the right of the anchor with the first entry level with it, or to the left if there is no room on the
        ///     right.
        /// </summary>
        Right
    }
}
