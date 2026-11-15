using Lumel.Styling;

namespace Lumel.Text
{
    /// <summary>
    ///     The style of a text area: its text, caret, selection, hint, line numbers and scroll bars, when idle,
    ///     hovered, focused, dragging a thumb and disabled.
    /// </summary>
    public class TextAreaStyle : Style<TextArea, TextAreaLook>
    {
        /// <summary>Creates a style that sets nothing, leaving the text area at its neutral values.</summary>
        public TextAreaStyle()
        {
        }

        /// <summary>
        ///     Creates a style that inherits everything from <paramref name="basedOn" /> and overrides whatever this
        ///     style sets.
        /// </summary>
        public TextAreaStyle(IStyle<TextArea> basedOn) : base(basedOn)
        {
        }

        /// <summary>Gets the look that applies when the text area has keyboard focus.</summary>
        public TextAreaLook Focused => this[State.Focused];

        /// <summary>Gets the look that applies while a thumb is being dragged.</summary>
        public TextAreaLook Dragging => this[TextArea.Dragging];
    }
}
