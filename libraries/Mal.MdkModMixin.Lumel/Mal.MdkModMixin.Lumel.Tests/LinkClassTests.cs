using Lumel.Drawing;
using Lumel.Images;
using Lumel.Input;
using Lumel.Layout;
using Lumel.Sim;
using Lumel.Text;
using VRageMath;
using Xunit;

namespace Lumel.Tests;

/// <summary>A link's class: written after its target, it picks a look of the host's, and a look can dot the underline and refuse presses.</summary>
public class LinkClassTests
{
    const float Wide = 400f;
    const float High = 400f;

    /// <summary>A class is read from after the target, and a link without one has none.</summary>
    [Fact]
    public void AClassFollowsTheTarget()
    {
        var runs = Markdown.Parse("A [term](key class=term) and [link](there)").Blocks[0].Runs;

        var term = Assert.IsType<LinkRun>(runs[1]);
        Assert.Equal("key", term.Link.Target);
        Assert.Equal("term", term.Link.Class);
        Assert.Null(Assert.IsType<LinkRun>(runs[3]).Link.Class);
    }

    /// <summary>Anything else after a target leaves what was written as words.</summary>
    [Theory]
    [InlineData("[a](b c)")]
    [InlineData("[a](b class=)")]
    [InlineData("[a](b class=x class=y)")]
    [InlineData("[a](b class=x!)")]
    public void AnythingElseAfterTheTargetIsWords(string written)
    {
        var runs = Markdown.Parse(written).Blocks[0].Runs;

        Assert.DoesNotContain(runs, run => run is LinkRun);
        Assert.Equal(written, string.Concat(runs.Select(run => run.Text)));
    }

    /// <summary>An image still takes its size and turn from the words after its locator, and a class is not one of them.</summary>
    [Fact]
    public void AnImageKeepsItsOwnWords()
    {
        var image = Assert.IsType<ImageRun>(Markdown.Parse("![a](b =16x8 90deg)").Blocks[0].Runs[0]);

        Assert.Equal(16f, image.Picture.Width);
        Assert.DoesNotContain(Markdown.Parse("![a](b class=x)").Blocks[0].Runs, run => run is ImageRun);
    }

    /// <summary>A link's class gives it its own color and underline, and a link of another class keeps the text's.</summary>
    [Fact]
    public void AClassGivesALinkItsLook()
    {
        var (_, easel) = Laid("A [term](key class=term) and [link](there)");

        Assert.Equal(Color.Orange, Word(easel, "term").Color);
        Assert.Equal(Color.White, Word(easel, "link").Color);
        var dots = Assert.Single(easel.Sprites, sprite => sprite.Material == LumelMaterials.Dots);
        Assert.Equal(Color.Orange, dots.Color);
        Assert.Equal(1, easel.Sprites.Count(sprite => sprite.Material == LumelMaterials.Fill && sprite.Color == Color.White));
    }

    /// <summary>A change to a class's look reaches what is drawn.</summary>
    [Fact]
    public void AChangedClassLookIsDrawn()
    {
        var (text, easel) = Laid("A [term](key class=term) here");

        text.Base.Links["term"].Color = Color.Purple;
        easel.Draw(Wide, High);

        Assert.Equal(Color.Purple, Word(easel, "term").Color);
    }

    /// <summary>A link the text is told to show plain looks like the words around it, whatever its class, and has no place to press.</summary>
    [Fact]
    public void APlainLinkIsTheWordsAroundIt()
    {
        var (text, easel) = Laid("A [term](key class=term) here");

        text.FnIsLinkPlain = link => link.Target == "key";
        easel.Draw(Wide, High);

        var drawn = Assert.Single(easel.Written, command => command.Text?.Contains("term") == true);
        Assert.Equal("A term here", drawn.Text?.Trim());
        Assert.DoesNotContain(easel.Sprites, sprite => sprite.Material == LumelMaterials.Dots);
        Assert.Null(text.LinkAt(new Vector2(text.ContentBounds.X + 40f, text.ContentBounds.Y + 10f)));
    }

    /// <summary>Setting the answer again lays the text out by the new answer, with the document unchanged.</summary>
    [Fact]
    public void AChangedAnswerIsReadWhenSetAgain()
    {
        var (text, easel) = Laid("[term](key class=term)");
        text.FnIsLinkPlain = _ => true;
        easel.Draw(Wide, High);
        Assert.DoesNotContain(easel.Sprites, sprite => sprite.Material == LumelMaterials.Dots);

        text.FnIsLinkPlain = _ => false;
        easel.Draw(Wide, High);

        Assert.Contains(easel.Sprites, sprite => sprite.Material == LumelMaterials.Dots);
    }

    /// <summary>An image inside a link's words is part of the link, between its words, and its entity reads as a character.</summary>
    [Fact]
    public void AnImageCanBeInsideALink()
    {
        var runs = Markdown.Parse("A [![mark](m)&nbsp;logic system](key class=term) here").Blocks[0].Runs;

        var image = Assert.IsType<ImageRun>(runs[1]);
        var words = Assert.IsType<LinkRun>(runs[2]);
        Assert.Same(words.Link, image.Link);
        Assert.Equal("m", image.Picture.Locator);
        Assert.Equal(Run.NoBreakSpace + "logic system", words.Text);
        Assert.Equal("term", words.Link.Class);
    }

    /// <summary>A link of nothing but an image is a link, and counts among the document's links once.</summary>
    [Fact]
    public void ALinkCanBeOnlyAnImage()
    {
        var document = Markdown.Parse("[![a](m)](key)");

        var image = Assert.IsType<ImageRun>(Assert.Single(document.Blocks[0].Runs));
        Assert.Equal("key", image.Link.Target);
        Assert.Single(document.Links);
    }

    /// <summary>Any bracket in a link's words but an image's leaves what was written as words.</summary>
    [Fact]
    public void OtherBracketsInALinksWordsAreWords()
    {
        Assert.DoesNotContain(Markdown.Parse("[a [b] c](key)").Blocks[0].Runs, run => run is LinkRun);
    }

    /// <summary>An image in a link takes the link's color in its class's look, an image outside any link stays as it is.</summary>
    [Fact]
    public void AnImageInALinkTakesTheLinksColor()
    {
        var (_, easel) = Pictured("[![a](m)&nbsp;term](key class=term) ![b](n)", null);

        Assert.Equal(Color.Orange, easel.Sprites.Single(s => s.Material.String == "m").Color);
        Assert.Equal(Color.White, easel.Sprites.Single(s => s.Material.String == "n").Color);
    }

    /// <summary>A plain link's image takes the color of the words around it.</summary>
    [Fact]
    public void APlainLinksImageIsTheWordsColor()
    {
        var (text, easel) = Pictured("[![a](m)&nbsp;term](key class=term)", _ => true);

        Assert.Equal(text.Color, easel.Sprites.Single(s => s.Material.String == "m").Color);
    }

    /// <summary>The underline runs under the words alone, past the image and the space joining them.</summary>
    [Fact]
    public void TheUnderlineSkipsTheImageAndTheSpace()
    {
        var (_, easel) = Pictured("[![a](m)&nbsp;term](key class=term)", null);

        var image = easel.Sprites.Single(s => s.Material.String == "m").Destination;
        var dots = Assert.Single(easel.Sprites, s => s.Material == LumelMaterials.Dots).Destination;
        Assert.True(dots.X > image.X + image.Width, $"the dots start at {dots.X}, under the image ending at {image.X + image.Width}");
    }

    /// <summary>The pointer finds the link on its image too, and the tip and press go with it.</summary>
    [Fact]
    public void TheLinkReachesItsImage()
    {
        var (text, _) = Pictured("[![a](m)&nbsp;term](key)", null);

        var onImage = new Vector2(text.ContentBounds.X + 4f, text.ContentBounds.Y + 10f);

        Assert.Equal("key", text.LinkAt(onImage)?.Target);
    }

    /// <summary>A term wrapped across two lines draws one sprite per line.</summary>
    [Fact]
    public void AWrappedTermDrawsOneSpritePerLine()
    {
        var words = string.Join(" ", Enumerable.Repeat("airlock", 16));
        var (_, easel) = Laid("[" + words + "](key class=term)");

        var lines = easel.Written.Select(w => w.Position.Y).Distinct().Count();
        Assert.True(lines > 1, "the term did not wrap");
        Assert.Equal(lines, easel.Sprites.Count(sprite => sprite.Material == LumelMaterials.Dots));
    }

    /// <summary>A press on a link whose look refuses presses follows nothing, and a link beside it still follows.</summary>
    [Fact]
    public void ALookCanRefusePresses()
    {
        var (text, _) = Laid("[term](key class=term) and more");
        Link followed = null;
        text.LinkClicked += link => followed = link;
        var input = new InputRoot(text);
        var onTerm = new Vector2(text.ContentBounds.X + 4f, text.ContentBounds.Y + 10f);

        input.PointerDown(onTerm, PointerButton.Left, 1, false, false);
        input.PointerUp(onTerm, PointerButton.Left);

        Assert.Null(followed);
        Assert.Equal("key", text.LinkAt(onTerm).Target);
    }

    /// <summary>A link a look leaves to be pressed is followed as any other.</summary>
    [Fact]
    public void ALinkOfAnotherClassIsStillFollowed()
    {
        var (text, _) = Laid("[link](there) and more");
        Link followed = null;
        text.LinkClicked += link => followed = link;
        var input = new InputRoot(text);
        var onLink = new Vector2(text.ContentBounds.X + 4f, text.ContentBounds.Y + 10f);

        input.PointerDown(onLink, PointerButton.Left, 1, false, false);
        input.PointerUp(onLink, PointerButton.Left);

        Assert.Equal("there", followed.Target);
    }

    /// <summary>A term keeps its tip, though it cannot be pressed.</summary>
    [Fact]
    public void ATermKeepsItsTip()
    {
        var (text, _) = Laid("[term](key class=term) and more");
        var tip = Tip.For(null, "A definition.");
        text.FnResolveLinkTip = link => link.Target == "key" ? tip : null;
        text.LinkClicked += _ => { };

        var onTerm = new Vector2(text.ContentBounds.X + 4f, text.ContentBounds.Y + 10f);

        Assert.Same(tip, text.TipAt(onTerm));
        Assert.Same(text, text.HitTest(onTerm));
    }

    /// <summary>A dotted line is a slice of the strip that ends on a whole dot, whatever its length.</summary>
    [Fact]
    public void ADottedLineEndsOnAWholeDot()
    {
        var easel = new Easel(new VStack { Children = { new Dotted(new RectangleF(0f, 0f, 38f, 2f)) } }, Wide, High);

        var dots = Assert.Single(easel.Sprites, sprite => sprite.Material == LumelMaterials.Dots);
        Assert.Equal(new Rectangle(0, 0, 10 * 16 - 8, 8), dots.Source);
    }

    /// <summary>A line longer than the strip goes on in a second sprite, with no break between them.</summary>
    [Fact]
    public void ALineLongerThanTheStripTakesASecondSprite()
    {
        var recording = new Recording(new RectangleF(0f, 0f, 1000f, 100f));

        DottedLine.Draw(recording, new RectangleF(10f, 0f, 600f, 2f), Color.White);

        Assert.Equal(2, recording.Pieces.Count);
        var first = recording.Pieces[0].Destination;
        var second = recording.Pieces[1].Destination;
        Assert.Equal(first.X + first.Width, second.X, 2);
        Assert.Equal(610f, second.X + second.Width, 2);
    }

    /// <summary>Where only whole pictures can be drawn, as on a text panel, the line is solid.</summary>
    [Fact]
    public void ATextPanelDrawsTheLineSolid()
    {
        var recording = new Recording(new RectangleF(0f, 0f, 1000f, 100f)) { SupportsSourceRectangles = false };

        DottedLine.Draw(recording, new RectangleF(10f, 0f, 60f, 2f), Color.White);

        Assert.Empty(recording.Pieces);
        Assert.Single(recording.Fills);
    }

    static (RichText, Easel) Laid(string markdown)
    {
        var text = new RichText
        {
            Document = Markdown.Parse(markdown),
            Base =
            {
                FontSize = 20f,
                LinkUnderlineThickness = 2f,
                Links = { ["term"] = { Color = Color.Orange, Underline = UnderlineStyle.Dotted, Presses = false } }
            },
            Placement = FixedSize.Width(Wide)
        };

        var easel = new Easel(new VStack { Children = { text } }, Wide, High);
        return (text, easel);
    }

    /// <summary>Lays out <paramref name="markdown" /> with every locator a picture, and the links answered by <paramref name="plain" />.</summary>
    static (RichText, Easel) Pictured(string markdown, Func<Link, bool>? plain)
    {
        var text = new RichText
        {
            Document = Markdown.Parse(markdown),
            FnIsLinkPlain = plain,
            Base =
            {
                FontSize = 20f,
                Color = Color.Gray,
                LinkUnderlineThickness = 2f,
                Links = { ["term"] = { Color = Color.Orange, Underline = UnderlineStyle.Dotted } }
            },
            Placement = FixedSize.Width(Wide)
        };

        var easel = new Easel(new VStack { Children = { text } }, Wide, High, new MaterialImages(_ => new Vector2(64f, 64f)));
        return (text, easel);
    }

    static DrawCommand Word(Easel easel, string text) =>
        Assert.Single(easel.Written, command => command.Text?.Trim() == text);

    /// <summary>An element that draws one dotted line.</summary>
    sealed class Dotted(RectangleF line) : Element
    {
        /// <inheritdoc />
        protected override void DrawContent(IGraphics g)
        {
            DottedLine.Draw(g, line, Color.White);
        }

        /// <inheritdoc />
        protected override Vector2 MeasureContent(IGraphics g, Vector2 available) => new Vector2(100f, 100f);
    }
}
