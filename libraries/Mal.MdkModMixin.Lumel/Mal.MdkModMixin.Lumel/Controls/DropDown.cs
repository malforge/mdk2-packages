using System;
using System.Collections.Generic;
using System.Text;
using Lumel.Drawing;
using Lumel.Input;
using Lumel.Menus;
using Lumel.Styling;
using Lumel.Text;
using VRage.Input;
using VRageMath;

namespace Lumel.Controls
{
    /// <summary>
    ///     A box showing the one item chosen of a list, with an arrow at its right, which opens the list under itself when
    ///     pressed, for another to be chosen.
    /// </summary>
    /// <remarks>
    ///     Its items' words are read as it is measured, so words changed later show once <see cref="Element.InvalidateMeasure" />
    ///     is called. While it holds the keyboard, Left and Right step through the shown and enabled choices, and a letter
    ///     chooses the next one starting with it. Where <see cref="IsSearchable" /> is set, the list opens with a search box
    ///     above its choices that holds the keyboard, and what it holds narrows them.
    /// </remarks>
    public class DropDown : Element, IPadded, IDropDown
    {
        /// <summary>The state of a drop-down while its list is open.</summary>
        public static readonly State Expanded = State.Declare("Expanded", 300);

        // The box's words take no look but the one the box gives them.
        static readonly IStyle<Label> Bare = new LabelStyle();

        readonly Label _shown = new Label { Style = Bare, TextWrapping = TextWrapping.Trim };
        readonly Menu _list;
        readonly RelayCommand _toggle;
        readonly StringBuilder _measuring = new StringBuilder();
        readonly List<Element> _narrowed = new List<Element>();

        SearchBox _search;
        string _searchHint;
        DropDownItem _chosen;
        string _hint = "";
        Thickness _padding;
        float _arrowSize;
        float _arrowSpacing;

        /// <summary>
        ///     Creates a drop-down with no items and nothing chosen, using the look its host's theme holds for a drop-down
        ///     until it is given a style.
        /// </summary>
        public DropDown()
        {
            var children = CreateChildren();
            children.Add(_shown);

            _list = new Menu { Anchor = this, MatchAnchorWidth = true };
            _list.Opened += () => Set(Expanded, true);
            _list.Closed += _ =>
            {
                Set(Expanded, false);
                EndSearch();
            };

            _toggle = new RelayCommand(Toggle);
            Command = _toggle;

            Font = VRage.Game.MyFontEnum.White;
            FontSize = TextLook.NeutralFontSize;
            Color = Color.White;
            HintColor = Color.Gray;
            Arrow = Color.White;
        }

        /// <summary>
        ///     Raised when the player chooses another item, from the list or with the keys, but not when
        ///     <see cref="Chosen" /> is set.
        /// </summary>
        public event Action ChoiceChanged;

        /// <summary>
        ///     Raised when the player changes what the list's search box holds, before the list is narrowed, so a handler can
        ///     change <see cref="Items" />.
        /// </summary>
        public event Action SearchChanged;

        /// <summary>Gets or sets the style of this drop-down, or null to use the default its host's theme holds for a drop-down.</summary>
        public new IStyle<DropDown> Style
        {
            get { return (IStyle<DropDown>)StyleSet; }
            set { SetStyle(value); }
        }

        /// <summary>Gets this drop-down's own look, applied in every state on top of the style's look.</summary>
        public new DropDownLook Base => (DropDownLook)base.Base;

        /// <summary>Gets this drop-down's own look when hovered, applied on top of the style's look.</summary>
        public new DropDownLook PointerOver => (DropDownLook)base.PointerOver;

        /// <summary>Gets this drop-down's own look when disabled, applied on top of the style's look.</summary>
        public new DropDownLook Disabled => (DropDownLook)base.Disabled;

        /// <summary>
        ///     Gets this drop-down's own look for <paramref name="state" />, or for every state that includes it, applied on
        ///     top of the style's look.
        /// </summary>
        public new DropDownLook this[State state] => (DropDownLook)base[state];

        /// <summary>Gets what the list holds, top to bottom: the choices, and separators or other menu items between them.</summary>
        public Collection Items => _list.Items;

        /// <summary>Gets or sets the item chosen, which the box shows, or null for none, which shows the hint.</summary>
        /// <remarks>
        ///     Any of the choices can be set, hidden and disabled ones too, as a choice only the application makes is. Throws
        ///     if set to an item that is not among <see cref="Items" />.
        /// </remarks>
        public DropDownItem Chosen
        {
            get { return _chosen; }
            set
            {
                if (value != null && Items.IndexOf(value) < 0)
                    throw new ArgumentException("A drop-down can only choose one of its own items.", nameof(value));

                if (value == _chosen)
                    return;

                _chosen = value;
                Show();
            }
        }

        /// <summary>Gets or sets the words the box shows while nothing is chosen, or empty for none.</summary>
        public string Hint
        {
            get { return _hint; }
            set
            {
                var hint = value ?? "";
                if (hint == _hint)
                    return;

                _hint = hint;
                Show();
                InvalidateMeasure();
            }
        }

        /// <summary>Whether the list opens with a search box above its choices, which narrows them.</summary>
        public bool IsSearchable
        {
            get { return _search != null; }
            set
            {
                if (value == IsSearchable)
                    return;

                if (value)
                {
                    _search = new SearchBox { Style = SearchStyle, Hint = _searchHint };
                    _search.Edited += Narrow;
                }
                else
                {
                    _search.Edited -= Narrow;
                    _search = null;
                }

                _list.Header = _search;
                _list.FocusTarget = _search?.Keyboard;
            }
        }

        /// <summary>Gets or sets the words the search box shows while it is empty and does not hold the keyboard, or null for none.</summary>
        public string SearchHint
        {
            get { return _searchHint; }
            set
            {
                _searchHint = value;
                if (_search != null)
                    _search.Hint = value;
            }
        }

        /// <summary>Gets what the search box holds, or empty for nothing.</summary>
        public string Search => _search?.Text ?? "";

        /// <summary>
        ///     Gets or sets the callback that returns true if a choice stays in the list for what is searched for, or null to
        ///     keep the choices whose words <see cref="TextSearch.Matches" /> the search.
        /// </summary>
        public Func<DropDownItem, string, bool> FnIsFound { get; set; }

        /// <summary>Whether the list is open.</summary>
        public bool IsOpen => _list.IsOpen;

        /// <summary>Gets the font the box's words are drawn in.</summary>
        /// <remarks>Set by this drop-down's looks.</remarks>
        public string Font { get; private set; }

        /// <summary>Gets the height of a line of the box's words, in UI units.</summary>
        /// <remarks>Set by this drop-down's looks.</remarks>
        public float FontSize { get; private set; }

        /// <summary>Gets the color of the chosen item's words in the box.</summary>
        /// <remarks>Set by this drop-down's looks.</remarks>
        public Color Color { get; private set; }

        /// <summary>Gets the color of the <see cref="Hint" />.</summary>
        /// <remarks>Set by this drop-down's looks.</remarks>
        public Color HintColor { get; private set; }

        /// <summary>Gets the color of the arrow.</summary>
        /// <remarks>Set by this drop-down's looks.</remarks>
        public Color Arrow { get; private set; }

        /// <summary>Gets the width and height of the arrow, in UI units.</summary>
        /// <remarks>Set by this drop-down's looks.</remarks>
        public float ArrowSize => _arrowSize;

        /// <summary>Gets the least space between the words and the arrow, in UI units.</summary>
        /// <remarks>Set by this drop-down's looks.</remarks>
        public float ArrowSpacing => _arrowSpacing;

        /// <summary>Gets the style of the list, or null to use the host theme's menu style.</summary>
        /// <remarks>Set by this drop-down's looks.</remarks>
        public IStyle<Menu> ListStyle { get; private set; }

        /// <summary>Gets the style of the search box in the list, or null to use the host theme's search box style.</summary>
        /// <remarks>Set by this drop-down's looks.</remarks>
        public IStyle<SearchBox> SearchStyle { get; private set; }

        /// <summary>Gets the space between the edge of the box and its words and arrow.</summary>
        /// <remarks>Set by this drop-down's looks.</remarks>
        public Thickness Padding => _padding;

        /// <inheritdoc />
        protected override Thickness Inset => _padding;

        /// <summary>Opens the list under the box, with the chosen item marked and lit, unless it is open already.</summary>
        /// <remarks>Throws where the box is on no screen or window.</remarks>
        public void Open()
        {
            if (_list.IsOpen)
                return;

            foreach (var element in Items)
            {
                var item = element as MenuItem;
                if (item != null)
                    item.IsChosen = item == _chosen;
            }

            _list.Highlight = _chosen != null && Usable(_chosen) ? _chosen : null;
            _list.Open();
        }

        /// <summary>Closes the list if it is open.</summary>
        public void Close()
        {
            _list.Close();
        }

        /// <inheritdoc />
        void IPadded.Pad(Thickness padding)
        {
            if (_padding == padding)
                return;

            _padding = padding;
            InvalidateMeasure();
        }

        /// <inheritdoc />
        /// <remarks>Makes <paramref name="item" /> the choice and raises <see cref="ChoiceChanged" /> where it is another.</remarks>
        void IDropDown.Picked(DropDownItem item)
        {
            Choose(item);
        }

        /// <inheritdoc />
        /// <remarks>Throws where the drop-down's command was replaced: pressing it opens its list, and does nothing else.</remarks>
        protected override void OnPressed()
        {
            if (Command != _toggle)
                throw new InvalidOperationException("A drop-down has no other command; pressing it opens its list.");
        }

        /// <inheritdoc />
        protected override bool Fits(IStyle style)
        {
            return style is IStyle<DropDown>;
        }

        /// <inheritdoc />
        protected override ElementLook NewLook()
        {
            return new DropDownLook();
        }

        /// <inheritdoc />
        protected override void Apply(ElementLook look)
        {
            base.Apply(look);

            var box = (DropDownLook)look;
            Font = box.Font ?? VRage.Game.MyFontEnum.White;
            FontSize = box.FontSize ?? TextLook.NeutralFontSize;
            Color = box.Color ?? Color.White;
            HintColor = box.HintColor ?? Color.Gray;
            Arrow = box.Arrow ?? Color;
            ListStyle = box.ListStyle;
            _list.Style = ListStyle;
            SearchStyle = box.SearchStyle;
            if (_search != null)
                _search.Style = SearchStyle;

            var arrowSize = box.ArrowSize ?? 0f;
            var arrowSpacing = box.ArrowSpacing ?? 0f;
            if (arrowSize != _arrowSize || arrowSpacing != _arrowSpacing)
            {
                _arrowSize = arrowSize;
                _arrowSpacing = arrowSpacing;
                InvalidateMeasure();
            }

            var words = _shown.Base;
            if (words.Font != Font)
                words.Font = Font;

            if (words.FontSize != FontSize)
                words.FontSize = FontSize;

            Show();
        }

        /// <inheritdoc />
        /// <remarks>As wide as the widest choice or the hint, and the arrow after it.</remarks>
        protected override Vector2 MeasureContent(IGraphics g, Vector2 available)
        {
            Show();

            var widest = Width(g, _hint);
            foreach (var element in Items)
            {
                var item = element as DropDownItem;
                if (item != null)
                    widest = MathHelper.Max(widest, Width(g, item.Text));
            }

            var aside = Aside();
            _shown.Measure(g, new Vector2(MathHelper.Max(0f, available.X - aside), available.Y));
            return new Vector2(widest + aside, MathHelper.Max(_shown.DesiredSize.Y, _arrowSize));
        }

        /// <inheritdoc />
        /// <remarks>Lays the words across what the arrow leaves, centered down.</remarks>
        protected override void ArrangeContent(RectangleF bounds)
        {
            var wide = MathHelper.Max(0f, bounds.Width - Aside());
            var tall = MathHelper.Min(_shown.DesiredSize.Y, bounds.Height);
            _shown.Arrange(new RectangleF(bounds.X, bounds.Y + (bounds.Height - tall) * 0.5f, wide, tall));
        }

        /// <inheritdoc />
        protected override void DrawContent(IGraphics g)
        {
            _shown.Draw(g);
            if (_arrowSize > 0f && Arrow.A > 0)
                DrawArrow(g);
        }

        /// <inheritdoc />
        /// <remarks>
        ///     While the list is closed, Left and Up choose the previous usable choice, and Right and Down the next, stopping at
        ///     the ends.
        /// </remarks>
        protected override bool OnKeyDown(InputRoot input, MyKeys key, bool shift, bool ctrl)
        {
            if (_list.IsOpen)
                return false;

            if (key == MyKeys.Left || key == MyKeys.Up)
                Step(-1);
            else if (key == MyKeys.Right || key == MyKeys.Down)
                Step(1);
            else
                return false;

            return true;
        }

        /// <inheritdoc />
        /// <remarks>Chooses the next usable choice after the chosen one whose words start with <paramref name="c" />, going round.</remarks>
        protected override void OnTextInput(char c)
        {
            var count = Items.Count;
            var from = _chosen == null ? -1 : Items.IndexOf(_chosen);
            var letter = char.ToUpperInvariant(c);
            for (var step = 1; step <= count; step++)
            {
                var item = Items[((from + step) % count + count) % count] as DropDownItem;
                if (item == null || !Usable(item))
                    continue;

                var text = item.Text;
                if (text.Length == 0 || char.ToUpperInvariant(text[0]) != letter)
                    continue;

                Choose(item);
                return;
            }
        }

        /// <summary>Opens the list if it is closed, and closes it otherwise.</summary>
        void Toggle()
        {
            if (_list.IsOpen)
                Close();
            else
                Open();
        }

        /// <summary>
        ///     Chooses the nearest usable choice <paramref name="by" /> one step after the chosen one, or before it where
        ///     it is negative, if there is one before the end.
        /// </summary>
        void Step(int by)
        {
            var count = Items.Count;
            var from = _chosen == null ? -1 : Items.IndexOf(_chosen);
            if (from < 0)
                from = by > 0 ? -1 : count;

            for (var at = from + by; at >= 0 && at < count; at += by)
            {
                var item = Items[at] as DropDownItem;
                if (item == null || !Usable(item))
                    continue;

                Choose(item);
                return;
            }
        }

        /// <summary>Makes <paramref name="item" /> the choice, as the player chose it, and raises <see cref="ChoiceChanged" /> where it is another.</summary>
        void Choose(DropDownItem item)
        {
            if (item == _chosen)
                return;

            Chosen = item;
            ChoiceChanged?.Invoke();
        }

        /// <summary>Puts the chosen item's words in the box, or the hint where nothing is chosen, each in its color.</summary>
        void Show()
        {
            _shown.Text = _chosen?.Text ?? _hint;

            var color = _chosen == null ? HintColor : Color;
            if (_shown.Base.Color != color)
                _shown.Base.Color = color;
        }

        /// <summary>
        ///     Hides what the search does not find, and lights the first usable choice found, or the chosen item where nothing
        ///     is searched for.
        /// </summary>
        /// <remarks>A choice the application hid stays out of this, so ending the search leaves it hidden.</remarks>
        void Narrow()
        {
            SearchChanged?.Invoke();
            ShowEverything();

            var search = Search;
            if (string.IsNullOrWhiteSpace(search))
            {
                _list.Highlight = _chosen != null && Usable(_chosen) ? _chosen : null;
                return;
            }

            DropDownItem first = null;
            foreach (var element in Items)
            {
                if (element is Separator || !element.IsVisible)
                    continue;

                var item = element as DropDownItem;
                if (item != null && IsFound(item, search))
                {
                    first = first ?? (item.IsEnabled ? item : null);
                    continue;
                }

                element.IsVisible = false;
                _narrowed.Add(element);
            }

            _list.Highlight = first;
        }

        /// <summary>Returns true if <paramref name="item" /> stays in the list for <paramref name="search" />.</summary>
        bool IsFound(DropDownItem item, string search)
        {
            return FnIsFound != null ? FnIsFound(item, search) : TextSearch.Matches(item.Text, search);
        }

        /// <summary>Empties the search box and brings back what it hid.</summary>
        void EndSearch()
        {
            if (_search == null)
                return;

            _search.Text = "";
            ShowEverything();
        }

        /// <summary>Brings back what the search hid.</summary>
        void ShowEverything()
        {
            foreach (var element in _narrowed)
                element.IsVisible = true;

            _narrowed.Clear();
        }

        /// <summary>Returns the width of <paramref name="text" /> in the box's font, or 0 for none.</summary>
        float Width(IGraphics g, string text)
        {
            if (string.IsNullOrEmpty(text))
                return 0f;

            _measuring.Clear().Append(text);
            return g.MeasureText(Font, _measuring, FontSize).X;
        }

        /// <summary>Returns the width the arrow takes at the right, its spacing included, or 0 without one.</summary>
        float Aside()
        {
            return _arrowSize > 0f ? _arrowSpacing + _arrowSize : 0f;
        }

        /// <summary>Draws the arrow, pointing down, at the right of the box's content, centered down.</summary>
        void DrawArrow(IGraphics g)
        {
            var bounds = ContentBounds;
            Chevrons.Down(g, new Vector2(bounds.X + bounds.Width - _arrowSize * 0.5f, bounds.Y + bounds.Height * 0.5f), _arrowSize, Arrow);
        }

        /// <summary>Returns true if <paramref name="item" /> is shown in the list and can be chosen there.</summary>
        static bool Usable(DropDownItem item)
        {
            return item.IsVisible && item.IsEnabled;
        }
    }
}
