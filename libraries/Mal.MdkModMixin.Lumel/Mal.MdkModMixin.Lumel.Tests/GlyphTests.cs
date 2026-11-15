using Lumel.Input;
using Lumel.Layout;
using VRageMath;
using Xunit;
using Lumel.Images;

namespace Lumel.Tests;

/// <summary>A symbol: sized and tinted by its style, and lit by it.</summary>
public class GlyphTests
{
    const float Wide = 400f;
    const float High = 200f;

    static readonly Color Faint = new Color(133, 161, 169);
    static readonly Color Ink = new Color(220, 245, 253);
    static readonly Color Dim = new Color(95, 114, 120);
    static readonly Color PanelHot = new Color(60, 76, 82);

    static readonly GlyphStyle Tool = new GlyphStyle
    {
        Base = { FontSize = 32f, Tint = Faint },
        PointerOver = { Background = PanelHot, Tint = Ink },
        Disabled = { Tint = Dim }
    };

    /// <summary>Its style sizes and tints the symbol.</summary>
    [Fact]
    public void ItsStyleSizesAndTintsIt()
    {
        var glyph = new Glyph { Symbol = "d", Style = Tool };
        var easel = new Easel(new VStack { Children = { glyph } }, Wide, High);

        Assert.Equal(32f, glyph.DesiredSize.Y, 2);
        Assert.Equal(Faint, Assert.Single(easel.Written).Color);
    }

    /// <summary>
    ///     On hover, the glyph takes its hover fill and tint, and returns to normal when the pointer leaves.
    /// </summary>
    [Fact]
    public void UnderThePointerItTakesItsFillAndTint()
    {
        var glyph = new Glyph { Symbol = "d", Style = Tool };
        glyph.Restyle();

        ((IInputTarget)glyph).IsPointerOver = true;
        Assert.Equal(PanelHot, glyph.Background);
        Assert.Equal(Ink, glyph.Tint);

        ((IInputTarget)glyph).IsPointerOver = false;
        Assert.Equal(Color.Transparent, glyph.Background);
        Assert.Equal(Faint, glyph.Tint);
    }

    /// <summary>A disabled glyph looks disabled whether or not the pointer is over it.</summary>
    [Fact]
    public void DisabledItLooksDisabledEvenUnderThePointer()
    {
        var glyph = new Glyph { Symbol = "d", Style = Tool };
        glyph.Restyle();

        ((IInputTarget)glyph).IsPointerOver = true;
        glyph.IsEnabled = false;

        Assert.Equal(Dim, glyph.Tint);
    }

    /// <summary>A halo is drawn around the symbol, centered on it, only while it has a tint, and takes no room of its own.</summary>
    [Fact]
    public void AHaloIsDrawnAroundTheSymbolWhileItHasATint()
    {
        var glyph = new Glyph { Symbol = "d", Style = Tool, Halo = "o", Base = { HaloSize = 64f } };
        var plain = new Glyph { Symbol = "d", Style = Tool };
        var easel = new Easel(new VStack { Children = { glyph, plain } }, Wide, High);
        Assert.Equal(plain.DesiredSize, glyph.DesiredSize);
        Assert.Equal(2, easel.Written.Count());

        glyph.Base.HaloTint = Ink;
        easel.Draw(Wide, High);
        Assert.Equal(3, easel.Written.Count());
        Assert.Equal(Ink, easel.Written.ElementAt(0).Color);
        Assert.Equal(Faint, easel.Written.ElementAt(1).Color);
    }

    /// <summary>A halo's tint pulsing measures no text.</summary>
    [Fact]
    public void AHalosTintMeasuresNoText()
    {
        var glyph = new Glyph { Symbol = "d", Style = Tool, Halo = "o", Base = { HaloSize = 64f } };
        var easel = new Easel(new VStack { Children = { glyph } }, Wide, High);
        var settled = easel.Measured;

        glyph.Base.HaloTint = Ink;
        easel.Draw(Wide, High);
        glyph.Base.HaloTint = Faint;
        easel.Draw(Wide, High);

        Assert.Equal(settled, easel.Measured);
    }

    /// <summary>The pointer entering and leaving measures no text.</summary>
    [Fact]
    public void ThePointerComingMeasuresNoText()
    {
        var glyph = new Glyph { Symbol = "d", Style = Tool };
        var easel = new Easel(new VStack { Children = { glyph } }, Wide, High);
        var settled = easel.Measured;

        ((IInputTarget)glyph).IsPointerOver = true;
        easel.Draw(Wide, High);
        ((IInputTarget)glyph).IsPointerOver = false;
        easel.Draw(Wide, High);

        Assert.Equal(settled, easel.Measured);
    }
}
