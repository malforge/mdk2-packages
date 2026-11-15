using Lumel.Layout;
using Lumel.Text;
using VRageMath;
using Xunit;
using Lumel.Sim;

namespace Lumel.Tests;

/// <summary>
///     Tests of callouts in markdown-lite: blocks between a fence naming classes and a bare fence, aligned as left,
///     center and right say, and styled by the style's looks for their other classes.
/// </summary>
public class CalloutTests
{
    const float Wide = 400f;
    const float High = 400f;

    static readonly Color Body = new Color(162, 195, 205);
    static readonly Color Rule = new Color(91, 115, 123);
    static readonly Color Stressed = new Color(220, 245, 253);

    /// <summary>
    ///     A fence with classes opens a callout containing everything up to the bare fence, and reading continues after
    ///     it.
    /// </summary>
    [Fact]
    public void AFenceOpensACalloutAndABareOneClosesIt()
    {
        var blocks = Markdown.Parse("::: warning center\n# Title\n\nWords\n:::\nafter").Blocks;

        var box = blocks[0];
        Assert.Equal(BlockKind.Callout, box.Kind);
        Assert.Equal(new[] { "warning", "center" }, box.Classes);
        Assert.Equal(new[] { BlockKind.Heading, BlockKind.Paragraph }, box.Blocks.Select(b => b.Kind));
        Assert.Equal("after", Words(blocks[1]));
    }

    /// <summary>A callout inside a callout is closed first, by the first bare fence after it.</summary>
    [Fact]
    public void CalloutsNest()
    {
        var box = Markdown.Parse("::: warning\n::: center\ninner\n:::\nouter\n:::").Blocks[0];

        var inner = box.Blocks[0];
        Assert.Equal(new[] { "center" }, inner.Classes);
        Assert.Equal("inner", Words(Assert.Single(inner.Blocks)));
        Assert.Equal("outer", Words(box.Blocks[1]));
    }

    /// <summary>More colons, or no space before the classes, still open a callout.</summary>
    [Theory]
    [InlineData(":::: note\nx\n::::")]
    [InlineData(":::note\nx\n:::")]
    public void FencesComeInMoreThanOneLength(string source)
    {
        var box = Assert.Single(Markdown.Parse(source).Blocks);

        Assert.Equal(new[] { "note" }, box.Classes);
        Assert.Equal("x", Words(Assert.Single(box.Blocks)));
    }

    /// <summary>A callout never closed runs to the end.</summary>
    [Fact]
    public void ACalloutNeverClosedRunsToTheEnd()
    {
        var box = Assert.Single(Markdown.Parse("::: note\none\n\ntwo").Blocks);

        Assert.Equal(2, box.Blocks.Count);
    }

    /// <summary>A bare fence with no callout open, and a fence naming something no class can be, are words.</summary>
    [Theory]
    [InlineData(":::")]
    [InlineData("::: not!a name")]
    public void NotAFenceIsWords(string source)
    {
        Assert.Equal(BlockKind.Paragraph, Assert.Single(Markdown.Parse(source).Blocks).Kind);
    }

    /// <summary>An escaped fence is the words it shows, so a page can write one out.</summary>
    [Fact]
    public void AnEscapedFenceIsWords()
    {
        var block = Assert.Single(Markdown.Parse("\\::: center").Blocks);

        Assert.Equal(BlockKind.Paragraph, block.Kind);
        Assert.Equal("::: center", Words(block));
    }

    /// <summary>A class with a look gives the callout that look, whatever the class's case: a bar, a background, and plain words in its color, strong ones keeping theirs.</summary>
    [Fact]
    public void AClassLookGivesTheCalloutItsLook()
    {
        var (text, easel) = Laid("::: Warning\nMind the **door**\n:::");
        Warning(text.Base.Callouts["warning"]);
        easel.Draw(Wide, High);

        Assert.Single(easel.Sprites, s => s.Color == Color.Orange);
        var fill = Assert.Single(easel.Sprites, s => s.Color == Color.Purple).Destination;
        Assert.True(fill.Width > Wide * 0.9f * easel.Pixels, "the fill does not reach across the room");
        Assert.Equal(Color.Yellow, Word(easel, "Mind the").Color);
        Assert.Equal(Stressed, Word(easel, "door").Color);
    }

    /// <summary>A look asking for the emphasized face draws the callout's words in it, inside callouts within it too, and code in its own.</summary>
    [Fact]
    public void AnEmphasizedLookFacesTheCalloutsWords()
    {
        var (text, easel) = Laid("Before\n\n::: aside\nMind the `door`\n\n::: center\nInside\n:::\n:::\n\nAfter");
        text.Base.EmphasisFont = "Italic";
        text.Base.CodeFont = "Mono";
        text.Base.Callouts["aside"].IsEmphasized = true;
        easel.Draw(Wide, High);

        Assert.Equal("Italic", Word(easel, "Mind the").Font);
        Assert.Equal("Italic", Word(easel, "Inside").Font);
        Assert.Equal("Mono", Word(easel, "door").Font);
        Assert.NotEqual("Italic", Word(easel, "Before").Font);
        Assert.NotEqual("Italic", Word(easel, "After").Font);
    }

    /// <summary>A class with no look changes nothing: no bar, no background, and the words where any others would be.</summary>
    [Fact]
    public void AClassWithNoLookChangesNothing()
    {
        var (text, easel) = Laid("Before\n\n::: other\nWords\n:::");
        text.Base.Callouts["warning"].Background = Color.Purple;
        easel.Draw(Wide, High);

        Assert.Empty(easel.Sprites);
        Assert.Equal(Word(easel, "Before").Position.X, Word(easel, "Words").Position.X);
        Assert.Equal(Body, Word(easel, "Words").Color);
    }

    /// <summary>center and right set where a callout's lines sit, with no look for them, and left sets them back inside a callout that moved them.</summary>
    [Fact]
    public void LeftCenterAndRightAreMarkdownLitesOwn()
    {
        var (_, easel) = Laid("Plain\n\n::: center\nMiddle\n\n::: left\nBack\n:::\n:::\n\n::: right\nEnd\n:::");

        var plain = Word(easel, "Plain").Position.X;
        Assert.True(Word(easel, "Middle").Position.X > plain + 40f, "center did not move the line in");
        Assert.Equal(plain, Word(easel, "Back").Position.X);
        Assert.True(Word(easel, "End").Position.X > Word(easel, "Middle").Position.X, "right is not further across than center");
    }

    /// <summary>A callout inside a callout comes to the same as one callout naming both classes.</summary>
    [Fact]
    public void ACalloutInACalloutIsOneCalloutNamingBoth()
    {
        var (one, apart) = Laid("::: warning\n::: center\nWords\n:::\n:::");
        Warning(one.Base.Callouts["warning"]);
        apart.Draw(Wide, High);

        var (both, together) = Laid("::: warning center\nWords\n:::");
        Warning(both.Base.Callouts["warning"]);
        together.Draw(Wide, High);

        Assert.Equal(Word(together, "Words").Position, Word(apart, "Words").Position);
        Assert.Equal(Word(together, "Words").Color, Word(apart, "Words").Color);
        Assert.Equal(together.Sprites.Select(s => s.Destination), apart.Sprites.Select(s => s.Destination));
    }

    /// <summary>A style gives a text its callout looks and its link colors like everything else it sets.</summary>
    [Fact]
    public void AStyleSetsCalloutsAndLinks()
    {
        var style = new RichTextStyle { Base = { Callouts = { ["note"] = { BarColor = Color.Orange } }, LinkColor = Color.Red, LinkBackground = Color.Blue } };
        var text = new RichText { Document = Markdown.Parse("::: note\nx\n:::"), Style = style, Placement = FixedSize.Width(Wide) };
        var easel = new Easel(new VStack { Children = { text } }, Wide, High);

        Assert.Single(easel.Sprites, s => s.Color == Color.Orange);
        Assert.Equal(Color.Red, text.LinkColor);
        Assert.Equal(Color.Blue, text.LinkBackground);
    }

    /// <summary>A style based on another adds a class's look without restating the others, and lays its own over the base's for a class both set.</summary>
    [Fact]
    public void ADerivedStyleAddsAClassLook()
    {
        var based = new RichTextStyle { Base = { Callouts = { ["warning"] = { BarColor = Color.Orange, Background = Color.Purple } } } };
        var derived = new RichTextStyle(based) { Base = { Callouts = { ["WARNING"] = { Background = Color.Blue }, ["note"] = { BarColor = Color.Green } } } };
        var text = new RichText { Document = Markdown.Parse("::: warning\nx\n:::\n\n::: note\ny\n:::"), Style = derived, Placement = FixedSize.Width(Wide) };
        var easel = new Easel(new VStack { Children = { text } }, Wide, High);

        Assert.Single(easel.Sprites, s => s.Color == Color.Orange);
        Assert.Single(easel.Sprites, s => s.Color == Color.Blue);
        Assert.Single(easel.Sprites, s => s.Color == Color.Green);
        Assert.DoesNotContain(easel.Sprites, s => s.Color == Color.Purple);
    }

    /// <summary>Callouts nest no deeper than markdown-lite allows, however many fences open them: a deeper fence is words.</summary>
    [Fact]
    public void CalloutsNestNoDeeperThanTheCap()
    {
        var blocks = Markdown.Parse(string.Concat(Enumerable.Repeat("::: a\n", 10000))).Blocks;

        var depth = 0;
        while (blocks.Count > 0 && blocks[0].Kind == BlockKind.Callout)
        {
            depth++;
            blocks = blocks[0].Blocks;
        }

        Assert.Equal(Markdown.MaxNestingDepth, depth);
        Assert.Equal(BlockKind.Paragraph, Assert.Single(blocks).Kind);
    }

    static (RichText, Easel) Laid(string markdown)
    {
        var text = new RichText
        {
            Document = Markdown.Parse(markdown),
            Base = { FontSize = 20f, Color = Body, StrongColor = Stressed, RuleColor = Rule, BlockSpacing = 8f, Indent = 20f },
            Placement = FixedSize.Width(Wide)
        };

        var easel = new Easel(new VStack { Children = { text } }, Wide, High);
        return (text, easel);
    }

    static void Warning(CalloutLook look)
    {
        look.BarColor = Color.Orange;
        look.Background = Color.Purple;
        look.Color = Color.Yellow;
    }

    static string Words(Block block) => string.Concat(block.Runs.Select(r => r.IsBreak ? "\n" : r.Text));

    static DrawCommand Word(Easel easel, string text) =>
        Assert.Single(easel.Written, command => command.Text?.Trim() == text);
}
