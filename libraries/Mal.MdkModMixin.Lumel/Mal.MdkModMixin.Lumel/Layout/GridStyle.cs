using Lumel.Styling;

namespace Lumel.Layout
{
    /// <summary>The style for a grid: its box and the space between its columns and rows.</summary>
    public class GridStyle : Style<Grid, GridLook>
    {
        /// <summary>Creates a style that sets nothing, leaving the grid at its neutral values.</summary>
        public GridStyle()
        {
        }

        /// <summary>
        ///     Creates a style that includes everything in <paramref name="basedOn" />, with whatever this style sets
        ///     applied on top.
        /// </summary>
        public GridStyle(IStyle<Element> basedOn) : base(basedOn)
        {
        }
    }
}
