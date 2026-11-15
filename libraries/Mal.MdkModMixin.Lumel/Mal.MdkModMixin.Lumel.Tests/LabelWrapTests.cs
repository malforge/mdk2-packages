using Lumel.Text;
using VRageMath;
using Xunit;

namespace Lumel.Tests;

/// <summary>
/// A label's wrap and trim, which halve their way to where a line breaks: they always finish, whatever the text and
/// the room, and cost a few measures a line rather than one a word.
/// </summary>
/// <remarks>Measured by a stand-in graphics where every character is half its size wide, so a line of 20 is 10 characters to the 100.</remarks>
public class LabelWrapTests
{
    /// <summary>Text and rooms that trap a wrap that does not always move on; each must finish, with every word still there.</summary>
    [Theory]
    [InlineData("averyveryverylongwordthatfitsnowhere short", 50f)]
    [InlineData("          ", 100f)]
    [InlineData("a b c d e f g h", 0f)]
    [InlineData("a b c d e f g h", -10f)]
    [InlineData("a b c d e f g h", float.NaN)]
    [InlineData("\n\n\n", 100f)]
    [InlineData("one\n\ntwo three four five six seven", 60f)]
    [InlineData("x", 1f)]
    public void AWrapAlwaysFinishes(string text, float room)
    {
        var (label, _) = Wrapped(text, room);

        Assert.Equal(Words(text), Words(string.Join(" ", label.Lines)));
    }

    /// <summary>A word too wide for any line has a line to itself, and the words after it go on as usual.</summary>
    [Fact]
    public void AWordTooWideHasALineToItself()
    {
        var (label, _) = Wrapped("tiny averyveryverylongword tiny", 100f);

        Assert.Equal(new[] { "tiny", "averyveryverylongword", "tiny" }, label.Lines);
    }

    /// <summary>A wrapped label measures a few times a line, however many words are on it.</summary>
    [Fact]
    public void AWrapMeasuresAFewTimesALine()
    {
        var words = string.Join(" ", Enumerable.Repeat("word", 2000));
        var (label, graphics) = Wrapped(words, 400f);

        var lines = label.Lines.Count;
        var limit = lines * (2 + (int)Math.Ceiling(Math.Log(2000, 2)));

        Assert.True(graphics.Calls <= limit, $"{graphics.Calls} measures for {lines} lines");
    }

    /// <summary>A trimmed label finds where to cut in a handful of measures, however long the text.</summary>
    [Fact]
    public void ATrimMeasuresAHandfulOfTimes()
    {
        var graphics = new Counting();
        var label = new Label { Text = new string('a', 5000), Base = { FontSize = 20f }, TextWrapping = TextWrapping.Trim };

        label.Measure(graphics, new Vector2(100f, 100f));

        Assert.True(graphics.Calls <= 2 + (int)Math.Ceiling(Math.Log(5000, 2)), $"{graphics.Calls} measures");
        Assert.EndsWith("…", label.Lines[0]);
    }

    /// <summary>A trim that has no room at all keeps only the ellipsis, and finishes.</summary>
    [Theory]
    [InlineData(0f)]
    [InlineData(-5f)]
    [InlineData(float.NaN)]
    public void ATrimWithNoRoomFinishes(float room)
    {
        var label = new Label { Text = "Airlock", Base = { FontSize = 20f }, TextWrapping = TextWrapping.Trim };

        label.Measure(new Counting(), new Vector2(room, 100f));

        Assert.Equal("…", label.Lines[0]);
    }

    static (Label, Counting) Wrapped(string text, float room)
    {
        var graphics = new Counting();
        var label = new Label { Text = text, Base = { FontSize = 20f }, TextWrapping = TextWrapping.Wrap };
        label.Measure(graphics, new Vector2(room, 100000f));
        return (label, graphics);
    }

    static string[] Words(string text) =>
        text.Split(new[] { ' ', '\n' }, StringSplitOptions.RemoveEmptyEntries);
}
