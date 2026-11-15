using Lumel.Layout;
using Lumel.Styling;
using Lumel.Text;
using VRage.Game;
using VRageMath;
using Xunit;
using Lumel.Sim;

namespace Lumel.Tests;

/// <summary>How each kind of run is set apart by its shape: strong and emphasized words by their faces, a link by the line under it, code by the box behind it.</summary>
public class RunLookTests
{
    const float Wide = 400f;
    const float ScreenWide = 1920f;
    const float ScreenHigh = 1080f;

    static readonly Color Body = new Color(162, 195, 205);
    static readonly Color Blue = new Color(79, 195, 247);
    static readonly Color Wash = new Color(255, 255, 255, 10);

    /// <summary>Strong, emphasized and both are each set in their own face, and code stays in the code face whatever else it is.</summary>
    [Fact]
    public void EachKindOfWordIsSetInItsOwnFace()
    {
        var easel = Laid("plain **bold** *slanted* ***both*** **`code`**", text =>
        {
            text.Base.StrongFont = "Bold";
            text.Base.EmphasisFont = "Italic";
            text.Base.StrongEmphasisFont = "BoldItalic";
            text.Base.CodeFont = "Mono";
        });

        Assert.Equal(MyFontEnum.White, Word(easel, "plain").Font);
        Assert.Equal("Bold", Word(easel, "bold").Font);
        Assert.Equal("Italic", Word(easel, "slanted").Font);
        Assert.Equal("BoldItalic", Word(easel, "both").Font);
        Assert.Equal("Mono", Word(easel, "code").Font);
    }

    /// <summary>A face that is not set falls back to the nearest one that is: both strong and emphasized to the strong face, and anything to the text's own.</summary>
    [Fact]
    public void AFaceNotSetFallsBackToTheNearest()
    {
        var easel = Laid("plain **bold** *slanted* ***both***", text =>
        {
            text.Style = new RichTextStyle { Base = { Font = MyFontEnum.White } };
            text.Base.StrongFont = "Bold";
        });

        Assert.Equal("Bold", Word(easel, "both").Font);
        Assert.Equal(MyFontEnum.White, Word(easel, "slanted").Font);
    }

    /// <summary>A heading at every level, and a table's heading row, are strong throughout: their words in the strong face, emphasized ones in the face for both.</summary>
    [Theory]
    [InlineData("# Title *slanted*")]
    [InlineData("### Title *slanted*")]
    [InlineData("| Title *slanted* |\n|--|\n| plain |")]
    public void AHeadingIsStrongThroughout(string markdown)
    {
        var easel = Laid(markdown, text =>
        {
            text.Base.StrongFont = "Bold";
            text.Base.EmphasisFont = "Italic";
            text.Base.StrongEmphasisFont = "BoldItalic";
        });

        Assert.Equal("Bold", Word(easel, "Title").Font);
        Assert.Equal("BoldItalic", Word(easel, "slanted").Font);
    }

    /// <summary>A heading face of its own takes the heading's plain words, and strong and emphasized words in it keep their own faces.</summary>
    [Fact]
    public void AHeadingFaceTakesItsPlainWords()
    {
        var easel = Laid("# Title *slanted*\n\nplain", text =>
        {
            text.Base.HeadingFont = "Display";
            text.Base.StrongFont = "Bold";
            text.Base.EmphasisFont = "Italic";
        });

        Assert.Equal("Display", Word(easel, "Title").Font);
        Assert.Equal("Italic", Word(easel, "slanted").Font);
        Assert.Equal(MyFontEnum.White, Word(easel, "plain").Font);
    }

    /// <summary>A link stands out by its color alone unless its style asks for a line under it.</summary>
    [Fact]
    public void ALinkHasNoLineUnderItUnlessAsked()
    {
        var easel = Laid("Pick a [logic system](logic-system) now", text => text.Base.LinkColor = Blue);

        Assert.DoesNotContain(easel.Sprites, sprite => sprite.Color == Blue);
    }

    /// <summary>A link asked to be underlined is, in its own color, under its own words: the line runs across the space inside it and stops at its last word.</summary>
    [Fact]
    public void ALinkIsUnderlinedAcrossItsWordsOnly()
    {
        var easel = Laid("Pick a [logic system](logic-system) now", text =>
        {
            text.Base.LinkColor = Blue;
            text.Base.LinkUnderlineThickness = 1f;
            text.Base.UnderlineOffset = 0.85f;
        });

        var link = Word(easel, "logic system");
        var line = Assert.Single(easel.Sprites, sprite => sprite.Color == Blue);

        Assert.Equal("logic system", link.Text);
        Assert.Equal(link.Position.X, line.Destination.X, 1f);
        Assert.Equal(Word(easel, "now").Position.X, line.Destination.Right, 1f);
        Assert.True(line.Destination.Y > link.Position.Y, "the line is not under the words");
        Assert.True(line.Destination.Height <= easel.Pixels * 2f, "the line is thicker than a hairline");
    }

    /// <summary>A style that gives a link's underline a thickness and no place draws it under the words, not across them.</summary>
    [Fact]
    public void AnUnderlineWithNoPlaceLiesUnderTheWords()
    {
        var easel = Laid("Pick a [logic system](logic-system) now", text =>
        {
            text.Style = new RichTextStyle();
            text.Base.LinkColor = Blue;
            text.Base.LinkUnderlineThickness = 1f;
        });

        var link = Word(easel, "logic system");
        var line = Assert.Single(easel.Sprites, sprite => sprite.Color == Blue);

        Assert.True(line.Destination.Y - link.Position.Y > 10f * easel.Pixels, "the line is not under the words");
    }

    /// <summary>Code stands on a box reaching as far past its words as its padding says, and the word after it starts where the box ends.</summary>
    [Fact]
    public void CodeStandsOnABoxClearOfItsNeighbors()
    {
        var easel = Laid("Press `Open door` now", text =>
        {
            text.Base.CodeBackground = Wash;
            text.Base.CodePadding = new Thickness(4f, 0f, 8f, 0f);
        });

        var code = Word(easel, "Open door");
        var box = Assert.Single(easel.Sprites, sprite => sprite.Color == Wash);

        Assert.Equal(4f * easel.Pixels, code.Position.X - box.Destination.X, 1f);
        Assert.Equal(Word(easel, "now").Position.X, box.Destination.Right, 1f);
    }

    /// <summary>Code with no look of its own stands on nothing and takes no room beyond its words.</summary>
    [Fact]
    public void NeutralCodeHasNoBox()
    {
        var easel = Laid("Press `Open door` now", text => text.Style = new RichTextStyle());

        Assert.Empty(easel.Sprites);
    }

    /// <summary>The game's look gives code a faint wash of white reaching four units past its words, and a link's underline a place just under them.</summary>
    [Fact]
    public void TheGamesLookGivesCodeAWashAndLinksAnUnderlinePlace()
    {
        var text = new RichText { Style = GameTheme.RichText };
        text.Restyle();

        Assert.Equal(new Color(255, 255, 255, 10), text.CodeBackground);
        Assert.Equal(new Thickness(4f, -1f), text.CodePadding);
        Assert.Equal(0.85f, text.UnderlineOffset);
    }

    static Easel Laid(string markdown, Action<RichText> styled)
    {
        var text = new RichText
        {
            Document = Markdown.Parse(markdown),
            Base = { FontSize = 20f, Color = Body, StrongColor = Body, EmphasisColor = Body },
            Placement = FixedSize.Width(Wide)
        };
        styled(text);

        return new Easel(new VStack { Children = { text } }, ScreenWide, ScreenHigh);
    }

    static DrawCommand Word(Easel easel, string text) =>
        Assert.Single(easel.Written, command => command.Text?.Trim() == text);
}
