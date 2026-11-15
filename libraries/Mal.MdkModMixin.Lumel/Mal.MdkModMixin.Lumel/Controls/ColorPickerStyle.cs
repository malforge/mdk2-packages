using Lumel.Styling;

namespace Lumel.Controls
{
    /// <summary>The style of a color picker: its box and the color it shows when idle, hovered, open and disabled.</summary>
    public class ColorPickerStyle : Style<ColorPicker, ColorPickerLook>
    {
        /// <summary>Creates a style that sets nothing, leaving the picker at its neutral values.</summary>
        public ColorPickerStyle()
        {
        }

        /// <summary>
        ///     Creates a style that includes everything in <paramref name="basedOn" />, with whatever this style sets
        ///     layered over it.
        /// </summary>
        public ColorPickerStyle(IStyle<ColorPicker> basedOn) : base(basedOn)
        {
        }

        /// <summary>Gets the look changes applied while the picker's popup is open.</summary>
        public ColorPickerLook Expanded => this[ColorPicker.Expanded];
    }
}
