using System.Text;
using Lumel.Drawing;
using VRage.Game.GUI.TextPanel;
using VRageMath;
using Xunit;

namespace Lumel.Tests;

/// <summary>Drawing on a text panel, written down as its sprites.</summary>
public class SpriteGraphicsTests
{
    /// <summary>Text in no font is drawn as nothing and measures nothing, as on the screen.</summary>
    [Fact]
    public void TextInNoFontIsNothing()
    {
        var sprites = new List<MySprite>();
        var graphics = new SpriteGraphics(null!, sprites, null!, () => 0);

        graphics.DrawText(null!, "Airlock", Vector2.Zero, 20f, Color.White);

        Assert.Empty(sprites);
        Assert.Equal(Vector2.Zero, graphics.MeasureText(null!, new StringBuilder("Airlock"), 20f));
    }

    /// <summary>Text in a font the panel can draw is drawn in it, whatever the game's definitions say, as in a harness that has none.</summary>
    [Fact]
    public void AFontThePanelHasIsDrawnIn()
    {
        var sprites = new List<MySprite>();
        var graphics = new SpriteGraphics(new PanelSurface("White", "Maltek"), sprites, null!, () => 0);
        graphics.Begin();

        graphics.DrawText("Maltek", "12", Vector2.Zero, 20f, Color.White);

        Assert.Equal("Maltek", sprites.Single().FontId);
    }

    /// <summary>Text in a font the panel cannot draw is drawn in the game's own face.</summary>
    [Fact]
    public void AFontThePanelLacksFallsBackToTheGamesFace()
    {
        var sprites = new List<MySprite>();
        var graphics = new SpriteGraphics(new PanelSurface("White"), sprites, null!, () => 0);
        graphics.Begin();

        graphics.DrawText("Maltek", "12", Vector2.Zero, 20f, Color.White);

        Assert.Equal("White", sprites.Single().FontId);
    }

    /// <summary>
    ///     A point maps to the panel's texture pixels past the corner the screen hides and through every transform, and
    ///     back to where it was.
    /// </summary>
    [Fact]
    public void APointMapsToTexturePixelsAndBack()
    {
        var sprites = new List<MySprite>();
        var surface = new PanelSurface("White") { SurfaceSize = new Vector2(512f, 307f) };
        var graphics = new SpriteGraphics(surface, sprites, null!, () => 0);
        graphics.Begin();
        graphics.PushTransform(new RenderTransform(new Vector2(4f, 8f), 0.6f), new Vector2(10f, 20f));
        graphics.PushTransform(RenderTransform.Moved(new Vector2(0f, 12f)), new Vector2(30f, 40f));

        var point = new Vector2(50f, 60f);
        var pixels = graphics.ToDevicePixels(point);

        Assert.Equal(new Vector2((50f - 10f) * 0.6f + 10f + 4f, (72f - 20f) * 0.6f + 20f + 8f + 102.5f), pixels);
        var back = graphics.FromDevicePixels(pixels);
        Assert.Equal(point.X, back.X, 3);
        Assert.Equal(point.Y, back.Y, 3);
    }

    /// <summary>Lumel's own fill, disc and ring are drawn as the panel's square, circle and hollow circle; any other material by its own name.</summary>
    [Fact]
    public void LumelsShapesAreThePanelsOwn()
    {
        var sprites = new List<MySprite>();
        var graphics = new SpriteGraphics(new PanelSurface("White"), sprites, null!, () => 0);
        graphics.Begin();
        var area = new RectangleF(0f, 0f, 16f, 16f);

        graphics.DrawSprite(LumelMaterials.Fill, area, Color.White, 0f);
        graphics.DrawSprite(LumelMaterials.Disc, area, Color.White, 0f);
        graphics.DrawSprite(LumelMaterials.Ring, area, Color.White, 0f);
        graphics.DrawSprite(VRage.Utils.MyStringId.GetOrCompute("Grid"), area, Color.White, 0f);

        Assert.Equal(new[] { "SquareSimple", "Circle", "CircleHollow", "Grid" }, sprites.Select(s => s.Data));
    }
}