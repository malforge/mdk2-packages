using Lumel.Styling;
using Lumel.Windows;
using VRageMath;

namespace Lumel.Controls
{
    /// <summary>
    ///     The look of a color picker in one state, in addition to what any element has: the color it shows and the edge
    ///     around it, the arrow at its right, and the styles of the popup it opens and of the Done button in it.
    /// </summary>
    public class ColorPickerLook : ElementLook
    {
        Vector2? _sampleSize;
        Color? _sampleEdge;
        Color? _arrow;
        float? _arrowSize;
        float? _arrowSpacing;
        IStyle<Popup> _popupStyle;
        IStyle<Button> _doneStyle;

        /// <summary>Gets or sets the width and height of the color shown, in UI units, or null to use the underlying looks.</summary>
        public Vector2? SampleSize
        {
            get { return _sampleSize; }
            set
            {
                _sampleSize = value;
                OnChanged();
            }
        }

        /// <summary>
        ///     Gets or sets the color of the hairline around the color shown, or null to use the underlying looks; transparent
        ///     draws none.
        /// </summary>
        public Color? SampleEdge
        {
            get { return _sampleEdge; }
            set
            {
                _sampleEdge = value;
                OnChanged();
            }
        }

        /// <summary>Gets or sets the color of the arrow, or null to use the underlying looks.</summary>
        public Color? Arrow
        {
            get { return _arrow; }
            set
            {
                _arrow = value;
                OnChanged();
            }
        }

        /// <summary>Gets or sets the width and height of the arrow, in UI units, or null to use the underlying looks.</summary>
        public float? ArrowSize
        {
            get { return _arrowSize; }
            set
            {
                _arrowSize = value;
                OnChanged();
            }
        }

        /// <summary>
        ///     Gets or sets the least space between the color shown and the arrow, in UI units, or null to use the underlying
        ///     looks.
        /// </summary>
        public float? ArrowSpacing
        {
            get { return _arrowSpacing; }
            set
            {
                _arrowSpacing = value;
                OnChanged();
            }
        }

        /// <summary>
        ///     Gets or sets the style of the popup the picker opens, or null to use the underlying looks, which leave it to
        ///     the host theme's popup style.
        /// </summary>
        public IStyle<Popup> PopupStyle
        {
            get { return _popupStyle; }
            set
            {
                _popupStyle = value;
                OnChanged();
            }
        }

        /// <summary>
        ///     Gets or sets the style of the Done button under the panel, or null to use the underlying looks, which leave it
        ///     to the host theme's button style.
        /// </summary>
        public IStyle<Button> DoneStyle
        {
            get { return _doneStyle; }
            set
            {
                _doneStyle = value;
                OnChanged();
            }
        }

        /// <inheritdoc />
        public override void Merge(ElementLook over)
        {
            base.Merge(over);
            var look = over as ColorPickerLook;
            if (look == null)
                return;

            _sampleSize = look._sampleSize ?? _sampleSize;
            _sampleEdge = look._sampleEdge ?? _sampleEdge;
            _arrow = look._arrow ?? _arrow;
            _arrowSize = look._arrowSize ?? _arrowSize;
            _arrowSpacing = look._arrowSpacing ?? _arrowSpacing;
            _popupStyle = look._popupStyle ?? _popupStyle;
            _doneStyle = look._doneStyle ?? _doneStyle;
        }

        /// <inheritdoc />
        public override void Clear()
        {
            base.Clear();
            _sampleSize = null;
            _sampleEdge = null;
            _arrow = null;
            _arrowSize = null;
            _arrowSpacing = null;
            _popupStyle = null;
            _doneStyle = null;
        }
    }
}
