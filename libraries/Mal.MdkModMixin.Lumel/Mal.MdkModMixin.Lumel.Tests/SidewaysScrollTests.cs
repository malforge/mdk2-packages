using Lumel.Controls;
using Lumel.Input;
using Lumel.Layout;
using Lumel.Text;
using VRageMath;
using Xunit;
using Lumel.Viewing;

namespace Lumel.Tests;

/// <summary>A scroll view that scrolls sideways, alone or both ways, by its bar along the foot, by Shift and the wheel, and to bring something into view.</summary>
public class SidewaysScrollTests
{
    const float Wide = 400f;
    const float High = 200f;
    const float Bar = 12f;

    static readonly ScrollViewStyle Barred = new ScrollViewStyle { Base = { ScrollBarWidth = Bar, Thumb = Color.White, MinThumbLength = 32f } };

    /// <summary>
    ///     A strip scrolls sideways and never past either end of its content, which takes the height the bar along the
    ///     foot leaves it.
    /// </summary>
    [Fact]
    public void AStripScrollsSidewaysInsideItsContent()
    {
        var (strip, content, easel, _) = Strip(1000f);

        Assert.True(strip.CanScrollHorizontally);
        Assert.False(strip.CanScroll);
        Assert.Equal(High - Bar, content.Bounds.Height);

        strip.ScrollHorizontallyBy(300f);
        easel.Draw(Wide, High);
        Assert.Equal(-300f, content.Bounds.X);

        strip.ScrollHorizontallyBy(5000f);
        easel.Draw(Wide, High);
        Assert.Equal(Wide - 1000f, content.Bounds.X);
        Assert.Equal(1000f - Wide, strip.HorizontalOffset);

        strip.ScrollToHorizontalOffset(-5000f);
        Assert.Equal(0f, strip.HorizontalOffset);
    }

    /// <summary>A press on the bar along the foot, away from its thumb, puts the thumb there.</summary>
    [Fact]
    public void APressOnTheBarAlongTheFootPutsTheThumbThere()
    {
        var (strip, content, easel, input) = Strip(1000f);
        var end = new Vector2(Wide - 1f, High - Bar / 2f);

        Assert.Same(strip, strip.HitTest(end));
        input.PointerDown(strip.HitTest(end), end, PointerButton.Left, 1, false, false);
        input.PointerUp(end, PointerButton.Left);
        easel.Draw(Wide, High);

        Assert.Equal(Wide - 1000f, content.Bounds.X);
    }

    /// <summary>
    ///     Over a column that scrolls down inside a strip that scrolls across, the wheel scrolls the column and a turn
    ///     with Shift held the strip; a turn the strip cannot take goes on outwards.
    /// </summary>
    [Fact]
    public void TheWheelScrollsTheColumnAndASidewaysTurnTheStrip()
    {
        var column = new ScrollView
        {
            Style = Barred,
            Placement = FixedSize.Width(300f),
            Content = new VStack { Children = { new VStack { Placement = FixedSize.Height(1000f) } } }
        };
        var strip = new ScrollView
        {
            Style = Barred,
            ScrollsHorizontally = true,
            ScrollsVertically = false,
            Placement = FixedSize.Size(Wide, High),
            Content = new HStack { Children = { column, new VStack { Placement = FixedSize.Width(1000f) } } }
        };
        var root = new VStack { Children = { strip } };
        _ = new Easel(root, Wide, High);
        var input = new InputRoot(root);
        var inColumn = new Vector2(150f, 50f);
        var beside = new Vector2(350f, 50f);

        Assert.True(input.Wheel(root.WheelAt(inColumn), new Vector2(0f, 40f)));
        Assert.Equal(40f, column.VerticalOffset);
        Assert.Equal(0f, strip.HorizontalOffset);

        Assert.True(input.Wheel(root.WheelAt(inColumn), new Vector2(40f, 0f)));
        Assert.Equal(40f, column.VerticalOffset);
        Assert.Equal(40f, strip.HorizontalOffset);

        Assert.False(input.Wheel(root.WheelAt(beside), new Vector2(0f, 40f)));
        Assert.Equal(40f, strip.HorizontalOffset);
    }

    /// <summary>Bringing a column out of sight into view scrolls the strip only as far as shows all of it.</summary>
    [Fact]
    public void BringingAColumnIntoViewScrollsTheStripToIt()
    {
        var row = new HStack();
        for (var i = 0; i < 5; i++)
            row.Children.Add(new VStack { Placement = FixedSize.Width(300f) });

        var strip = new ScrollView { Style = Barred, ScrollsHorizontally = true, ScrollsVertically = false, Placement = FixedSize.Size(Wide, High), Content = row };
        var easel = new Easel(new VStack { Children = { strip } }, Wide, High);

        strip.BringIntoView(row.Children[2]);
        easel.Draw(Wide, High);

        Assert.Equal(900f - Wide, strip.HorizontalOffset);
        Assert.Equal(Wide, row.Children[2].Bounds.X + row.Children[2].Bounds.Width, 1);
    }

    /// <summary>
    ///     Scrolling both ways, each bar takes its room from the content the other way, and the corner where they would
    ///     meet belongs to neither.
    /// </summary>
    [Fact]
    public void ScrollingBothWaysKeepsTheBarsApart()
    {
        var content = new VStack { Placement = FixedSize.Size(1000f, 1000f) };
        var scroll = new ScrollView { Style = Barred, ScrollsHorizontally = true, Placement = FixedSize.Size(Wide, High), Content = content };
        _ = new Easel(new VStack { Children = { scroll } }, Wide, High);

        scroll.ScrollToVerticalOffset(5000f);
        scroll.ScrollToHorizontalOffset(5000f);

        Assert.Equal(1000f - (High - Bar), scroll.VerticalOffset);
        Assert.Equal(1000f - (Wide - Bar), scroll.HorizontalOffset);
        Assert.Same(scroll, scroll.HitTest(new Vector2(Wide - Bar / 2f, High - Bar - 1f)));
        Assert.Same(scroll, scroll.HitTest(new Vector2(Wide - Bar - 1f, High - Bar / 2f)));
        Assert.Null(scroll.HitTest(new Vector2(Wide - Bar / 2f, High - Bar / 2f)));
    }

    /// <summary>Content scrolled across reaches under the bars, and a press on a bar goes to the bar and not to what lies beneath.</summary>
    [Fact]
    public void APressOnABarOverContentGoesToTheBar()
    {
        var content = new Button { Text = "Under", Command = new RelayCommand(() => { }), Placement = FixedSize.Size(1000f, 1000f) };
        var scroll = new ScrollView { Style = Barred, ScrollsHorizontally = true, Placement = FixedSize.Size(Wide, High), Content = content };
        _ = new Easel(new VStack { Children = { scroll } }, Wide, High);

        Assert.Same(content, scroll.HitTest(new Vector2(100f, 100f)));
        Assert.Same(scroll, scroll.HitTest(new Vector2(Wide - Bar / 2f, 100f)));
        Assert.Same(scroll, scroll.HitTest(new Vector2(100f, High - Bar / 2f)));
    }

    /// <summary>A text area takes a sideways turn of the wheel along a line too wide for it.</summary>
    [Fact]
    public void ATextAreaTakesASidewaysTurn()
    {
        var listing = new TextArea { IsReadOnly = true, Text = new string('x', 400) };
        var root = new VStack { Children = { listing } };
        _ = new Easel(root, Wide, High);

        Assert.True(new InputRoot(root).Wheel(listing, new Vector2(40f, 0f)));
        Assert.Equal(40f, listing.HorizontalOffset);
    }

    static (ScrollView, Element, Easel, InputRoot) Strip(float wide)
    {
        var content = new HStack { Children = { new VStack { Placement = FixedSize.Width(wide) } } };
        var strip = new ScrollView { Style = Barred, ScrollsHorizontally = true, ScrollsVertically = false, Content = content, Placement = FixedSize.Size(Wide, High) };
        var root = new VStack { Children = { strip } };
        var easel = new Easel(root, Wide, High);
        return (strip, content, easel, new InputRoot(root));
    }
}
