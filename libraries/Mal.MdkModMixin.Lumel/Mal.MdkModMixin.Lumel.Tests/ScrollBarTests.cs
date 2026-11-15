using Lumel.Input;
using Lumel.Layout;
using VRageMath;
using Xunit;
using Lumel.Viewing;

namespace Lumel.Tests;

/// <summary>A scroll: as much of its content as fits, moved by the wheel and by a thumb on a bar that takes the pointer itself.</summary>
public class ScrollBarTests
{
    const float Wide = 400f;
    const float High = 200f;
    const float Bar = 12f;

    static readonly Color Thumb = new Color(95, 114, 120);
    static readonly Color Held = new Color(133, 161, 169);

    static readonly ScrollViewStyle Barred = new ScrollViewStyle
    {
        Base = { ScrollBarWidth = Bar, Thumb = Thumb, MinThumbLength = 32f },
        Dragging = { Thumb = Held }
    };

    /// <summary>Scrolling moves the content up, and never past either end of it.</summary>
    [Fact]
    public void ScrollingStaysInsideTheContent()
    {
        var (scroll, content, easel, _) = Laid(1000f);

        scroll.ScrollBy(300f);
        easel.Draw(Wide, High);
        Assert.Equal(-300f, content.Bounds.Y);

        scroll.ScrollBy(5000f);
        easel.Draw(Wide, High);
        Assert.Equal(High - 1000f, content.Bounds.Y);

        scroll.ScrollBy(-5000f);
        easel.Draw(Wide, High);
        Assert.Equal(0f, content.Bounds.Y);
    }

    /// <summary>Scrolling to an offset moves the content there, held inside it however far it is asked to go.</summary>
    [Fact]
    public void ScrollingToAnOffsetStaysInsideTheContent()
    {
        var (scroll, content, easel, _) = Laid(1000f);

        scroll.ScrollToVerticalOffset(300f);
        Assert.Equal(300f, scroll.VerticalOffset);
        easel.Draw(Wide, High);
        Assert.Equal(-300f, content.Bounds.Y);

        scroll.ScrollToVerticalOffset(5000f);
        Assert.Equal(1000f - High, scroll.VerticalOffset);

        scroll.ScrollToVerticalOffset(-5000f);
        Assert.Equal(0f, scroll.VerticalOffset);
    }

    /// <summary>The content is as wide as the room the bar leaves it.</summary>
    [Fact]
    public void TheBarTakesItsWidthFromTheContent()
    {
        var (_, content, _, _) = Laid(1000f);

        Assert.Equal(Wide - Bar, content.Bounds.Width);
    }

    /// <summary>The gap beside the bar is taken from the content too, and the bar stays at the edge.</summary>
    [Fact]
    public void TheGapStandsBetweenTheContentAndTheBar()
    {
        var content = new VStack { Children = { new VStack { Placement = FixedSize.Height(1000f) } } };
        var scroll = new ScrollView
        {
            Style = new ScrollViewStyle(Barred) { Base = { ScrollBarGap = 8f } },
            Content = content,
            Placement = FixedSize.Size(Wide, High)
        };
        var easel = new Easel(new VStack { Children = { scroll } }, Wide, High);

        Assert.Equal(Wide - Bar - 8f, content.Bounds.Width);
        Assert.Equal((Wide - Bar) * easel.Pixels, easel.Sprites.Single(s => s.Color == Thumb).Destination.X, 1f);
    }

    /// <summary>Dragging the thumb moves the content in proportion, and the thumb takes its held color while it is held.</summary>
    [Fact]
    public void DraggingTheThumbMovesTheContent()
    {
        var (scroll, content, easel, input) = Laid(1000f);
        var top = new Vector2(Wide - Bar / 2f, 10f);

        input.PointerDown(scroll.HitTest(top), top, PointerButton.Left, 1, false, false);
        Assert.True(scroll.IsDragging);
        Assert.Equal(Held, scroll.Thumb);

        input.PointerMove(top + new Vector2(0f, 40f));
        easel.Draw(Wide, High);

        // The thumb is 40 of the 200 and travels 160 while the content travels 800: five to one.
        Assert.Equal(-200f, content.Bounds.Y, 1);

        input.PointerUp(top + new Vector2(0f, 40f), PointerButton.Left);
        Assert.False(scroll.IsDragging);
        Assert.Equal(Thumb, scroll.Thumb);
    }

    /// <summary>A press on the track away from the thumb puts the thumb there first.</summary>
    [Fact]
    public void APressOnTheTrackPutsTheThumbThere()
    {
        var (scroll, content, easel, input) = Laid(1000f);
        var bottom = new Vector2(Wide - Bar / 2f, High - 1f);

        input.PointerDown(scroll.HitTest(bottom), bottom, PointerButton.Left, 1, false, false);
        input.PointerUp(bottom, PointerButton.Left);
        easel.Draw(Wide, High);

        Assert.Equal(High - 1000f, content.Bounds.Y);
    }

    /// <summary>Only the bar is the scroll's own to press; everywhere else a press reaches the content or nothing.</summary>
    [Fact]
    public void OnlyTheBarIsPressed()
    {
        var (scroll, _, _, _) = Laid(1000f);

        Assert.Same(scroll, scroll.HitTest(new Vector2(Wide - Bar / 2f, 100f)));
        Assert.Null(scroll.HitTest(new Vector2(100f, 100f)));
    }

    /// <summary>Content that fits draws no bar, and a press where the bar would be reaches nothing.</summary>
    [Fact]
    public void ContentThatFitsHasNoBar()
    {
        var (scroll, _, _, _) = Laid(100f);

        Assert.False(scroll.CanScroll);
        Assert.Null(scroll.HitTest(new Vector2(Wide - Bar / 2f, 50f)));
    }

    /// <summary>A scroll given no style shows its bar, in the neutral look, wherever it can move.</summary>
    [Fact]
    public void AnUnstyledScrollShowsItsBar()
    {
        var content = new VStack { Children = { new VStack { Placement = FixedSize.Height(1000f) } } };
        var scroll = new ScrollView { Content = content, Placement = FixedSize.Size(Wide, High) };
        var easel = new Easel(new VStack { Children = { scroll } }, Wide, High);

        var thumb = Assert.Single(easel.Sprites, s => s.Color == ScrollViewLook.NeutralThumb);
        Assert.Equal(ScrollViewLook.NeutralScrollBarWidth * easel.Pixels, thumb.Destination.Width, 1f);
    }

    /// <summary>An offset set in the same frame as new content is kept against that content, not cut to what the old content allowed.</summary>
    [Fact]
    public void AnOffsetSetWithNewContentIsKept()
    {
        var (scroll, _, easel, _) = Laid(50f);

        scroll.Content = new VStack { Children = { new VStack { Placement = FixedSize.Height(1000f) } } };
        scroll.ScrollToVerticalOffset(300f);
        easel.Draw(Wide, High);

        Assert.Equal(300f, scroll.VerticalOffset);
    }

    /// <summary>
    ///     A scroll view sized to its content gives the content its whole width until the content runs past it, and
    ///     only then keeps room for the bar.
    /// </summary>
    [Theory]
    [InlineData(100f, Wide)]
    [InlineData(1000f, Wide - Bar)]
    public void ASizedScrollKeepsBarRoomOnlyWhenItScrolls(float tall, float wide)
    {
        var content = new VStack { Children = { new VStack { Placement = FixedSize.Height(tall) } } };
        var scroll = new ScrollView { Style = Barred, FitsContent = true, Content = content, Placement = FixedSize.Width(Wide) };
        _ = new Easel(new VStack { Children = { scroll } }, Wide, High);

        Assert.Equal(wide, content.Bounds.Width);
    }

    static (ScrollView, Element, Easel, InputRoot) Laid(float tall)
    {
        var content = new VStack { Children = { new VStack { Placement = FixedSize.Height(tall) } } };
        var scroll = new ScrollView { Style = Barred, Content = content, Placement = FixedSize.Size(Wide, High) };
        var root = new VStack { Children = { scroll } };
        var easel = new Easel(root, Wide, High);
        return (scroll, content, easel, new InputRoot(root));
    }
}
