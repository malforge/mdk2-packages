using System;
using Lumel.Drawing;
using Lumel.Images;
using Lumel.Input;
using Lumel.Layout;
using Lumel.Styling;
using Lumel.Text;
using VRageMath;

namespace Lumel.Controls
{
    /// <summary>A text box for what to look for, with a cross after it that empties it, shown while it holds anything.</summary>
    public class SearchBox : HStack
    {
        readonly TextBox _box;
        readonly Glyph _clear;

        /// <summary>
        ///     Creates an empty search box, using the look from the host's theme for a search box until it is given a style.
        /// </summary>
        public SearchBox()
        {
            Children.Add(new TextBox { Focusable = true, Placement = Flex.Grow }.As(out _box));
            Children.Add(new Glyph { IsVisible = false, FocusTarget = _box, Command = new RelayCommand(Empty) }.As(out _clear));
            _box.Edited += OnEdited;
        }

        /// <summary>Raised when the player changes the text, by typing or with the cross, but not when <see cref="Text" /> is set.</summary>
        public event Action Edited;

        /// <summary>
        ///     Gets or sets the style of this search box, or null to use the default its host's theme holds for a search box.
        /// </summary>
        public new IStyle<SearchBox> Style
        {
            get { return (IStyle<SearchBox>)StyleSet; }
            set { SetStyle(value); }
        }

        /// <summary>Gets this search box's own look, applied in every state on top of the style's look.</summary>
        public new SearchBoxLook Base => (SearchBoxLook)base.Base;

        /// <summary>Gets this search box's own look when hovered, applied on top of the style's look.</summary>
        public new SearchBoxLook PointerOver => (SearchBoxLook)base.PointerOver;

        /// <summary>Gets this search box's own look when disabled, applied on top of the style's look.</summary>
        public new SearchBoxLook Disabled => (SearchBoxLook)base.Disabled;

        /// <summary>
        ///     Gets this search box's own look for <paramref name="state" />, or for every state that includes it, applied on
        ///     top of the style's look.
        /// </summary>
        public new SearchBoxLook this[State state] => (SearchBoxLook)base[state];

        /// <summary>Gets or sets what is being looked for.</summary>
        public string Text
        {
            get { return _box.Text; }
            set
            {
                _box.Text = value;
                ShowCross();
            }
        }

        /// <summary>Gets or sets the words shown while the box is empty and does not hold the keyboard, or null for none.</summary>
        public string Hint
        {
            get { return _box.Hint; }
            set { _box.Hint = value; }
        }

        /// <summary>Gets the element that takes the keyboard for this search box.</summary>
        public Element Keyboard => _box;

        /// <inheritdoc />
        protected override bool Fits(IStyle style)
        {
            return style is IStyle<SearchBox>;
        }

        /// <inheritdoc />
        protected override ElementLook NewLook()
        {
            return new SearchBoxLook();
        }

        /// <inheritdoc />
        protected override void Apply(ElementLook look)
        {
            base.Apply(look);

            var search = (SearchBoxLook)look;
            _box.Style = search.BoxStyle;
            _clear.Style = search.ClearStyle;
            _clear.Symbol = search.ClearSymbol;

            var size = search.ClearSize ?? 0f;
            _clear.Placement = FixedSize.Size(size, size);
        }

        /// <inheritdoc />
        /// <remarks>As wide as its text needs, and the box takes whatever more it is given.</remarks>
        protected override Vector2 MeasureContent(IGraphics g, Vector2 available)
        {
            var needs = base.MeasureContent(g, new Vector2(float.PositiveInfinity, available.Y));
            return base.MeasureContent(g, new Vector2(needs.X, available.Y));
        }

        void OnEdited()
        {
            ShowCross();
            Edited?.Invoke();
        }

        /// <summary>Shows the cross while there is something for it to empty.</summary>
        void ShowCross()
        {
            _clear.IsVisible = _box.Length > 0;
        }

        /// <summary>Empties the box, as the player's edit.</summary>
        void Empty()
        {
            _box.Text = "";
            OnEdited();
        }
    }
}
