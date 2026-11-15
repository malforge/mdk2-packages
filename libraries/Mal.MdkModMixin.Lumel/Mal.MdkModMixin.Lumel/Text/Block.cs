using System;
using System.Collections.Generic;

namespace Lumel.Text
{
    /// <summary>
    ///     One block of a document: a paragraph, heading, list item, rule, table, quote or callout, along with its
    ///     runs.
    /// </summary>
    /// <remarks>
    ///     A table's blocks are its rows, a row's blocks are its cells, and a quote's or a callout's blocks are what it holds;
    ///     anything else holds runs and no blocks. Never changes once made.
    /// </remarks>
    public sealed class Block
    {
        static readonly Run[] NoRuns = new Run[0];
        static readonly Block[] NoBlocks = new Block[0];
        static readonly Column[] NoColumns = new Column[0];
        static readonly string[] NoClasses = new string[0];

        Block(BlockKind kind, int level, int number, bool isHeader, IEnumerable<Run> runs, IEnumerable<Block> blocks, IEnumerable<Column> columns,
            IEnumerable<string> classes)
        {
            Kind = kind;
            Level = level;
            Number = number;
            IsHeader = isHeader;
            Runs = runs == null ? NoRuns : new List<Run>(runs).ToArray();
            Blocks = blocks == null ? NoBlocks : new List<Block>(blocks).ToArray();
            Columns = columns == null ? NoColumns : new List<Column>(columns).ToArray();
            Classes = classes == null ? NoClasses : new List<string>(classes).ToArray();
        }

        /// <summary>Gets the kind of block.</summary>
        public BlockKind Kind { get; }

        /// <summary>Gets a heading's level, starting at 1, or 0 for any other block.</summary>
        public int Level { get; }

        /// <summary>Gets an ordered list item's number, or 0 for any other block.</summary>
        public int Number { get; }

        /// <summary>Whether this block is a table's heading row or one of that row's cells.</summary>
        public bool IsHeader { get; }

        /// <summary>Gets the classes named by a callout's fence, in order, or none for any other block.</summary>
        public IReadOnlyList<string> Classes { get; }

        /// <summary>Gets the runs of text in the block, in order.</summary>
        public IReadOnlyList<Run> Runs { get; }

        /// <summary>Gets a table's rows, a row's cells, or the blocks held by a quote or callout, in order.</summary>
        public IReadOnlyList<Block> Blocks { get; }

        /// <summary>Gets how each of a table's columns is laid out, in order.</summary>
        public IReadOnlyList<Column> Columns { get; }

        /// <summary>Creates a paragraph from <paramref name="runs" />.</summary>
        public static Block Paragraph(IEnumerable<Run> runs)
        {
            return new Block(BlockKind.Paragraph, 0, 0, false, runs, null, null, null);
        }

        /// <summary>
        ///     Creates a heading of <paramref name="runs" /> at <paramref name="level" />, starting at 1.
        /// </summary>
        public static Block Heading(int level, IEnumerable<Run> runs)
        {
            if (level < 1)
                throw new ArgumentException("A heading's level starts at 1.", nameof(level));

            return new Block(BlockKind.Heading, level, 0, false, runs, null, null, null);
        }

        /// <summary>Creates a bulleted list item from <paramref name="runs" />.</summary>
        public static Block BulletListItem(IEnumerable<Run> runs)
        {
            return new Block(BlockKind.BulletListItem, 0, 0, false, runs, null, null, null);
        }

        /// <summary>
        ///     Creates an ordered list item with the given <paramref name="number" />, from <paramref name="runs" />.
        /// </summary>
        public static Block OrderedListItem(int number, IEnumerable<Run> runs)
        {
            return new Block(BlockKind.OrderedListItem, 0, number, false, runs, null, null, null);
        }

        /// <summary>Creates a horizontal rule.</summary>
        public static Block Rule()
        {
            return new Block(BlockKind.Rule, 0, 0, false, null, null, null, null);
        }

        /// <summary>Creates a table with the given <paramref name="columns" /> and <paramref name="rows" />.</summary>
        public static Block Table(IEnumerable<Column> columns, IEnumerable<Block> rows)
        {
            return new Block(BlockKind.Table, 0, 0, false, null, OfKind(rows, BlockKind.Row, nameof(rows)), columns, null);
        }

        /// <summary>
        ///     Creates a table row from <paramref name="cells" />, which is the heading row when
        ///     <paramref name="isHeader" /> is true.
        /// </summary>
        public static Block Row(bool isHeader, IEnumerable<Block> cells)
        {
            return new Block(BlockKind.Row, 0, 0, isHeader, null, OfKind(cells, BlockKind.Cell, nameof(cells)), null, null);
        }

        /// <summary>
        ///     Creates a table cell from <paramref name="runs" />, which belongs to the heading row when
        ///     <paramref name="isHeader" /> is true.
        /// </summary>
        public static Block Cell(bool isHeader, IEnumerable<Run> runs)
        {
            return new Block(BlockKind.Cell, 0, 0, isHeader, runs, null, null, null);
        }

        /// <summary>Creates a quote that holds <paramref name="blocks" />.</summary>
        public static Block Quote(IEnumerable<Block> blocks)
        {
            return new Block(BlockKind.Quote, 0, 0, false, null, blocks, null, null);
        }

        /// <summary>
        ///     Creates a callout with the given <paramref name="classes" /> that holds <paramref name="blocks" />.
        /// </summary>
        public static Block Callout(IEnumerable<string> classes, IEnumerable<Block> blocks)
        {
            return new Block(BlockKind.Callout, 0, 0, false, null, blocks, null, classes);
        }

        /// <summary>
        ///     Returns a copy of this block with <paramref name="runs" /> in place of its own and everything else
        ///     unchanged.
        /// </summary>
        public Block WithRuns(IEnumerable<Run> runs)
        {
            return new Block(Kind, Level, Number, IsHeader, runs, Blocks, Columns, Classes);
        }

        /// <summary>
        ///     Returns a copy of this block with <paramref name="blocks" /> in place of its own and everything else
        ///     unchanged.
        /// </summary>
        public Block WithBlocks(IEnumerable<Block> blocks)
        {
            return new Block(Kind, Level, Number, IsHeader, Runs, blocks, Columns, Classes);
        }

        /// <summary>
        ///     Returns <paramref name="blocks" /> after checking that every one is of <paramref name="kind" />,
        ///     reporting <paramref name="parameter" /> as the offending parameter if not.
        /// </summary>
        static IEnumerable<Block> OfKind(IEnumerable<Block> blocks, BlockKind kind, string parameter)
        {
            if (blocks == null)
                return null;

            var checkedBlocks = new List<Block>(blocks);
            foreach (var block in checkedBlocks)
            {
                if (block == null || block.Kind != kind)
                    throw new ArgumentException("Every block here must be a " + kind + ".", parameter);
            }

            return checkedBlocks;
        }
    }
}
