using Xunit;
using Lumel.Text;

namespace Lumel.Tests;

/// <summary>Markdown-lite read into a document: CommonMark's meaning for what it supports, and plain text for the rest.</summary>
public class MarkdownTests
{
    /// <summary>Headings at every level keep their level; a hash with no space after it is a word.</summary>
    [Fact]
    public void HeadingsKeepTheirLevel()
    {
        var blocks = Markdown.Parse("# One\n### Three ###\n####### Seven\n#hashtag").Blocks;

        Assert.Equal(BlockKind.Heading, blocks[0].Kind);
        Assert.Equal(1, blocks[0].Level);
        Assert.Equal("One", Words(blocks[0]));
        Assert.Equal(3, blocks[1].Level);
        Assert.Equal("Three", Words(blocks[1]));
        Assert.Equal(BlockKind.Paragraph, blocks[2].Kind);
        Assert.Equal("####### Seven\n#hashtag", Words(blocks[2]));
    }

    /// <summary>Three or more of one mark, spaced or not, is a rule, and wins over a list item.</summary>
    [Theory]
    [InlineData("---")]
    [InlineData("***")]
    [InlineData("___")]
    [InlineData("- - -")]
    [InlineData(" *  *  * ")]
    public void ARuleIsThreeOfOneMark(string line)
    {
        var blocks = Markdown.Parse(line).Blocks;

        Assert.Single(blocks);
        Assert.Equal(BlockKind.Rule, blocks[0].Kind);
    }

    /// <summary>Dashes, stars and pluses start bullets; numbers with a dot or a bracket start counted items, keeping their number.</summary>
    [Fact]
    public void ListItemsAreBulletsOrNumbered()
    {
        var blocks = Markdown.Parse("- one\n* two\n+ three\n4. four\n5) five").Blocks;

        Assert.Equal(BlockKind.BulletListItem, blocks[0].Kind);
        Assert.Equal(BlockKind.BulletListItem, blocks[1].Kind);
        Assert.Equal(BlockKind.BulletListItem, blocks[2].Kind);
        Assert.Equal(BlockKind.OrderedListItem, blocks[3].Kind);
        Assert.Equal(4, blocks[3].Number);
        Assert.Equal(5, blocks[4].Number);
        Assert.Equal("five", Words(blocks[4]));
    }

    /// <summary>A single newline is a line break inside a block, and a blank line starts the next block.</summary>
    [Fact]
    public void ANewlineBreaksAndABlankLineEnds()
    {
        var blocks = Markdown.Parse("one\ntwo\n\nthree").Blocks;

        Assert.Equal(2, blocks.Count);
        Assert.Equal("one\ntwo", Words(blocks[0]));
        Assert.True(blocks[0].Runs[1].IsBreak);
        Assert.Equal("three", Words(blocks[1]));
    }

    /// <summary>A plain line after a list item carries on that item, on a line of its own.</summary>
    [Fact]
    public void ALineAfterAnItemCarriesItOn()
    {
        var blocks = Markdown.Parse("- one\ncarried on").Blocks;

        Assert.Single(blocks);
        Assert.Equal("one\ncarried on", Words(blocks[0]));
    }

    /// <summary>Two marks make strong, one makes emphasis, and three make both.</summary>
    [Theory]
    [InlineData("**strong**", "strong", RunKind.Strong)]
    [InlineData("__strong__", "strong", RunKind.Strong)]
    [InlineData("*emphasis*", "emphasis", RunKind.Emphasis)]
    [InlineData("_emphasis_", "emphasis", RunKind.Emphasis)]
    [InlineData("***both***", "both", RunKind.Strong | RunKind.Emphasis)]
    public void MarksSetWordsApart(string source, string text, RunKind kind)
    {
        var run = Markdown.Parse(source).Blocks[0].Runs[0];

        Assert.Equal(text, run.Text);
        Assert.Equal(kind, run.Kind);
    }

    /// <summary>Emphasis nests inside strong, and the words around both stay plain.</summary>
    [Fact]
    public void EmphasisNestsInsideStrong()
    {
        var runs = Markdown.Parse("a **b *c* d** e").Blocks[0].Runs;

        Assert.Equal(new[] { "a ", "b ", "c", " d", " e" }, Texts(runs));
        Assert.Equal(RunKind.Plain, runs[0].Kind);
        Assert.Equal(RunKind.Strong, runs[1].Kind);
        Assert.Equal(RunKind.Strong | RunKind.Emphasis, runs[2].Kind);
        Assert.Equal(RunKind.Strong, runs[3].Kind);
        Assert.Equal(RunKind.Plain, runs[4].Kind);
    }

    /// <summary>An underscore inside a word is a character, so names written with them survive.</summary>
    [Fact]
    public void UnderscoresInsideWordsAreCharacters()
    {
        var runs = Markdown.Parse("snake_case_name").Blocks[0].Runs;

        Assert.Single(runs);
        Assert.Equal("snake_case_name", runs[0].Text);
        Assert.Equal(RunKind.Plain, runs[0].Kind);
    }

    /// <summary>A named or numbered entity reference stands for its character.</summary>
    [Theory]
    [InlineData("10&nbsp;SC", "10 SC")]
    [InlineData("Salt &amp; pepper", "Salt & pepper")]
    [InlineData("&lt;tag&gt; &quot;said&quot; it&apos;s", "<tag> \"said\" it's")]
    [InlineData("A&#160;B", "A B")]
    [InlineData("A&#xA0;B &#X41;", "A B A")]
    [InlineData("&#0; &#x110000;", "� �")]
    public void AnEntityStandsForItsCharacter(string written, string read)
    {
        var runs = Markdown.Parse(written).Blocks[0].Runs;

        Assert.Equal(read, string.Concat(runs.Select(run => run.Text)));
    }

    /// <summary>An ampersand that starts no known reference is only a character, and code keeps references as written.</summary>
    [Theory]
    [InlineData("Fish & chips")]
    [InlineData("&nope; and &nbsp")]
    [InlineData("&#; &#x; &#12a;")]
    [InlineData("&#12345678;")]
    public void AnUnknownReferenceIsOnlyCharacters(string written)
    {
        var runs = Markdown.Parse(written).Blocks[0].Runs;

        Assert.Equal(written, string.Concat(runs.Select(run => run.Text)));
    }

    /// <summary>Inside code a reference is the characters it is written with.</summary>
    [Fact]
    public void CodeKeepsAReferenceAsWritten()
    {
        var runs = Markdown.Parse("`&nbsp;`").Blocks[0].Runs;

        Assert.Equal("&nbsp;", Assert.Single(runs).Text);
    }

    /// <summary>A star with space on both sides opens and closes nothing.</summary>
    [Fact]
    public void ALoneStarIsACharacter()
    {
        var runs = Markdown.Parse("2 * 3 = 6").Blocks[0].Runs;

        Assert.Single(runs);
        Assert.Equal("2 * 3 = 6", runs[0].Text);
    }

    /// <summary>A mark nobody closes stays as it was written.</summary>
    [Fact]
    public void AnUnclosedMarkStaysAsWritten()
    {
        var runs = Markdown.Parse("**never closed").Blocks[0].Runs;

        Assert.Equal("**never closed", string.Concat(Texts(runs)));
        Assert.All(runs, r => Assert.Equal(RunKind.Plain, r.Kind));
    }

    /// <summary>A code span keeps what is inside it as it is, marks included.</summary>
    [Fact]
    public void CodeKeepsItsInsideAsItIs()
    {
        var runs = Markdown.Parse("run `when *pressed*` now").Blocks[0].Runs;

        Assert.Equal("when *pressed*", runs[1].Text);
        Assert.Equal(RunKind.Code, runs[1].Kind);
    }

    /// <summary>Anything escaped reads as exactly what it says, block marks included.</summary>
    [Theory]
    [InlineData("**Airlock**")]
    [InlineData("# not a heading")]
    [InlineData("1. not counted")]
    [InlineData("- not a bullet")]
    [InlineData("---")]
    [InlineData("back\\slash and `tick`")]
    public void AnEscapedNameReadsAsItIs(string name)
    {
        var blocks = Markdown.Parse(Markdown.Escape(name)).Blocks;

        Assert.Single(blocks);
        Assert.Equal(BlockKind.Paragraph, blocks[0].Kind);
        Assert.Single(blocks[0].Runs);
        Assert.Equal(name, blocks[0].Runs[0].Text);
        Assert.Equal(RunKind.Plain, blocks[0].Runs[0].Kind);
    }

    /// <summary>Plain text is one paragraph, broken where it has newlines, and never read for marks.</summary>
    [Fact]
    public void PlainTextIsReadForNothing()
    {
        var blocks = Document.Plain("**one**\n# two").Blocks;

        Assert.Single(blocks);
        Assert.Equal("**one**\n# two", Words(blocks[0]));
    }

    /// <summary>A table has the columns its delimiter row describes, a heading row, and rows of cells that hold words.</summary>
    [Fact]
    public void ATableHasColumnsAndRows()
    {
        var table = Assert.Single(Markdown.Parse("| Item | Price |\n|:---*|**---:|\n| Power cells | **450 SC** |\n| Ice | 2 SC |").Blocks);

        Assert.Equal(BlockKind.Table, table.Kind);
        Assert.Equal(new[] { TextAlignment.Left, TextAlignment.Right }, table.Columns.Select(c => c.TextAlignment));
        Assert.Equal(new[] { 1, 2 }, table.Columns.Select(c => c.Weight));
        Assert.Equal(3, table.Blocks.Count);
        Assert.True(table.Blocks[0].IsHeader);
        Assert.Equal("Item", Words(table.Blocks[0].Blocks[0]));
        Assert.Equal(BlockKind.Cell, table.Blocks[1].Blocks[1].Kind);
        Assert.Equal(RunKind.Strong, table.Blocks[1].Blocks[1].Runs[0].Kind);
        Assert.Equal("2 SC", Words(table.Blocks[2].Blocks[1]));
    }

    /// <summary>A heading row of empty cells is no heading, and a row with too few or too many cells has as many as there are columns.</summary>
    [Fact]
    public void RowsFitTheColumns()
    {
        var table = Assert.Single(Markdown.Parse("| | |\n|--|:-:|\n| one |\n| a | b | dropped |").Blocks);

        Assert.Equal(TextAlignment.Center, table.Columns[1].TextAlignment);
        Assert.Equal(2, table.Blocks.Count);
        Assert.All(table.Blocks, row => Assert.False(row.IsHeader));
        Assert.All(table.Blocks, row => Assert.Equal(2, row.Blocks.Count));
        Assert.Equal("", Words(table.Blocks[0].Blocks[1]));
        Assert.Equal("b", Words(table.Blocks[1].Blocks[1]));
    }

    /// <summary>A table can start straight after the words that introduce it, and ends at a blank line or the first line with no pipe.</summary>
    [Fact]
    public void ATableFollowsWordsAndEnds()
    {
        var blocks = Markdown.Parse("Prices today\nper unit\n| | |\n|--|--|\n| a | b |\nno pipe here\n\n| x |").Blocks;

        Assert.Equal(new[] { BlockKind.Paragraph, BlockKind.Table, BlockKind.Paragraph, BlockKind.Paragraph }, blocks.Select(b => b.Kind));
        Assert.Equal("Prices today\nper unit", Words(blocks[0]));
        Assert.Equal("no pipe here", Words(blocks[2]));
        Assert.Equal("| x |", Words(blocks[3]));
    }

    /// <summary>Pipes without a delimiter row under them are the words they are, and an escaped pipe stays in its cell.</summary>
    [Fact]
    public void PipesWithoutADelimiterRowAreWords()
    {
        Assert.Equal(BlockKind.Paragraph, Assert.Single(Markdown.Parse("| a | b |\n| -- | x |").Blocks).Kind);

        var table = Assert.Single(Markdown.Parse("| | |\n|--|--|\n| a \\| b | c |").Blocks);
        Assert.Equal("a | b", Words(table.Blocks[0].Blocks[0]));
    }

    /// <summary>A delimiter row under a list item is the item's words, because only a paragraph's last line can head a table.</summary>
    [Fact]
    public void AListItemHeadsNoTable()
    {
        var blocks = Markdown.Parse("- a | b\n|--|--|").Blocks;

        Assert.Equal(BlockKind.BulletListItem, Assert.Single(blocks).Kind);
    }

    /// <summary>Returns the words of <paramref name="block" />, with its line breaks as newlines.</summary>
    static string Words(Block block) =>
        string.Concat(block.Runs.Select(r => r.IsBreak ? "\n" : r.Text));

    /// <summary>A block is made whole and refuses what its kind cannot hold: a table only rows, a row only cells, a heading a level from one.</summary>
    [Fact]
    public void ABlockRefusesWhatItsKindCannotHold()
    {
        var paragraph = Block.Paragraph(new[] { new Run("words", RunKind.Plain) });

        Assert.Throws<ArgumentException>(() => Block.Table(new[] { new Column(TextAlignment.Left, 0) }, new[] { paragraph }));
        Assert.Throws<ArgumentException>(() => Block.Row(false, new[] { paragraph }));
        Assert.Throws<ArgumentException>(() => Block.Heading(0, new Run[0]));
    }

    /// <summary>A block keeps what it was made with, whatever happens to the list it was given.</summary>
    [Fact]
    public void ABlockKeepsWhatItWasMadeWith()
    {
        var runs = new List<Run> { new Run("words", RunKind.Plain) };
        var block = Block.Paragraph(runs);

        runs.Add(Run.Break);

        Assert.Single(block.Runs);
    }

    static string[] Texts(IReadOnlyList<Run> runs) => runs.Select(r => r.Text).ToArray();

}
