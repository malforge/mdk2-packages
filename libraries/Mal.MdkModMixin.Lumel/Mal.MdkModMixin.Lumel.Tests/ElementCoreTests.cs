using Lumel.Input;
using Lumel.Layout;
using Lumel.Text;
using VRageMath;
using Xunit;

namespace Lumel.Tests;

/// <summary>What every element has: its children, its border, the keyboard leaving it, and the room round a display.</summary>
public class ElementCoreTests
{
    static readonly RectangleF Area = new RectangleF(0f, 0f, 400f, 300f);

    /// <summary>A null child is refused, leaving the children as they were.</summary>
    [Fact]
    public void ANullChildIsRefused()
    {
        var stack = new VStack();

        Assert.Throws<ArgumentNullException>(() => stack.Children.Add(null!));
        Assert.Equal(0, stack.Children.Count);
    }

    /// <summary>An element given itself as a child refuses it, leaving the children as they were.</summary>
    [Fact]
    public void AnElementCannotHoldItself()
    {
        var stack = new VStack();

        Assert.Throws<ArgumentException>(() => stack.Children.Add(stack));
        Assert.Equal(0, stack.Children.Count);
        Assert.Null(stack.Parent);
    }

    /// <summary>A children collection made for an element other than through its own CreateChildren is refused, leaving it with none.</summary>
    [Fact]
    public void AChildrenCollectionMadeFromOutsideIsRefused()
    {
        var label = new Label();

        Assert.Throws<InvalidOperationException>(() => new Grafted(label));
        Assert.Equal(0, label.VisualChildCount);
    }

    /// <summary>A collection of children made from outside the element it would belong to.</summary>
    sealed class Grafted(Element owner) : Element.Collection(owner);

    /// <summary>A border no look gives a thickness is one unit thick; a look's thickness is drawn as it says.</summary>
    [Fact]
    public void ABorderIsAsThickAsItsLookSays()
    {
        var thin = new HStack { Base = { Border = Color.Red }, Placement = FixedSize.Size(100f, 50f) };
        var thick = new HStack { Base = { Border = Color.Red, BorderThickness = 4f }, Placement = FixedSize.Size(100f, 50f) };

        Assert.Equal(1f, Outline(thin).Height);
        Assert.Equal(4f, Outline(thick).Height);
        Assert.Equal(4f, thick.BorderThickness);
    }

    /// <summary>A border of no thickness is not drawn.</summary>
    [Fact]
    public void ABorderOfNoThicknessIsNotDrawn()
    {
        var none = new HStack { Base = { Border = Color.Red, BorderThickness = 0f }, Placement = FixedSize.Size(100f, 50f) };
        var g = Drawn(none);

        Assert.Empty(g.Fills);
    }

    /// <summary>An element is told as the keyboard leaves it.</summary>
    [Fact]
    public void AnElementIsToldTheKeyboardHasLeft()
    {
        var box = new Leaving();
        var input = new InputRoot(new VStack { Children = { box } });

        input.Focus(box);
        Assert.Equal(0, box.Left);

        input.Focus(null);
        Assert.Equal(1, box.Left);
    }

    static Recording Drawn(Element element)
    {
        var g = new Recording(Area);
        element.Measure(g, new Vector2(Area.Width, Area.Height));
        element.Arrange(new RectangleF(0f, 0f, 100f, 50f));
        element.Draw(g);
        return g;
    }

    /// <summary>Returns the top line of the border of <paramref name="element" /> as drawn.</summary>
    static RectangleF Outline(Element element) => Drawn(element).Fills[0];

    sealed class Leaving : Label
    {
        public Leaving()
        {
            Focusable = true;
        }

        public int Left { get; private set; }

        protected override void OnLostFocus() => Left++;
    }
}
