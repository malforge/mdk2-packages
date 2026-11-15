using Lumel.Styling;

namespace Lumel.Controls
{
    /// <summary>The style of a spin box: the space between its parts, and the styles of its number box and buttons.</summary>
    public class SpinBoxStyle : Style<SpinBox, SpinBoxLook>
    {
        /// <summary>Creates a style that sets nothing, leaving the spin box at its neutral values.</summary>
        public SpinBoxStyle()
        {
        }

        /// <summary>
        ///     Creates a style that includes everything in <paramref name="basedOn" />, with whatever this style sets
        ///     layered over it.
        /// </summary>
        public SpinBoxStyle(IStyle<SpinBox> basedOn) : base(basedOn)
        {
        }
    }
}
