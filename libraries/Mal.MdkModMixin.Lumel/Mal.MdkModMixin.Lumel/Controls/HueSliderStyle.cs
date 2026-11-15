using Lumel.Styling;

namespace Lumel.Controls
{
    /// <summary>The style of a hue slider: its picture and its marker.</summary>
    public class HueSliderStyle : Style<HueSlider, HueSliderLook>
    {
        /// <summary>Creates a style that sets nothing, leaving the slider at its neutral values.</summary>
        public HueSliderStyle()
        {
        }

        /// <summary>
        ///     Creates a style that includes everything in <paramref name="basedOn" />, with whatever this style sets
        ///     layered over it.
        /// </summary>
        public HueSliderStyle(IStyle<HueSlider> basedOn) : base(basedOn)
        {
        }
    }
}
