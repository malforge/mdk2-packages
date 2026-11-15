namespace Lumel.Controls
{
    /// <summary>
    ///     Describes a row of a <see cref="ReorderList" /> that was dropped in a new place: where it came from, where
    ///     it went and whether it was indented into a group.
    /// </summary>
    public sealed class ReorderedEventArgs
    {
        /// <summary>
        ///     Creates a description of a row moved from index <paramref name="from" /> to <paramref name="to" />,
        ///     counted among the rows not dragged, and indented if <paramref name="isIndented" /> is true.
        /// </summary>
        public ReorderedEventArgs(int from, int to, bool isIndented)
        {
            From = from;
            To = to;
            IsIndented = isIndented;
        }

        /// <summary>Gets the index in the list the row came from.</summary>
        public int From { get; }

        /// <summary>Gets how many of the rows that were not dragged are above the row after the drop.</summary>
        public int To { get; }

        /// <summary>Whether the row was indented into the group it landed at the end of.</summary>
        public bool IsIndented { get; }
    }
}
