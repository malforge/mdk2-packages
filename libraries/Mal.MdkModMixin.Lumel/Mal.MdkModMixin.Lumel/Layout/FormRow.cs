namespace Lumel.Layout
{
    /// <summary>
    ///     One row of a <see cref="Form" />: a label (or none, for one element across the whole width), a field, and
    ///     where the label goes.
    /// </summary>
    public sealed class FormRow
    {
        /// <summary>
        ///     Creates a row of <paramref name="label" /> (or null) and <paramref name="field" />, with the label
        ///     placed as <paramref name="labelPlacement" /> says.
        /// </summary>
        public FormRow(Element label, Element field, FormLabelPlacement labelPlacement)
        {
            Label = label;
            Field = field;
            LabelPlacement = labelPlacement;
        }

        /// <summary>Gets the element that names the row, or null if the field stands alone.</summary>
        public Element Label { get; }

        /// <summary>Gets the element where the row's value is given.</summary>
        public Element Field { get; }

        /// <summary>Gets where the label goes, in particular whether it may sit beside the field.</summary>
        public FormLabelPlacement LabelPlacement { get; }
    }
}
