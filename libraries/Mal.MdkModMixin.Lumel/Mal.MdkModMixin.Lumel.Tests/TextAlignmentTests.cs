using Lumel.Layout;
using Lumel.Styling;
using Lumel.Text;
using Xunit;

namespace Lumel.Tests;

/// <summary>Where a text control's lines sit across its room, as its TextAlignment look says.</summary>
public class TextAlignmentTests
{
    const float Wide = 400f;
    const float High = 200f;
    const float Room = 300f;

    /// <summary>A label's lines sit against the left, in the middle or against the right of its room, wrapped lines each on their own.</summary>
    [Theory]
    [InlineData(TextAlignment.Left)]
    [InlineData(TextAlignment.Center)]
    [InlineData(TextAlignment.Right)]
    public void ALabelSetsItsLinesWhereItsLookSays(TextAlignment alignment)
    {
        var label = new Label
        {
            Text = "Airlock\nthe outer door",
            TextWrapping = TextWrapping.Wrap,
            Base = { TextAlignment = alignment },
            Placement = FixedSize.Width(Room)
        };
        var easel = new Easel(new VStack { Children = { label } }, Wide, High);

        foreach (var line in new[] { "Airlock", "the outer door" })
        {
            var drawn = Assert.Single(easel.Written, c => c.Text == line);
            var left = label.ContentBounds.X * easel.Pixels;
            var right = (label.ContentBounds.X + Room) * easel.Pixels;
            var width = easel.WidthOf(line);
            var expected = alignment == TextAlignment.Left ? left : alignment == TextAlignment.Center ? (left + right - width) / 2f : right - width;
            Assert.Equal(expected, drawn.Position.X, 1f);
        }
    }

    /// <summary>A trimmed label's one line sits where its look says.</summary>
    [Fact]
    public void ATrimmedLabelSitsWhereItsLookSays()
    {
        var label = new Label { Text = "Air", TextWrapping = TextWrapping.Trim, Base = { TextAlignment = TextAlignment.Right }, Placement = FixedSize.Width(Room) };
        var easel = new Easel(new VStack { Children = { label } }, Wide, High);

        var drawn = Assert.Single(easel.Written);
        Assert.Equal((label.ContentBounds.X + Room) * easel.Pixels - easel.WidthOf("Air"), drawn.Position.X, 1f);
    }

    /// <summary>A text box's line, and its hint, sit where its look says while they are shorter than the box.</summary>
    [Theory]
    [InlineData(TextAlignment.Left)]
    [InlineData(TextAlignment.Center)]
    [InlineData(TextAlignment.Right)]
    public void ATextBoxSetsItsLineWhereItsLookSays(TextAlignment alignment)
    {
        var box = new TextBox { Text = "Air", Base = { TextAlignment = alignment, CaretWidth = 0f }, Placement = FixedSize.Width(Room) };
        var hinted = new TextBox { Hint = "Air", Base = { TextAlignment = alignment, CaretWidth = 0f }, Placement = FixedSize.Width(Room) };
        var easel = new Easel(new VStack { Children = { box, hinted } }, Wide, High);

        var drawn = easel.Written.ToList();
        Assert.Equal(2, drawn.Count);
        foreach (var (element, command) in new[] { (box, drawn[0]), (hinted, drawn[1]) })
        {
            var left = element.ContentBounds.X * easel.Pixels;
            var room = element.ContentBounds.Width * easel.Pixels;
            var width = easel.WidthOf("Air");
            var expected = alignment == TextAlignment.Left ? left : alignment == TextAlignment.Center ? left + (room - width) / 2f : left + room - width;
            Assert.Equal(expected, command.Position.X, 1f);
        }
    }

    /// <summary>A right-aligned hint stays flush right when a look changes the box's font size.</summary>
    [Fact]
    public void AHintStaysFlushRightWhenTheFontSizeChanges()
    {
        var hinted = new TextBox { Hint = "Air", Base = { TextAlignment = TextAlignment.Right, CaretWidth = 0f }, Placement = FixedSize.Width(Room) };
        var easel = new Easel(new VStack { Children = { hinted } }, Wide, High);

        hinted.Base.FontSize = 32f;
        easel.Draw(Wide, High);

        var drawn = Assert.Single(easel.Written);
        var right = (hinted.ContentBounds.X + hinted.ContentBounds.Width) * easel.Pixels;
        Assert.Equal(right - easel.WidthOf("Air", 32f), drawn.Position.X, 1f);
    }

    /// <summary>A number box's numbers sit against its right-hand end by the game's look, which a style can set back to the left.</summary>
    [Fact]
    public void ANumberBoxSitsRightByTheGamesLook()
    {
        var game = new NumberBox();
        var left = new NumberBox { Style = new NumberBoxStyle(GameTheme.NumberBox) { Base = { TextAlignment = TextAlignment.Left } } };
        game.Restyle();
        left.Restyle();

        Assert.Equal(TextAlignment.Right, game.TextAlignment);
        Assert.Equal(TextAlignment.Left, left.TextAlignment);
    }

    /// <summary>A number box takes a number box's style, and refuses one for any text box.</summary>
    [Fact]
    public void ANumberBoxRefusesATextBoxStyle()
    {
        var box = new NumberBox();

        Assert.Throws<ArgumentException>(() => ((TextBox)box).Style = new TextBoxStyle());
    }
}
