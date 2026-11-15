using Lumel.Styling;

namespace Lumel.Controls
{
    /// <summary>The style of a separator: its line and the space around it.</summary>
    public class SeparatorStyle : Style<Separator, SeparatorLook>
    {
        /// <summary>Creates a style that sets nothing, leaving the separator at its neutral values.</summary>
        public SeparatorStyle()
        {
        }

        /// <summary>
        ///     Creates a style that includes everything in <paramref name="basedOn" />, with whatever this style sets
        ///     layered over it.
        /// </summary>
        public SeparatorStyle(IStyle<Separator> basedOn) : base(basedOn)
        {
        }
    }
}
