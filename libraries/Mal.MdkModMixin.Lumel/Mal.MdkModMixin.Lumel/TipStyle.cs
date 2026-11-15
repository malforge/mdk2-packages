using Lumel.Styling;

namespace Lumel
{
    /// <summary>The style of a tip: its box, text, shadow, widest width and delay before it opens.</summary>
    public class TipStyle : Style<Tip, TipLook>
    {
        /// <summary>Creates a style that sets nothing, leaving the tip at its neutral values.</summary>
        public TipStyle()
        {
        }

        /// <summary>
        ///     Creates a style that includes everything in <paramref name="basedOn" />, with whatever this style sets
        ///     layered over it.
        /// </summary>
        public TipStyle(IStyle<Tip> basedOn) : base(basedOn)
        {
        }
    }
}
