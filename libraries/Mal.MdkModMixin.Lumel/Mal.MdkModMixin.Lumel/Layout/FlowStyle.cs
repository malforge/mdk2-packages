using Lumel.Styling;

namespace Lumel.Layout
{
    /// <summary>The style for a flow: its box and the space between its children and lines.</summary>
    public class FlowStyle : Style<Flow, FlowLook>
    {
        /// <summary>Creates a style that sets nothing, leaving the flow at its neutral values.</summary>
        public FlowStyle()
        {
        }

        /// <summary>
        ///     Creates a style that includes everything in <paramref name="basedOn" />, with whatever this style sets
        ///     applied on top.
        /// </summary>
        public FlowStyle(IStyle<Element> basedOn) : base(basedOn)
        {
        }
    }
}
