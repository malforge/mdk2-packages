using Lumel.Styling;

namespace Lumel.Layout
{
    /// <summary>
    ///     The look of a grid in one state: the space between its columns and rows, on top of what any element look
    ///     sets.
    /// </summary>
    public class GridLook : ElementLook
    {
        float? _columnSpacing;
        float? _rowSpacing;

        /// <summary>
        ///     Gets or sets the blank space between one column and the next, or null to inherit it from the looks
        ///     beneath.
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

        /// <inheritdoc />
        public override void Merge(ElementLook over)
        {
            base.Merge(over);
            var look = over as GridLook;
            if (look == null)
                return;

            _columnSpacing = look._columnSpacing ?? _columnSpacing;
            _rowSpacing = look._rowSpacing ?? _rowSpacing;
        }

        /// <inheritdoc />
        public override void Clear()
        {
            base.Clear();
            _columnSpacing = null;
            _rowSpacing = null;
        }
    }
}
