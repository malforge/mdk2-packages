using Lumel.Images;
using Lumel.Styling;

namespace Lumel
{
    /// <summary>
    ///     The look of a codex tip in one state, adding its tallest height, the looks of its steps and how long it stays
    ///     open after the pointer leaves to what any tip look sets.
    /// </summary>
    public class CodexTipLook : TipLook
    {
        string _backSymbol;
        int? _closeDelay;
        int? _replaceDelay;
        float? _stagger;
        string _forwardSymbol;
        float? _maxHeight;
        float? _spacing;
        IStyle<Glyph> _stepStyle;

        /// <summary>
        ///     Gets or sets the maximum height of the tip in UI units, or null to use the underlying looks.
        /// </summary>
        public float? MaxHeight
        {
            get { return _maxHeight; }
            set
            {
                _maxHeight = value;
                OnChanged();
            }
        }

        /// <summary>
        ///     Gets or sets the space between the row of steps, the page's text and the pin hint, or null to use the
        ///     underlying looks.
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
        ///     Gets or sets the style of the back and forward steps, or null to use the underlying looks.
        /// </summary>
        public IStyle<Glyph> StepStyle
        {
            get { return _stepStyle; }
            set
            {
                _stepStyle = value;
                OnChanged();
            }
        }

        /// <summary>
        ///     Gets or sets the symbol of the step back to the previous page, or null to use the underlying looks.
        /// </summary>
        public string BackSymbol
        {
            get { return _backSymbol; }
            set
            {
                _backSymbol = value;
                OnChanged();
            }
        }

        /// <summary>
        ///     Gets or sets the symbol of the step forward to the page that was stepped back from, or null to use the
        ///     underlying looks.
        /// </summary>
        public string ForwardSymbol
        {
            get { return _forwardSymbol; }
            set
            {
                _forwardSymbol = value;
                OnChanged();
            }
        }

        /// <summary>
        ///     Gets or sets how far a tip beside another stands along the edge they share from that tip's corner, or null to
        ///     inherit it from the looks beneath.
        /// </summary>
        public float? Stagger
        {
            get { return _stagger; }
            set
            {
                _stagger = value;
                OnChanged();
            }
        }

        /// <summary>
        ///     Gets or sets how long the tip stays open after the pointer leaves both it and its element, in frames, or null
        ///     to use the underlying looks.
        /// </summary>
        public int? CloseDelay
        {
            get { return _closeDelay; }
            set
            {
                _closeDelay = value;
                OnChanged();
            }
        }

        /// <summary>
        ///     Gets or sets how long the pointer rests on a link before its page replaces the one open beside the tip, in
        ///     frames, or null to use the underlying looks, which leave it to <see cref="TipLook.ShowDelay" />.
        /// </summary>
        public int? ReplaceDelay
        {
            get { return _replaceDelay; }
            set
            {
                _replaceDelay = value;
                OnChanged();
            }
        }

        /// <inheritdoc />
        public override void Merge(ElementLook over)
        {
            base.Merge(over);
            var look = over as CodexTipLook;
            if (look == null)
                return;

            _maxHeight = look._maxHeight ?? _maxHeight;
            _spacing = look._spacing ?? _spacing;
            _stepStyle = look._stepStyle ?? _stepStyle;
            _backSymbol = look._backSymbol ?? _backSymbol;
            _forwardSymbol = look._forwardSymbol ?? _forwardSymbol;
            _closeDelay = look._closeDelay ?? _closeDelay;
            _replaceDelay = look._replaceDelay ?? _replaceDelay;
            _stagger = look._stagger ?? _stagger;
        }

        /// <inheritdoc />
        public override void Clear()
        {
            base.Clear();
            _maxHeight = null;
            _spacing = null;
            _stepStyle = null;
            _backSymbol = null;
            _forwardSymbol = null;
            _closeDelay = null;
            _replaceDelay = null;
            _stagger = null;
        }
    }
}
