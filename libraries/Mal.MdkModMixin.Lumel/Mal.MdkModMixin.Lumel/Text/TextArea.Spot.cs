namespace Lumel.Text
{
    partial class TextArea
    {
        /// <summary>A position between two characters: a line and a column within it.</summary>
        struct Spot
        {
            public readonly int Line;
            public readonly int Column;

            public Spot(int line, int column)
            {
                Line = line;
                Column = column;
            }

            public bool Before(Spot other)
            {
                return Line < other.Line || (Line == other.Line && Column < other.Column);
            }
        }
    }
}
