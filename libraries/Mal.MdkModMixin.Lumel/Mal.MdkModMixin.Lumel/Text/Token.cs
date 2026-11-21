using Lumel.Controls;
using Lumel.Input;
using Lumel.Styling;
using VRageMath;

namespace Lumel.Text
{
    /// <summary>
    ///     A thing named in a <see cref="TokenBox" /> as one unit among its words, drawn as a pill: the caret steps over it,
    ///     a selection takes it whole, and Backspace or Delete removes it whole.
    /// </summary>
    /// <remarks>
    ///     It shows its <see cref="Button.Text" />, or content given to it, such as an icon beside words. A press on it raises
    ///     <see cref="TokenBox.TokenPressed" />. A token is in one box at a time, and once.
    /// </remarks>
    public class Token : Button
    {
        /// <summary>The state of a token marked as wrong, such as a reference to nothing.</summary>
        public static readonly State Marked = State.Declare("Marked", 400);

        /// <summary>The state of a token inside its box's selection.</summary>
        public static readonly State Selected = State.Declare("Selected", 250);

        /// <summary>Creates a token showing <paramref name="text" />, standing for <paramref name="value" />.</summary>
        public Token(string text, object value)
        {
            Text = text;
            Value = value;
        }

        /// <summary>
        ///     Gets or sets the style of this token, or null to use the default from the host's theme for a token.
        /// </summary>
        public new IStyle<Token> Style
        {
            get { return (IStyle<Token>)StyleSet; }
            set { SetStyle(value); }
        }

        /// <summary>Gets or sets what the token stands for, which the application gives it.</summary>
        public object Value { get; set; }

        /// <summary>Gets or sets whether the token is marked as wrong, which its style shows.</summary>
        /// <remarks>Its <see cref="Element.Tip" /> can say why.</remarks>
        public bool IsMarked
        {
            get { return Is(Marked); }
            set { Set(Marked, value); }
        }

        /// <summary>Whether the token is inside its box's selection.</summary>
        /// <remarks>Set by the box as its selection moves.</remarks>
        public bool IsSelected => Is(Selected);

        /// <inheritdoc />
        protected override bool TakesPointer => true;

        /// <inheritdoc />
        /// <remarks>A left press released on the token raises its box's <see cref="TokenBox.TokenPressed" />.</remarks>
        protected override void OnPointerUp(Vector2 point, PointerButton button)
        {
            var pressed = IsPressed && button == PointerButton.Left;
            base.OnPointerUp(point, button);
            if (pressed)
                (Parent as ITokenHolder)?.Press(this);
        }

        /// <inheritdoc />
        protected override bool Fits(IStyle style)
        {
            return style is IStyle<Token>;
        }
    }
}
