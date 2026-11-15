using Lumel.Styling;

namespace Lumel.Controls
{
    /// <summary>
    ///     The style of a <see cref="Swatch" />: its border around the color it shows, and how strongly it shows it.
    /// </summary>
    public class SwatchStyle : Style<Swatch, SwatchLook>
    {
        /// <summary>Creates a style that sets nothing, leaving the swatch at its neutral values.</summary>
        public SwatchStyle()
        {
        }

        /// <summary>
        ///     Creates a style that includes everything in <paramref name="basedOn" />, with this style's own settings
        ///     applied over it.
        /// </summary>
        public SwatchStyle(IStyle<Element> basedOn) : base(basedOn)
        {
        }
    }
}
