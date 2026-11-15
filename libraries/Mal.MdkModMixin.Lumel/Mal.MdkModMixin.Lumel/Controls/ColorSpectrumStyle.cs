using Lumel.Styling;

namespace Lumel.Controls
{
    /// <summary>The style of a color spectrum: its pictures and its marker.</summary>
    public class ColorSpectrumStyle : Style<ColorSpectrum, ColorSpectrumLook>
    {
        /// <summary>Creates a style that sets nothing, leaving the spectrum at its neutral values.</summary>
        public ColorSpectrumStyle()
        {
        }

        /// <summary>
        ///     Creates a style that includes everything in <paramref name="basedOn" />, with whatever this style sets
        ///     layered over it.
        /// </summary>
        public ColorSpectrumStyle(IStyle<ColorSpectrum> basedOn) : base(basedOn)
        {
        }
    }
}
