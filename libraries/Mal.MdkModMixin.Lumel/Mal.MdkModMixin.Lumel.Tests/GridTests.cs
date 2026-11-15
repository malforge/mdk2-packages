using Lumel.Layout;
using Lumel.Text;
using Xunit;

namespace Lumel.Tests;

/// <summary>How a Grid sizes its tracks and places its cells.</summary>
public class GridTests
{
    const float Wide = 400f;
    const float High = 200f;

    /// <summary>An auto column is as wide as the widest cell in it, so the next column starts in the same place on every row.</summary>
    [Fact]
    public void AnAutoColumnIsAsWideAsItsWidestCell()
    {
        Label shortName, longName, first, second;
        var grid = new Grid
        {
            Base = { ColumnSpacing = 6f },
            Cells =
            {
                [0, 0] = new Label { Text = "Speed" }.As(out shortName),
                [0, 1] = new Label { Text = "10" }.As(out first),
                [1, 0] = new Label { Text = "Top speed, when laden" }.As(out longName),
                [1, 1] = new Label { Text = "20" }.As(out second)
            }
        };

        _ = new Easel(grid, Wide, High);

        var column = longName.DesiredSize.X;
        Assert.True(column > shortName.DesiredSize.X);
        Assert.Equal(column + 6f, first.Bounds.X, 3);
        Assert.Equal(column + 6f, second.Bounds.X, 3);
        Assert.Equal(column, shortName.Bounds.Width, 3);
    }

    /// <summary>A fixed column is its size, and a flex column takes what the others leave.</summary>
    [Fact]
    public void AFlexColumnTakesWhatTheOthersLeave()
    {
        Label grip, number, sentence;
        var grid = new Grid
        {
            Columns = { 26, GridLength.Auto, GridLength.Flex(1) },
            Cells =
            {
                [0, 0] = new Label { Text = "::" }.As(out grip),
                [0, 1] = new Label { Text = "12" }.As(out number),
                [0, 2] = new Label { Text = "Close the inner door" }.As(out sentence)
            }
        };

        _ = new Easel(grid, Wide, High);

        Assert.Equal(26f, grip.Bounds.Width, 3);
        Assert.Equal(26f, number.Bounds.X, 3);
        Assert.Equal(26f + number.DesiredSize.X, sentence.Bounds.X, 3);
        Assert.Equal(Wide - 26f - number.DesiredSize.X, sentence.Bounds.Width, 3);
    }

    /// <summary>Flex columns share what is left by weight.</summary>
    [Fact]
    public void FlexColumnsShareByWeight()
    {
        Label one, three;
        var grid = new Grid
        {
            Columns = { GridLength.Flex(1), GridLength.Flex(3) },
            Cells =
            {
                [0, 0] = new Label().As(out one),
                [0, 1] = new Label().As(out three)
            }
        };

        _ = new Easel(grid, Wide, High);

        Assert.Equal(100f, one.Bounds.Width, 3);
        Assert.Equal(300f, three.Bounds.Width, 3);
    }

    /// <summary>A cell spanning columns is as wide as all of them and the spacing between.</summary>
    [Fact]
    public void ASpanCoversItsColumnsAndTheSpacingBetween()
    {
        Label spanning;
        var grid = new Grid
        {
            Base = { ColumnSpacing = 4f },
            Columns = { 30, 50 },
            Cells =
            {
                [0, 0] = new Label { Text = "a" },
                [0, 1] = new Label { Text = "b" },
                [1, 0, 1, 2] = new Label { Text = "Both" }.As(out spanning)
            }
        };

        _ = new Easel(grid, Wide, High);

        Assert.Equal(0f, spanning.Bounds.X, 3);
        Assert.Equal(84f, spanning.Bounds.Width, 3);
    }

    /// <summary>A cell that would overlap one already holding something is refused, and the grid keeps what it had.</summary>
    [Fact]
    public void ACellOverlappingAnotherIsRefused()
    {
        var kept = new Label { Text = "Kept" };
        var grid = new Grid { Cells = { [0, 0, 1, 2] = kept } };

        Assert.Throws<InvalidOperationException>(() => grid.Cells[0, 1] = new Label { Text = "In the way" });
        Assert.Same(kept, grid.Cells[0, 0]);
        Assert.Same(kept, grid.Cells[0, 1]);
    }

    /// <summary>Setting a cell that already holds exactly that cell replaces what was there, and null empties it.</summary>
    [Fact]
    public void SettingTheSameCellReplacesAndNullEmpties()
    {
        var first = new Label { Text = "First" };
        var second = new Label { Text = "Second" };
        var grid = new Grid { Cells = { [0, 0] = first } };

        grid.Cells[0, 0] = second;
        Assert.Same(second, grid.Cells[0, 0]);
        Assert.Null(first.Parent);

        grid.Cells[0, 0] = null;
        Assert.Null(grid.Cells[0, 0]);
        Assert.Null(second.Parent);
    }

    /// <summary>A grid refuses a child placed in a way it cannot honor.</summary>
    [Fact]
    public void AGridRefusesAPlacementItCannotHonor()
    {
        var grid = new Grid();

        Assert.Throws<InvalidOperationException>(() => grid.Cells[0, 0] = new Label { Placement = new Elsewhere() });
        Assert.Null(grid.Cells[0, 0]);
    }

    /// <summary>A grid measures no text on a frame where nothing changed.</summary>
    [Fact]
    public void AnIdleGridMeasuresNoText()
    {
        var grid = new Grid
        {
            Columns = { GridLength.Auto, GridLength.Flex(1) },
            Cells =
            {
                [0, 0] = new Label { Text = "Name" },
                [0, 1] = new Label { Text = "Airlock" },
                [1, 0] = new Label { Text = "Speed" },
                [1, 1] = new Label { Text = "12" }
            }
        };

        var easel = new Easel(grid, Wide, High);
        var settled = easel.Measured;
        easel.Draw(Wide, High);
        easel.Draw(Wide, High);

        Assert.Equal(settled, easel.Measured);
    }

    /// <summary>A placement no grid understands.</summary>
    sealed class Elsewhere : IPlacement
    {
        public event Action? Changed
        {
            add { }
            remove { }
        }
    }
}
