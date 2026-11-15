using Lumel.Styling;

namespace Lumel.Controls
{
    /// <summary>
    ///     The style of a reorder list: its box, the space between its rows and how far a row in a group is indented.
    /// </summary>
    public class ReorderListStyle : Style<ReorderList, ReorderListLook>
    {
        /// <summary>Creates a style that sets nothing, leaving the list at its neutral values.</summary>
        public ReorderListStyle()
        {
        }

        /// <summary>
        ///     Creates a style that includes everything in <paramref name="basedOn" />, with whatever this style sets
        ///     layered over it.
        /// </summary>
        public ReorderListStyle(IStyle<Element> basedOn) : base(basedOn)
        {
        }
    }
}
