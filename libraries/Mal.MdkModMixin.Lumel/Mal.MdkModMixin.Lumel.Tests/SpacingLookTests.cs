using Lumel.Controls;
using Lumel.Layout;
using Lumel.Styling;
using Lumel.Text;
using Lumel.Windows;
using VRageMath;
using Xunit;

namespace Lumel.Tests;

/// <summary>The room a container leaves between what it holds is a look: set by its style, by its own look, and neutral where neither says.</summary>
public class SpacingLookTests
{
    const float Wide = 400f;
    const float High = 400f;

    /// <summary>A stack's style sets the room between its children, and its own look wins over the style's.</summary>
    [Fact]
    public void AStacksSpacingIsItsLooks()
    {
        var spaced = new StackStyle { Base = { Spacing = 12f } };
        Label first, second, third, fourth;
        var styled = new VStack { Style = spaced, Children = { new Label { Text = "a" }.As(out first), new Label { Text = "b" }.As(out second) } };
        var own = new VStack { Style = spaced, Base = { Spacing = 20f }, Children = { new Label { Text = "c" }.As(out third), new Label { Text = "d" }.As(out fourth) } };
        _ = new Easel(new HStack { Children = { styled, own } }, Wide, High);

        Assert.Equal(12f, styled.Spacing);
        Assert.Equal(first.Bounds.Bottom + 12f, second.Bounds.Y, 3);
        Assert.Equal(20f, own.Spacing);
        Assert.Equal(third.Bounds.Bottom + 20f, fourth.Bounds.Y, 3);
    }

    /// <summary>A stack no look gives room to puts its children edge to edge.</summary>
    [Fact]
    public void AStackWithNoSpacingPutsItsChildrenEdgeToEdge()
    {
        Label first, second;
        var stack = new HStack { Children = { new Label { Text = "a" }.As(out first), new Label { Text = "b" }.As(out second) } };
        _ = new Easel(stack, Wide, High);

        Assert.Equal(0f, stack.Spacing);
        Assert.Equal(first.Bounds.Right, second.Bounds.X, 3);
    }

    /// <summary>Changing a stack's own spacing lays it out again with the new room.</summary>
    [Fact]
    public void ChangingASpacingLaysTheStackOutAgain()
    {
        Label first, second;
        var stack = new VStack { Children = { new Label { Text = "a" }.As(out first), new Label { Text = "b" }.As(out second) } };
        var easel = new Easel(stack, Wide, High);

        stack.Base.Spacing = 16f;
        easel.Draw(Wide, High);

        Assert.Equal(first.Bounds.Bottom + 16f, second.Bounds.Y, 3);
    }

    /// <summary>A form's spacings are its looks, at eight between columns and rows and four under a label where none is set.</summary>
    [Fact]
    public void AFormsSpacingsAreItsLooks()
    {
        var plain = new Form();
        var styled = new Form { Style = new FormStyle { Base = { ColumnSpacing = 16f, RowSpacing = 12f, LabelSpacing = 8f } } };
        plain.Restyle();
        styled.Restyle();

        Assert.Equal(8f, plain.ColumnSpacing);
        Assert.Equal(8f, plain.RowSpacing);
        Assert.Equal(4f, plain.LabelSpacing);
        Assert.Equal(16f, styled.ColumnSpacing);
        Assert.Equal(12f, styled.RowSpacing);
        Assert.Equal(8f, styled.LabelSpacing);
    }

    /// <summary>A form lays its rows out with the room its look gives between them, and its labels above their fields where they are placed so.</summary>
    [Fact]
    public void AFormLaysOutWithItsLooksSpacings()
    {
        Label name, field, other, below;
        var form = new Form
        {
            Base = { RowSpacing = 12f, LabelSpacing = 8f },
            Rows =
            {
                { new Label { Text = "Name" }.As(out name), new Label { Text = "x" }.As(out field) },
                { new Label { Text = "Other" }.As(out other), new Label { Text = "y" }.As(out below), FormLabelPlacement.Above }
            }
        };
        _ = new Easel(form, Wide, High);

        Assert.Equal(name.Bounds.Y, field.Bounds.Y, 3);
        Assert.Equal(name.Bounds.Bottom + 12f, other.Bounds.Y, 3);
        Assert.Equal(other.Bounds.Bottom + 8f, below.Bounds.Y, 3);
    }

    /// <summary>A grid's spacings are its looks, and its style sets them.</summary>
    [Fact]
    public void AGridsSpacingsAreItsLooks()
    {
        Label first, second, under;
        var grid = new Grid
        {
            Style = new GridStyle { Base = { ColumnSpacing = 10f, RowSpacing = 6f } },
            Cells =
            {
                [0, 0] = new Label { Text = "a" }.As(out first),
                [0, 1] = new Label { Text = "b" }.As(out second),
                [1, 0] = new Label { Text = "c" }.As(out under)
            }
        };
        _ = new Easel(grid, Wide, High);

        Assert.Equal(first.Bounds.Right + 10f, second.Bounds.X, 3);
        Assert.Equal(first.Bounds.Bottom + 6f, under.Bounds.Y, 3);
    }

    /// <summary>A tab view leaves twelve between its headers and its page unless its look says otherwise.</summary>
    [Fact]
    public void ATabViewsSpacingIsItsLook()
    {
        Label page;
        Tab tab;
        var tabs = new TabView { Tabs = { new Tab { Header = "One", Content = new Label { Text = "page" }.As(out page) }.As(out tab) } };
        var easel = new Easel(new VStack { Children = { tabs } }, Wide, High);
        Assert.Equal(tab.Bounds.Bottom + 12f, page.Bounds.Y, 3);

        tabs.Base.Spacing = 4f;
        easel.Draw(Wide, High);
        Assert.Equal(tab.Bounds.Bottom + 4f, page.Bounds.Y, 3);
    }

    /// <summary>A message box's spacings are its looks: the game's look leaves twenty-four under the message and eight between the buttons.</summary>
    [Fact]
    public void AMessageBoxsSpacingsAreItsLooks()
    {
        var box = new MessageBox();
        box.Restyle();
        Assert.Equal(24f, box.MessageSpacing);
        Assert.Equal(8f, box.ButtonSpacing);

        box.Base.MessageSpacing = 16f;
        box.Base.ButtonSpacing = 4f;
        Assert.Equal(16f, box.MessageSpacing);
        Assert.Equal(4f, box.ButtonSpacing);
    }

    /// <summary>A popup hangs as far from its anchor as its look says.</summary>
    [Fact]
    public void APopupsGapIsItsLook()
    {
        var popup = new Popup { Style = new PopupStyle { Base = { Gap = 8f } } };
        popup.Restyle();
        Assert.Equal(8f, popup.Gap);

        popup.Base.Gap = 4f;
        Assert.Equal(4f, popup.Gap);
    }

    /// <summary>A style for any element still dresses a stack, when a stack's style is based on it.</summary>
    [Fact]
    public void AStackStyleCanBeBasedOnAnElementsStyle()
    {
        var panel = new ElementStyle { Base = { Background = Color.Red } };
        var stack = new VStack { Style = new StackStyle(panel) { Base = { Spacing = 4f } } };
        stack.Restyle();

        Assert.Equal(Color.Red, stack.Background);
        Assert.Equal(4f, stack.Spacing);
    }
}
