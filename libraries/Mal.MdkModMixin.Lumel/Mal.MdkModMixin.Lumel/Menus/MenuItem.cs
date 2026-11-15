using System;
using Lumel.Controls;
using Lumel.Drawing;
using Lumel.Input;
using Lumel.Styling;
using Lumel.Text;
using VRageMath;

namespace Lumel.Menus
{
    /// <summary>
    ///     One entry of a <see cref="Menu" />: a button showing its own text or the content given to it at its left, after
    ///     a mark where it is the chosen one, and the words of its shortcut, if it has one, at its right, followed by an
    ///     arrow where it opens a submenu.
    /// </summary>
    /// <remarks>
    ///     Pressing it closes the menu it is in before its command runs, unless it opens a submenu, which pressing it
    ///     opens beside it.
    /// </remarks>
    public class MenuItem : Button
    {
        /// <summary>The state of the item its menu has lit, under the pointer or moved to by the arrow keys.</summary>
        public static readonly State Highlighted = State.Declare("Highlighted", 100);

        /// <summary>The state of an item marked as the chosen one, such as the current choice or the default.</summary>
        public static readonly State Chosen = State.Declare("Chosen", 400);

        // The shortcut's words take no look but the one the item gives them.
        static readonly IStyle<Label> Bare = new LabelStyle();

        readonly Label _shortcut = new Label { Style = Bare, IsVisible = false };
        float _shortcutSpacing;
        float _markSize;
        float _markSpacing;
        float _gutter;
        float _arrowSize;
        float _arrowSpacing;
        float _arrowColumn;
        Menu _submenu;
        RelayCommand _opens;

        /// <summary>Creates an item with no text, using the look its host's theme holds for a menu item until it is given a style.</summary>
        public MenuItem()
        {
            AddPart(_shortcut);
        }

        /// <summary>Gets or sets the style of this item, or null to use the default its host's theme holds for a menu item.</summary>
        public new IStyle<MenuItem> Style
        {
            get { return (IStyle<MenuItem>)StyleSet; }
            set { SetStyle(value); }
        }

        /// <summary>Gets this item's own look, applied in every state on top of the style's look.</summary>
        public new MenuItemLook Base => (MenuItemLook)base.Base;

        /// <summary>Gets this item's own look when hovered, applied on top of the style's look.</summary>
        public new MenuItemLook PointerOver => (MenuItemLook)base.PointerOver;

        /// <summary>Gets this item's own look when disabled, applied on top of the style's look.</summary>
        public new MenuItemLook Disabled => (MenuItemLook)base.Disabled;

        /// <summary>
        ///     Gets this item's own look for <paramref name="state" />, or for every state that includes it, applied on
        ///     top of the style's look.
        /// </summary>
        public new MenuItemLook this[State state] => (MenuItemLook)base[state];

        /// <summary>Gets or sets the words naming the key or control that does the same as the item, or empty for none.</summary>
        public string Shortcut
        {
            get { return _shortcut.Text; }
            set
            {
                _shortcut.Text = value ?? "";
                _shortcut.IsVisible = !string.IsNullOrEmpty(value);
            }
        }

        /// <summary>
        ///     Whether the item is the chosen one, such as the current choice or the default, which shows a mark before its
        ///     words or content.
        /// </summary>
        /// <remarks>While any item of a menu is chosen, every item of it keeps room for the mark, so their words line up.</remarks>
        public bool IsChosen
        {
            get { return Is(Chosen); }
            set
            {
                if (value == IsChosen)
                    return;

                Set(Chosen, value);
                InvalidateMeasure();
            }
        }

        /// <summary>
        ///     Gets or sets the menu this item opens beside itself, or null for an item that runs its command.
        /// </summary>
        /// <remarks>
        ///     An item with a submenu has a command of its own that opens it, and the submenu's anchor and side are set as it
        ///     opens. Throws if set while the item has another command.
        /// </remarks>
        public Menu Submenu
        {
            get { return _submenu; }
            set
            {
                if (ReferenceEquals(value, _submenu))
                    return;

                if (value != null && Command != null && Command != _opens)
                    throw new InvalidOperationException("An item that opens a submenu has no other command.");

                _submenu?.Close();
                _submenu = value;
                Command = value == null ? null : _opens ?? (_opens = new RelayCommand(OpenSubmenu));
                InvalidateMeasure();
            }
        }

        /// <summary>Gets the menu the item is in, or null if it is in none.</summary>
        public Menu Menu => Owner();

        /// <summary>Whether the item is the one its menu has lit, which Enter presses.</summary>
        /// <remarks>Set by the menu the item is in, through <see cref="Menu.Highlight" />.</remarks>
        public bool IsHighlighted => Is(Highlighted);

        /// <summary>Gets the color of the shortcut's words.</summary>
        /// <remarks>Set by this item's looks.</remarks>
        public Color ShortcutColor { get; private set; }

        /// <summary>Gets the height of a line of the shortcut's words, in UI units.</summary>
        /// <remarks>Set by this item's looks.</remarks>
        public float ShortcutFontSize { get; private set; }

        /// <summary>Gets the least space between the item's words or content and its shortcut, in UI units.</summary>
        /// <remarks>Set by this item's looks.</remarks>
        public float ShortcutSpacing => _shortcutSpacing;

        /// <summary>Gets the color of the mark a chosen item shows.</summary>
        /// <remarks>Set by this item's looks.</remarks>
        public Color Mark { get; private set; }

        /// <summary>Gets the width and height of the mark, in UI units.</summary>
        /// <remarks>Set by this item's looks.</remarks>
        public float MarkSize => _markSize;

        /// <summary>Gets the space between the mark and the item's words or content, in UI units.</summary>
        /// <remarks>Set by this item's looks.</remarks>
        public float MarkSpacing => _markSpacing;

        /// <summary>Gets the color of the arrow an item that opens a submenu shows.</summary>
        /// <remarks>Set by this item's looks.</remarks>
        public Color Arrow { get; private set; }

        /// <summary>Gets the width and height of the arrow, in UI units.</summary>
        /// <remarks>Set by this item's looks.</remarks>
        public float ArrowSize => _arrowSize;

        /// <summary>Gets the least space between the item's words, content or shortcut and the arrow's column, in UI units.</summary>
        /// <remarks>Set by this item's looks.</remarks>
        public float ArrowSpacing => _arrowSpacing;

        /// <summary>Gets the alignment of the item's words or content: at its left, centered down.</summary>
        protected override Align Aligns => Align.FillWidth | Align.Center;

        /// <inheritdoc />
        protected override bool Fits(IStyle style)
        {
            return style is IStyle<MenuItem>;
        }

        /// <inheritdoc />
        protected override ElementLook NewLook()
        {
            return new MenuItemLook();
        }

        /// <inheritdoc />
        protected override void Apply(ElementLook look)
        {
            base.Apply(look);

            var item = (MenuItemLook)look;
            ShortcutColor = item.ShortcutColor ?? Color;
            ShortcutFontSize = item.ShortcutFontSize ?? FontSize;

            Mark = item.Mark ?? Color;
            Arrow = item.Arrow ?? Color;
            var spacing = item.ShortcutSpacing ?? 0f;
            var markSize = item.MarkSize ?? 0f;
            var markSpacing = item.MarkSpacing ?? 0f;
            var arrowSize = item.ArrowSize ?? 0f;
            var arrowSpacing = item.ArrowSpacing ?? 0f;
            if (spacing != _shortcutSpacing || markSize != _markSize || markSpacing != _markSpacing || arrowSize != _arrowSize || arrowSpacing != _arrowSpacing)
            {
                _shortcutSpacing = spacing;
                _markSize = markSize;
                _markSpacing = markSpacing;
                _arrowSize = arrowSize;
                _arrowSpacing = arrowSpacing;
                InvalidateMeasure();
            }

            var words = _shortcut.Base;
            if (words.Font != Font)
                words.Font = Font;

            if (words.FontSize != ShortcutFontSize)
                words.FontSize = ShortcutFontSize;

            if (words.Color != ShortcutColor)
                words.Color = ShortcutColor;
        }

        /// <inheritdoc />
        /// <remarks>
        ///     Tells the menu the item is in, which closes, unless the item opens a submenu. Throws where the item has a
        ///     submenu and its command was replaced since.
        /// </remarks>
        protected override void OnPressed()
        {
            if (_submenu == null)
                ((IMenu)Owner())?.Pressing(this);
            else if (Command != _opens)
                throw new InvalidOperationException("An item that opens a submenu has no other command.");
        }

        /// <inheritdoc />
        /// <remarks>
        ///     Lights the item in its menu as the pointer comes onto it, if it can be used, and puts it out as the pointer
        ///     leaves, unless its submenu is open; either way its menu is told, which waits to open or close a submenu.
        /// </remarks>
        protected override void OnPointerOverChanged()
        {
            var menu = Owner();
            if (menu == null)
                return;

            if (IsPointerOver && IsEnabled)
                menu.Highlight = this;
            else if (!IsPointerOver && IsHighlighted && !(_submenu?.IsOpen ?? false))
                menu.Highlight = null;

            ((IMenu)menu).PointerOn(this, IsPointerOver);
        }

        /// <summary>Opens the submenu beside the item, through the menu the item is in.</summary>
        /// <remarks>Throws where the item is in no menu.</remarks>
        void OpenSubmenu()
        {
            var menu = Owner();
            if (menu == null)
                throw new InvalidOperationException("An item opens its submenu from the menu it is in, and this one is in none.");

            ((IMenu)menu).Opening(this);
        }

        /// <summary>Returns the menu the item is in, or null if it is in none.</summary>
        Menu Owner()
        {
            for (var at = Parent; at != null; at = at.Parent)
            {
                var menu = at as Menu;
                if (menu != null)
                    return menu;
            }

            return null;
        }

        /// <inheritdoc />
        /// <remarks>Measures the words or content in what the mark's column, the shortcut and the arrow's column leave.</remarks>
        protected override Vector2 MeasureContent(IGraphics g, Vector2 available)
        {
            var menu = Owner();
            _gutter = (menu?.HasChosen ?? IsChosen) ? _markSize + _markSpacing : 0f;
            _arrowColumn = (menu?.HasSubmenus ?? _submenu != null) ? _arrowSpacing + _arrowSize : 0f;

            _shortcut.Measure(g, available);
            var aside = Aside();
            var held = base.MeasureContent(g, new Vector2(MathHelper.Max(0f, available.X - _gutter - aside), available.Y));
            return new Vector2(_gutter + held.X + aside, MathHelper.Max(MathHelper.Max(held.Y, _shortcut.DesiredSize.Y), _gutter > 0f ? _markSize : 0f));
        }

        /// <inheritdoc />
        /// <remarks>
        ///     Keeps the mark's column at the left and the arrow's at the right, lays the shortcut against the arrow's column,
        ///     and the words or content across the rest.
        /// </remarks>
        protected override void ArrangeContent(RectangleF bounds)
        {
            var aside = MathHelper.Min(Aside(), bounds.Width);
            var gutter = MathHelper.Min(_gutter, bounds.Width - aside);
            base.ArrangeContent(new RectangleF(bounds.X + gutter, bounds.Y, bounds.Width - aside - gutter, bounds.Height));

            if (!_shortcut.IsVisible)
                return;

            var wide = MathHelper.Max(0f, aside - _shortcutSpacing - _arrowColumn);
            var tall = _shortcut.DesiredSize.Y;
            _shortcut.Arrange(new RectangleF(bounds.X + bounds.Width - _arrowColumn - wide, bounds.Y + (bounds.Height - tall) * 0.5f, wide, tall));
        }

        /// <inheritdoc />
        protected override void DrawContent(IGraphics g)
        {
            if (IsChosen && _markSize > 0f)
            {
                var bounds = ContentBounds;
                g.Fill(new RectangleF(bounds.X, bounds.Y + (bounds.Height - _markSize) * 0.5f, _markSize, _markSize), Mark);
            }

            base.DrawContent(g);
            _shortcut.Draw(g);

            if (_submenu != null && _arrowSize > 0f)
                DrawArrow(g);
        }

        /// <summary>Draws the arrow, pointing right, at the right of the item's content, centered down.</summary>
        void DrawArrow(IGraphics g)
        {
            var bounds = ContentBounds;
            var size = _arrowSize;
            var stroke = MathHelper.Max(1f, size * 0.25f);
            var length = size * 0.7071f + stroke * 0.5f;
            var x = bounds.X + bounds.Width - size * 0.5f - length * 0.5f;
            var middle = bounds.Y + bounds.Height * 0.5f;

            g.Fill(new RectangleF(x, middle - size * 0.25f - stroke * 0.5f, length, stroke), Arrow, MathHelper.PiOver4);
            g.Fill(new RectangleF(x, middle + size * 0.25f - stroke * 0.5f, length, stroke), Arrow, -MathHelper.PiOver4);
        }

        /// <summary>
        ///     Returns the width the shortcut and the arrow's column take at the right, the shortcut's spacing included, or 0
        ///     without either.
        /// </summary>
        float Aside()
        {
            return (_shortcut.IsVisible ? _shortcut.DesiredSize.X + _shortcutSpacing : 0f) + _arrowColumn;
        }
    }
}
