using VRageMath;
using Lumel.Styling;

namespace Lumel.Text
{
    /// <summary>
    ///     The look of a text area in one state, adding line numbers and scroll bars to what any editable text has.
    /// </summary>
    public class TextAreaLook : EditableTextLook
    {
        float? _lineBreakSelectionWidth;
        Color? _lineNumberColor;
        Thickness? _lineNumbersPadding;
        float? _minThumbLength;
        float? _scrollBarWidth;
        float? _scrollBarGap;
        Color? _thumb;

        /// <summary>The color of the line numbers, or null to inherit it from the looks beneath.</summary>
        public Color? LineNumberColor
        {
            get { return _lineNumberColor; }
            set
            {
                _lineNumberColor = value;
                OnChanged();
            }
        }

        /// <summary>
        ///     The padding on either side of the line numbers, with the right side between the numbers and the text, or
        ///     null to inherit it from the looks beneath.
        /// </summary>
        public Thickness? LineNumbersPadding
        {
            get { return _lineNumbersPadding; }
            set
            {
                _lineNumbersPadding = value;
                OnChanged();
            }
        }

        /// <summary>
        ///     How far the selection extends past the end of a line that continues onto the next, or null to inherit it
        ///     from the looks beneath.
        /// </summary>
        public float? LineBreakSelectionWidth
        {
            get { return _lineBreakSelectionWidth; }
            set
            {
                _lineBreakSelectionWidth = value;
                OnChanged();
            }
        }

        /// <summary>
        ///     The thickness of the scroll bars (0 for none), or null to inherit it from the looks beneath.
        /// </summary>
        public float? ScrollBarWidth
        {
            get { return _scrollBarWidth; }
            set
            {
                _scrollBarWidth = value;
                OnChanged();
            }
        }

        /// <summary>The space between the text and the scroll bars, or null to inherit it from the looks beneath.</summary>
        public float? ScrollBarGap
        {
            get { return _scrollBarGap; }
            set
            {
                _scrollBarGap = value;
                OnChanged();
            }
        }

        /// <summary>The color of the scroll bar thumbs, or null to inherit it from the looks beneath.</summary>
        public Color? Thumb
        {
            get { return _thumb; }
            set
            {
                _thumb = value;
                OnChanged();
            }
        }

        /// <summary>The minimum length of a thumb, or null to inherit it from the looks beneath.</summary>
        public float? MinThumbLength
        {
            get { return _minThumbLength; }
            set
            {
                _minThumbLength = value;
                OnChanged();
            }
        }

        /// <inheritdoc />
        public override void Merge(ElementLook over)
        {
            base.Merge(over);
            var look = over as TextAreaLook;
            if (look == null)
                return;

            _lineNumberColor = look._lineNumberColor ?? _lineNumberColor;
            _lineNumbersPadding = look._lineNumbersPadding ?? _lineNumbersPadding;
            _lineBreakSelectionWidth = look._lineBreakSelectionWidth ?? _lineBreakSelectionWidth;
            _scrollBarWidth = look._scrollBarWidth ?? _scrollBarWidth;
            _scrollBarGap = look._scrollBarGap ?? _scrollBarGap;
            _thumb = look._thumb ?? _thumb;
            _minThumbLength = look._minThumbLength ?? _minThumbLength;
        }

        /// <inheritdoc />
        public override void Clear()
        {
            base.Clear();
            _lineNumberColor = null;
            _lineNumbersPadding = null;
            _lineBreakSelectionWidth = null;
            _scrollBarWidth = null;
            _scrollBarGap = null;
            _thumb = null;
            _minThumbLength = null;
        }
    }
}
