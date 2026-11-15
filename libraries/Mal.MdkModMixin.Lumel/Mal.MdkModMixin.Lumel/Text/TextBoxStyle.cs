using Lumel.Styling;

namespace Lumel.Text
{
    /// <summary>
    ///     The style of a text box: its text, caret, selection, hint and box, when idle, hovered, focused and disabled.
    /// </summary>
    public class TextBoxStyle : Style<TextBox, TextBoxLook>
    {
        /// <summary>Creates a style that sets nothing, leaving the text box at its neutral values.</summary>
        public TextBoxStyle()
        {
        }

        /// <summary>
        ///     Creates a style that inherits everything from <paramref name="basedOn" /> and overrides whatever this
        ///     style sets.
        /// </summary>
        public TextBoxStyle(IStyle<TextBox> basedOn) : base(basedOn)
        {
        }

        /// <summary>Gets the look that applies when the text box has keyboard focus.</summary>
        public TextBoxLook Focused => this[State.Focused];
    }
}
