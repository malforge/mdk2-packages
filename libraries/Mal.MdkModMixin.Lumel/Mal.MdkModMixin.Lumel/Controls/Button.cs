using Lumel.Input;
using Lumel.Text;
using VRage.Game;
using VRageMath;
using Lumel.Styling;

namespace Lumel.Controls
{
    /// <summary>
    ///     A pressable control with its own fill, showing either its own text or the content given to it, centered.
    /// </summary>
    /// <remarks>
    /// The button's looks dress its own words. Content given to it is dressed by whoever gives it, and is drawn only: a press
    /// anywhere on the button reaches the button, unless <see cref="Element.InputTransparency" /> is set to let it through.
    /// </remarks>
    public class Button : ContentControl, ITextual
    {
        /// <summary>The state of a button while the pointer is pressed on it.</summary>
        public static readonly State Pressed = State.Declare("Pressed", 300);

        // The button's own words take no look but the one the button gives them.
        static readonly IStyle<Label> Bare = new LabelStyle();

        readonly Label _label = new Label { Style = Bare, TextWrapping = TextWrapping.Trim };

        /// <summary>
        ///     Creates a button with no text, using the look from the host's theme for a button until it is given a
        ///     style.
        /// </summary>
        public Button()
        {
            Content = _label;
            InputTransparency = InputTransparency.Children;
            Font = MyFontEnum.White;
            FontSize = TextLook.NeutralFontSize;
            Color = Color.White;
        }

        /// <summary>
        ///     Gets or sets the style of this button, or null to use the default from the host's theme for a button.
        /// </summary>
        public new IStyle<Button> Style
        {
            get { return (IStyle<Button>)StyleSet; }
            set { SetStyle(value); }
        }

        /// <summary>
        ///     Gets this button's own look, applied in every state and layered over the look from its style.
        /// </summary>
        public new TextLook Base => (TextLook)base.Base;

        /// <summary>Gets this button's own look applied on hover, layered over the look from its style.</summary>
        public new TextLook PointerOver => (TextLook)base.PointerOver;

        /// <summary>Gets this button's own look applied when disabled, layered over the look from its style.</summary>
        public new TextLook Disabled => (TextLook)base.Disabled;

        /// <summary>
        ///     Gets this button's own look for <paramref name="state" />, or for every state it joins, layered over the
        ///     look from its style.
        /// </summary>
        public new TextLook this[State state] => (TextLook)base[state];

        /// <summary>
        ///     Gets or sets the text on the button, which is shown instead of any content given to the button; empty
        ///     while the button shows its content.
        /// </summary>
        /// <remarks>Text wider than the button is cut short with an ellipsis.</remarks>
        public string Text
        {
            get { return ReferenceEquals(Content, _label) ? _label.Text : ""; }
            set
            {
                _label.Text = value;
                Content = _label;
            }
        }

        /// <summary>Whether the pointer is pressed on the button.</summary>
        /// <remarks>Set by input, from a left press begun on the button until its release, while the pointer is on it.</remarks>
        public bool IsPressed
        {
            get { return Is(Pressed); }
            private set { Set(Pressed, value); }
        }

        /// <summary>Gets the font the button's own text is drawn in.</summary>
        /// <remarks>Set by this button's looks.</remarks>
        public string Font { get; private set; }

        /// <summary>Gets the height of a line of the button's own text, in UI units.</summary>
        /// <remarks>Set by this button's looks.</remarks>
        public float FontSize { get; private set; }

        /// <summary>Gets the color of the button's own text.</summary>
        /// <remarks>Set by this button's looks.</remarks>
        public Color Color { get; private set; }

        /// <summary>
        ///     Gets the alignment of the button's contents, which are centered in the space the button is given even
        ///     when it is larger than they need.
        /// </summary>
        protected override Align Aligns => Align.Center;

        /// <inheritdoc />
        protected override void OnPointerDown(Vector2 point, PointerButton button, int clickCount, bool shift, bool ctrl)
        {
            if (button == PointerButton.Left)
                IsPressed = true;
        }

        /// <inheritdoc />
        protected override void OnPointerMove(Vector2 point, PointerButton button)
        {
            if (button == PointerButton.Left)
                IsPressed = Holds(point);
        }

        /// <inheritdoc />
        protected override void OnPointerUp(Vector2 point, PointerButton button)
        {
            if (button == PointerButton.Left)
                IsPressed = false;
        }

        /// <inheritdoc />
        protected override void OnPointerCaptureLost()
        {
            IsPressed = false;
        }

        /// <inheritdoc />
        protected override bool Fits(IStyle style)
        {
            return style is IStyle<Button>;
        }

        /// <inheritdoc />
        protected override ElementLook NewLook()
        {
            return new TextLook();
        }

        /// <inheritdoc />
        protected override void Apply(ElementLook look)
        {
            base.Apply(look);

            var text = (TextLook)look;
            Font = text.Font ?? MyFontEnum.White;
            FontSize = text.FontSize ?? TextLook.NeutralFontSize;
            Color = text.Color ?? Color.White;

            Dress(_label.Base);
        }

        /// <summary>
        ///     Applies the font, size and color of the button's own text to <paramref name="words" />, changing only
        ///     what differs.
        /// </summary>
        void Dress(TextLook words)
        {
            if (words.Font != Font)
                words.Font = Font;

            if (words.FontSize != FontSize)
                words.FontSize = FontSize;

            if (words.Color != Color)
                words.Color = Color;
        }
    }
}
