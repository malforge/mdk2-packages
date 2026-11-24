using Lumel.Menus;
using Lumel.Styling;
using Lumel.Text;
using VRageMath;

namespace Lumel.Controls
{
    /// <summary>
    ///     The look of a combo box in one state: its box, the style of the field typed into, the arrow at its right and the
    ///     style of the list it opens.
    /// </summary>
    public class ComboBoxLook : ElementLook
    {
        Color? _arrow;
        float? _arrowSize;
        float? _arrowSpacing;
        IStyle<TextBox> _fieldStyle;
        IStyle<Menu> _listStyle;

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

        /// <summary>Gets or sets the space between the field and the arrow, in UI units, or null to use the underlying looks.</summary>
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
        ///     Gets or sets the style of the field typed into, or null to use the underlying looks, which leave it to the host
        ///     theme's text box style.
        /// </summary>
        public IStyle<TextBox> FieldStyle
        {
            get { return _fieldStyle; }
            set
            {
                _fieldStyle = value;
                OnChanged();
            }
        }

        /// <summary>
        ///     Gets or sets the style of the list the combo box opens, or null to use the underlying looks, which leave it to
        ///     the host theme's menu style.
        /// </summary>
        public IStyle<Menu> ListStyle
        {
            get { return _listStyle; }
            set
            {
                _listStyle = value;
                OnChanged();
            }
        }

        /// <inheritdoc />
        public override void Merge(ElementLook over)
        {
            base.Merge(over);
            var look = over as ComboBoxLook;
            if (look == null)
                return;

            _arrow = look._arrow ?? _arrow;
            _arrowSize = look._arrowSize ?? _arrowSize;
            _arrowSpacing = look._arrowSpacing ?? _arrowSpacing;
            _fieldStyle = look._fieldStyle ?? _fieldStyle;
            _listStyle = look._listStyle ?? _listStyle;
        }

        /// <inheritdoc />
        public override void Clear()
        {
            base.Clear();
            _arrow = null;
            _arrowSize = null;
            _arrowSpacing = null;
            _fieldStyle = null;
            _listStyle = null;
        }
    }
}
