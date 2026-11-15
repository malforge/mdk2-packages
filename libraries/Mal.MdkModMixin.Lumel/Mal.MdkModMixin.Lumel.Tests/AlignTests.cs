using Lumel.Layout;
using Lumel.Text;
using Xunit;

namespace Lumel.Tests;

/// <summary>Tests where an element sits in the room its container gives it.</summary>
public class AlignTests
{
    const float Wide = 400f;
    const float High = 200f;

    /// <summary>
    ///     A child with no alignment is placed the way its container places it: a row centers it vertically, a column
    ///     stretches it across.
    /// </summary>
    [Fact]
    public void SayingNothingLeavesItToTheHolder()
    {
        Label inRow, inColumn;
        var row = new HStack { Children = { new Label { Text = "Row" }.As(out inRow) } };
        var column = new VStack { Children = { new Label { Text = "Column" }.As(out inColumn) } };

        _ = new Easel(row, Wide, High);
        _ = new Easel(column, Wide, High);

        Assert.Equal((High - inRow.Bounds.Height) / 2f, inRow.Bounds.Y, 3);
        Assert.Equal(Wide, inColumn.Bounds.Width, 3);
    }

    /// <summary>In a row, an edge pins a child to it, and both edges stretch it vertically.</summary>
    [Fact]
    public void InARowAnEdgePinsAndBothStretch()
    {
        Label top, bottom, tall;
        var row = new HStack
        {
            Children =
            {
                new Label { Text = "Top", Align = Align.Top }.As(out top),
                new Label { Text = "Bottom", Align = Align.Bottom }.As(out bottom),
                new Label { Text = "Tall", Align = Align.FillHeight }.As(out tall)
            }
        };

        _ = new Easel(row, Wide, High);

        Assert.Equal(0f, top.Bounds.Y, 3);
        Assert.Equal(High, bottom.Bounds.Bottom, 3);
        Assert.Equal(0f, tall.Bounds.Y, 3);
        Assert.Equal(High, tall.Bounds.Height, 3);
    }

    /// <summary>In a column, a child set left or centered keeps the width it asked for instead of stretching.</summary>
    [Fact]
    public void InAColumnLeftOrCenterKeepsItsWidth()
    {
        Label left, center, right;
        var column = new VStack
        {
            Children =
            {
                new Label { Text = "Left", Align = Align.Left }.As(out left),
                new Label { Text = "Center", Align = Align.Center }.As(out center),
                new Label { Text = "Right", Align = Align.Right }.As(out right)
            }
        };

        _ = new Easel(column, Wide, High);

        Assert.Equal(0f, left.Bounds.X, 3);
        Assert.True(left.Bounds.Width < Wide);
        Assert.Equal((Wide - center.Bounds.Width) / 2f, center.Bounds.X, 3);
        Assert.Equal(Wide, right.Bounds.Right, 3);
    }

    /// <summary>
    ///     Center applies to every axis that names no edge: in a grid cell, centered horizontally and stretched
    ///     vertically.
    /// </summary>
    [Fact]
    public void CenterAppliesWhereNoEdgeIsNamed()
    {
        Label held;
        var grid = new Grid
        {
            Columns = { GridLength.Flex(1) },
            Rows = { GridLength.Flex(1) },
            Cells = { [0, 0] = new Label { Text = "Held", Align = Align.Center | Align.FillHeight }.As(out held) }
        };

        _ = new Easel(grid, Wide, High);

        Assert.Equal((Wide - held.Bounds.Width) / 2f, held.Bounds.X, 3);
        Assert.Equal(0f, held.Bounds.Y, 3);
        Assert.Equal(High, held.Bounds.Height, 3);
    }

    /// <summary>Changing a child's alignment places it again.</summary>
    [Fact]
    public void ChangingTheAlignmentPlacesItAgain()
    {
        Label moved;
        var row = new HStack { Children = { new Label { Text = "Moved" }.As(out moved) } };
        var easel = new Easel(row, Wide, High);

        moved.Align = Align.Top;
        easel.Draw(Wide, High);

        Assert.Equal(0f, moved.Bounds.Y, 3);
    }
}
