using VRageMath;
using Lumel.Styling;

namespace Lumel.Text
{
    /// <summary>
    ///     The look of editable text in one state, adding the selection, caret and hint to what any text look sets.
    /// </summary>
    public class EditableTextLook : TextLook
    {
        Color? _caretColor;
        float? _caretWidth;
        Color? _hintColor;
        string _hintFont;
        Color? _selectionColor;

        /// <summary>
        ///     Gets or sets the color painted behind the selection, or null to inherit it from the looks beneath.
        /// </summary>
        public Color? SelectionColor
        {
            get { return _selectionColor; }
            set
            {
                _selectionColor = value;
                OnChanged();
            }
        }

        /// <summary>Gets or sets the caret color, or null to inherit it from the looks beneath.</summary>
        public Color? CaretColor
        {
            get { return _caretColor; }
            set
            {
                _caretColor = value;
                OnChanged();
            }
        }

        /// <summary>
        ///     Gets or sets the width the caret is drawn at, or null to inherit it from the looks beneath.
        /// </summary>
        public float? CaretWidth
        {
            get { return _caretWidth; }
            set
            {
                _caretWidth = value;
                OnChanged();
            }
        }

        /// <summary>
        ///     Gets or sets the color of the hint shown while the box is empty, or null to inherit it from the looks
        ///     beneath.
        /// </summary>
        public Color? HintColor
        {
            get { return _hintColor; }
            set
            {
                _hintColor = value;
                OnChanged();
            }
        }

        /// <summary>Gets or sets the font of the hint, or null to inherit it from the looks beneath.</summary>
        public string HintFont
        {
            get { return _hintFont; }
            set
            {
                _hintFont = value;
                OnChanged();
            }
        }

        /// <inheritdoc />
        public override void Merge(ElementLook over)
        {
            base.Merge(over);
            var look = over as EditableTextLook;
            if (look == null)
                return;

            _selectionColor = look._selectionColor ?? _selectionColor;
            _caretColor = look._caretColor ?? _caretColor;
            _caretWidth = look._caretWidth ?? _caretWidth;
            _hintColor = look._hintColor ?? _hintColor;
            _hintFont = look._hintFont ?? _hintFont;
        }

        /// <inheritdoc />
        public override void Clear()
        {
            base.Clear();
            _selectionColor = null;
            _caretColor = null;
            _caretWidth = null;
            _hintColor = null;
            _hintFont = null;
        }
    }
}
