using Lumel.Controls;
using Lumel.Layout;
using Lumel.Styling;
using Lumel.Text;
using VRageMath;
using Xunit;

namespace Lumel.Tests;

/// <summary>The colors chosen last, a palette of fixed places, and what a color panel lets an application mark.</summary>
public class ColorHistoryTests
{
    static readonly Color Red = new Color(255, 0, 0);
    static readonly Color Green = new Color(0, 255, 0);
    static readonly Color Blue = new Color(0, 0, 255);

    /// <summary>A color added goes first, once, and the oldest drop off past the capacity.</summary>
    [Fact]
    public void AddingPutsTheColorFirstOnce()
    {
        var history = new ColorHistory(2);
        var told = 0;
        history.Changed += () => told++;

        history.Add(Red);
        history.Add(Green);
        history.Add(Red);
        history.Add(Red);
        history.Add(Blue);

        Assert.Equal(new[] { Blue, Red }, history.Colors);
        Assert.Equal(4, told);
    }

    /// <summary>Loading a history tells nobody, and refuses too many colors or one twice.</summary>
    [Fact]
    public void LoadingTellsNobodyAndRefusesTooMany()
    {
        var history = new ColorHistory(2);
        var told = 0;
        history.Changed += () => told++;

        history.SetColors(new[] { Green, Red });

        Assert.Equal(new[] { Green, Red }, history.Colors);
        Assert.Equal(0, told);
        Assert.Throws<ArgumentException>(() => history.SetColors(new[] { Red, Green, Blue }));
        Assert.Throws<ArgumentException>(() => history.SetColors(new[] { Red, Red }));
    }

    /// <summary>A palette of fixed places keeps its length, its unfilled places inert, and refuses more colors than places.</summary>
    [Fact]
    public void APaletteKeepsItsPlaces()
    {
        var palette = new ColorPalette { Places = 3 };

        palette.SetColors(new[] { Red });

        Assert.Equal(3, palette.Children.Count);
        Assert.True(palette.Children[0].IsEnabled);
        Assert.False(palette.Children[2].IsEnabled);
        Assert.Equal(Color.Transparent, ((Swatch)palette.Children[2]).Color);
        Assert.Throws<ArgumentException>(() => palette.SetColors(new[] { Red, Green, Blue, Color.White }));
        Assert.Throws<ArgumentException>(() => palette.Places = -1);
    }

    /// <summary>With no color, the color made shows the fill its look gives, and the color once there is one.</summary>
    [Fact]
    public void NoColorShowsTheEmptyFill()
    {
        var panel = new ColorPanel { Base = { EmptyFill = Green }, Color = null };
        var sample = (Swatch)((HStack)panel.Children[1]).Children[0];

        panel.Restyle();
        var empty = sample.Color;
        panel.Color = Red;

        Assert.Equal(Green, empty);
        Assert.Equal(Red, sample.Color);
    }

    /// <summary>A panel carries an application's tips on its parts and its state on the code box.</summary>
    [Fact]
    public void APanelCarriesTipsAndTheCodesState()
    {
        var marked = State.Declare("TestRefused", 500);
        var panel = new ColorPanel();
        var hue = Tip.For(null, "The hue.");

        panel.HueTip = hue;
        panel.SetCodeState(marked, true);

        Assert.Same(hue, panel.HueTip);
        var tabs = (TabView)((HStack)panel.Children[1]).Children[1];
        var code = (TextBox)tabs.Tabs[2].Content;
        Assert.True(code.Is(marked));
    }
}
