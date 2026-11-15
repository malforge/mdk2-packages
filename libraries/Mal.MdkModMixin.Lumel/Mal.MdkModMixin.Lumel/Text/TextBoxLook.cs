using Lumel.Styling;

namespace Lumel.Text
{
    /// <summary>
    ///     The look of a text box in one state, adding the alignment of its line to what any editable text has.
    /// </summary>
    public class TextBoxLook : EditableTextLook
    {
        TextAlignment? _textAlignment;

        /// <summary>
        ///     How the line is aligned horizontally when it is shorter than the box, or null to inherit it from the
        ///     looks beneath.
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
            var look = over as TextBoxLook;
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
