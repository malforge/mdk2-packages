using Lumel.Images;
using Lumel.Layout;
using Lumel.Text;
using VRage.Utils;
using VRageMath;
using Xunit;
using Lumel.Sim;

namespace Lumel.Tests;

/// <summary>Pictures: an image element sized by its picture's shape, and images in markdown-lite.</summary>
public class ImageTests
{
    const float Wide = 400f;
    const float High = 400f;

    static readonly Color Body = new Color(162, 195, 205);

    /// <summary>A picture twice as wide as it is tall.</summary>
    static readonly Sprite Banner = new Sprite(MyStringId.GetOrCompute("Banner"), new Vector2(200f, 100f));

    /// <summary>A picture whose size nobody knows.</summary>
    static readonly Sprite Unknown = new Sprite(MyStringId.GetOrCompute("Unknown"), Vector2.Zero);

    /// <summary>An image given its width takes its height from the picture's shape.</summary>
    [Fact]
    public void AnImageGivenAWidthKeepsItsShape()
    {
        var image = new Image { Source = Banner, Placement = FixedSize.Width(100f) };
        _ = new Easel(new VStack { Children = { new HStack { Children = { image } } } }, Wide, High);

        Assert.Equal(new Vector2(100f, 50f), new Vector2(image.Bounds.Width, image.Bounds.Height));
    }

    /// <summary>An image given its height takes its width from the picture's shape.</summary>
    [Fact]
    public void AnImageGivenAHeightKeepsItsShape()
    {
        var image = new Image { Source = Banner, Placement = FixedSize.Height(30f) };
        _ = new Easel(new VStack { Children = { new HStack { Children = { image } } } }, Wide, High);

        Assert.Equal(new Vector2(60f, 30f), new Vector2(image.Bounds.Width, image.Bounds.Height));
    }

    /// <summary>An image given no size fills the width it is offered, in the picture's shape.</summary>
    [Fact]
    public void AnImageGivenNoSizeFillsItsRoomInShape()
    {
        var image = new Image { Source = Banner };
        _ = new Easel(new VStack { Children = { image } }, Wide, High);

        Assert.Equal(new Vector2(Wide, Wide / 2f), new Vector2(image.Bounds.Width, image.Bounds.Height));
    }

    /// <summary>A turned image is drawn turned, in the room it would have unturned.</summary>
    [Fact]
    public void ATurnedImageIsDrawnTurnedInItsOwnRoom()
    {
        var image = new Image { Source = Banner, Placement = FixedSize.Width(100f), Rotation = MathHelper.PiOver4 };
        var easel = new Easel(new VStack { Children = { new HStack { Children = { image } } } }, Wide, High);

        var drawn = Assert.Single(easel.Sprites, s => s.Material.String == "Banner");
        Assert.Equal(MathHelper.PiOver4, drawn.Rotation, 3);
        Assert.Equal(new Vector2(100f, 50f), new Vector2(image.Bounds.Width, image.Bounds.Height));
    }

    /// <summary>A turn set after the image is laid out shows at its next draw.</summary>
    [Fact]
    public void ATurnShowsAtTheNextDraw()
    {
        var image = new Image { Source = Banner, Placement = FixedSize.Width(100f) };
        var easel = new Easel(new VStack { Children = { new HStack { Children = { image } } } }, Wide, High);

        image.Rotation = MathHelper.Pi;
        easel.Draw(Wide, High);

        var drawn = Assert.Single(easel.Sprites, s => s.Material.String == "Banner");
        Assert.Equal(MathHelper.Pi, drawn.Rotation, 3);
    }

    /// <summary>A picture of no known size is taken as square.</summary>
    [Fact]
    public void APictureOfNoKnownSizeIsSquare()
    {
        var image = new Image { Source = Unknown, Placement = FixedSize.Width(40f) };
        _ = new Easel(new VStack { Children = { new HStack { Children = { image } } } }, Wide, High);

        Assert.Equal(40f, image.Bounds.Height);
    }

    /// <summary>An image in markdown gives its picture's locator, describes it, and may say how big it is.</summary>
    [Theory]
    [InlineData("![The logo](Logo)", float.NaN, float.NaN)]
    [InlineData("![](Logo =64)", 64f, float.NaN)]
    [InlineData("![](Logo =64x)", 64f, float.NaN)]
    [InlineData("![](Logo =64x32)", 64f, 32f)]
    [InlineData("![](Logo =x32.5)", float.NaN, 32.5f)]
    public void AnImageIsReadWithItsSize(string source, float width, float height)
    {
        var run = Assert.Single(Markdown.Parse(source).Blocks[0].Runs);

        var image = Assert.IsType<ImageRun>(run);
        Assert.Equal("Logo", image.Picture.Locator);
        Assert.Equal(width, image.Picture.Width);
        Assert.Equal(height, image.Picture.Height);
    }

    /// <summary>An image's description is its words, kept for when the picture is missing.</summary>
    [Fact]
    public void AnImageKeepsItsDescription()
    {
        Assert.Equal("The logo", Assert.Single(Markdown.Parse("![The logo](Logo)").Blocks[0].Runs).Text);
    }

    /// <summary>An image may be turned, written in degrees clockwise after the locator, before or after its size.</summary>
    [Theory]
    [InlineData("![](Logo 90deg)", 90f, float.NaN)]
    [InlineData("![](Logo =64 -45deg)", -45f, 64f)]
    [InlineData("![](Logo 12.5deg =64x32)", 12.5f, 64f)]
    [InlineData("![](Logo)", 0f, float.NaN)]
    public void AnImageIsReadWithItsTurn(string source, float rotation, float width)
    {
        var image = Assert.IsType<ImageRun>(Assert.Single(Markdown.Parse(source).Blocks[0].Runs));

        Assert.Equal(rotation, image.Picture.Rotation);
        Assert.Equal(width, image.Picture.Width);
    }

    /// <summary>A turned image takes the room of its turned outline, and is drawn turned in the middle of it.</summary>
    [Fact]
    public void ATurnedImageTakesTheRoomOfItsOutline()
    {
        var easel = Laid("Fuel ![](Banner =40x20 90deg) low");

        var drawn = Assert.Single(easel.Sprites, s => s.Material.String == "Banner");
        var words = Word(easel, "low").Position.X - Word(easel, "Fuel").Position.X;
        var plain = Laid("Fuel ![](Banner =40x20) low");
        var unturned = Word(plain, "low").Position.X - Word(plain, "Fuel").Position.X;

        Assert.Equal(MathHelper.PiOver2, drawn.Rotation, 3);
        Assert.Equal(20f * easel.Pixels, unturned - words, 1f);
    }

    /// <summary>An image whose size is not a size, whose turn is not a turn, which says either twice, or whose brackets do not close, is the text it is.</summary>
    [Theory]
    [InlineData("![](Logo =big)")]
    [InlineData("![](Logo =0)")]
    [InlineData("![](Logo 64)")]
    [InlineData("![](Logo deg)")]
    [InlineData("![](Logo 90deg 90deg)")]
    [InlineData("![](Logo =64 =32)")]
    [InlineData("![](Logo")]
    [InlineData("![]()")]
    public void AMalformedImageIsText(string source)
    {
        var runs = Markdown.Parse(source).Blocks[0].Runs;

        Assert.DoesNotContain(runs, r => r is ImageRun);
        Assert.Equal(source, string.Concat(runs.Select(r => r.Text)));
    }

    /// <summary>An image stands among words and in a table's cell.</summary>
    [Fact]
    public void AnImageStandsAmongWordsAndInCells()
    {
        var runs = Markdown.Parse("Fuel ![](Tank) low").Blocks[0].Runs;
        Assert.Equal(new[] { "Fuel ", "", " low" }, runs.Select(r => r is ImageRun ? "" : r.Text));

        var table = Markdown.Parse("| | |\n|--|--|\n| ![](Tank) | low |").Blocks[0];
        Assert.IsType<ImageRun>(Assert.Single(table.Blocks[0].Blocks[0].Runs));
    }

    /// <summary>An image with no size written is as tall as its line, and as wide as its picture's shape makes it.</summary>
    [Fact]
    public void AnImageIsAsTallAsItsLine()
    {
        var easel = Laid("Fuel ![](Banner) low");

        var drawn = Assert.Single(easel.Sprites, s => s.Material.String == "Banner").Destination;
        Assert.Equal(20f * easel.Pixels, drawn.Height, 1f);
        Assert.Equal(2f * drawn.Height, drawn.Width, 1f);
        Assert.True(Word(easel, "low").Position.X >= drawn.X + drawn.Width - 1f, "the words after the image overlap it");
    }

    /// <summary>An image taller than its line makes the line taller, its words standing on the line's foot.</summary>
    [Fact]
    public void ATallImageGrowsItsLine()
    {
        var easel = Laid("Fuel ![](Banner =x60) low\nnext");

        var drawn = Assert.Single(easel.Sprites, s => s.Material.String == "Banner").Destination;
        var low = Word(easel, "low").Position.Y;
        Assert.Equal(drawn.Y + drawn.Height - 20f * easel.Pixels, low, 2f);
        Assert.True(Word(easel, "next").Position.Y >= drawn.Y + drawn.Height - 1f, "the next line overlaps the image");
    }

    /// <summary>An image that does not fit after the words before it starts the next line, against the left edge.</summary>
    [Fact]
    public void AnImageThatDoesNotFitStartsALine()
    {
        var easel = Laid("Fuel tanks ![](Banner =400x100)");

        var drawn = Assert.Single(easel.Sprites, s => s.Material.String == "Banner").Destination;
        var words = Word(easel, "Fuel tanks").Position;
        Assert.Equal(words.X, drawn.X, 1f);
        Assert.True(drawn.Y >= words.Y + 20f * easel.Pixels - 1f, "the image stayed on the words' line");
    }

    /// <summary>A centered column sets each cell's lines in its middle.</summary>
    [Fact]
    public void ACenteredColumnCentersItsCells()
    {
        var easel = Laid("| | |\n|:-:|--|\n| A much wider cell | x |\n| ab | y |");

        var wide = Word(easel, "A much wider cell");
        var narrow = Word(easel, "ab");
        Assert.True(narrow.Position.X > wide.Position.X + 1f, "the narrow cell is not set in from the column's edge");
        Assert.True(narrow.Position.X < Word(easel, "y").Position.X, "the narrow cell left its column");
    }

    /// <summary>An image whose picture is not found says its description in its place.</summary>
    [Fact]
    public void AMissingPictureSaysItsDescription()
    {
        var easel = Laid("Welcome ![to Gabbo's](Nowhere)");

        Assert.Empty(easel.Sprites);
        Assert.Equal(Body, Word(easel, "Welcome to Gabbo's").Color);
    }

    /// <summary>A locator reaches the host's resolver exactly as written, whatever it looks like.</summary>
    [Fact]
    public void ALocatorReachesTheResolverAsWritten()
    {
        var pictures = new Pictures();
        Laid("A ![signal](mark:signal =20) and ![](Textures\\GUI\\Icon.dds)", pictures);

        Assert.Equal(new[] { "mark:signal", "Textures\\GUI\\Icon.dds" }, pictures.Asked);
    }

    /// <summary>A picture joined to the link after it by a no-break space stays on its line, wherever the line breaks.</summary>
    [Fact]
    public void AMarkKeepsToTheLinkAfterItsNoBreakSpace()
    {
        for (var n = 1; n < 40; n++)
        {
            var easel = Laid(string.Join(" ", Enumerable.Repeat("word", n)) + " ![](Banner =20)&nbsp;[triggers](trigger) end");

            var drawn = Assert.Single(easel.Sprites, s => s.Material.String == "Banner").Destination;
            var link = Word(easel, "triggers");
            // Beside it on one line, the link starts just past the picture; carried to the next line, it starts at the left.
            var line = 20f * easel.Pixels;
            Assert.True(link.Position.X > drawn.X && link.Position.X < drawn.X + drawn.Width + line,
                "with " + n + " words the picture and its link were split across lines");
        }
    }

    static Easel Laid(string markdown) => Laid(markdown, new Pictures());

    static Easel Laid(string markdown, Pictures pictures)
    {
        var text = new RichText
        {
            Document = Markdown.Parse(markdown),
            Base = { FontSize = 20f, Color = Body, BlockSpacing = 8f, Indent = 20f },
            Placement = FixedSize.Width(Wide)
        };

        return new Easel(new VStack { Children = { text } }, Wide, High, pictures);
    }

    static DrawCommand Word(Easel easel, string text) =>
        Assert.Single(easel.Written, command => command.Text?.Trim() == text);

    /// <summary>
    ///     An <see cref="IImageCatalog" /> that resolves the two pictures above by locator and records every locator it
    ///     is asked about.
    /// </summary>
    sealed class Pictures : IImageCatalog
    {
        public List<string> Asked { get; } = new List<string>();

        public IImageSource? Find(string locator)
        {
            Asked.Add(locator);
            return locator == "Banner" ? Banner : locator == "Unknown" ? Unknown : null;
        }
    }
}
