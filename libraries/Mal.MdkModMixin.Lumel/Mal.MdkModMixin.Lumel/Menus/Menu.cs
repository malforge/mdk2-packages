using System;
using Lumel.Controls;
using Lumel.Drawing;
using Lumel.Input;
using Lumel.Layout;
using Lumel.Styling;
using Lumel.Viewing;
using Lumel.Windows;
using VRageMath;

namespace Lumel.Menus
{
    /// <summary>
    ///     A popup listing commands as <see cref="MenuItem" />s, top to bottom, which closes as one of them is pressed.
    /// </summary>
    /// <remarks>
    ///     The menu takes the keyboard as it opens, and gives it back to its anchor as it closes. One item at a time is lit:
    ///     the one the pointer comes onto, or the one Up and Down move to among the items that can be used. Enter presses
    ///     it, and Escape closes the menu. A separator among the items is shown only between visible items.
    ///     Items that run past the room on the screen scroll.
    ///     An item with a <see cref="MenuItem.Submenu" /> opens it beside itself when pressed, when Right is pressed on it, or
    ///     once the pointer has rested on it for the look's delay, and stays lit while it is open. Left and Escape close a
    ///     submenu and give the keyboard back to the menu it opened from. The pointer resting on another item for the
    ///     same delay closes the open submenu, so the pointer can cross other items on its way into it.
    /// </remarks>
    public class Menu : Popup, IMenu
    {
        // The column takes no look but its own, whatever the host's theme holds for a stack.
        static readonly IStyle<Stack> Bare = new StackStyle();

        readonly ScrollView _scroll;
        readonly VStack _items;
        Element _header;
        VStack _column;
        ICountdown _wait;
        MenuItem _waitingOn;

        /// <summary>Creates an empty menu, using the look its host's theme holds for a menu until it is given a style.</summary>
        public Menu()
        {
            // Under a header it asks for what its items need, and takes what the screen leaves it.
            Content = new ScrollView { FitsContent = true, Placement = Flex.Spacer, Content = new VStack { Style = Bare }.As(out _items) }.As(out _scroll);
            Closed += _ => Collapse();
            Focusable = true;
        }

        /// <summary>Gets or sets the style of this menu, or null to use the default its host's theme holds for a menu.</summary>
        public new IStyle<Menu> Style
        {
            get { return (IStyle<Menu>)StyleSet; }
            set { SetStyle(value); }
        }

        /// <summary>Gets this menu's own look, applied in every state on top of the style's look.</summary>
        public new MenuLook Base => (MenuLook)base.Base;

        /// <summary>Gets this menu's own look when hovered, applied on top of the style's look.</summary>
        public new MenuLook PointerOver => (MenuLook)base.PointerOver;

        /// <summary>Gets this menu's own look when disabled, applied on top of the style's look.</summary>
        public new MenuLook Disabled => (MenuLook)base.Disabled;

        /// <summary>
        ///     Gets this menu's own look for <paramref name="state" />, or for every state that includes it, applied on
        ///     top of the style's look.
        /// </summary>
        public new MenuLook this[State state] => (MenuLook)base[state];

        /// <summary>Gets the style of the scroll that holds the items, or null to use the host theme's scroll view style.</summary>
        /// <remarks>Set by this menu's looks.</remarks>
        public IStyle<ScrollView> ScrollStyle { get; private set; }

        /// <summary>Whether any of its items is chosen, so that every item keeps room for the chosen mark.</summary>
        /// <remarks>Worked out as the menu is measured.</remarks>
        public bool HasChosen { get; private set; }

        /// <summary>Whether any of its items opens a submenu, so that every item keeps room for the submenu's arrow.</summary>
        /// <remarks>Worked out as the menu is measured.</remarks>
        public bool HasSubmenus { get; private set; }

        /// <summary>
        ///     Gets the ticks the pointer rests on an item before the item's submenu opens, or the open submenu of another
        ///     item closes.
        /// </summary>
        /// <remarks>Set by this menu's looks.</remarks>
        public int SubmenuDelay { get; private set; }

        /// <summary>Gets the blank space between <see cref="Header" /> and the items.</summary>
        /// <remarks>Set by this menu's looks.</remarks>
        public float HeaderSpacing { get; private set; }

        /// <summary>Gets or sets what stands above the items and stays while they scroll, or null for nothing.</summary>
        public Element Header
        {
            get { return _header; }
            set
            {
                if (ReferenceEquals(value, _header))
                    return;

                _header = value;
                Content = null;
                _column?.Children.Clear();
                _column = null;
                if (value == null)
                {
                    Content = _scroll;
                    return;
                }

                Content = new VStack { Style = Bare, Base = { Spacing = HeaderSpacing }, Children = { value, _scroll } }.As(out _column);
            }
        }

        /// <summary>Gets the submenu open beside one of the items, or null if none is.</summary>
        public Menu ShownSubmenu
        {
            get
            {
                foreach (var element in Items)
                {
                    var submenu = (element as MenuItem)?.Submenu;
                    if (submenu != null && submenu.IsOpen)
                        return submenu;
                }

                return null;
            }
        }

        /// <summary>Gets the menu this one opened from as the submenu of one of its items, or null if it is a menu of its own.</summary>
        public Menu ParentMenu
        {
            get
            {
                var item = Anchor as MenuItem;
                return item != null && item.Submenu == this ? item.Menu : null;
            }
        }

        /// <summary>
        ///     Raised as an item is pressed, with that item, before the menu closes and before the item's command runs; also
        ///     as an item of a submenu opened from this menu is pressed, after the submenu raises it.
        /// </summary>
        /// <remarks>
        ///     A menu that closes without this being raised was dismissed, as by Escape or a press outside it. An item that
        ///     opens a submenu raises nothing as it is pressed.
        /// </remarks>
        public event Action<MenuItem> ItemPressed;

        /// <summary>Gets what the menu lists, top to bottom: its items, and separators between them.</summary>
        public Collection Items => _items.Children;

        /// <summary>
        ///     Gets or sets the item that is lit, which Enter presses, or null if none is: the one under the pointer or
        ///     the one the arrow keys moved to last.
        /// </summary>
        /// <remarks>
        ///     The item set is scrolled into view, unless the pointer is on it. Lighting an item of a submenu lights the item
        ///     it opened from. Throws if set to an item that is not among <see cref="Items" />.
        /// </remarks>
        public MenuItem Highlight
        {
            get
            {
                foreach (var element in Items)
                {
                    var item = element as MenuItem;
                    if (item != null && item.IsHighlighted)
                        return item;
                }

                return null;
            }
            set
            {
                if (value != null && value.Parent != _items)
                    throw new ArgumentException("A menu can only light one of its own items.", nameof(value));

                Unhighlight();
                if (value == null)
                    return;

                value.Set(MenuItem.Highlighted, true);
                if (!value.IsPointerOver)
                    _scroll.BringIntoView(value);

                var parent = ParentMenu;
                if (parent == null)
                    return;

                parent.StopWaiting();
                parent.Highlight = (MenuItem)Anchor;
            }
        }

        /// <inheritdoc />
        /// <remarks>
        ///     Raises <see cref="ItemPressed" /> here and on each menu this opened from, then closes this menu and each of
        ///     them, from this one out.
        /// </remarks>
        void IMenu.Pressing(MenuItem item)
        {
            for (var menu = this; menu != null; menu = menu.ParentMenu)
                menu.ItemPressed?.Invoke(item);

            var closing = this;
            while (closing != null)
            {
                var parent = closing.ParentMenu;
                closing.Close();
                closing = parent;
            }
        }

        /// <inheritdoc />
        void IMenu.Opening(MenuItem item)
        {
            Expand(item);
        }

        /// <inheritdoc />
        /// <remarks>
        ///     Starts the wait on the item the pointer comes onto, after which its submenu opens, or the open one closes;
        ///     the pointer leaving the item ends the wait.
        /// </remarks>
        void IMenu.PointerOn(MenuItem item, bool on)
        {
            if (on)
            {
                StopWaiting();
                _waitingOn = item;
                _wait = Windows.After(this, SubmenuDelay, Rested);
            }
            else if (_waitingOn == item)
                StopWaiting();
        }

        /// <summary>
        ///     Opens the submenu of the item the pointer has rested on for <see cref="SubmenuDelay" />, or closes the open
        ///     one where that item has none.
        /// </summary>
        void Rested()
        {
            var item = _waitingOn;
            _wait = null;
            _waitingOn = null;
            if (item.Submenu != null && item.IsEnabled)
                Expand(item);
            else
                ShownSubmenu?.Close();
        }

        /// <summary>Ends the wait on the item the pointer rests on, if there is one.</summary>
        void StopWaiting()
        {
            _wait?.Cancel();
            _wait = null;
            _waitingOn = null;
        }

        /// <inheritdoc />
        /// <remarks>Its first visible entry is level with the item it opens from, as a submenu.</remarks>
        protected override float Lead()
        {
            foreach (var element in Items)
            {
                if (element.IsVisible)
                    return Padding.Top + element.DesiredSize.Y * 0.5f;
            }

            return base.Lead();
        }

        /// <inheritdoc />
        protected override bool Fits(IStyle style)
        {
            return style is IStyle<Menu>;
        }

        /// <inheritdoc />
        protected override ElementLook NewLook()
        {
            return new MenuLook();
        }

        /// <inheritdoc />
        protected override void Apply(ElementLook look)
        {
            base.Apply(look);
            var menu = (MenuLook)look;
            ScrollStyle = menu.ScrollStyle;
            _scroll.Style = ScrollStyle;
            SubmenuDelay = menu.SubmenuDelay ?? 0;
            HeaderSpacing = menu.HeaderSpacing ?? 0f;
            if (_column != null && _column.Base.Spacing != HeaderSpacing)
                _column.Base.Spacing = HeaderSpacing;
        }

        /// <inheritdoc />
        /// <remarks>
        ///     Shows one separator between each two runs of visible items, and hides the rest, and works out
        ///     <see cref="HasChosen" /> and <see cref="HasSubmenus" />.
        /// </remarks>
        protected override Vector2 MeasureContent(IGraphics g, Vector2 available)
        {
            Separator waiting = null;
            var above = false;
            var chosen = false;
            var submenus = false;
            foreach (var element in Items)
            {
                var item = element as MenuItem;
                chosen |= item?.IsChosen ?? false;
                submenus |= item?.Submenu != null;

                var separator = element as Separator;
                if (separator == null)
                {
                    if (!element.IsVisible)
                        continue;

                    if (waiting != null)
                        waiting.IsVisible = true;

                    waiting = null;
                    above = true;
                    continue;
                }

                if (waiting != null || !above)
                {
                    separator.IsVisible = false;
                    continue;
                }

                waiting = separator;
                above = false;
            }

            if (waiting != null)
                waiting.IsVisible = false;

            if (chosen != HasChosen || submenus != HasSubmenus)
            {
                HasChosen = chosen;
                HasSubmenus = submenus;
                foreach (var element in Items)
                    element.InvalidateMeasure();
            }

            return base.MeasureContent(g, available);
        }

        /// <inheritdoc />
        /// <remarks>
        ///     Up and Down move the highlight, Enter presses the highlighted item, and Escape closes the menu. Right, or
        ///     Enter on an item that opens a submenu, opens it with its first usable item lit; Left and Escape in a submenu
        ///     close it and give the keyboard back to the menu it opened from. In a submenu, Right is taken where it opens
        ///     nothing, so it reaches none of the menus beneath.
        /// </remarks>
        protected override bool OnIntent(InputRoot input, Intent intent)
        {
            var highlight = Highlight;
            switch (intent)
            {
                case Intent.Up:
                    return Step(input, -1);
                case Intent.Down:
                    return Step(input, 1);
                case Intent.Right:
                    if (highlight?.Submenu == null || !highlight.IsEnabled)
                        return ParentMenu != null;

                    Expand(highlight);
                    highlight.Submenu.Enter(input);
                    return true;
                case Intent.Left:
                    if (ParentMenu == null)
                        return false;

                    Back(input);
                    return true;
                case Intent.Accept:
                    if (highlight == null || !input.Press(highlight))
                        return false;

                    if (highlight.Submenu != null && highlight.Submenu.IsOpen)
                        highlight.Submenu.Enter(input);

                    return true;
                case Intent.Cancel:
                    if (ParentMenu != null)
                        Back(input);
                    else
                        Close();

                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        ///     Opens the submenu of <paramref name="item" /> beside it, closing the one open beside another item, and keeps
        ///     the item lit.
        /// </summary>
        void Expand(MenuItem item)
        {
            StopWaiting();
            var submenu = item.Submenu;
            var shown = ShownSubmenu;
            if (shown != null && shown != submenu)
                shown.Close();

            Highlight = item;
            if (submenu.IsOpen)
                return;

            submenu.Anchor = item;
            submenu.Side = PopupSide.Right;
            submenu.Open();
        }

        /// <summary>Lights the first usable item of this submenu, as the keys come into it, unless one is lit already.</summary>
        void Enter(InputRoot input)
        {
            if (Highlight == null)
                Step(input, 1);
        }

        /// <summary>Closes this submenu and gives the keyboard back to the menu it opened from.</summary>
        void Back(InputRoot input)
        {
            var parent = ParentMenu;
            Close();
            input.Focus(parent);
        }

        /// <summary>
        ///     Moves the highlight <paramref name="by" /> one usable item down, or up where it is negative, around the
        ///     ends, telling <paramref name="input" /> the keys reached it, and returns true if an item can be used.
        /// </summary>
        bool Step(InputRoot input, int by)
        {
            var count = Items.Count;
            var from = -1;
            for (var i = 0; i < count; i++)
            {
                var item = Items[i] as MenuItem;
                if (item != null && item.IsHighlighted)
                    from = i;
            }

            if (from < 0)
                from = by > 0 ? -1 : count;

            for (var step = 1; step <= count; step++)
            {
                var at = ((from + by * step) % count + count) % count;
                var item = Items[at] as MenuItem;
                if (item == null || !item.IsVisible || !item.IsEnabled)
                    continue;

                Highlight = item;
                input.Reach(item);
                return true;
            }

            return false;
        }

        /// <summary>Takes the highlight off every item, ends any wait on the pointer, and closes the open submenu.</summary>
        void Collapse()
        {
            Unhighlight();
            StopWaiting();
            ShownSubmenu?.Close();
        }

        /// <summary>Takes the highlight off every item.</summary>
        void Unhighlight()
        {
            foreach (var element in Items)
                (element as MenuItem)?.Set(MenuItem.Highlighted, false);
        }
    }
}
