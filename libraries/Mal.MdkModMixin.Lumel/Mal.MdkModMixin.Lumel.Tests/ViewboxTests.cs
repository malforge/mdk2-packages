using Lumel.Controls;
using Lumel.Input;
using Lumel.Layout;
using Lumel.Text;
using Lumel.Viewing;
using VRageMath;
using Xunit;

namespace Lumel.Tests;

/// <summary>A viewbox: its content scaled to fill the room as nearly as it can without spilling out, text flowing anew at each scale.</summary>
public class ViewboxTests
{
    const float Wide = 400f;
    const float High = 400f;

    static readonly string Long = string.Join("\n\n", Enumerable.Repeat("The airlock cycles when the inner door shuts.", 12));

    /// <summary>A document too tall for its room shrinks until it fits, close to the room's height.</summary>
    [Fact]
    public void ATallDocumentShrinksToItsRoom()
    {
        var box = Boxed(Long, 120f, _ => { });

        Assert.True(box.Scale < 1f, $"scaled by {box.Scale}");
        Assert.True(box.DesiredSize.Y <= 120f + 0.01f, $"{box.DesiredSize.Y} is taller than the room");
        Assert.True(box.DesiredSize.Y > 120f * 0.9f, $"{box.DesiredSize.Y} shrank further than it had to");
    }

    /// <summary>A short document grows until it meets the room on one side.</summary>
    [Fact]
    public void AShortDocumentGrowsToFillItsRoom()
    {
        var box = Boxed("Airlock", 300f, _ => { });

        Assert.True(box.Scale > 1f, $"scaled by {box.Scale}");
        Assert.True(box.DesiredSize.X > Wide * 0.9f || box.DesiredSize.Y > 300f * 0.9f, $"{box.DesiredSize} meets neither side");
        Assert.True(box.DesiredSize.X <= Wide + 0.01f && box.DesiredSize.Y <= 300f + 0.01f, $"{box.DesiredSize} spills out");
    }

    /// <summary>Text keeps the lines it was written with: a paragraph wider than the room is one line scaled to its width, never wrapped.</summary>
    [Fact]
    public void AParagraphKeepsItsLine()
    {
        var text = new RichText { Document = Markdown.Parse("The airlock cycles when the inner door shuts and the outer door is sealed.\nNext"), Base = { FontSize = 20f } };
        var box = new Viewbox { Content = text, Placement = FixedSize.Size(Wide, 300f) };
        var easel = new Easel(new VStack { Children = { box } }, Wide, High);

        var first = easel.Written.Where(w => w.Text?.Trim().Length > 0 && w.Text.Trim() != "Next").Select(w => w.Position.Y).Distinct();
        Assert.Single(first);
        Assert.True(box.Scale < 1f, $"scaled by {box.Scale}");
        Assert.Equal(Wide, box.DesiredSize.X, 1);
    }

    /// <summary>A table with a starred column, given all the room it could want, is as wide as its cells rather than taking all of it, so it grows to the room like anything else.</summary>
    [Fact]
    public void AStarredTableGrowsLikeAnythingElse()
    {
        var box = Boxed("| Tank | Fill |\n|:--*|--:|\n| Oxygen | 80% |", 300f, _ => { });

        Assert.True(box.Scale > 1f, $"scaled by {box.Scale}");
        Assert.Equal(Wide, box.DesiredSize.X, 1);
    }

    /// <summary>A centered part stays centered in the room, padding and all, where the room's height is what limits the scale.</summary>
    [Fact]
    public void ACenteredPartStaysCentered()
    {
        var text = new RichText { Document = Markdown.Parse("::: center\nAirlock\n:::\n\nThe outer door"), Base = { FontSize = 20f } };
        var box = new Viewbox { Content = text, Base = { Padding = new Thickness(25f) }, Placement = FixedSize.Size(Wide, 120f) };
        var easel = new Easel(new VStack { Children = { box } }, Wide, High);

        var word = Assert.Single(easel.Written, w => w.Text?.Trim() == "Airlock");
        var drawnWide = easel.WidthOf("Airlock") * 20f / easel.LineHeight * box.Scale;
        var middle = (box.ContentBounds.X + box.ContentBounds.Width * 0.5f) * easel.Pixels;

        var widest = easel.WidthOf("The outer door") * 20f / easel.LineHeight * box.Scale;
        Assert.True(widest < box.ContentBounds.Width * easel.Pixels - 20f, "the height did not limit the scale");
        Assert.Equal(middle, word.Position.X + drawnWide * 0.5f, 2f);
    }

    /// <summary>A viewbox that only shrinks leaves a document that fits at its own size.</summary>
    [Fact]
    public void OneThatOnlyShrinksKeepsAShortDocumentsSize()
    {
        var box = Boxed("Airlock", 300f, b => b.StretchDirection = StretchDirection.DownOnly);

        Assert.Equal(1f, box.Scale);
        Assert.Equal(20f, box.DesiredSize.Y);
    }

    /// <summary>A viewbox that only grows leaves a document too tall for it at its own size.</summary>
    [Fact]
    public void OneThatOnlyGrowsKeepsATallDocumentsSize()
    {
        var box = Boxed(Long, 120f, b => b.StretchDirection = StretchDirection.UpOnly);

        Assert.Equal(1f, box.Scale);
        Assert.True(box.DesiredSize.Y > 120f, "the document shrank");
    }

    /// <summary>A viewbox that stretches neither way leaves a document at its own size, short or tall.</summary>
    [Fact]
    public void OneThatStretchesNeitherWayKeepsEveryDocumentsSize()
    {
        var shortOne = Boxed("Airlock", 300f, b => b.StretchDirection = StretchDirection.None);
        var tallOne = Boxed(Long, 120f, b => b.StretchDirection = StretchDirection.None);

        Assert.Equal(1f, shortOne.Scale);
        Assert.Equal(20f, shortOne.DesiredSize.Y);
        Assert.Equal(1f, tallOne.Scale);
        Assert.True(tallOne.DesiredSize.Y > 120f, "the document shrank");
    }

    /// <summary>Content whose size does not change with its width is scaled exactly to meet the nearer side.</summary>
    [Fact]
    public void FixedContentMeetsTheNearerSide()
    {
        var label = new Label { Text = "Airlock", Base = { FontSize = 20f } };
        var box = new Viewbox { Content = label, Placement = FixedSize.Size(Wide, 300f) };
        _ = new Easel(new VStack { Children = { box } }, Wide, High);

        Assert.Equal(Wide, box.DesiredSize.X, 2);
        Assert.Equal(20f * box.Scale, box.DesiredSize.Y, 2);
    }

    /// <summary>A point drawn over a link reaches the text and finds the link, however much the viewbox has scaled it.</summary>
    [Fact]
    public void APointReachesTheLinkDrawnUnderIt()
    {
        var text = new RichText { Document = Markdown.Parse("[codex](codex) and more"), Base = { FontSize = 20f } };
        text.LinkClicked += _ => { };
        var box = new Viewbox { Content = text, Placement = FixedSize.Size(Wide, 300f) };
        _ = new Easel(new VStack { Children = { box } }, Wide, High);

        var origin = new Vector2(box.ContentBounds.X, box.ContentBounds.Y);
        var laid = new Vector2(text.ContentBounds.X + 4f, text.ContentBounds.Y + 10f);
        var drawn = (laid - origin) * box.Scale + origin;

        Assert.True(box.Scale > 1f, $"scaled by {box.Scale}");
        Assert.Same(text, box.HitTest(drawn));
        Assert.Equal("codex", text.LinkAt(text.Local(drawn))?.Target);
    }

    /// <summary>The scaled text is drawn across the room, its words grown with it.</summary>
    [Fact]
    public void TheScaledTextIsDrawnAcrossTheRoom()
    {
        var text = new RichText { Document = Markdown.Parse("Airlock"), Base = { FontSize = 20f } };
        var box = new Viewbox { Content = text, Placement = FixedSize.Size(Wide, 300f) };
        var easel = new Easel(new VStack { Children = { box } }, Wide, High);

        var word = Assert.Single(easel.Written, command => command.Text?.Trim() == "Airlock");
        Assert.True(box.Scale > 1f, $"scaled by {box.Scale}");
        var drawnWide = easel.WidthOf("Airlock") * 20f / easel.LineHeight * box.Scale;
        Assert.Equal(Wide * easel.Pixels, drawnWide, Wide * easel.Pixels * 0.02f);
        Assert.Equal(box.ContentBounds.X * easel.Pixels, word.Position.X, 1f);
    }

    /// <summary>Fit scales a page of fixed size until it meets the room on one axis, keeps the page's size, and centers it in the room left over.</summary>
    [Fact]
    public void FitCentersAPageOfItsOwnSize()
    {
        var page = Page();
        var box = new Viewbox { Content = page, Stretch = Stretch.Fit, Placement = FixedSize.Size(Wide, 200f) };
        _ = new Easel(new VStack { Children = { box } }, Wide, High);

        Assert.Equal(2f, box.Scale, 3);
        Assert.Equal(new Vector2(200f, 200f), box.DesiredSize);
        Assert.Equal(100f, page.Bounds.Width);
        Assert.Equal(100f, page.Bounds.Height);
        Assert.Equal(100f, Drawn(box, page).X, 2);
        Assert.Equal(0f, Drawn(box, page).Y, 2);
    }

    /// <summary>Fit places the page by its own alignment where it gives one.</summary>
    [Fact]
    public void FitPlacesThePageByItsAlignment()
    {
        var page = Page();
        page.Align = Align.Right;
        var box = new Viewbox { Content = page, Stretch = Stretch.Fit, Placement = FixedSize.Size(Wide, 200f) };
        _ = new Easel(new VStack { Children = { box } }, Wide, High);

        Assert.Equal(200f, Drawn(box, page).X, 2);
    }

    /// <summary>Fill scales the page until it covers the room, keeps the page's size, and clips what overflows.</summary>
    [Fact]
    public void FillCoversTheRoomAndClipsTheRest()
    {
        var button = new Button { Text = "Vent", Command = new RelayCommand(() => { }), Placement = FixedSize.Size(100f, 100f) };
        var box = new Viewbox { Content = button, Stretch = Stretch.Fill, Placement = FixedSize.Size(Wide, 200f) };
        _ = new Easel(new VStack { Children = { box } }, Wide, High);

        Assert.Equal(4f, box.Scale, 3);
        Assert.Equal(new Vector2(Wide, 200f), box.DesiredSize);
        Assert.Equal(100f, button.Bounds.Width);
        Assert.Equal(-100f, Drawn(box, button).Y, 2);
        Assert.Same(button, box.HitTest(new Vector2(200f, 100f)));
        Assert.Null(box.HitTest(new Vector2(200f, 250f)));
    }

    /// <summary>Fit and expand lays the page out again across the whole room at its scale, on both axes.</summary>
    [Fact]
    public void FitAndExpandGrowsIntoTheRoomBothWays()
    {
        var page = new VStack { Children = { new Label { Text = "Airlock", Base = { FontSize = 20f } } } };
        var box = new Viewbox { Content = page, Placement = FixedSize.Size(Wide, 300f) };
        _ = new Easel(new VStack { Children = { box } }, Wide, High);

        Assert.Equal(Stretch.FitAndExpand, box.Stretch);
        Assert.True(box.Scale > 1f, $"scaled by {box.Scale}");
        Assert.Equal(Wide, page.Bounds.Width * box.Scale, 1);
        Assert.Equal(300f, page.Bounds.Height * box.Scale, 1);
    }

    /// <summary>Fill and contract scales as fill does, then lays the page out again in the room that scale leaves, so nothing overflows.</summary>
    [Fact]
    public void FillAndContractShrinksThePageToTheRoom()
    {
        var page = new VStack { Children = { new Label { Text = "Airlock", Base = { FontSize = 20f } } } };
        var box = new Viewbox { Content = page, Stretch = Stretch.FillAndContract, Placement = FixedSize.Size(Wide, 300f) };
        _ = new Easel(new VStack { Children = { box } }, Wide, High);

        Assert.Equal(15f, box.Scale, 3);
        Assert.Equal(Wide, page.Bounds.Width * box.Scale, 1);
        Assert.Equal(300f, page.Bounds.Height * box.Scale, 1);
    }

    /// <summary>Fill and contract takes the scale from a page's fixed size, then lays the page out in the room that scale leaves.</summary>
    [Fact]
    public void FillAndContractLaysAFixedPageInTheRoom()
    {
        var page = Page();
        var box = new Viewbox { Content = page, Stretch = Stretch.FillAndContract, Placement = FixedSize.Size(Wide, 200f) };
        _ = new Easel(new VStack { Children = { box } }, Wide, High);

        Assert.Equal(4f, box.Scale, 3);
        Assert.Equal(Wide, page.Bounds.Width * box.Scale, 1);
        Assert.Equal(200f, page.Bounds.Height * box.Scale, 1);
    }

    /// <summary>Fill and contract clips what still overflows the room once the content is laid out in it.</summary>
    [Fact]
    public void FillAndContractClipsWhatStillOverflows()
    {
        var button = new Button { Text = "Vent", Command = new RelayCommand(() => { }), Placement = FixedSize.Size(100f, 400f) };
        var box = new Viewbox { Content = new VStack { Children = { button } }, Stretch = Stretch.FillAndContract, Placement = FixedSize.Size(Wide, 200f) };
        _ = new Easel(new VStack { Children = { box } }, Wide, High);

        Assert.Equal(4f, box.Scale, 3);
        Assert.Same(button, box.HitTest(new Vector2(50f, 100f)));
        Assert.Null(box.HitTest(new Vector2(50f, 300f)));
    }

    /// <summary>A fit that only shrinks leaves a small page at its own size, centered.</summary>
    [Fact]
    public void AFitThatOnlyShrinksCentersASmallPage()
    {
        var page = Page();
        var box = new Viewbox { Content = page, Stretch = Stretch.Fit, StretchDirection = StretchDirection.DownOnly, Placement = FixedSize.Size(Wide, 200f) };
        _ = new Easel(new VStack { Children = { box } }, Wide, High);

        Assert.Equal(1f, box.Scale);
        Assert.Equal(150f, Drawn(box, page).X, 2);
        Assert.Equal(50f, Drawn(box, page).Y, 2);
    }

    /// <summary>A page 100 square by its placement, holding a word smaller than itself.</summary>
    static VStack Page()
    {
        return new VStack
        {
            Placement = FixedSize.Size(100f, 100f),
            Children = { new Label { Text = "Airlock", Base = { FontSize = 20f } } }
        };
    }

    /// <summary>Returns where the corner of <paramref name="content" /> is drawn, scaled by <paramref name="box" />, from the box's corner.</summary>
    static Vector2 Drawn(Viewbox box, Element content)
    {
        var origin = new Vector2(box.ContentBounds.X, box.ContentBounds.Y);
        return (new Vector2(content.Bounds.X, content.Bounds.Y) - origin) * box.Scale;
    }

    static Viewbox Boxed(string markdown, float high, Action<Viewbox> set)
    {
        var text = new RichText { Document = Markdown.Parse(markdown), Base = { FontSize = 20f, BlockSpacing = 8f } };
        var box = new Viewbox { Content = text, Placement = FixedSize.Size(Wide, high) };
        set(box);

        _ = new Easel(new VStack { Children = { box } }, Wide, High);
        return box;
    }
}
