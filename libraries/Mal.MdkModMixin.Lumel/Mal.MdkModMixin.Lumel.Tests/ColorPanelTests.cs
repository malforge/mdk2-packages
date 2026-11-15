using Lumel.Controls;
using Lumel.Input;
using Lumel.Layout;
using Lumel.Screens;
using Lumel.Text;
using Lumel.Windows;
using VRage.Input;
using VRageMath;
using Xunit;

namespace Lumel.Tests;

/// <summary>A color panel: every part follows the color, each way of giving one, and its palettes.</summary>
public class ColorPanelTests
{
    static readonly RectangleF Area = new RectangleF(0f, 0f, 1200f, 900f);

    /// <summary>A color set in code shows in every part, and tells nobody.</summary>
    [Fact]
    public void ASetColorShowsEverywhere()
    {
        var scene = new Scene();

        scene.Panel.Color = new Color(255, 0, 0);

        Assert.Equal("#FF0000", Code(scene).Text);
        Assert.Equal(255, Box(scene, ColorEntry.Rgb, 0).Value);
        Assert.Equal(0, Box(scene, ColorEntry.Hsv, 0).Value);
        Assert.Equal(100, Box(scene, ColorEntry.Hsv, 1).Value);
        Assert.Equal(0, scene.Changes);
    }

    /// <summary>A code typed in, with or without its hash, gives the color as it reads as one, and one with alpha gives the alpha too.</summary>
    [Theory]
    [InlineData("#3366CC", 51, 102, 204, 255)]
    [InlineData("3366cc", 51, 102, 204, 255)]
    [InlineData("#3366CC80", 51, 102, 204, 128)]
    public void ATypedCodeGivesTheColor(string code, int red, int green, int blue, int alpha)
    {
        var scene = new Scene();

        scene.Type(Code(scene), code);

        Assert.Equal(new Color(red, green, blue, alpha), scene.Panel.Color);
        Assert.NotEqual(0, scene.Changes);
        Assert.Equal(green, Box(scene, ColorEntry.Rgb, 1).Value);
    }

    /// <summary>A code that does not read as a color yet changes nothing.</summary>
    [Fact]
    public void AnUnfinishedCodeChangesNothing()
    {
        var scene = new Scene();
        scene.Panel.Color = Color.White;

        scene.Type(Code(scene), "#12");

        Assert.Equal(Color.White, scene.Panel.Color);
        Assert.Equal(0, scene.Changes);
    }

    /// <summary>A channel typed in gives the color, and keeps what is typed while the rest follow.</summary>
    [Fact]
    public void ATypedChannelGivesTheColor()
    {
        var scene = new Scene();
        scene.Panel.Color = Color.Black;
        var red = Box(scene, ColorEntry.Rgb, 0);

        scene.Type(red, "128");

        Assert.Equal(new Color(128, 0, 0), scene.Panel.Color);
        Assert.Equal("128", red.Text);
        Assert.Equal("#800000", Code(scene).Text);
    }

    /// <summary>A gray given by red, green and blue keeps the hue the panel had.</summary>
    [Fact]
    public void AGrayKeepsTheHue()
    {
        var scene = new Scene();
        scene.Panel.Color = new Color(0, 0, 255);
        var hue = Box(scene, ColorEntry.Hsv, 0).Value;

        scene.Type(Code(scene), "#808080");

        Assert.Equal(hue, Box(scene, ColorEntry.Hsv, 0).Value);
        Assert.Equal(new Color(128, 128, 128), scene.Panel.Color);
    }

    /// <summary>Pressing a swatch of a palette gives its color, keeping the alpha, and tells the palette's listeners too.</summary>
    [Fact]
    public void APaletteGivesItsColor()
    {
        var scene = new Scene();
        scene.Panel.Color = new Color(0, 0, 0, 128);
        var told = new List<Color>();
        scene.Palette.Chosen += told.Add;

        scene.Press(scene.Palette.Children[1]);

        Assert.Equal(new Color(0, 255, 0, 128), scene.Panel.Color);
        Assert.Equal(new[] { new Color(0, 255, 0) }, told);
        Assert.Equal(1, scene.Changes);
    }

    /// <summary>A palette given fewer colors keeps the swatches it needs and drops the rest.</summary>
    [Fact]
    public void APaletteKeepsItsSwatches()
    {
        var palette = new ColorPalette();
        palette.SetColors(new[] { Color.Red, Color.Green, Color.Blue });
        var first = palette.Children[0];

        palette.SetColors(new[] { Color.White });

        Assert.Single(palette.Children);
        Assert.Same(first, palette.Children[0]);
        Assert.Equal(Color.White, ((Swatch)first).Color);
    }

    /// <summary>Enter in a box carries out the panel's submit.</summary>
    [Fact]
    public void EnterSubmits()
    {
        var scene = new Scene();
        var submitted = 0;
        scene.Panel.Submit = new RelayCommand(() => submitted++);
        scene.Workspace.Input.Focus(Box(scene, ColorEntry.Hsv, 0));

        scene.Workspace.Key(MyKeys.Enter, false, false);

        Assert.Equal(1, submitted);
    }

    /// <summary>The player showing another tab tells the application; setting the tab tells nobody.</summary>
    [Fact]
    public void ShowingATabTellsTheApplication()
    {
        var scene = new Scene();
        var told = 0;
        scene.Panel.EntryChanged += () => told++;

        scene.Panel.Entry = ColorEntry.Rgb;
        scene.Draw();
        scene.Press(Entries(scene).Tabs[2]);

        Assert.Equal(ColorEntry.Hex, scene.Panel.Entry);
        Assert.Equal(1, told);
    }

    /// <summary>Returns the box the code is typed in, showing its tab.</summary>
    static TextBox Code(Scene scene)
    {
        return (TextBox)Shown(scene, ColorEntry.Hex);
    }

    /// <summary>Returns the box for channel <paramref name="channel" /> of the way <paramref name="entry" />, showing its tab.</summary>
    static NumberBox Box(Scene scene, ColorEntry entry, int channel)
    {
        var channels = (HStack)Shown(scene, entry);
        return (NumberBox)((HStack)channels.Children[channel]).Children[1];
    }

    /// <summary>Shows the tab for <paramref name="entry" /> and returns its page.</summary>
    static Element Shown(Scene scene, ColorEntry entry)
    {
        scene.Panel.Entry = entry;
        scene.Draw();
        return Entries(scene).Tabs[(int)entry].Content;
    }

    static TabView Entries(Scene scene)
    {
        return (TabView)((HStack)scene.Panel.Children[1]).Children[1];
    }

    /// <summary>A panel with one palette of red, green and blue, in a window on a workspace, counting the player's changes.</summary>
    sealed class Scene
    {
        public readonly ColorPanel Panel;
        public readonly ColorPalette Palette = new ColorPalette();
        public readonly Workspace Workspace = new Workspace();

        public Scene()
        {
            Palette.SetColors(new[] { new Color(255, 0, 0), new Color(0, 255, 0), new Color(0, 0, 255) });
            Panel = new ColorPanel { Palettes = { Palette } };
            Panel.ColorChanged += () => Changes++;
            Workspace.Windows.Open(new Window { Content = Panel });
            Draw();
        }

        public int Changes { get; private set; }

        public void Draw()
        {
            Workspace.DrawScreen(new Recording(Area), Area);
        }

        public void Type(TextBox box, string text)
        {
            Workspace.Input.Focus(box);
            box.SelectAll();
            foreach (var c in text)
                Workspace.Text(c);

            Draw();
        }

        public void Press(Element element)
        {
            var middle = new Vector2(element.Bounds.X + element.Bounds.Width / 2f, element.Bounds.Y + element.Bounds.Height / 2f);
            Workspace.PointerDown(middle, PointerButton.Left, 1, false, false);
            Workspace.PointerUp(middle, PointerButton.Left);
            Draw();
        }
    }
}
