using Lumel.Layout;
using Lumel.Text;
using VRageMath;
using Xunit;
using Lumel.Viewing;
using Lumel.Windows;
using Lumel.Sim;

namespace Lumel.Tests;

/// <summary>Containers and padding: room inside what holds others and inside the leaves that have a use for it, and clipping where it belongs.</summary>
public class ContainerTests
{
    const float Wide = 1440f;
    const float High = 1080f;

    /// <summary>A container's padding is room its children are placed inside.</summary>
    [Fact]
    public void AContainersPaddingIsRoomInsideIt()
    {
        var child = new Label { Placement = FixedSize.Size(100f, 40f) };
        var holder = new VStack { Base = { Padding = new Thickness(10f, 20f) }, Children = { child } };
        _ = new Easel(new VStack { Children = { holder } }, Wide, High);

        Assert.Equal(new RectangleF(10f, 20f, 100f, 40f), child.Bounds);
        Assert.Equal(new Vector2(120f, 80f), holder.DesiredSize);
    }

    /// <summary>
    ///     A negative side of padding reaches past the edge: equal and opposite sides move what is held by that much at
    ///     its full size, and the holder keeps the size of what it holds.
    /// </summary>
    [Fact]
    public void NegativePaddingMovesWhatIsHeld()
    {
        var child = new Label { Placement = FixedSize.Size(100f, 40f) };
        var holder = new VStack { Base = { Padding = new Thickness(0f, 12f, 0f, -12f) }, Children = { child } };
        _ = new Easel(new VStack { Children = { holder } }, Wide, High);

        Assert.Equal(new Vector2(100f, 40f), holder.DesiredSize);
        Assert.Equal(new RectangleF(0f, 12f, 100f, 40f), child.Bounds);
    }

    /// <summary>
    ///     A form asks for the width its rows need, however much room it is offered, and a stretching field still fills
    ///     the width its holder gives it.
    /// </summary>
    [Fact]
    public void AFormIsAsWideAsItsRows()
    {
        Label field;
        var form = new Form
        {
            Rows =
            {
                { new Label { Text = "Falloff" }, new Label { Text = "2", Placement = FixedSize.Width(80f) } },
                new Label { Text = "Wide", Align = Align.FillWidth }.As(out field)
            }
        };
        _ = new Easel(new VStack { Children = { new VStack { Placement = FixedSize.Width(600f), Children = { form } } } }, Wide, High);

        Assert.True(form.DesiredSize.X < 300f, $"the form asks for {form.DesiredSize.X}");
        Assert.Equal(600f, form.Bounds.Width, 1);
        Assert.Equal(600f, field.Bounds.Width, 1);
    }

    /// <summary>A style's padding reaches whatever it styles that has padding: a leaf and a container alike.</summary>
    [Fact]
    public void AStylesPaddingReachesLeavesAndContainers()
    {
        var style = new StackStyle { Base = { Padding = new Thickness(6f) } };
        var text = new LabelStyle { Base = { Padding = new Thickness(4f) } };

        var holder = new VStack { Style = style };
        var words = new Label { Style = text };
        holder.Restyle();
        words.Restyle();

        Assert.Equal(new Thickness(6f), holder.Padding);
        Assert.Equal(new Thickness(4f), words.Padding);
    }

    /// <summary>A container does not clip: what it holds may draw outside it.</summary>
    [Fact]
    public void AContainerDoesNotClip()
    {
        var holder = new VStack { Children = { new Label { Text = "Inside" } } };
        var easel = new Easel(new VStack { Children = { holder } }, Wide, High);

        Assert.DoesNotContain(easel.Commands, command => command.Kind == DrawKind.SetClip);
    }

    /// <summary>A content control holds one piece of content: given another, it lets go of the first.</summary>
    [Fact]
    public void AContentControlHoldsOneThing()
    {
        var first = new Label { Text = "First" };
        var second = new Label { Text = "Second" };
        var reveal = new Reveal { Content = first, IsOpen = true };

        reveal.Content = second;

        Assert.Null(first.Parent);
        Assert.Same(reveal, second.Parent);
        Assert.Same(second, reveal.Content);
    }

    /// <summary>A content control's content fills the room inside its padding, unless the kind of control says otherwise.</summary>
    [Fact]
    public void ContentFillsTheRoomInsidePadding()
    {
        var content = new Label { Text = "Held" };
        var window = new Window
        {
            Base = { Padding = new Thickness(8f) },
            Content = content,
            Placement = FixedSize.Size(200f, 100f)
        };
        _ = new Easel(new VStack { Children = { window } }, Wide, High);

        Assert.Equal(new RectangleF(8f, 8f, 184f, 84f), content.Bounds);
    }

    /// <summary>What must clip to work does so on its own, with nothing to switch.</summary>
    [Fact]
    public void WhatMustClipClipsOnItsOwn()
    {
        var scroll = new ScrollView { Placement = FixedSize.Size(200f, 100f), Content = new Label { Text = "Held" } };
        var easel = new Easel(new VStack { Children = { scroll } }, Wide, High);
        Assert.Contains(easel.Commands, command => command.Kind == DrawKind.SetClip);

        var box = new TextBox { Placement = FixedSize.Size(200f, 40f), Text = "Typed" };
        easel = new Easel(new VStack { Children = { box } }, Wide, High);
        Assert.Contains(easel.Commands, command => command.Kind == DrawKind.SetClip);
    }

    /// <summary>A scroll view holding content of a fixed height scrolls through all of it.</summary>
    [Fact]
    public void AScrollViewScrollsThroughContentOfAFixedHeight()
    {
        var content = new VStack { Placement = FixedSize.Height(1000f) };
        var view = new ScrollView { Placement = FixedSize.Size(200f, 100f), Content = content };
        _ = new Easel(new VStack { Children = { view } }, Wide, High);

        Assert.True(view.CanScroll, "the view found nothing to scroll");
        Assert.Equal(1000f, content.Bounds.Height);
    }

    /// <summary>A content control gives what it holds the size its placement fixes.</summary>
    [Fact]
    public void AContentControlKeepsItsContentsFixedSize()
    {
        var content = new VStack { Placement = FixedSize.Size(48f, 20f) };
        var window = new Window { Content = content };
        var layer = new WindowLayer();
        layer.Open(window);

        layer.Draw(new Counting(), new RectangleF(0f, 0f, Wide, High));

        Assert.Equal(48f, content.Bounds.Width);
        Assert.Equal(20f, content.Bounds.Height);
    }

    /// <summary>A content control refuses content placed in a way it cannot honor.</summary>
    [Fact]
    public void AContentControlRefusesAPlacementItCannotHonor()
    {
        var window = new Window();

        Assert.Throws<InvalidOperationException>(() => window.Content = new VStack { Placement = new Elsewhere() });
    }

    /// <summary>A placement no content control understands.</summary>
    sealed class Elsewhere : IPlacement
    {
        public event Action Changed
        {
            add { }
            remove { }
        }
    }
}
