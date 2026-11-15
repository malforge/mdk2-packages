using Lumel.Layout;
using Lumel.Text;
using VRageMath;
using Xunit;

namespace Lumel.Tests;

/// <summary>How a flow lays its children along lines, and that it draws what the wrap it replaces drew.</summary>
public class FlowTests
{
    /// <summary>Children that fit share a line, spaced apart, and the next one that does not starts another below it.</summary>
    [Fact]
    public void AChildThatDoesNotFitStartsTheNextLine()
    {
        var first = Box(100f, 20f);
        var second = Box(100f, 20f);
        var third = Box(100f, 20f);
        var flow = new Flow { Base = { Spacing = 10f, LineSpacing = 5f }, Children = { first, second, third } };

        _ = new Easel(new VStack { Children = { flow } }, 250f, 200f);

        Assert.Equal(new RectangleF(0f, 0f, 100f, 20f), first.Bounds);
        Assert.Equal(new RectangleF(110f, 0f, 100f, 20f), second.Bounds);
        Assert.Equal(new RectangleF(0f, 25f, 100f, 20f), third.Bounds);
        Assert.Equal(new Vector2(210f, 45f), flow.DesiredSize);
    }

    /// <summary>Every line after the first starts in by the flow's indent, and has that much less room.</summary>
    [Fact]
    public void LinesAfterTheFirstAreIndented()
    {
        var first = Box(100f, 20f);
        var second = Box(100f, 20f);
        var third = Box(100f, 20f);
        var flow = new Flow { Base = { Spacing = 10f, LineSpacing = 5f, Indent = 40f }, Children = { first, second, third } };

        _ = new Easel(new VStack { Children = { flow } }, 250f, 200f);

        Assert.Equal(new RectangleF(0f, 0f, 100f, 20f), first.Bounds);
        Assert.Equal(new RectangleF(110f, 0f, 100f, 20f), second.Bounds);
        Assert.Equal(new RectangleF(40f, 25f, 100f, 20f), third.Bounds);
    }

    /// <summary>A child that fits the first line's room but not an indented line's starts a line of its own.</summary>
    [Fact]
    public void AnIndentedLineHasLessRoom()
    {
        var first = Box(100f, 20f);
        var second = Box(200f, 20f);
        var third = Box(100f, 20f);
        var flow = new Flow { Base = { Spacing = 10f, LineSpacing = 5f, Indent = 40f }, Children = { first, second, third } };

        _ = new Easel(new VStack { Children = { flow } }, 250f, 200f);

        Assert.Equal(new RectangleF(40f, 25f, 200f, 20f), second.Bounds);
        Assert.Equal(new RectangleF(40f, 50f, 100f, 20f), third.Bounds);
    }

    /// <summary>A child shorter than its line sits on the line's middle.</summary>
    [Fact]
    public void AShorterChildIsCenteredDownItsLine()
    {
        var tall = Box(50f, 40f);
        var low = Box(50f, 20f);
        var flow = new Flow { Children = { tall, low } };

        _ = new Easel(new VStack { Children = { flow } }, 250f, 200f);

        Assert.Equal(10f, low.Bounds.Y);
        Assert.Equal(40f, flow.DesiredSize.Y);
    }

    /// <summary>A child that is not visible takes no room and no spacing.</summary>
    [Fact]
    public void AnInvisibleChildTakesNoRoom()
    {
        var first = Box(50f, 20f);
        var hidden = Box(50f, 20f);
        hidden.IsVisible = false;
        var last = Box(50f, 20f);
        var flow = new Flow { Base = { Spacing = 10f }, Children = { first, hidden, last } };

        _ = new Easel(new VStack { Children = { flow } }, 250f, 200f);

        Assert.Equal(60f, last.Bounds.X);
        Assert.Equal(110f, flow.DesiredSize.X);
    }

    /// <summary>A child wider than the room has a line to itself at its own width, and the others carry on after it.</summary>
    [Fact]
    public void AChildWiderThanTheRoomHasALineToItself()
    {
        var wide = Box(300f, 20f);
        var next = Box(50f, 20f);
        var flow = new Flow { Children = { wide, next } };

        _ = new Easel(new VStack { Children = { flow } }, 250f, 200f);

        Assert.Equal(new RectangleF(0f, 0f, 300f, 20f), wide.Bounds);
        Assert.Equal(new RectangleF(0f, 20f, 50f, 20f), next.Bounds);
    }

    /// <summary>A placement a flow cannot honor is refused when the child is added.</summary>
    [Fact]
    public void APlacementItCannotHonorIsRefused()
    {
        var flow = new Flow();

        Assert.Throws<InvalidOperationException>(() => flow.Children.Add(new Label { Placement = new Elsewhere() }));
    }

    static Label Box(float width, float height)
    {
        return new Label { Placement = FixedSize.Size(width, height) };
    }

    /// <summary>A placement no flow understands.</summary>
    sealed class Elsewhere : IPlacement
    {
        public event Action? Changed
        {
            add { }
            remove { }
        }
    }
}
