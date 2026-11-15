using Lumel.Styling;

namespace Lumel.Layout
{
    /// <summary>
    ///     The look of a flow in one state: the space between its children and lines, on top of what any element look
    ///     sets.
    /// </summary>
    public class FlowLook : ElementLook
    {
        float? _indent;
        float? _lineSpacing;
        float? _spacing;

        /// <summary>
        ///     Gets or sets the blank space between one child and the next along a line, or null to inherit it from the
        ///     looks beneath.
        /// </summary>
        public float? Spacing
        {
            get { return _spacing; }
            set
            {
                _spacing = value;
                OnChanged();
            }
        }

        /// <summary>
        ///     Gets or sets the blank space between one line and the next, or null to inherit it from the looks
        ///     beneath.
        /// </summary>
        public float? LineSpacing
        {
            get { return _lineSpacing; }
            set
            {
                _lineSpacing = value;
                OnChanged();
            }
        }

        /// <summary>
        ///     Gets or sets the blank space before every line after the first, or null to inherit it from the looks
        ///     beneath.
        /// </summary>
        public float? Indent
        {
            get { return _indent; }
            set
            {
                _indent = value;
                OnChanged();
            }
        }

        /// <inheritdoc />
        public override void Merge(ElementLook over)
        {
            base.Merge(over);
            var look = over as FlowLook;
            if (look == null)
                return;

            _spacing = look._spacing ?? _spacing;
            _lineSpacing = look._lineSpacing ?? _lineSpacing;
            _indent = look._indent ?? _indent;
        }

        /// <inheritdoc />
        public override void Clear()
        {
            base.Clear();
            _spacing = null;
            _lineSpacing = null;
            _indent = null;
        }
    }
}
