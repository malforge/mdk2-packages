using Lumel.Layout;
using VRageMath;
using Xunit;
using Lumel.Text;
using Lumel.Sim;

namespace Lumel.Tests;

/// <summary>A document drawn: wrapped to its room, each kind of run in its own color, and measured only when something about it changes.</summary>
public class RichTextTests
{
    const float Wide = 400f;
    const float High = 400f;

    static readonly Color Body = new Color(162, 195, 205);
    static readonly Color Bright = new Color(220, 245, 253);

    /// <summary>A heading that is not the first stands further from what is above it than one paragraph does from another.</summary>
    [Fact]
    public void AHeadingHasMoreRoomAbove()
    {
        var (_, easel) = Laid("One\n\nTwo\n\n# Three\n\nFour", Wide);

        var paragraphs = Word(easel, "Two").Position.Y - Word(easel, "One").Position.Y;
        var heading = Word(easel, "Three").Position.Y - Word(easel, "Two").Position.Y;
        Assert.True(heading > paragraphs, "the heading has no more room above it than a paragraph");
    }

    /// <summary>Headings down to the ruled level have a rule under them, and deeper ones none.</summary>
    /// <remarks>Drawn at a full screen's size, where a rule one unit thick is at least a pixel.</remarks>
    [Fact]
    public void RuledHeadingsHaveARuleUnder()
    {
        var text = new RichText
        {
            Document = Markdown.Parse("## Ruled\n\n### Not"),
            Base = { FontSize = 20f, BlockSpacing = 8f, RuleColor = Color.Orange, UnderlinedHeadingLevel = 2 },
            Placement = FixedSize.Width(Wide)
        };
        var easel = new Easel(new VStack { Children = { text } }, 1920f, 1080f);

        var rule = Assert.Single(easel.Sprites, s => s.Color == Color.Orange).Destination;
        Assert.True(rule.Y > Word(easel, "Ruled").Position.Y, "the rule is not under the heading");
        Assert.True(rule.Y < Word(easel, "Not").Position.Y, "the rule is not between the two headings");
    }

    /// <summary>Markdown given again as it was is not read again; different markdown is.</summary>
    [Fact]
    public void TheSameMarkdownIsReadOnce()
    {
        var text = new RichText { Markdown = "Made from **Airlock**" };
        var read = text.Document;

        text.Markdown = string.Concat("Made from ", "**Airlock**");
        Assert.Same(read, text.Document);

        text.Markdown = "Made from **Hangar**";
        Assert.NotSame(read, text.Document);
    }

    /// <summary>A document given as one has no markdown, and the markdown it replaced is read again when given again.</summary>
    [Fact]
    public void ADocumentGivenForgetsTheMarkdown()
    {
        var text = Marked("Made from **Airlock**");
        var given = Markdown.Parse("Other");

        text.Document = given;
        Assert.Null(text.Markdown);

        text.Markdown = "Made from **Airlock**";
        Assert.NotSame(given, text.Document);
        Assert.Equal("Made from **Airlock**", text.Markdown);
    }

    /// <summary>Strong words take the strong color and the words around them the body color.</summary>
    [Fact]
    public void StrongWordsTakeTheStrongColor()
    {
        var (_, easel) = Laid("Made from **Airlock** blueprint", Wide);

        Assert.Equal(Body, Word(easel, "Made from").Color);
        Assert.Equal(Bright, Word(easel, "Airlock").Color);
        Assert.Equal(Body, Word(easel, "blueprint").Color);
    }

    /// <summary>Words that do not fit the room carry on on the next line, and none is cut.</summary>
    [Fact]
    public void WordsThatDoNotFitWrap()
    {
        const string said = "one two three four five six seven eight nine ten";
        var (text, easel) = Laid(said, 120f);

        var lines = easel.Written.Select(c => c.Position.Y).Distinct().Count();

        Assert.True(lines > 1, "nothing wrapped");
        Assert.True(text.DesiredSize.X <= 120f, "a line is wider than the room");
        Assert.Equal(said.Replace(" ", ""), string.Concat(easel.Written.Select(c => c.Text)).Replace(" ", ""));
    }

    /// <summary>A single newline starts a new line, and a blank line starts a new block below it.</summary>
    [Fact]
    public void ANewlineStartsALine()
    {
        var (_, easel) = Laid("one\ntwo", Wide);

        Assert.True(Word(easel, "two").Position.Y > Word(easel, "one").Position.Y);
    }

    /// <summary>A bullet's words stand in, and a numbered item draws its number.</summary>
    [Fact]
    public void ListItemsStandIn()
    {
        var (_, easel) = Laid("plain\n\n- item\n\n3. counted", Wide);

        Assert.True(Word(easel, "item").Position.X > Word(easel, "plain").Position.X);
        Assert.Contains(easel.Written, c => c.Text == "3.");
    }

    /// <summary>Drawing again measures nothing, and nor does changing a color.</summary>
    [Fact]
    public void DrawingAgainMeasuresNothing()
    {
        var (text, easel) = Laid("# Title\nSome **strong** words, and *some* not.\n\n- one\n- two", Wide);

        easel.Draw(Wide, High);
        var settled = easel.Measured;

        text.Base.StrongColor = Color.Red;
        easel.Draw(Wide, High);
        easel.Draw(Wide, High);

        Assert.Equal(settled, easel.Measured);
    }

    /// <summary>A slot says its argument, set apart as the slot is.</summary>
    [Fact]
    public void ASlotSaysItsArgument()
    {
        var named = new Arguments();
        named.Set("name", "Airlock");
        var (_, easel) = Laid(Template.Parse("Made from **{name}** blueprint"), Wide, named);

        Assert.Equal(Bright, Word(easel, "Airlock").Color);
        Assert.Equal(Body, Word(easel, "blueprint").Color);
    }

    /// <summary>A new argument lays the words out again from the same document, which is never read again.</summary>
    [Fact]
    public void ANewArgumentLaysOutAgainFromTheSameDocument()
    {
        var named = new Arguments();
        named.Set("name", "Airlock");
        var (text, easel) = Laid(Template.Parse("Made from **{name}** blueprint"), Wide, named);
        var document = text.Document;

        named.Set("name", "Hangar Door");
        easel.Draw(Wide, High);

        Assert.Same(document, text.Document);
        Assert.Contains(easel.Written, c => c.Text?.Trim() == "Hangar Door");
        Assert.DoesNotContain(easel.Written, c => c.Text?.Trim() == "Airlock");
    }

    /// <summary>Setting an argument to what it already is lays nothing out.</summary>
    [Fact]
    public void TheSameArgumentLaysNothingOut()
    {
        var named = new Arguments();
        named.Set("name", "Airlock");
        var (_, easel) = Laid(Template.Parse("Made from **{name}** blueprint"), Wide, named);

        easel.Draw(Wide, High);
        var settled = easel.Measured;

        named.Set("name", "Airlock");
        easel.Draw(Wide, High);

        Assert.Equal(settled, easel.Measured);
    }

    /// <summary>An argument is words, never markdown.</summary>
    [Fact]
    public void AnArgumentIsNeverMarkdown()
    {
        var named = new Arguments();
        named.Set("name", "*Test* rig");
        var (_, easel) = Laid(Template.Parse("Made from {name}"), Wide, named);

        Assert.Contains(easel.Written, c => c.Text?.Contains("*Test* rig") == true);
    }

    /// <summary>A table with a starred column fills its room, the right-aligned column against the right edge.</summary>
    [Fact]
    public void AStarredColumnTakesTheSpareWidth()
    {
        var (text, easel) = Laid("| | |\n|:-*|--:|\n| Power cells | 450 |", Wide);

        Assert.Equal(Wide, text.DesiredSize.X);
        Assert.True(Word(easel, "450").Position.X > Wide * 0.6f * easel.Pixels, "the price is not near the right edge");
    }

    /// <summary>Starred columns share the table's width by their stars, whatever their cells hold: one star and three give a quarter and three quarters.</summary>
    [Fact]
    public void StarsShareTheWidthWhateverTheCellsHold()
    {
        var (shortFirst, shortEasel) = Laid("| | |\n|:--*|:--***|\n| A | Bee |", Wide);
        var (longFirst, longEasel) = Laid("| | |\n|:--*|:--***|\n| Ack ack ack | Bee |", Wide);

        Assert.Equal(Wide, shortFirst.DesiredSize.X);
        Assert.Equal(Wide, longFirst.DesiredSize.X);

        var second = Word(shortEasel, "Bee").Position.X;
        Assert.Equal(second, Word(longEasel, "Bee").Position.X);
        Assert.True(second > Wide * 0.25f * shortEasel.Pixels && second < Wide * 0.4f * shortEasel.Pixels,
            "the second column does not start a quarter of the way across");
    }

    /// <summary>A starred column narrower than its cell wraps the cell to its share.</summary>
    [Fact]
    public void AStarredCellWrapsToItsShare()
    {
        var (_, easel) = Laid("| | |\n|:--*|:--*******|\n| one two three four five six | x |\n| Below | y |", Wide);

        var lines = easel.Written.Where(c => c.Position.Y < Word(easel, "Below").Position.Y).Select(c => c.Position.Y).Distinct().Count();
        Assert.True(lines > 1, "the cell kept to one line past its share");
    }

    /// <summary>Words joined by a no-break space stay on one line wherever the line breaks, drawn with a space between.</summary>
    [Fact]
    public void ANoBreakSpaceKeepsItsWordsTogether()
    {
        for (var n = 1; n < 40; n++)
        {
            var (_, easel) = Laid(string.Join(" ", Enumerable.Repeat("word", n)) + " ten&nbsp;SC end", Wide);

            var holding = Assert.Single(easel.Written, command => command.Text?.Contains("SC") == true);
            Assert.Contains("ten SC", holding.Text);
        }
    }

    /// <summary>A table with no star is only as wide as its cells and the gaps between them.</summary>
    [Fact]
    public void ATableWithNoStarHugsItsCells()
    {
        var (text, _) = Laid("| | |\n|:--|--:|\n| Ice | 2 |", Wide);

        Assert.True(text.DesiredSize.X < Wide / 2f, "the table spread across its room");
    }

    /// <summary>A heading row's cells take the heading color.</summary>
    [Fact]
    public void AHeadingRowTakesTheHeadingColor()
    {
        var (_, easel) = Laid("| Item | Price |\n|--|--|\n| Ice | 2 |", Wide);

        Assert.Equal(Bright, Word(easel, "Item").Color);
        Assert.Equal(Body, Word(easel, "Ice").Color);
    }

    /// <summary>A table too wide for its room narrows its widest column, whose cell wraps, and the row grows to hold it.</summary>
    [Fact]
    public void ATooWideTableWrapsItsWidestColumn()
    {
        var (_, easel) = Laid("| | |\n|--|--|\n| one two three four five six seven eight nine ten eleven twelve | Next |\n| Below | x |", Wide);

        var lines = easel.Written.Where(c => c.Position.Y < Word(easel, "Below").Position.Y).Select(c => c.Position.Y).Distinct().Count();
        Assert.True(lines > 1, "the long cell did not wrap");
        Assert.Equal(Word(easel, "Next").Position.Y, easel.Written.Min(c => c.Position.Y));
    }

    /// <summary>A slot in a cell takes its words from the arguments.</summary>
    [Fact]
    public void ASlotInACellIsFilled()
    {
        var named = new Arguments();
        named.Set("price", "12 SC");
        var (_, easel) = Laid(Template.Parse("| | |\n|--|--:|\n| Whiskey | {price} |"), Wide, named);

        Assert.Equal(Body, Word(easel, "12 SC").Color);
    }

    /// <summary>Centered, every line stands in the middle of the room, each on its own.</summary>
    [Fact]
    public void ACenteredDocumentCentersEachLine()
    {
        var (text, easel) = Laid("# Gabbo's\nBar and lounge", Wide);
        var left = Word(easel, "Bar and lounge").Position.X;

        text.Base.TextAlignment = TextAlignment.Center;
        easel.Draw(Wide, High);

        var title = Word(easel, "Gabbo's").Position.X;
        var line = Word(easel, "Bar and lounge").Position.X;
        Assert.True(line > left + Wide * 0.2f * easel.Pixels, "the line did not move to the middle");
        Assert.NotEqual(title, line);
    }

    /// <summary>Set to the right, a line ends against the right edge.</summary>
    [Fact]
    public void ARightAlignedLineEndsAtTheEdge()
    {
        var (text, easel) = Laid("Short", Wide);
        text.Base.TextAlignment = TextAlignment.Right;
        easel.Draw(Wide, High);

        Assert.True(Word(easel, "Short").Position.X > Wide * 0.7f * easel.Pixels, "the line is not against the right edge");
    }

    /// <summary>A table narrower than its room stands in the middle of a centered document, its cells set as their columns say.</summary>
    [Fact]
    public void ACenteredDocumentCentersANarrowTable()
    {
        var (text, easel) = Laid("| | |\n|:--|--:|\n| Ice | 2 |", Wide);
        var left = Word(easel, "Ice").Position.X;

        text.Base.TextAlignment = TextAlignment.Center;
        easel.Draw(Wide, High);

        Assert.True(Word(easel, "Ice").Position.X > left + Wide * 0.2f * easel.Pixels, "the table did not move to the middle");
        Assert.True(Word(easel, "2").Position.X > Word(easel, "Ice").Position.X, "the cells lost their columns");
    }


    static (RichText, Easel) Laid(string markdown, float wide) => Laid(Markdown.Parse(markdown), wide, null);

    static (RichText, Easel) Laid(Document document, float wide, Arguments? arguments)
    {
        var text = new RichText
        {
            Document = document,
            Arguments = arguments,
            Base = { FontSize = 20f, Color = Body, StrongColor = Bright, EmphasisColor = Bright, HeadingColor = Bright, BlockSpacing = 8f, Indent = 20f },
            Placement = FixedSize.Width(wide)
        };

        var easel = new Easel(new VStack { Children = { text } }, Wide, High);
        return (text, easel);
    }

    /// <summary>Creates rich text from <paramref name="markdown" />.</summary>
    static RichText Marked(string markdown) => new RichText { Markdown = markdown };

    static DrawCommand Word(Easel easel, string text) =>
        Assert.Single(easel.Written, command => command.Text?.Trim() == text);
}
