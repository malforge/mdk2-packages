using Lumel.Controls;
using Lumel.Layout;
using Lumel.Text;
using Lumel.Viewing;
using Xunit;

namespace Lumel.Tests;

/// <summary>How HStack and VStack lay out their children.</summary>
public class StackTests
{
    const float Wide = 400f;
    const float High = 200f;

    /// <summary>A child is offered the room left after the children before it and the space between them.</summary>
    [Fact]
    public void AChildIsOfferedTheRoomLeftPastTheSpacing()
    {
        ScrollView rest;
        var stack = new VStack
        {
            Base = { Spacing = 8f },
            Children =
            {
                new Label { Placement = FixedSize.Height(40f) },
                new ScrollView { FitsContent = true, Content = new VStack { Placement = FixedSize.Height(1000f) } }.As(out rest)
            }
        };

        _ = new Easel(stack, Wide, High);

        Assert.Equal(High - 48f, rest.DesiredSize.Y, 3);
    }

    /// <summary>Children that grow share the room left by weight.</summary>
    [Fact]
    public void GrowingChildrenShareTheRoomLeftByWeight()
    {
        Label one, three;
        var stack = new HStack
        {
            Children =
            {
                new Label { Placement = FixedSize.Width(40f) },
                new Label { Placement = Flex.By(1f) }.As(out one),
                new Label { Placement = Flex.By(3f) }.As(out three)
            }
        };

        _ = new Easel(stack, Wide, High);

        Assert.Equal(90f, one.Bounds.Width, 3);
        Assert.Equal(270f, three.Bounds.Width, 3);
        Assert.Equal(130f, three.Bounds.X, 3);
    }

    /// <summary>
    ///     A stack with a growing child asks for what its children need, not all the room it is offered, and a holder
    ///     that stretches it still gives the growing child the rest.
    /// </summary>
    [Fact]
    public void AGrowingChildMakesItsStackAskForNoMoreThanItNeeds()
    {
        Label grows;
        var row = new HStack
        {
            Base = { Spacing = 8f },
            Children =
            {
                new Label { Placement = FixedSize.Width(40f) },
                new Label { Placement = FixedSize.Width(60f) }.As(out grows)
            }
        };
        grows.Placement = Flex.Grow;
        var sized = new HStack { Children = { row } };
        var stretched = new HStack { Children = { new Label { Placement = FixedSize.Width(40f) }, new Label { Placement = Flex.Grow } } };

        _ = new Easel(new VStack { Children = { sized, stretched } }, Wide, High);

        Assert.True(row.DesiredSize.X < Wide, $"the row asks for {row.DesiredSize.X}");
        Assert.Equal(Wide, stretched.Bounds.Width, 3);
        Assert.Equal(Wide - 40f, stretched.Children[1].Bounds.Width, 3);
    }

    /// <summary>A growing slider asks for its own short length, and a row stretched wider gives it the rest.</summary>
    [Fact]
    public void AGrowingSliderAsksForItsOwnLengthAndTakesTheRest()
    {
        Slider slider;
        var row = new HStack { Children = { new Label { Placement = FixedSize.Width(40f) }, new Slider { Placement = Flex.Grow }.As(out slider) } };
        var sized = new HStack { Children = { new HStack { Children = { new Slider { Placement = Flex.Grow } } } } };

        _ = new Easel(new VStack { Children = { row, sized } }, Wide, High);

        Assert.True(sized.DesiredSize.X < 100f, $"a row holding only a slider asks for {sized.DesiredSize.X}");
        Assert.Equal(Wide - 40f, slider.Bounds.Width, 3);
    }

    /// <summary>A child taking only spare room asks for none, so its stack keeps its children's size, and pushes what follows to the far end of a wider one.</summary>
    [Fact]
    public void ASpareChildTakesOnlyTheRoomLeftWhenArranged()
    {
        HStack row;
        Label last;
        var column = new VStack
        {
            Children =
            {
                new HStack { Placement = FixedSize.Width(300f) },
                new HStack
                {
                    Children =
                    {
                        new Label { Placement = FixedSize.Width(40f) },
                        new HStack { Placement = Flex.Spacer },
                        new Label { Placement = FixedSize.Width(60f) }.As(out last)
                    }
                }.As(out row)
            }
        };

        _ = new Easel(new HStack { Children = { column } }, Wide, High);

        Assert.Equal(100f, row.DesiredSize.X, 3);
        Assert.Equal(300f, column.DesiredSize.X, 3);
        Assert.Equal(240f, last.Bounds.X - column.Bounds.X, 3);
    }

    /// <summary>A fixed width holds a child to it, whatever the child asks for.</summary>
    [Fact]
    public void AFixedWidthHoldsAChildToIt()
    {
        Label held;
        var stack = new HStack
        {
            Children = { new Label { Text = "Far too long for the room", Placement = FixedSize.Width(30f) }.As(out held) }
        };

        _ = new Easel(stack, Wide, High);

        Assert.Equal(30f, held.Bounds.Width, 3);
    }

    /// <summary>Changing a child's placement lays the stack out again.</summary>
    [Fact]
    public void ChangingAPlacementLaysTheStackOutAgain()
    {
        Label moved;
        var stack = new HStack { Children = { new Label { Text = "Name" }.As(out moved) } };
        var easel = new Easel(stack, Wide, High);

        moved.Placement = FixedSize.Width(50f);
        easel.Draw(Wide, High);

        Assert.Equal(50f, moved.Bounds.Width, 3);
    }

    /// <summary>A stack refuses a child placed in a way it cannot honor, as the child joins.</summary>
    [Fact]
    public void AStackRefusesAPlacementItCannotHonor()
    {
        var stack = new VStack();

        Assert.Throws<InvalidOperationException>(() => stack.Children.Add(new Label { Placement = new Elsewhere() }));
        Assert.Equal(0, stack.Children.Count);
    }

    /// <summary>A stack refuses a placement it cannot honor when a child it holds changes to it, and keeps the old one.</summary>
    [Fact]
    public void AStackRefusesAChangeToAPlacementItCannotHonor()
    {
        Label child;
        _ = new VStack { Children = { new Label { Placement = Flex.Grow }.As(out child) } };

        Assert.Throws<InvalidOperationException>(() => child.Placement = new Elsewhere());
        Assert.Same(Flex.Grow, child.Placement);
    }

    /// <summary>A stack measures no text on a frame where nothing changed.</summary>
    [Fact]
    public void AnIdleStackMeasuresNoText()
    {
        var stack = new HStack
        {
            Children =
            {
                new Label { Text = "Left" },
                new Label { Text = "Middle", Placement = Flex.Grow },
                new Label { Text = "Right" }
            }
        };

        var easel = new Easel(stack, Wide, High);
        var settled = easel.Measured;
        easel.Draw(Wide, High);
        easel.Draw(Wide, High);

        Assert.Equal(settled, easel.Measured);
    }

    /// <summary>A placement no stack understands.</summary>
    sealed class Elsewhere : IPlacement
    {
        public event Action? Changed
        {
            add { }
            remove { }
        }
    }
}
