using Lumel.Styling;

namespace Lumel.Controls
{
    /// <summary>The style of a slider: its track, the fill along it and its thumb, in each of its states.</summary>
    public class SliderStyle : Style<Slider, SliderLook>
    {
        /// <summary>Creates a style that sets nothing, leaving the slider at its neutral values.</summary>
        public SliderStyle()
        {
        }

        /// <summary>
        ///     Creates a style that includes everything in <paramref name="basedOn" />, with whatever this style sets
        ///     layered over it.
        /// </summary>
        public SliderStyle(IStyle<Slider> basedOn) : base(basedOn)
        {
        }
    }
}
