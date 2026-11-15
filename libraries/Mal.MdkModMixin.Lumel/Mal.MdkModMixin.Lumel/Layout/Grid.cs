using System;
using System.Collections.Generic;
using Lumel.Drawing;
using Lumel.Styling;
using VRageMath;

namespace Lumel.Layout
{
    /// <summary>
    ///     A container that places elements in cells of rows and columns, each track sized outright, by its largest
    ///     cell, or by a share of the remaining room.
    /// </summary>
    /// <remarks>One element to a cell, stretched across it and centered down it.</remarks>
    public partial class Grid : Container
    {
        readonly List<Cell> _cells = new List<Cell>();
        readonly Collection _children;

        int _columnCount;
        bool _columnsShare;
        float _columnSpacing;
        float[] _heights = new float[0];
        int _rowCount;
        bool _rowsShare;
        float _rowSpacing;
        float[] _widths = new float[0];
        float[] _xs = new float[0];
        float[] _ys = new float[0];

        /// <summary>Creates an empty grid in which every track is auto-sized.</summary>
        public Grid()
        {
            _children = CreateChildren();
            Columns = new OwnedTracks(InvalidateMeasure);
            Rows = new OwnedTracks(InvalidateMeasure);
            Cells = new OwnedCells(At, Place);
        }

        /// <summary>Gets or sets the style of this grid, or null to use the theme's default style for grids.</summary>
        public new IStyle<Grid> Style
        {
            get { return (IStyle<Grid>)StyleSet; }
            set { SetStyle(value); }
        }

        /// <summary>Gets this grid's own look, applied in every state on top of its style's.</summary>
        public new GridLook Base => (GridLook)base.Base;

        /// <summary>Gets this grid's own look for when it is hovered, applied on top of its style's.</summary>
        public new GridLook PointerOver => (GridLook)base.PointerOver;

        /// <summary>Gets this grid's own look for when it is disabled, applied on top of its style's.</summary>
        public new GridLook Disabled => (GridLook)base.Disabled;

        /// <summary>
        ///     Gets this grid's own look for <paramref name="state" />, or for every state that includes it, applied on
        ///     top of its style's.
        /// </summary>
        public new GridLook this[State state] => (GridLook)base[state];

        /// <summary>Gets the width of each column, left to right.</summary>
        public GridTracks Columns { get; }

        /// <summary>Gets the height of each row, top to bottom.</summary>
        public GridTracks Rows { get; }

        /// <summary>Gets the elements in each cell.</summary>
        public GridCells Cells { get; }

        /// <summary>Gets the blank space between one column and the next.</summary>
        /// <remarks>Set by this grid's looks.</remarks>
        public float ColumnSpacing => _columnSpacing;

        /// <summary>Gets the blank space between one row and the next.</summary>
        /// <remarks>Set by this grid's looks.</remarks>
        public float RowSpacing => _rowSpacing;

        /// <inheritdoc />
        protected override bool Fits(IStyle style)
        {
            return style is IStyle<Grid>;
        }

        /// <inheritdoc />
        protected override ElementLook NewLook()
        {
            return new GridLook();
        }

        /// <inheritdoc />
        protected override void Apply(ElementLook look)
        {
            base.Apply(look);

            var grid = (GridLook)look;
            var columns = grid.ColumnSpacing ?? 0f;
            var rows = grid.RowSpacing ?? 0f;
            if (columns == _columnSpacing && rows == _rowSpacing)
                return;

            _columnSpacing = columns;
            _rowSpacing = rows;
            InvalidateMeasure();
        }

        /// <summary>A cell stretches what it holds horizontally and centers it vertically.</summary>
        protected override Align Aligns => Align.FillWidth | Align.Center;

        /// <inheritdoc />
        protected override Vector2 MeasureContent(IGraphics g, Vector2 available)
        {
            Count();

            var boundedAcross = available.X < Unbounded;
            var boundedDown = available.Y < Unbounded;

            for (var c = 0; c < _columnCount; c++)
                _widths[c] = Columns[c].IsFixed ? Columns[c].Size : 0f;

            foreach (var cell in _cells)
            {
                var track = Columns[cell.Column];

                if (cell.Columns == 1 && (track.IsFixed || (track.IsFlex && boundedAcross)))
                    continue;

                // A cell spanning a flex column, where the width is bounded, takes what the flex columns are given, as a cell
                // in one does: it widens no column.
                if (cell.Columns > 1 && boundedAcross && HasFlex(Columns, cell.Column, cell.Columns))
                    continue;

                cell.Element.Measure(g, new Vector2(Given(cell.Width, available.X), Given(cell.Height, available.Y)));

                if (!cell.Element.IsVisible)
                    continue;

                var wanted = Given(cell.Width, cell.Element.DesiredSize.X);

                if (cell.Columns == 1)
                {
                    if (wanted > _widths[cell.Column])
                        _widths[cell.Column] = wanted;
                }
                else
                {
                    Widen(_widths, Columns, cell.Column, cell.Columns, _columnSpacing, wanted);
                }
            }

            _columnsShare = Share(_widths, Columns, _columnCount, available.X, _columnSpacing);

            for (var r = 0; r < _rowCount; r++)
                _heights[r] = Rows[r].IsFixed ? Rows[r].Size : 0f;

            foreach (var cell in _cells)
            {
                var room = Span(_widths, cell.Column, cell.Columns, _columnSpacing);

                cell.Element.Measure(g, new Vector2(Given(cell.Width, room), Given(cell.Height, available.Y)));

                if (!cell.Element.IsVisible)
                    continue;

                var track = Rows[cell.Row];
                if (cell.Rows == 1 && (track.IsFixed || (track.IsFlex && boundedDown)))
                    continue;

                // As for columns: a cell spanning a flex row, where the height is bounded, heightens no row.
                if (cell.Rows > 1 && boundedDown && HasFlex(Rows, cell.Row, cell.Rows))
                    continue;

                var wanted = Given(cell.Height, cell.Element.DesiredSize.Y);

                if (cell.Rows == 1)
                {
                    if (wanted > _heights[cell.Row])
                        _heights[cell.Row] = wanted;
                }
                else
                {
                    Widen(_heights, Rows, cell.Row, cell.Rows, _rowSpacing, wanted);
                }
            }

            _rowsShare = Share(_heights, Rows, _rowCount, available.Y, _rowSpacing);

            return new Vector2(
                _columnsShare ? available.X : Span(_widths, 0, _columnCount, _columnSpacing),
                _rowsShare ? available.Y : Span(_heights, 0, _rowCount, _rowSpacing));
        }

        /// <inheritdoc />
        protected override void ArrangeContent(RectangleF bounds)
        {
            if (_columnsShare)
                Share(_widths, Columns, _columnCount, bounds.Width, _columnSpacing);

            if (_rowsShare)
                Share(_heights, Rows, _rowCount, bounds.Height, _rowSpacing);

            Offsets(_xs, _widths, _columnCount, bounds.X, _columnSpacing);
            Offsets(_ys, _heights, _rowCount, bounds.Y, _rowSpacing);

            foreach (var cell in _cells)
            {
                if (!cell.Element.IsVisible)
                    continue;

                var width = Span(_widths, cell.Column, cell.Columns, _columnSpacing);
                var height = Span(_heights, cell.Row, cell.Rows, _rowSpacing);

                float x;
                float wide;
                float y;
                float tall;
                Span(cell.Element, true, _xs[cell.Column], width, cell.Width, cell.Element.DesiredSize.X, out x, out wide);
                Span(cell.Element, false, _ys[cell.Row], height, cell.Height, cell.Element.DesiredSize.Y, out y, out tall);
                cell.Element.Arrange(new RectangleF(x, y, wide, tall));
            }
        }

        /// <inheritdoc />
        protected override void DrawContent(IGraphics g)
        {
            foreach (var cell in _cells)
                cell.Element.Draw(g);
        }

        /// <inheritdoc />
        protected override Element HitTestContent(Vector2 point)
        {
            for (var i = _cells.Count - 1; i >= 0; i--)
            {
                var found = _cells[i].Element.HitTest(point);
                if (found != null)
                    return found;
            }

            return null;
        }

        /// <summary>Rejects a child whose placement is not <see cref="ISized" />.</summary>
        protected override void Admit(Element child, IPlacement placement)
        {
            if (placement != null && !(placement is ISized))
                throw new InvalidOperationException(
                    GetType().Name + " cannot place a child by " + placement.GetType().Name + ".");
        }

        /// <inheritdoc />
        protected override void OnChildRemoved(Element child, int index)
        {
            for (var i = 0; i < _cells.Count; i++)
            {
                if (_cells[i].Element != child)
                    continue;

                _cells.RemoveAt(i);
                return;
            }
        }

        /// <inheritdoc />
        protected override void OnChildPlacementChanged(Element child)
        {
            foreach (var cell in _cells)
                if (cell.Element == child)
                    cell.Resolve(child.Placement);
        }

        Element At(int row, int column)
        {
            foreach (var cell in _cells)
                if (cell.Covers(row, column))
                    return cell.Element;

            return null;
        }

        void Place(Element child, int row, int column, int rows, int columns)
        {
            if (row < 0 || column < 0 || rows < 1 || columns < 1)
                throw new ArgumentException("A cell needs a row and column from zero and a span of at least one.");

            var placing = new Cell(child, row, column, rows, columns);
            Cell same = null;

            foreach (var cell in _cells)
            {
                if (cell.Element == child)
                    continue;

                if (cell.Row == row && cell.Column == column && cell.Rows == rows && cell.Columns == columns)
                    same = cell;
                else if (cell.Overlaps(placing))
                    throw new InvalidOperationException(
                        "The cell at row " + row + ", column " + column + " overlaps one already holding something.");
            }

            if (child != null)
                Admit(child, child.Placement);

            if (same != null)
                _children.Remove(same.Element);

            if (child == null)
                return;

            _children.Add(child);
            placing.Resolve(child.Placement);
            _cells.Add(placing);
        }

        /// <summary>Works out how many columns and rows there are and makes room to hold their sizes.</summary>
        void Count()
        {
            _columnCount = Columns.Count;
            _rowCount = Rows.Count;

            foreach (var cell in _cells)
            {
                if (cell.Column + cell.Columns > _columnCount)
                    _columnCount = cell.Column + cell.Columns;

                if (cell.Row + cell.Rows > _rowCount)
                    _rowCount = cell.Row + cell.Rows;
            }

            Hold(ref _widths, _columnCount);
            Hold(ref _xs, _columnCount);
            Hold(ref _heights, _rowCount);
            Hold(ref _ys, _rowCount);
        }

        static void Hold(ref float[] sizes, int count)
        {
            if (sizes.Length < count)
                sizes = new float[count];
        }

        /// <summary>
        ///     Shares the part of <paramref name="room" /> that the fixed and auto tracks leave among the flex tracks,
        ///     and returns true if it did.
        /// </summary>
        static bool Share(float[] sizes, GridTracks tracks, int count, float room, float spacing)
        {
            if (room >= Unbounded)
                return false;

            var weight = 0f;
            var used = count > 1 ? spacing * (count - 1) : 0f;

            for (var i = 0; i < count; i++)
            {
                if (tracks[i].IsFlex)
                    weight += tracks[i].Weight;
                else
                    used += sizes[i];
            }

            if (!(weight > 0f))
                return false;

            var spare = room - used;
            if (spare < 0f)
                spare = 0f;

            for (var i = 0; i < count; i++)
                if (tracks[i].IsFlex)
                    sizes[i] = spare * tracks[i].Weight / weight;

            return true;
        }

        /// <summary>
        ///     Makes a span of <paramref name="count" /> tracks starting at <paramref name="from" /> at least
        ///     <paramref name="wanted" /> long, adding the shortfall to the last track in the span that is not fixed.
        /// </summary>
        static void Widen(float[] sizes, GridTracks tracks, int from, int count, float spacing, float wanted)
        {
            var missing = wanted - Span(sizes, from, count, spacing);
            if (!(missing > 0f))
                return;

            for (var i = from + count - 1; i >= from; i--)
            {
                if (tracks[i].IsFixed)
                    continue;

                sizes[i] += missing;
                return;
            }
        }

        /// <summary>
        ///     Returns true if any of the <paramref name="count" /> tracks starting at <paramref name="from" /> is
        ///     flex.
        /// </summary>
        static bool HasFlex(GridTracks tracks, int from, int count)
        {
            for (var i = from; i < from + count; i++)
            {
                if (tracks[i].IsFlex)
                    return true;
            }

            return false;
        }

        static float Span(float[] sizes, int from, int count, float spacing)
        {
            var total = count > 1 ? spacing * (count - 1) : 0f;

            for (var i = from; i < from + count; i++)
                total += sizes[i];

            return total;
        }

        static void Offsets(float[] at, float[] sizes, int count, float start, float spacing)
        {
            for (var i = 0; i < count; i++)
            {
                at[i] = start;
                start += sizes[i] + spacing;
            }
        }

        /// <summary>
        ///     Returns <paramref name="size" /> unless it is float.NaN, in which case returns
        ///     <paramref name="otherwise" />.
        /// </summary>
        static float Given(float size, float otherwise)
        {
            return float.IsNaN(size) ? otherwise : size;
        }
    }
}
