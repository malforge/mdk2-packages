using Lumel.Styling;

namespace Lumel.Controls
{
    /// <summary>The style of a color palette: the room between its swatches, their size and their style.</summary>
    public class ColorPaletteStyle : Style<ColorPalette, ColorPaletteLook>
    {
        /// <summary>Creates a style that sets nothing, leaving the palette at its neutral values.</summary>
        public ColorPaletteStyle()
        {
        }

        /// <summary>
        ///     Creates a style that includes everything in <paramref name="basedOn" />, with whatever this style sets
        ///     layered over it.
        /// </summary>
        public ColorPaletteStyle(IStyle<ColorPalette> basedOn) : base(basedOn)
        {
        }
    }
}
