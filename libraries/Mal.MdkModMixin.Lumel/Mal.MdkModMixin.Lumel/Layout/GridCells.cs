using System;

namespace Lumel.Layout
{
    /// <summary>The elements in each cell of a grid, addressed by row and column.</summary>
    /// <remarks>Made by the grid that holds them.</remarks>
    public abstract class GridCells
    {
        readonly Func<int, int, Element> _at;
        readonly Action<Element, int, int, int, int> _place;

        /// <summary>
        ///     Creates cells that are read through <paramref name="at" />, by row and column, and filled through
        ///     <paramref name="place" />, with the element, its row and column, and the number of rows and columns it
        ///     spans.
        /// </summary>
        protected GridCells(Func<int, int, Element> at, Action<Element, int, int, int, int> place)
        {
            _at = at;
            _place = place;
        }

        /// <summary>
        ///     Gets or sets the element in the cell at <paramref name="row" /> and <paramref name="column" />, or null.
        /// </summary>
        /// <remarks>Setting puts one there, replacing whatever held exactly that cell; null empties it.</remarks>
        public Element this[int row, int column]
        {
            get { return _at(row, column); }
            set { _place(value, row, column, 1, 1); }
        }

        /// <summary>
        ///     Gets or sets the element that starts at <paramref name="row" /> and <paramref name="column" /> and spans
        ///     <paramref name="rows" /> rows and <paramref name="columns" /> columns, or null.
        /// </summary>
        /// <remarks>Setting puts one there, replacing whatever held exactly that span; null empties it.</remarks>
        public Element this[int row, int column, int rows, int columns]
        {
            get { return _at(row, column); }
            set { _place(value, row, column, rows, columns); }
        }
    }
}
