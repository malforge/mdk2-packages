using Lumel.Drawing;
using Lumel.Text;
using VRageMath;
using Lumel.Styling;

namespace Lumel.Controls
{
    /// <summary>
    ///     A box that is either checked or not, with text beside it describing what it stands for; pressing either one
    ///     presses the box.
    /// </summary>
    /// <remarks>
    ///     Checked, a square fills the middle of the box; partly checked, standing for several of which only some are checked,
    ///     a bar does. What a press does is its <see cref="Element.Command" />, which sets <see cref="IsChecked" /> as it sees fit.
    /// </remarks>
    public class CheckBox : Element, IPadded, ITextual
    {
        /// <summary>The state of a check box while it is checked.</summary>
        public static readonly State Checked = State.Declare("Checked", 60);

        /// <summary>
        ///     The state of a check box while it shows that some, but not all, of what it stands for is checked.
        /// </summary>
        public static readonly State Indeterminate = State.Declare("Indeterminate", 50);

        // The box's words take no look but the one the box gives them.
        static readonly IStyle<Label> Bare = new LabelStyle();

        readonly Label _word = new Label { Style = Bare, TextWrapping = TextWrapping.Trim };

        Thickness _padding;
        float? _boxSize;

        /// <summary>
        ///     Creates an unchecked check box with no text, using the look from the host's theme for a check box until
        ///     it is given a style.
        /// </summary>
        public CheckBox()
        {
            var children = CreateChildren();
            children.Add(_word);
            _word.IsVisible = false;
            Font = VRage.Game.MyFontEnum.White;
            FontSize = TextLook.NeutralFontSize;
            Color = Color.White;
            Box = Color.White;
            BoxThickness = 1f;
            Mark = Color.White;
        }

        /// <summary>
        ///     Gets or sets the style of this check box, or null to use the default from the host's theme for a check
        ///     box.
        /// </summary>
        public new IStyle<CheckBox> Style
        {
            get { return (IStyle<CheckBox>)StyleSet; }
            set { SetStyle(value); }
        }

        /// <summary>
        ///     Gets this check box's own look, applied in every state and layered over the look from its style.
        /// </summary>
        public new CheckBoxLook Base => (CheckBoxLook)base.Base;

        /// <summary>Gets this check box's own look applied on hover, layered over the look from its style.</summary>
        public new CheckBoxLook PointerOver => (CheckBoxLook)base.PointerOver;

        /// <summary>
        ///     Gets this check box's own look applied when disabled, layered over the look from its style.
        /// </summary>
        public new CheckBoxLook Disabled => (CheckBoxLook)base.Disabled;

        /// <summary>
        ///     Gets this check box's own look for <paramref name="state" />, or for every state it joins, layered over
        ///     the look from its style.
        /// </summary>
        public new CheckBoxLook this[State state] => (CheckBoxLook)base[state];

        /// <summary>Whether the check box is checked.</summary>
        /// <remarks>While it is checked, it shows its looks for <see cref="Checked" />.</remarks>
        public bool IsChecked
        {
            get { return Is(Checked); }
            set { Set(Checked, value); }
        }

        /// <summary>
        ///     Whether the check box shows that some, but not all, of what it stands for is checked; ignored while it
        ///     is checked.
        /// </summary>
        /// <remarks>While it is partly checked, it shows its looks for <see cref="Indeterminate" />, under those for <see cref="Checked" />.</remarks>
        public bool IsIndeterminate
        {
            get { return Is(Indeterminate); }
            set { Set(Indeterminate, value); }
        }

        /// <summary>Gets or sets the text beside the box, or empty to show the box alone.</summary>
        public string Text
        {
            get { return _word.Text; }
            set
            {
                _word.Text = value;
                _word.IsVisible = !string.IsNullOrEmpty(value);
                InvalidateMeasure();
            }
        }

        /// <summary>Gets the font the text is drawn in.</summary>
        /// <remarks>Set by this box's looks.</remarks>
        public string Font { get; private set; }

        /// <summary>Gets the height of a line of text, in UI units.</summary>
        /// <remarks>Set by this box's looks.</remarks>
        public float FontSize { get; private set; }

        /// <summary>Gets the color of the text.</summary>
        /// <remarks>Set by this box's looks.</remarks>
        public Color Color { get; private set; }

        /// <summary>Gets the space around the text.</summary>
        /// <remarks>Set by this box's looks.</remarks>
        public Thickness WordPadding { get; private set; }

        /// <summary>
        ///     Gets the width and height of the box in UI units, or null to make it as tall as a line of text.
        /// </summary>
        /// <remarks>Set by this box's looks.</remarks>
        public float? BoxSize
        {
            get { return _boxSize; }
            private set
            {
                if (_boxSize == value)
                    return;

                _boxSize = value;
                InvalidateMeasure();
            }
        }

        /// <summary>Gets the color of the box's border.</summary>
        /// <remarks>Set by this box's looks.</remarks>
        public Color Box { get; private set; }

        /// <summary>Gets the thickness of the box's border, in UI units.</summary>
        /// <remarks>Set by this box's looks.</remarks>
        public float BoxThickness { get; private set; }

        /// <summary>Gets the fill color of the inside of the box, or transparent to show what is behind it.</summary>
        /// <remarks>Set by this box's looks.</remarks>
        public Color BoxFill { get; private set; }

        /// <summary>Gets the color of the square or bar that shows the box as checked or partly checked.</summary>
        /// <remarks>Set by this box's looks.</remarks>
        public Color Mark { get; private set; }

        /// <inheritdoc />
        protected override bool Fits(IStyle style)
        {
            return style is IStyle<CheckBox>;
        }

        /// <inheritdoc />
        protected override ElementLook NewLook()
        {
            return new CheckBoxLook();
        }

        /// <inheritdoc />
        protected override void Apply(ElementLook look)
        {
            base.Apply(look);

            var box = (CheckBoxLook)look;
            Font = box.Font ?? VRage.Game.MyFontEnum.White;
            FontSize = box.FontSize ?? TextLook.NeutralFontSize;
            Color = box.Color ?? Color.White;
            WordPadding = box.WordPadding ?? default(Thickness);
            BoxSize = box.BoxSize;
            Box = box.Box ?? Color.White;
            BoxThickness = box.BoxThickness ?? 1f;
            BoxFill = box.BoxFill ?? Color.Transparent;
            Mark = box.Mark ?? Color.White;

            var words = _word.Base;
            if (words.Font != Font)
                words.Font = Font;

            if (words.FontSize != FontSize)
                words.FontSize = FontSize;

            if (words.Color != Color)
                words.Color = Color;

            if (words.Padding != WordPadding)
                words.Padding = WordPadding;
        }

        /// <summary>Gets the space between the edge of the check box and its box and text.</summary>
        /// <remarks>Set by this element's looks.</remarks>
        public Thickness Padding => _padding;

        /// <inheritdoc />
        void IPadded.Pad(Thickness padding)
        {
            if (_padding == padding)
                return;

            _padding = padding;
            InvalidateMeasure();
        }

        /// <inheritdoc />
        protected override Thickness Inset => _padding;

        float Side => _boxSize ?? FontSize;

        /// <inheritdoc />
        protected override Vector2 MeasureContent(IGraphics g, Vector2 available)
        {
            var side = Side;
            if (!_word.IsVisible)
                return new Vector2(side, side);

            _word.Measure(g, new Vector2(available.X - side, available.Y));
            var words = _word.DesiredSize;
            return new Vector2(side + words.X, words.Y > side ? words.Y : side);
        }

        /// <inheritdoc />
        protected override void ArrangeContent(RectangleF bounds)
        {
            var side = Side;
            if (_word.IsVisible)
                _word.Arrange(new RectangleF(bounds.X + side, bounds.Y, bounds.Width - side, bounds.Height));
        }

        /// <inheritdoc />
        protected override void DrawContent(IGraphics g)
        {
            var room = ContentBounds;
            var side = Side;
            var box = new RectangleF(room.X, room.Y + (room.Height - side) * 0.5f, side, side);

            if (BoxFill.A > 0)
                g.Fill(box, BoxFill);

            if (Box.A > 0)
                Outline(g, box, Box, BoxThickness);

            // The mark stands a quarter of the box in from each edge; partly checked, it is a bar a quarter of the box high.
            var inset = side * 0.25f;
            if (IsChecked)
                g.Fill(new RectangleF(box.X + inset, box.Y + inset, side - inset * 2f, side - inset * 2f), Mark);
            else if (IsIndeterminate)
                g.Fill(new RectangleF(box.X + inset, box.Y + (side - inset) * 0.5f, side - inset * 2f, inset), Mark);

            if (_word.IsVisible)
                _word.Draw(g);
        }
    }
}
