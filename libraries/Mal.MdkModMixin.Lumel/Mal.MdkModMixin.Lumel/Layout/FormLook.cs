using Lumel.Styling;

namespace Lumel.Layout
{
    /// <summary>
    ///     The look of a form in one state: the space between its labels, fields and rows, on top of what any element
    ///     look sets.
    /// </summary>
    public class FormLook : ElementLook
    {
        float? _columnSpacing;
        float? _labelSpacing;
        float? _rowSpacing;

        /// <summary>
        ///     Gets or sets the blank space between a label and its field when they sit side by side, or null to
        ///     inherit it from the looks beneath.
        /// </summary>
        public float? ColumnSpacing
        {
            get { return _columnSpacing; }
            set
            {
                _columnSpacing = value;
                OnChanged();
            }
        }

        /// <summary>
        ///     Gets or sets the blank space between one row and the next, or null to inherit it from the looks beneath.
        /// </summary>
        public float? RowSpacing
        {
            get { return _rowSpacing; }
            set
            {
                _rowSpacing = value;
                OnChanged();
            }
        }

        /// <summary>
        ///     Gets or sets the blank space between a label and the field under it, or null to inherit it from the
        ///     looks beneath.
        /// </summary>
        public float? LabelSpacing
        {
            get { return _labelSpacing; }
            set
            {
                _labelSpacing = value;
                OnChanged();
            }
        }

        /// <inheritdoc />
        public override void Merge(ElementLook over)
        {
            base.Merge(over);
            var look = over as FormLook;
            if (look == null)
                return;

            _columnSpacing = look._columnSpacing ?? _columnSpacing;
            _rowSpacing = look._rowSpacing ?? _rowSpacing;
            _labelSpacing = look._labelSpacing ?? _labelSpacing;
        }

        /// <inheritdoc />
        public override void Clear()
        {
            base.Clear();
            _columnSpacing = null;
            _rowSpacing = null;
            _labelSpacing = null;
        }
    }
}
