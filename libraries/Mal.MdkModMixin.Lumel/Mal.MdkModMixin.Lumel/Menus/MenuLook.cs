using Lumel.Styling;
using Lumel.Viewing;
using Lumel.Windows;

namespace Lumel.Menus
{
    /// <summary>
    ///     The look of a <see cref="Menu" /> in one state, adding the style of its scroll and how long it waits on the
    ///     pointer before opening or closing a submenu to what any popup look sets.
    /// </summary>
    public class MenuLook : PopupLook
    {
        IStyle<ScrollView> _scrollStyle;
        int? _submenuDelay;
        float? _headerSpacing;

        /// <summary>
        ///     The style of the scroll that holds the menu's items, or null to inherit it from the looks beneath.
        /// </summary>
        public IStyle<ScrollView> ScrollStyle
        {
            get { return _scrollStyle; }
            set
            {
                _scrollStyle = value;
                OnChanged();
            }
        }

        /// <summary>
        ///     The ticks the pointer rests on an item before the item's submenu opens, or the open submenu of another item
        ///     closes, or null to inherit it from the looks beneath.
        /// </summary>
        /// <remarks>The wait lets the pointer cross other items on its way into an open submenu.</remarks>
        public int? SubmenuDelay
        {
            get { return _submenuDelay; }
            set
            {
                _submenuDelay = value;
                OnChanged();
            }
        }

        /// <summary>
        ///     The blank space between the menu's <see cref="Menu.Header" /> and its items, or null to inherit it from the looks
        ///     beneath.
        /// </summary>
        public float? HeaderSpacing
        {
            get { return _headerSpacing; }
            set
            {
                _headerSpacing = value;
                OnChanged();
            }
        }

        /// <inheritdoc />
        public override void Merge(ElementLook over)
        {
            base.Merge(over);
            var look = over as MenuLook;
            if (look == null)
                return;

            _scrollStyle = look._scrollStyle ?? _scrollStyle;
            _submenuDelay = look._submenuDelay ?? _submenuDelay;
            _headerSpacing = look._headerSpacing ?? _headerSpacing;
        }

        /// <inheritdoc />
        public override void Clear()
        {
            base.Clear();
            _scrollStyle = null;
            _submenuDelay = null;
            _headerSpacing = null;
        }
    }
}
