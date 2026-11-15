namespace Lumel.Layout
{
    partial class Grid
    {
        /// <summary>Where one element sits, and what its placement says about its size.</summary>
        sealed class Cell
        {
            public readonly int Column;
            public readonly int Columns;
            public readonly Element Element;
            public readonly int Row;
            public readonly int Rows;
            public float Height = float.NaN;
            public float Width = float.NaN;

            public Cell(Element element, int row, int column, int rows, int columns)
            {
                Element = element;
                Row = row;
                Column = column;
                Rows = rows;
                Columns = columns;
            }

            public bool Covers(int row, int column)
            {
                return row >= Row && row < Row + Rows && column >= Column && column < Column + Columns;
            }

            public bool Overlaps(Cell other)
            {
                return Row < other.Row + other.Rows && other.Row < Row + Rows
                    && Column < other.Column + other.Columns && other.Column < Column + Columns;
            }

            public void Resolve(IPlacement placement)
            {
                var sized = placement as ISized;
                Width = sized?.Width ?? float.NaN;
                Height = sized?.Height ?? float.NaN;
            }
        }
    }
}
