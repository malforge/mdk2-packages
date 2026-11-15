using Lumel.Screens;
using VRageMath;
using Xunit;

namespace Lumel.Tests;

/// <summary>Telling a frame whose input the game has cleared from a frame in play.</summary>
public class GameInputTests
{
    /// <summary>The mouse at (-1, -1), where the game puts it out of focus, is a cleared frame.</summary>
    [Fact]
    public void TheOutOfFocusMousePositionIsCleared()
    {
        Assert.True(GameInput.IsClearedAt(new Vector2(-1, -1)));
    }

    /// <summary>A position with either coordinate at zero or above is not a cleared frame, the top left corner included.</summary>
    [Theory]
    [InlineData(0, 0)]
    [InlineData(1922, 1078)]
    [InlineData(-1, 500)]
    [InlineData(500, -1)]
    public void APositionNotNegativeOnBothAxesIsNotCleared(float x, float y)
    {
        Assert.False(GameInput.IsClearedAt(new Vector2(x, y)));
    }
}
