using Lumel.Layout;
using Lumel.Text;
using VRageMath;
using Xunit;
using Lumel.Sim;

namespace Lumel.Tests;

/// <summary>Quotes in markdown-lite: blocks set apart behind a bar, and nothing more.</summary>
public class QuoteTests
{
    const float Wide = 400f;
    const float High = 400f;

    static readonly Color Body = new Color(162, 195, 205);
    static readonly Color Rule = new Color(91, 115, 123);

    /// <summary>Lines starting with &gt; are one quote, read like any other text.</summary>
    [Fact]
    public void QuotedLinesAreOneQuote()
    {
        var blocks = Markdown.Parse("> one\n> two\n>\n> - item\nafter").Blocks;

        var quote = blocks[0];
        Assert.Equal(BlockKind.Quote, quote.Kind);
        Assert.Equal(new[] { BlockKind.Paragraph, BlockKind.BulletListItem }, quote.Blocks.Select(b => b.Kind));
        Assert.Equal("one\ntwo", Words(quote.Blocks[0]));
        Assert.Equal("after", Words(blocks[1]));
    }

    /// <summary>A quote's first line is words whatever it says, and a quote inside a quote is words too.</summary>
    [Theory]
    [InlineData("> [!NOTE]", "[!NOTE]")]
    [InlineData("> > nested", "> nested")]
    public void AQuotesLinesAreWords(string source, string words)
    {
        var quote = Assert.Single(Markdown.Parse(source).Blocks);

        Assert.Equal(BlockKind.Quote, quote.Kind);
        Assert.Equal(words, Words(Assert.Single(quote.Blocks)));
    }

    /// <summary>A slot in a quote takes its words from the arguments, as anywhere else.</summary>
    [Fact]
    public void ASlotInAQuoteIsASlot()
    {
        var quote = Assert.Single(Template.Parse("> Open {name}").Blocks);

        Assert.Equal("name", Assert.Single(quote.Blocks).Runs[1].Slot);
    }

    /// <summary>A quote is set in from a bar down its edge, in the rule color, and its words keep the text's color.</summary>
    [Fact]
    public void AQuoteStandsBesideABar()
    {
        var easel = Laid("Before\n\n> Quoted");

        var bar = Assert.Single(easel.Sprites, s => s.Color == Rule).Destination;
        Assert.True(Word(easel, "Quoted").Position.X > Word(easel, "Before").Position.X, "the quote is not set in");
        Assert.True(bar.X < Word(easel, "Quoted").Position.X, "the bar is not before the words");
        Assert.Equal(Body, Word(easel, "Quoted").Color);
    }

    static Easel Laid(string markdown)
    {
        var text = new RichText
        {
            Document = Markdown.Parse(markdown),
            Base = { FontSize = 20f, Color = Body, RuleColor = Rule, QuoteBarColor = Rule, BlockSpacing = 8f, Indent = 20f },
            Placement = FixedSize.Width(Wide)
        };

        return new Easel(new VStack { Children = { text } }, Wide, High);
    }

    static string Words(Block block) => string.Concat(block.Runs.Select(r => r.IsBreak ? "\n" : r.Text));

    static DrawCommand Word(Easel easel, string text) =>
        Assert.Single(easel.Written, command => command.Text?.Trim() == text);
}
