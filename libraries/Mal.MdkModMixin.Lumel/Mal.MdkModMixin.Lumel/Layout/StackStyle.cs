using Lumel.Styling;

namespace Lumel.Layout
{
    /// <summary>The style for a stack: its box and the space between its children.</summary>
    public class StackStyle : Style<Stack, StackLook>
    {
        /// <summary>Creates a style that sets nothing, leaving the stack at its neutral values.</summary>
        public StackStyle()
        {
        }

        /// <summary>
        ///     Creates a style that includes everything in <paramref name="basedOn" />, with whatever this style sets
        ///     applied on top.
        /// </summary>
        public StackStyle(IStyle<Element> basedOn) : base(basedOn)
        {
        }
    }
}
