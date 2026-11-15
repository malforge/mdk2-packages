using Lumel.Styling;

namespace Lumel.Controls
{
    /// <summary>The style of a radio button: its text, ring and dot when idle, checked, on hover and when disabled.</summary>
    public class RadioButtonStyle : Style<RadioButton, RadioButtonLook>
    {
        /// <summary>Creates a style that sets nothing, leaving the radio button at its neutral values.</summary>
        public RadioButtonStyle()
        {
        }

        /// <summary>
        ///     Creates a style that includes everything in <paramref name="basedOn" />, with whatever this style sets
        ///     layered over it.
        /// </summary>
        public RadioButtonStyle(IStyle<RadioButton> basedOn) : base(basedOn)
        {
        }

        /// <summary>Gets the look changes applied while the button is checked.</summary>
        public RadioButtonLook Checked => this[RadioButton.Checked];
    }
}
