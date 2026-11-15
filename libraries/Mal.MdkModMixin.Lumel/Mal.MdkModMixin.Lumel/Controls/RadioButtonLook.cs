using Lumel.Styling;
using VRageMath;

namespace Lumel.Controls
{
    /// <summary>The look of a radio button in one state: its text, its ring and the dot in the ring.</summary>
    /// <remarks><see cref="TextLook.Color" /> is the words.</remarks>
    public class RadioButtonLook : TextLook
    {
        Color? _dot;
        float? _markSize;
        Color? _ring;
        Color? _ringFill;
        Thickness? _wordPadding;

        /// <summary>Gets or sets the color of the ring, an eighth of its width thick, or null to use the underlying looks.</summary>
        public Color? Ring
        {
            get { return _ring; }
            set
            {
                _ring = value;
                OnChanged();
            }
        }

        /// <summary>Gets or sets the fill color of the inside of the ring, or null to use the underlying looks.</summary>
        public Color? RingFill
        {
            get { return _ringFill; }
            set
            {
                _ringFill = value;
                OnChanged();
            }
        }

        /// <summary>Gets or sets the color of the dot that shows the button as checked, or null to use the underlying looks.</summary>
        public Color? Dot
        {
            get { return _dot; }
            set
            {
                _dot = value;
                OnChanged();
            }
        }

        /// <summary>
        ///     Gets or sets the width and height of the ring, or null to use the underlying looks, which make it as tall as
        ///     a line of text.
        /// </summary>
        public float? MarkSize
        {
            get { return _markSize; }
            set
            {
                _markSize = value;
                OnChanged();
            }
        }

        /// <summary>Gets or sets the space around the text, or null to use the underlying looks.</summary>
        public Thickness? WordPadding
        {
            get { return _wordPadding; }
            set
            {
                _wordPadding = value;
                OnChanged();
            }
        }

        /// <inheritdoc />
        public override void Merge(ElementLook over)
        {
            base.Merge(over);
            var look = over as RadioButtonLook;
            if (look == null)
                return;

            _ring = look._ring ?? _ring;
            _ringFill = look._ringFill ?? _ringFill;
            _dot = look._dot ?? _dot;
            _markSize = look._markSize ?? _markSize;
            _wordPadding = look._wordPadding ?? _wordPadding;
        }

        /// <inheritdoc />
        public override void Clear()
        {
            base.Clear();
            _ring = null;
            _ringFill = null;
            _dot = null;
            _markSize = null;
            _wordPadding = null;
        }
    }
}
