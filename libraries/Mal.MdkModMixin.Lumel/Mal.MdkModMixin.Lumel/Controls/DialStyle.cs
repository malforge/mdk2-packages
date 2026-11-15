using Lumel.Styling;

namespace Lumel.Controls
{
    /// <summary>
    ///     The style of a dial: its marks and its needle when idle, on hover, while turned and when disabled.
    /// </summary>
    public class DialStyle : Style<Dial, DialLook>
    {
        /// <summary>Creates a style that sets nothing, leaving the dial at its neutral values.</summary>
        public DialStyle()
        {
        }

        /// <summary>
        ///     Creates a style that includes everything in <paramref name="basedOn" />, with whatever this style sets
        ///     layered over it.
        /// </summary>
        public DialStyle(IStyle<Dial> basedOn) : base(basedOn)
        {
        }

        /// <summary>Gets the look changes applied while the pointer is turning the needle.</summary>
        public DialLook Dragging => this[Dial.Dragging];
    }
}
