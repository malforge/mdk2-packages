using Lumel.Styling;
using VRageMath;

namespace Lumel.Menus
{
    /// <summary>
    ///     The look of a <see cref="MenuItem" /> in one state, adding its shortcut's words, its chosen mark and the arrow
    ///     of an item that opens a submenu to what any text look sets.
    /// </summary>
    public class MenuItemLook : TextLook
    {
        Color? _shortcutColor;
        float? _shortcutFontSize;
        float? _shortcutSpacing;
        Color? _mark;
        float? _markSize;
        float? _markSpacing;
        Color? _arrow;
        float? _arrowSize;
        float? _arrowSpacing;

        /// <summary>The color of the shortcut's words, or null to use the item's text color.</summary>
        public Color? ShortcutColor
        {
            get { return _shortcutColor; }
            set
            {
                _shortcutColor = value;
                OnChanged();
            }
        }

        /// <summary>The height of a line of the shortcut's words in UI units, or null to use the item's font size.</summary>
        public float? ShortcutFontSize
        {
            get { return _shortcutFontSize; }
            set
            {
                _shortcutFontSize = value;
                OnChanged();
            }
        }

        /// <summary>
        ///     The least space between the item's words or content and its shortcut in UI units, or null to inherit it from
        ///     the looks beneath.
        /// </summary>
        public float? ShortcutSpacing
        {
            get { return _shortcutSpacing; }
            set
            {
                _shortcutSpacing = value;
                OnChanged();
            }
        }

        /// <summary>The color of the mark a chosen item shows before its words, or null to use the item's text color.</summary>
        public Color? Mark
        {
            get { return _mark; }
            set
            {
                _mark = value;
                OnChanged();
            }
        }

        /// <summary>The width and height of the mark in UI units, or null to inherit it from the looks beneath.</summary>
        public float? MarkSize
        {
            get { return _markSize; }
            set
            {
                _markSize = value;
                OnChanged();
            }
        }

        /// <summary>The space between the mark and the item's words or content in UI units, or null to inherit it from the looks beneath.</summary>
        public float? MarkSpacing
        {
            get { return _markSpacing; }
            set
            {
                _markSpacing = value;
                OnChanged();
            }
        }

        /// <summary>
        ///     The color of the arrow an item that opens a submenu shows at its right, or null to use the item's text color.
        /// </summary>
        public Color? Arrow
        {
            get { return _arrow; }
            set
            {
                _arrow = value;
                OnChanged();
            }
        }

        /// <summary>The width and height of the arrow in UI units, or null to inherit it from the looks beneath.</summary>
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
        ///     The least space between the item's words, content or shortcut and the arrow's column in UI units, or null to
        ///     inherit it from the looks beneath.
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

        /// <inheritdoc />
        public override void Merge(ElementLook over)
        {
            base.Merge(over);
            var look = over as MenuItemLook;
            if (look == null)
                return;

            _shortcutColor = look._shortcutColor ?? _shortcutColor;
            _shortcutFontSize = look._shortcutFontSize ?? _shortcutFontSize;
            _shortcutSpacing = look._shortcutSpacing ?? _shortcutSpacing;
            _mark = look._mark ?? _mark;
            _markSize = look._markSize ?? _markSize;
            _markSpacing = look._markSpacing ?? _markSpacing;
            _arrow = look._arrow ?? _arrow;
            _arrowSize = look._arrowSize ?? _arrowSize;
            _arrowSpacing = look._arrowSpacing ?? _arrowSpacing;
        }

        /// <inheritdoc />
        public override void Clear()
        {
            base.Clear();
            _shortcutColor = null;
            _shortcutFontSize = null;
            _shortcutSpacing = null;
            _mark = null;
            _markSize = null;
            _markSpacing = null;
            _arrow = null;
            _arrowSize = null;
            _arrowSpacing = null;
        }
    }
}
