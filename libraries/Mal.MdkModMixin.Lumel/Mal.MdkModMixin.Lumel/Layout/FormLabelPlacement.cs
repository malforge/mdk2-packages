namespace Lumel.Layout
{
    /// <summary>
    ///     Where a row of a <see cref="Form" /> puts its label: beside its field while both fit, or always above it.
    /// </summary>
    public enum FormLabelPlacement
    {
        /// <summary>
        ///     Label and field side by side if the row has room for both, otherwise the field under the label.
        /// </summary>
        Auto,

        /// <summary>
        ///     The field is always under its label, across the whole width, for text and anything else that needs the
        ///     room.
        /// </summary>
        Above
    }
}
