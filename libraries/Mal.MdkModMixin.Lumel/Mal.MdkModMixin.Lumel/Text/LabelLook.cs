using Lumel.Styling;

namespace Lumel.Text
{
    /// <summary>
    ///     The look of a label in one state, adding the alignment of its lines to what any text look sets.
    /// </summary>
    public class LabelLook : TextLook
    {
        TextAlignment? _textAlignment;

        /// <summary>
        ///     Gets or sets how the lines are aligned within the width given to the label, or null to inherit it from
        ///     the looks beneath.
        /// </summary>
        public TextAlignment? TextAlignment
        {
            get { return _textAlignment; }
            set
            {
                _textAlignment = value;
                OnChanged();
            }
        }

        /// <inheritdoc />
        public override void Merge(ElementLook over)
        {
            base.Merge(over);
            var look = over as LabelLook;
            if (look == null)
                return;

            _textAlignment = look._textAlignment ?? _textAlignment;
        }

        /// <inheritdoc />
        public override void Clear()
        {
            base.Clear();
            _textAlignment = null;
        }
    }
}
