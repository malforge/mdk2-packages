using Lumel.Styling;

namespace Lumel.Text
{
    /// <summary>The style of a number box: the text box style, applied to a box that holds a number.</summary>
    public class NumberBoxStyle : Style<NumberBox, TextBoxLook>
    {
        /// <summary>Creates a style that sets nothing, leaving the box at its default values.</summary>
        public NumberBoxStyle()
        {
        }

        /// <summary>
        ///     Creates a style that inherits everything from <paramref name="basedOn" /> and applies its own settings
        ///     over it.
        /// </summary>
        public NumberBoxStyle(IStyle<TextBox> basedOn) : base(basedOn)
        {
        }

        /// <summary>Gets the look that applies while the box has keyboard focus.</summary>
        public TextBoxLook Focused => this[State.Focused];
    }
}
