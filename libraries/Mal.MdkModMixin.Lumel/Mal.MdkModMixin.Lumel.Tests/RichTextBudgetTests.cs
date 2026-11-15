using System.Text;
using VRageMath;
using Xunit;
using Lumel.Text;

namespace Lumel.Tests;

/// <summary>
/// What rich text costs, counted rather than timed: how often it measures, how many characters it hands
/// to be measured, and how many bytes it allocates, and that each grows no faster than the text does.
/// </summary>
/// <remarks>
/// Counted against a stand-in graphics that measures every character as half its size wide, so the
/// figures are exact and the same on every machine. Run alone, because the allocation counts are per
/// thread and a test running beside these would not share a thread with them anyway, but the collector
/// is shared.
/// </remarks>
[Collection(Alone.Name)]
public class RichTextBudgetTests
{
    const float Room = 400f;

    /// <summary>Wrapped text measures each character a bounded number of times, however long the text is.</summary>
    [Fact]
    public void WrappedLayoutGrowsWithTheText()
    {
        var small = Measuring(Words(200), Room);
        var large = Measuring(Words(20000), Room);

        Assert.True(small.PerCharacter < 16f, $"{small.PerCharacter:0.0} characters measured per character");
        Assert.True(large.PerCharacter < small.PerCharacter * 1.2f,
            $"{small.PerCharacter:0.0} per character at 200 words, {large.PerCharacter:0.0} at 20000");
    }

    /// <summary>Text with no limit on its width measures each character a bounded number of times too.</summary>
    [Fact]
    public void UnwrappedLayoutGrowsWithTheText()
    {
        var small = Measuring(Words(200, false), Element.Unbounded);
        var large = Measuring(Words(20000, false), Element.Unbounded);

        Assert.True(large.PerCharacter < small.PerCharacter * 1.2f,
            $"{small.PerCharacter:0.0} per character at 200 words, {large.PerCharacter:0.0} at 20000");
    }

    /// <summary>Styled words cost no more to lay out than plain ones, relative to their length.</summary>
    [Fact]
    public void StyledLayoutGrowsWithTheText()
    {
        var small = Measuring(Styled(50), Room);
        var large = Measuring(Styled(5000), Room);

        Assert.True(large.PerCharacter < small.PerCharacter * 1.2f,
            $"{small.PerCharacter:0.0} per character at 50 sentences, {large.PerCharacter:0.0} at 5000");
    }

    /// <summary>Words in colored spans cost no more to lay out than plain ones, relative to their length.</summary>
    [Fact]
    public void SpannedLayoutGrowsWithTheText()
    {
        var small = Measuring(Spanned(50), Room);
        var large = Measuring(Spanned(5000), Room);

        Assert.True(large.PerCharacter < small.PerCharacter * 1.2f,
            $"{small.PerCharacter:0.0} per character at 50 sentences, {large.PerCharacter:0.0} at 5000");
    }

    /// <summary>A table measures each character a bounded number of times, however many rows it has.</summary>
    [Fact]
    public void TableLayoutGrowsWithItsRows()
    {
        var small = Measuring(PriceList(20), Room);
        var large = Measuring(PriceList(2000), Room);

        Assert.True(large.PerCharacter < small.PerCharacter * 1.2f,
            $"{small.PerCharacter:0.0} per character at 20 rows, {large.PerCharacter:0.0} at 2000");
    }

    /// <summary>Callouts measure each character a bounded number of times, however many blocks they hold or however deep they nest.</summary>
    [Fact]
    public void CalloutLayoutGrowsWithItsBlocks()
    {
        var small = Measuring(InCallouts(20), Room);
        var large = Measuring(InCallouts(2000), Room);

        Assert.True(large.PerCharacter < small.PerCharacter * 1.2f,
            $"{small.PerCharacter:0.0} per character at 20 paragraphs, {large.PerCharacter:0.0} at 2000");
    }

    /// <summary>Reading markdown allocates in proportion to its length, whatever its marks do.</summary>
    [Theory]
    [InlineData("plain")]
    [InlineData("closers")]
    [InlineData("openers")]
    [InlineData("nested")]
    [InlineData("mixed")]
    [InlineData("fences unclosed")]
    [InlineData("fences closing nothing")]
    [InlineData("fences nested")]
    [InlineData("spans")]
    [InlineData("spans unclosed")]
    public void ParsingGrowsWithTheText(string shape)
    {
        var small = Parsing(Source(shape, 500));
        var large = Parsing(Source(shape, 50000));

        Assert.True(large < small * 1.5f, $"{small:0.0} bytes per character at 500 marks, {large:0.0} at 50000");
    }

    /// <summary>Reading pathological marks takes no longer per character than reading plain words.</summary>
    [Theory]
    [InlineData("closers")]
    [InlineData("openers")]
    [InlineData("nested")]
    [InlineData("fences unclosed")]
    [InlineData("fences closing nothing")]
    [InlineData("fences nested")]
    [InlineData("spans")]
    [InlineData("spans unclosed")]
    public void ParsingPathologicalMarksStaysLinear(string shape)
    {
        var clock = System.Diagnostics.Stopwatch.StartNew();
        Markdown.Parse(Source(shape, 50000));
        var took = clock.Elapsed.TotalMilliseconds;

        Assert.True(took < 1000d, $"{shape}: {took:0} ms for 50000 marks");
    }

    /// <summary>A frame after the text is laid out measures nothing and allocates nothing.</summary>
    [Fact]
    public void AFrameCostsNothing()
    {
        var graphics = new Counting();
        var text = Laid(graphics, Markdown.Parse(Styled(200)), Room);

        graphics.Reset();
        var allocated = 0L;

        // A collection inside the loop makes the count include the unused part of the thread's allocation
        // context, up to 8 KB, which is no allocation at all; one that lands there anyway, from another
        // test's thread, voids the count and it is taken again.
        for (var attempt = 0; attempt < 3; attempt++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            var collections = GC.CollectionCount(0);
            var before = GC.GetAllocatedBytesForCurrentThread();

            for (var i = 0; i < 100; i++)
                Frame(graphics, text, Room);

            allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            if (GC.CollectionCount(0) == collections)
                break;
        }

        Assert.Equal(0, graphics.Calls);
        Assert.Equal(0L, allocated);
    }

    /// <summary>Changing a color lays nothing out again; changing a size lays it out once.</summary>
    [Fact]
    public void AColorLaysNothingOutAndASizeLaysOutOnce()
    {
        var graphics = new Counting();
        var text = Laid(graphics, Markdown.Parse(Styled(20)), Room);

        graphics.Reset();
        text.Base.StrongColor = Color.Red;
        Frame(graphics, text, Room);
        Assert.Equal(0, graphics.Calls);

        text.Base.FontSize = 30f;
        Frame(graphics, text, Room);
        var once = graphics.Calls;
        Frame(graphics, text, Room);

        Assert.True(once > 0, "a new size was not laid out");
        Assert.Equal(once, graphics.Calls);
    }

    static Cost Measuring(string markdown, float room)
    {
        var graphics = new Counting();
        var document = Markdown.Parse(markdown);
        Laid(graphics, document, room);

        return new Cost { PerCharacter = graphics.Characters / (float)Characters(document.Blocks) };
    }

    /// <summary>
    ///     Counts the characters in the runs of <paramref name="blocks" /> and of the blocks nested inside them.
    /// </summary>
    static int Characters(IReadOnlyList<Block> blocks)
    {
        var count = 0;
        foreach (var block in blocks)
        {
            count += Characters(block.Blocks);
            foreach (var run in block.Runs)
                count += run.Text.Length;
        }

        return count;
    }

    /// <summary>
    ///     Builds a price list as a markdown table: a heading row followed by <paramref name="rows" /> item rows, with
    ///     the names column taking the spare width.
    /// </summary>
    static string PriceList(int rows)
    {
        var text = new StringBuilder("| Item | Price |\n|:---*|---:|\n");
        for (var i = 0; i < rows; i++)
            text.Append("| Power cell ").Append(i).Append(" | **").Append(i * 7).Append(" SC** |\n");

        return text.ToString();
    }

    static float Parsing(string source)
    {
        Markdown.Parse(source);

        var before = GC.GetAllocatedBytesForCurrentThread();
        Markdown.Parse(source);
        return (GC.GetAllocatedBytesForCurrentThread() - before) / (float)source.Length;
    }

    static RichText Laid(Counting graphics, Document document, float room)
    {
        var text = new RichText { Document = document, Base = { FontSize = 20f, BlockSpacing = 8f, Indent = 20f } };
        Frame(graphics, text, room);
        return text;
    }

    static void Frame(Counting graphics, RichText text, float room)
    {
        text.Measure(graphics, new Vector2(room, Element.Unbounded));
        text.Arrange(new RectangleF(0f, 0f, text.DesiredSize.X, text.DesiredSize.Y));
        text.Draw(graphics);
    }

    /// <summary>
    ///     Builds <paramref name="count" /> ordinary words at eight per sentence, with each sentence as its own
    ///     paragraph, or all of them in one paragraph when <paramref name="paragraphs" /> is false.
    /// </summary>
    static string Words(int count, bool paragraphs = true)
    {
        var words = new StringBuilder();
        for (var i = 0; i < count; i++)
        {
            words.Append(i % 8 == 7 && paragraphs ? "airlock.\n\n" : "airlock ");
        }

        return words.ToString();
    }

    /// <summary>
    ///     Builds <paramref name="sentences" /> sentences that mix strong, emphasis, code and escapes, with an
    ///     occasional list and rule.
    /// </summary>
    static string Styled(int sentences)
    {
        var text = new StringBuilder();
        for (var i = 0; i < sentences; i++)
        {
            text.Append("The **airlock** cycles *when* the `door` shuts, then \\*vents\\*.\n");
            if (i % 5 == 4)
                text.Append("\n- one item\n2. counted\n\n---\n\n");
        }

        return text.ToString();
    }

    /// <summary>Builds <paramref name="sentences" /> sentences, each with words in two colored spans.</summary>
    static string Spanned(int sentences)
    {
        var text = new StringBuilder();
        for (var i = 0; i < sentences; i++)
            text.Append("Words and [a colored few]{color=#ffaa00} then [more]{color=red} plain. ");

        return text.ToString();
    }

    /// <summary>
    ///     Builds <paramref name="paragraphs" /> paragraphs inside callouts nested three deep, with the innermost
    ///     centered.
    /// </summary>
    static string InCallouts(int paragraphs)
    {
        var text = new StringBuilder();
        for (var i = 0; i < paragraphs; i += 10)
        {
            text.Append("::: warning\n::: player\n::: center\n");
            for (var j = 0; j < 10; j++)
                text.Append("The airlock cycles when the `door` shuts, then it vents.\n\n");

            text.Append(":::\n:::\n:::\n\n");
        }

        return text.ToString();
    }

    /// <summary>
    ///     Builds markdown of the given <paramref name="shape" /> that stresses the emphasis matcher and the box
    ///     reader, <paramref name="marks" /> marks long.
    /// </summary>
    static string Source(string shape, int marks)
    {
        var text = new StringBuilder();
        for (var i = 0; i < marks; i++)
        {
            switch (shape)
            {
                case "closers":
                    text.Append("a* ");
                    break;
                case "openers":
                    text.Append(" *a");
                    break;
                case "nested":
                    text.Append(i < marks / 2 ? "*a " : "a* ");
                    break;
                case "mixed":
                    text.Append(i % 3 == 0 ? "**a " : i % 3 == 1 ? "_b_ " : "c** ");
                    break;
                case "fences unclosed":
                    text.Append("::: a\n");
                    break;
                case "fences closing nothing":
                    text.Append(":::\n");
                    break;
                case "spans":
                    text.Append("[a]{color=red} ");
                    break;
                case "spans unclosed":
                    text.Append("[a]{color=red ");
                    break;
                case "fences nested":
                    text.Append(i < marks / 2 ? "::: a\n" : ":::\n");
                    break;
                default:
                    text.Append("word ");
                    break;
            }
        }

        return text.ToString();
    }

    struct Cost
    {
        public float PerCharacter;
    }
}
