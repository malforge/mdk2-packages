using Lumel.Layout;
using Lumel.Sim;
using Lumel.Text;
using VRageMath;
using Xunit;

namespace Lumel.Tests;

/// <summary>A span: words in brackets with braces after them, giving the words a color, or a class where the parse reads classes.</summary>
public class SpanTests
{
    const float Wide = 400f;
    const float High = 400f;

    /// <summary>A named color and a written one are read in any case, and the words keep no brackets.</summary>
    [Theory]
    [InlineData("[words]{color=red}", 255, 0, 0, 255)]
    [InlineData("[words]{color=Orange}", 255, 165, 0, 255)]
    [InlineData("[words]{color=#102030}", 16, 32, 48, 255)]
    [InlineData("[words]{color=#80102030}", 16, 32, 48, 128)]
    public void AColorIsRead(string written, int r, int g, int b, int a)
    {
        var run = Assert.Single(Markdown.Parse(written).Blocks[0].Runs);

        Assert.Equal("words", run.Text);
        Assert.Equal(new Color(r, g, b, a), run.Color);
        Assert.Null(run.Class);
    }

    /// <summary>A class is read only where the parse is told to read classes; elsewhere the span is words.</summary>
    [Fact]
    public void AClassIsReadOnlyWhereAsked()
    {
        const string written = "Then [Hold Alt]{.hint} here";

        var plain = Markdown.Parse(written).Blocks[0].Runs;
        Assert.Equal(written, string.Concat(plain.Select(run => run.Text)));
        Assert.All(plain, run => Assert.Null(run.Class));

        var classed = Markdown.Parse(written, true).Blocks[0].Runs;
        Assert.Equal("hint", Assert.Single(classed, run => run.Text == "Hold Alt").Class);
    }

    /// <summary>Anything else in the braces leaves what was written as words.</summary>
    [Theory]
    [InlineData("[a]{}")]
    [InlineData("[a]{color=nope}")]
    [InlineData("[a]{color=#12345}")]
    [InlineData("[a]{color=#1234567g}")]
    [InlineData("[a]{color=red color=blue}")]
    [InlineData("[a]{.x .y}")]
    [InlineData("[a]{.x!}")]
    [InlineData("[a]{size=2}")]
    public void AnythingElseIsWords(string written)
    {
        var runs = Markdown.Parse(written, true).Blocks[0].Runs;

        Assert.Equal(written, string.Concat(runs.Select(run => run.Text)));
        Assert.All(runs, run => Assert.Null(run.Color));
        Assert.All(runs, run => Assert.Null(run.Class));
    }

    /// <summary>A span's words are drawn in its color, over strong text's own, and the words around it keep the text's.</summary>
    [Fact]
    public void ASpansColorIsDrawn()
    {
        var easel = Laid(Markdown.Parse("Plain **[loud]{color=lime}** after"));

        Assert.Equal(new Color(0, 255, 0), Word(easel, "loud").Color);
        Assert.Equal(Color.White, Word(easel, "Plain").Color);
        Assert.Equal(Color.White, Word(easel, "after").Color);
    }

    /// <summary>A span's class takes the color its look gives, and a color written on the span wins over it.</summary>
    [Fact]
    public void AClassTakesItsLooksColor()
    {
        var easel = Laid(Markdown.Parse("[hint]{.hint} and [told]{.hint color=red} and [none]{.other}", true));

        Assert.Equal(Color.Orange, Word(easel, "hint").Color);
        Assert.Equal(new Color(255, 0, 0), Word(easel, "told").Color);
        Assert.Equal(Color.White, Assert.Single(easel.Written, command => command.Text?.Contains("none") == true).Color);
    }

    /// <summary>A slot in a span's words takes the span's color with its argument.</summary>
    [Fact]
    public void ASlotInASpanKeepsItsColor()
    {
        var text = new RichText
        {
            Document = Template.Parse("Hold [{key} to explore]{color=red}"),
            Arguments = Key("Alt"),
            Base = { FontSize = 20f, Color = Color.White },
            Placement = FixedSize.Width(Wide)
        };
        var easel = new Easel(new VStack { Children = { text } }, Wide, High);

        Assert.All(easel.Written.Where(command => command.Text?.Trim() != "Hold"), command => Assert.Equal(new Color(255, 0, 0), command.Color));
        Assert.Contains(easel.Written, command => command.Text?.Contains("Alt") == true);
    }

    static Arguments Key(string value)
    {
        var arguments = new Arguments();
        arguments.Set("key", value);
        return arguments;
    }

    static Easel Laid(Document document)
    {
        var text = new RichText
        {
            Document = document,
            Base = { FontSize = 20f, Color = Color.White, StrongColor = Color.White, Spans = { ["hint"] = { Color = Color.Orange } } },
            Placement = FixedSize.Width(Wide)
        };

        return new Easel(new VStack { Children = { text } }, Wide, High);
    }

    static DrawCommand Word(Easel easel, string text) =>
        Assert.Single(easel.Written, command => command.Text?.Trim() == text);
}
