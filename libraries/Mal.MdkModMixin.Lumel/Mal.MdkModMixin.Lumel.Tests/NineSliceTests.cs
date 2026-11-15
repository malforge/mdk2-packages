using Lumel.Drawing;
using Lumel.Images;
using Lumel.Sim;
using Sandbox.ModAPI;
using VRage.Utils;
using VRageMath;
using Xunit;

namespace Lumel.Tests;

/// <summary>A picture drawn in nine parts on the game's screen, its parts meeting in whole pixels.</summary>
public class NineSliceTests
{
    /// <summary>Each part starts on the pixel where the one before it ends, at sizes that fall between pixels.</summary>
    [Theory]
    [InlineData(0f, 0f)]
    [InlineData(10.3f, 7.6f)]
    [InlineData(101.5f, 33.25f)]
    public void ThePartsMeetWithoutAGapOrAnOverlap(float x, float y)
    {
        var device = new ScreenSpaceDevice(LumelContent.Instance) { SafeArea = new RectangleF(0f, 0f, 1000f, 750f), ViewportSize = new Vector2(1000f, 750f) };
        var graphics = new ScreenSpaceGraphics(device, MyScreenSpaceLayer.AboveHud, new MaterialImages(device.TextureSize), () => 0);
        device.BeginFrame();
        graphics.Begin();

        new NineSlice(MyStringId.GetOrCompute("Shade"), 64, 24).Draw(graphics, new RectangleF(x, y, 333.3f, 211.7f), 23.7f, Color.Black);

        var parts = device.Commands.Select(c => c.Destination).ToList();
        var columns = parts.Select(p => p.X).Distinct().OrderBy(at => at).ToList();
        var rows = parts.Select(p => p.Y).Distinct().OrderBy(at => at).ToList();

        Assert.Equal(3, columns.Count);
        Assert.Equal(3, rows.Count);
        Assert.Equal(columns[1], parts.First(p => p.X == columns[0]).Right);
        Assert.Equal(columns[2], parts.First(p => p.X == columns[1]).Right);
        Assert.Equal(rows[1], parts.First(p => p.Y == rows[0]).Bottom);
        Assert.Equal(rows[2], parts.First(p => p.Y == rows[1]).Bottom);
    }
}
