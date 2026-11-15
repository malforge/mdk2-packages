using Lumel.Menus;
using Lumel.Styling;
using VRageMath;

namespace Lumel.Controls
{
    /// <summary>
    ///     The look of a drop-down in one state: its words, the hint it shows with nothing chosen, the arrow at its right
    ///     and the style of the list it opens.
    /// </summary>
    /// <remarks><see cref="TextLook.Color" /> is the chosen item's words.</remarks>
    public class DropDownLook : TextLook
    {
        Color? _arrow;
        float? _arrowSize;
        float? _arrowSpacing;
        Color? _hintColor;
        IStyle<Menu> _listStyle;
        IStyle<SearchBox> _searchStyle;

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
        ///     Gets or sets the least space between the words and the arrow, in UI units, or null to use the underlying
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

        /// <summary>Gets or sets the color of the hint, or null to use the underlying looks.</summary>
        public Color? HintColor
        {
            get { return _hintColor; }
            set
            {
                _hintColor = value;
                OnChanged();
            }
        }

        /// <summary>
        ///     Gets or sets the style of the list the drop-down opens, or null to use the underlying looks, which leave it
        ///     to the host theme's menu style.
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

        /// <summary>
        ///     Gets or sets the style of the search box in the list, or null to use the underlying looks, which leave it to the
        ///     host theme's search box style.
        /// </summary>
        public IStyle<SearchBox> SearchStyle
        {
            get { return _searchStyle; }
            set
            {
                _searchStyle = value;
                OnChanged();
            }
        }

        /// <inheritdoc />
        public override void Merge(ElementLook over)
        {
            base.Merge(over);
            var look = over as DropDownLook;
            if (look == null)
                return;

            _arrow = look._arrow ?? _arrow;
            _arrowSize = look._arrowSize ?? _arrowSize;
            _arrowSpacing = look._arrowSpacing ?? _arrowSpacing;
            _hintColor = look._hintColor ?? _hintColor;
            _listStyle = look._listStyle ?? _listStyle;
            _searchStyle = look._searchStyle ?? _searchStyle;
        }

        /// <inheritdoc />
        public override void Clear()
        {
            base.Clear();
            _arrow = null;
            _arrowSize = null;
            _arrowSpacing = null;
            _hintColor = null;
            _listStyle = null;
            _searchStyle = null;
        }
    }
}
