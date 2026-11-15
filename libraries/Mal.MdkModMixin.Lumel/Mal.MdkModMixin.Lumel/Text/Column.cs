namespace Lumel.Text
{
    /// <summary>
    ///     One column of a table, as given by its delimiter cell: <c>:---</c>, <c>:---:</c> or <c>---:</c> for where
    ///     its cells sit, and a star for each share it takes of the room the unstarred columns leave.
    /// </summary>
    public struct Column
    {
        /// <summary>
        ///     Creates a column whose cells are aligned by <paramref name="textAlignment" /> and which takes
        ///     <paramref name="weight" /> shares of the room the unstarred columns leave.
        /// </summary>
        public Column(TextAlignment textAlignment, int weight)
        {
            TextAlignment = textAlignment;
            Weight = weight;
        }

        /// <summary>Gets how the column's cells are aligned across it.</summary>
        public TextAlignment TextAlignment { get; }

        /// <summary>
        ///     Gets how many shares of the room left by the unstarred columns this column takes, or 0 for a column as
        ///     wide as its widest cell.
        /// </summary>
        public int Weight { get; }
    }
}
