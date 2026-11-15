using Lumel.Drawing;
using Lumel.Layout;
using VRageMath;
using Xunit;

namespace Lumel.Tests;

/// <summary>Whatever is asked to be drawn is drawn: on the screen, nothing thin rounds away to no pixels.</summary>
public class PixelRoundingTests
{
    const float Wide = 400f;
    const float High = 400f;

    /// <summary>A fill thinner than a pixel is drawn one pixel thin, wherever it falls.</summary>
    [Fact]
    public void AThinFillStaysOnePixel()
    {
        var easel = Drawn(g =>
        {
            for (var i = 0; i < 40; i++)
                g.Fill(new RectangleF(0f, i * 3.1f, 100f, 0.2f), Color.White);
        });

        Assert.Equal(40, easel.Sprites.Count());
        Assert.All(easel.Sprites, s => Assert.Equal(1, s.Destination.Height));
    }

    /// <summary>A slice of a picture thinner than a pixel is drawn one pixel thin too.</summary>
    [Fact]
    public void AThinSliceStaysOnePixel()
    {
        var easel = Drawn(g =>
        {
            for (var i = 0; i < 40; i++)
                g.DrawSprite(LumelMaterials.Dots, new RectangleF(0f, i * 3.1f, 100f, 0.2f), new Rectangle(0, 0, 16, 8), Color.White);
        });

        Assert.Equal(40, easel.Sprites.Count());
        Assert.All(easel.Sprites, s => Assert.Equal(1, s.Destination.Height));
    }

    /// <summary>
    ///     An element finds where it lands in the window's pixels through every transform above it, and back, and so
    ///     can draw on whole pixels.
    /// </summary>
    [Fact]
    public void AnElementFindsItsPixelsThroughItsTransforms()
    {
        Vector2 corner = default, back = default;
        var drawing = new Drawing(g =>
        {
            corner = g.ToDevicePixels(new Vector2(10f, 20f));
            back = g.FromDevicePixels(corner);
        });
        var moved = new VStack { RenderTransform = new RenderTransform(new Vector2(6f, 4f), 2f), Children = { drawing } };
        var easel = new Easel(new VStack { Children = { moved } }, Wide, High);

        Assert.Equal(new Vector2(26f, 44f) * easel.Pixels, corner);
        Assert.Equal(10f, back.X, 3);
        Assert.Equal(20f, back.Y, 3);
    }

    /// <summary>An empty rectangle still draws nothing.</summary>
    [Fact]
    public void AnEmptyFillDrawsNothing()
    {
        var easel = Drawn(g => g.Fill(new RectangleF(0f, 0f, 100f, 0f), Color.White));

        Assert.Empty(easel.Sprites);
    }

    static Easel Drawn(Action<IGraphics> draw)
    {
        return new Easel(new VStack { Children = { new Drawing(draw) } }, Wide, High);
    }

    /// <summary>An element that draws what it is given.</summary>
    sealed class Drawing(Action<IGraphics> draw) : Element
    {
        /// <inheritdoc />
        protected override void DrawContent(IGraphics g) => draw(g);

        /// <inheritdoc />
        protected override Vector2 MeasureContent(IGraphics g, Vector2 available) => new Vector2(100f, 100f);
    }
}
