using Lumel.Styling;

namespace Lumel.Controls
{
    /// <summary>The style of a button: its text and box when idle, on hover, while pressed and when disabled.</summary>
    public class ButtonStyle : Style<Button, TextLook>
    {
        /// <summary>Creates a style that sets nothing, leaving the button at its neutral values.</summary>
        public ButtonStyle()
        {
        }

        /// <summary>
        ///     Creates a style that includes everything in <paramref name="basedOn" />, with whatever this style sets
        ///     layered over it.
        /// </summary>
        public ButtonStyle(IStyle<Button> basedOn) : base(basedOn)
        {
        }

        /// <summary>Gets the look changes applied while the pointer is pressed on the button.</summary>
        public TextLook Pressed => this[Button.Pressed];
    }
}
