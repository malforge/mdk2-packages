using System;

namespace Lumel.Layout
{
    public partial class Grid
    {
        /// <summary>The columns or rows of one grid, which only that grid creates.</summary>
        sealed class OwnedTracks : GridTracks
        {
            public OwnedTracks(Action changed) : base(changed)
            {
            }
        }

        /// <summary>The cells of one grid, which only that grid creates.</summary>
        sealed class OwnedCells : GridCells
        {
            public OwnedCells(Func<int, int, Element> at, Action<Element, int, int, int, int> place) : base(at, place)
            {
            }
        }
    }
}
