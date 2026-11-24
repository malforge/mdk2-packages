using System;
using System.Collections.Generic;
using Lumel.Drawing;
using Lumel.Input;
using Lumel.Menus;
using Lumel.Styling;
using Lumel.Text;
using VRageMath;

namespace Lumel.Controls
{
    /// <summary>
    ///     A field to type words into, with an arrow at its right that opens a list of choices under it: the player types
    ///     anything, or picks one of the choices, and either is the answer.
    /// </summary>
    /// <remarks>
    ///     Typing narrows the list to the choices whose words match what is typed, and opens it while any do. The field
    ///     keeps the keyboard while the list is open: Up and Down light a choice, Enter picks the lit one or takes what is
    ///     typed, and Escape closes the list. What the player gives is raised as <see cref="Committed" />: a choice
    ///     picked, or what is typed once Enter is pressed or the field loses the keyboard.
    /// </remarks>
    public class ComboBox : Element, IPadded, IComboBox
    {
        /// <summary>The state of a combo box while its list is open.</summary>
        public static readonly State Expanded = State.Declare("Expanded", 300);

        readonly TextBox _field = new TextBox();
        readonly Menu _list;
        readonly RelayCommand _toggle;
        readonly List<Element> _narrowed = new List<Element>();

        string _committed = "";
        Thickness _padding;
        float _arrowSize;
        float _arrowSpacing;

        /// <summary>
        ///     Creates an empty combo box with no choices, using the look its host's theme holds for a combo box until it is
        ///     given a style.
        /// </summary>
        public ComboBox()
        {
            var children = CreateChildren();
            children.Add(_field);
            _field.Edited += OnFieldEdited;
            _field.GotFocus += () => Set(State.Focused, true);
            _field.LostFocus += OnFieldLostFocus;

            // The list leaves the keyboard in the field, and a press anywhere on it gives the keyboard back to the field.
            _list = new Menu { Anchor = this, MatchAnchorWidth = true, Focusable = false, FocusTarget = _field };
            _list.Opened += () => Set(Expanded, true);
            _list.Closed += _ =>
            {
                Set(Expanded, false);
                ShowEverything();
            };

            _toggle = new RelayCommand(Toggle);
            Command = _toggle;
            FocusTarget = _field;
            Arrow = Color.White;
        }

        /// <summary>Raised when the player changes the words in the field, by typing, before the list is narrowed, so a handler can change <see cref="Items" />.</summary>
        public event Action Edited;

        /// <summary>
        ///     Raised when the player gives an answer that differs from the last one: a choice picked, or what is typed once
        ///     Enter is pressed or the field loses the keyboard.
        /// </summary>
        /// <remarks>Words the application sets as <see cref="Text" /> become the answer the next one is compared with.</remarks>
        public event Action Committed;

        /// <summary>Gets or sets the style of this combo box, or null to use the default its host's theme holds for a combo box.</summary>
        public new IStyle<ComboBox> Style
        {
            get { return (IStyle<ComboBox>)StyleSet; }
            set { SetStyle(value); }
        }

        /// <summary>Gets this combo box's own look, applied in every state on top of the style's look.</summary>
        public new ComboBoxLook Base => (ComboBoxLook)base.Base;

        /// <summary>Gets this combo box's own look when hovered, applied on top of the style's look.</summary>
        public new ComboBoxLook PointerOver => (ComboBoxLook)base.PointerOver;

        /// <summary>Gets this combo box's own look when disabled, applied on top of the style's look.</summary>
        public new ComboBoxLook Disabled => (ComboBoxLook)base.Disabled;

        /// <summary>
        ///     Gets this combo box's own look for <paramref name="state" />, or for every state that includes it, applied on
        ///     top of the style's look.
        /// </summary>
        public new ComboBoxLook this[State state] => (ComboBoxLook)base[state];

        /// <summary>Gets what the list holds, top to bottom: the choices, and separators or other menu items between them.</summary>
        public Collection Items => _list.Items;

        /// <summary>Gets or sets the words in the field, which become the answer <see cref="Committed" /> compares with.</summary>
        public string Text
        {
            get { return _field.Text; }
            set
            {
                _field.Text = value ?? "";
                _committed = _field.Text;
            }
        }

        /// <summary>Gets or sets the words the field shows while it is empty and does not hold the keyboard, or null for none.</summary>
        public string Hint
        {
            get { return _field.Hint; }
            set { _field.Hint = value; }
        }

        /// <summary>Gets or sets the most characters the player can type into the field.</summary>
        public int MaxLength
        {
            get { return _field.MaxLength; }
            set { _field.MaxLength = value; }
        }

        /// <summary>
        ///     Gets or sets the callback that returns true if a choice stays in the list for what is typed, or null to keep
        ///     the choices whose words <see cref="TextSearch.Matches" /> it.
        /// </summary>
        public Func<ComboBoxItem, string, bool> FnIsFound { get; set; }

        /// <summary>Whether the list is open.</summary>
        public bool IsOpen => _list.IsOpen;

        /// <summary>Gets the field the player types into, which holds the keyboard for the combo box.</summary>
        public Element Field => _field;

        /// <summary>Gets the color of the arrow.</summary>
        /// <remarks>Set by this combo box's looks.</remarks>
        public Color Arrow { get; private set; }

        /// <summary>Gets the width and height of the arrow, in UI units.</summary>
        /// <remarks>Set by this combo box's looks.</remarks>
        public float ArrowSize => _arrowSize;

        /// <summary>Gets the space between the field and the arrow, in UI units.</summary>
        /// <remarks>Set by this combo box's looks.</remarks>
        public float ArrowSpacing => _arrowSpacing;

        /// <summary>Gets the style of the field, or null to use the host theme's text box style.</summary>
        /// <remarks>Set by this combo box's looks.</remarks>
        public IStyle<TextBox> FieldStyle { get; private set; }

        /// <summary>Gets the style of the list, or null to use the host theme's menu style.</summary>
        /// <remarks>Set by this combo box's looks.</remarks>
        public IStyle<Menu> ListStyle { get; private set; }

        /// <summary>Gets the space between the edge of the box and its field and arrow.</summary>
        /// <remarks>Set by this combo box's looks.</remarks>
        public Thickness Padding => _padding;

        /// <inheritdoc />
        protected override Thickness Inset => _padding;

        /// <summary>
        ///     Opens the list under the box with every choice in it, the one whose words are in the field marked and lit,
        ///     unless it is open already.
        /// </summary>
        /// <remarks>Throws where the box is on no screen or window.</remarks>
        public void Open()
        {
            if (_list.IsOpen)
                return;

            ShowEverything();
            var current = Current();
            Show(current != null && Usable(current) ? current : null);
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
        /// <remarks>Puts the words of <paramref name="item" /> in the field and gives them as the answer.</remarks>
        void IComboBox.Picked(ComboBoxItem item)
        {
            _field.Text = item.Text;
            _field.MoveCaret(_field.Length, false);
            Commit();
        }

        /// <inheritdoc />
        /// <remarks>Throws where the combo box's command was replaced: pressing it opens its list, and does nothing else.</remarks>
        protected override void OnPressed()
        {
            if (Command != _toggle)
                throw new InvalidOperationException("A combo box has no other command; pressing it opens its list.");
        }

        /// <inheritdoc />
        protected override bool Fits(IStyle style)
        {
            return style is IStyle<ComboBox>;
        }

        /// <inheritdoc />
        protected override ElementLook NewLook()
        {
            return new ComboBoxLook();
        }

        /// <inheritdoc />
        protected override void Apply(ElementLook look)
        {
            base.Apply(look);

            var box = (ComboBoxLook)look;
            Arrow = box.Arrow ?? Color.White;
            FieldStyle = box.FieldStyle;
            _field.Style = FieldStyle;
            _field.IsEnabled = !Is(State.Disabled);
            ListStyle = box.ListStyle;
            _list.Style = ListStyle;

            var arrowSize = box.ArrowSize ?? 0f;
            var arrowSpacing = box.ArrowSpacing ?? 0f;
            if (arrowSize == _arrowSize && arrowSpacing == _arrowSpacing)
                return;

            _arrowSize = arrowSize;
            _arrowSpacing = arrowSpacing;
            InvalidateMeasure();
        }

        /// <inheritdoc />
        /// <remarks>As large as the field asks, and the arrow after it.</remarks>
        protected override Vector2 MeasureContent(IGraphics g, Vector2 available)
        {
            var aside = Aside();
            _field.Measure(g, new Vector2(MathHelper.Max(0f, available.X - aside), available.Y));
            var field = _field.DesiredSize;
            return new Vector2(field.X + aside, MathHelper.Max(field.Y, _arrowSize));
        }

        /// <inheritdoc />
        /// <remarks>Lays the field across what the arrow leaves, centered down.</remarks>
        protected override void ArrangeContent(RectangleF bounds)
        {
            var wide = MathHelper.Max(0f, bounds.Width - Aside());
            var tall = MathHelper.Min(_field.DesiredSize.Y, bounds.Height);
            _field.Arrange(new RectangleF(bounds.X, bounds.Y + (bounds.Height - tall) * 0.5f, wide, tall));
        }

        /// <inheritdoc />
        /// <remarks>The field takes every press on the box left of the middle of the space before the arrow, and the box the rest.</remarks>
        protected override Element HitTestContent(Vector2 point)
        {
            var field = _field.Bounds;
            return Hits(point) && point.X < field.X + field.Width + _arrowSpacing * 0.5f ? _field.HitTest(Clamped(point, field)) : null;
        }

        /// <inheritdoc />
        protected override void DrawContent(IGraphics g)
        {
            _field.Draw(g);
            if (_arrowSize > 0f && Arrow.A > 0)
            {
                var bounds = ContentBounds;
                Chevrons.Down(g, new Vector2(bounds.X + bounds.Width - _arrowSize * 0.5f, bounds.Y + bounds.Height * 0.5f), _arrowSize, Arrow);
            }
        }

        /// <inheritdoc />
        /// <remarks>
        ///     Down and Up open the list where it is closed, and light the next or previous usable choice where it is open.
        ///     Enter picks the lit choice, or gives what is typed, closing the list; with the list closed it goes on to what
        ///     holds the box once the answer is given. Escape closes an open list.
        /// </remarks>
        protected override bool OnIntent(InputRoot input, Intent intent)
        {
            switch (intent)
            {
                case Intent.Up:
                case Intent.Down:
                    if (!_list.IsOpen)
                        Open();
                    else
                        Step(input, intent == Intent.Down ? 1 : -1);

                    return true;
                case Intent.Accept:
                    if (!_list.IsOpen)
                    {
                        Commit();
                        return false;
                    }

                    var lit = _list.Highlight;
                    if (lit != null && input.Press(lit))
                        return true;

                    Close();
                    Commit();
                    return true;
                case Intent.Cancel:
                    if (!_list.IsOpen)
                        return false;

                    Close();
                    return true;
                default:
                    return false;
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
        ///     Opens the list, if it is closed, with the choices whose words are in the field marked and
        ///     <paramref name="lit" /> lit, or none where it is null.
        /// </summary>
        void Show(MenuItem lit)
        {
            var text = Text;
            foreach (var element in Items)
            {
                var item = element as MenuItem;
                if (item == null)
                    continue;

                item.FocusTarget = _field;
                item.IsChosen = item is ComboBoxItem && Same(item.Text, text);
            }

            if (!_list.IsOpen)
                _list.Open();

            _list.Highlight = lit;
        }

        /// <summary>Gives what the field holds as the answer, where it differs from the last.</summary>
        void Commit()
        {
            var text = _field.Text;
            if (text == _committed)
                return;

            _committed = text;
            Committed?.Invoke();
        }

        void OnFieldEdited()
        {
            Edited?.Invoke();
            Narrow();
        }

        /// <summary>Closes the list and gives what is typed as the answer, as the field loses the keyboard.</summary>
        void OnFieldLostFocus()
        {
            Set(State.Focused, false);
            Close();
            Commit();
        }

        /// <summary>
        ///     Hides the choices that do not match what is typed, with nothing lit, and opens the list while any usable choice
        ///     is left, or closes it where none is.
        /// </summary>
        /// <remarks>A choice the application hid stays out of this, so closing the list leaves it hidden.</remarks>
        void Narrow()
        {
            ShowEverything();

            var text = Text;
            var found = false;
            foreach (var element in Items)
            {
                if (!element.IsVisible)
                    continue;

                var item = element as ComboBoxItem;
                if (item != null && (string.IsNullOrWhiteSpace(text) || IsFound(item, text)))
                {
                    found |= item.IsEnabled;
                    continue;
                }

                if (element is Separator || string.IsNullOrWhiteSpace(text))
                    continue;

                element.IsVisible = false;
                _narrowed.Add(element);
            }

            if (found)
                Show(null);
            else
                Close();
        }

        /// <summary>Returns true if <paramref name="item" /> stays in the list for <paramref name="text" />.</summary>
        bool IsFound(ComboBoxItem item, string text)
        {
            return FnIsFound != null ? FnIsFound(item, text) : TextSearch.Matches(item.Text, text);
        }

        /// <summary>Brings back what narrowing hid.</summary>
        void ShowEverything()
        {
            foreach (var element in _narrowed)
                element.IsVisible = true;

            _narrowed.Clear();
        }

        /// <summary>Returns the first choice whose words are the field's, or null if there is none.</summary>
        ComboBoxItem Current()
        {
            var text = Text;
            foreach (var element in Items)
            {
                var item = element as ComboBoxItem;
                if (item != null && Same(item.Text, text))
                    return item;
            }

            return null;
        }

        /// <summary>
        ///     Lights the next usable item <paramref name="by" /> one step down, or up where it is negative, around the ends,
        ///     telling <paramref name="input" /> the keys reached it.
        /// </summary>
        void Step(InputRoot input, int by)
        {
            var count = Items.Count;
            if (count == 0)
                return;

            var lit = _list.Highlight;
            var from = lit == null ? (by > 0 ? -1 : count) : Items.IndexOf(lit);
            for (var step = 1; step <= count; step++)
            {
                var item = Items[((from + by * step) % count + count) % count] as MenuItem;
                if (item == null || !Usable(item))
                    continue;

                _list.Highlight = item;
                input.Reach(item);
                return;
            }
        }

        /// <summary>Returns the width the arrow takes at the right, its spacing included, or 0 without one.</summary>
        float Aside()
        {
            return _arrowSize > 0f ? _arrowSpacing + _arrowSize : 0f;
        }

        /// <summary>Returns <paramref name="point" /> moved to the nearest point of <paramref name="bounds" />.</summary>
        static Vector2 Clamped(Vector2 point, RectangleF bounds)
        {
            return new Vector2(MathHelper.Clamp(point.X, bounds.X, bounds.X + bounds.Width), MathHelper.Clamp(point.Y, bounds.Y, bounds.Y + bounds.Height));
        }

        /// <summary>Returns true if <paramref name="a" /> and <paramref name="b" /> are the same words, whatever their case.</summary>
        static bool Same(string a, string b)
        {
            return string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Returns true if <paramref name="item" /> is shown in the list and can be picked there.</summary>
        static bool Usable(MenuItem item)
        {
            return item.IsVisible && item.IsEnabled;
        }
    }
}
