
namespace Lumel.Text
{
    /// <summary>The kind of a block in a document.</summary>
    public enum BlockKind
    {
        /// <summary>A paragraph of running text.</summary>
        Paragraph,

        /// <summary>A heading, at the level the block carries.</summary>
        Heading,

        /// <summary>An item of a bulleted list, whose items all carry the same marker.</summary>
        BulletListItem,

        /// <summary>An item of a numbered list, at the number the block carries.</summary>
        OrderedListItem,

        /// <summary>A horizontal line that divides what is above it from what is below.</summary>
        Rule,

        /// <summary>A table: its blocks are its rows, and its columns say how each column is laid out.</summary>
        Table,

        /// <summary>A table row: its blocks are its cells, one for each column.</summary>
        Row,

        /// <summary>A table cell, which holds text.</summary>
        Cell,

        /// <summary>A quotation: its blocks are the blocks it holds.</summary>
        Quote,

        /// <summary>A callout styled by the classes its fence names: its blocks are the blocks it holds.</summary>
        Callout
    }
}
