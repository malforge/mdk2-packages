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

/// <summary>A color picker: the panel it opens, and the color given only by Done or Enter.</summary>
public class ColorPickerTests
{
    static readonly RectangleF Area = new RectangleF(0f, 0f, 800f, 1000f);

    static readonly Color Blue = new Color(0, 0, 255);

    static readonly Color Red = new Color(255, 0, 0);

    /// <summary>Pressing the box opens the panel under it, showing the picker's color.</summary>
    [Fact]
    public void PressingTheBoxOpensThePanel()
    {
        var scene = new Scene();

        scene.Press(scene.Picker);

        Assert.True(scene.Picker.IsOpen);
        Assert.True(scene.Picker.Is(ColorPicker.Expanded));
        Assert.Equal(Blue, scene.Panel.Color);
        Assert.True(scene.Panel.Bounds.Y >= scene.Picker.Bounds.Bottom);
    }

    /// <summary>A color made in the panel is not the picker's until it is given.</summary>
    [Fact]
    public void AColorMadeIsNotGivenAtOnce()
    {
        var scene = new Scene();
        scene.Press(scene.Picker);

        scene.Press(scene.Palette.Children[0]);

        Assert.Equal(Red, scene.Panel.Color);
        Assert.Equal(Blue, scene.Picker.Color);
        Assert.Equal(0, scene.Changes);
    }

    /// <summary>Done gives the picker the color made, puts the panel away and tells the application.</summary>
    [Fact]
    public void DoneGivesTheColor()
    {
        var scene = new Scene();
        scene.Press(scene.Picker);
        scene.Press(scene.Palette.Children[0]);

        scene.Press(scene.Done);

        Assert.False(scene.Picker.IsOpen);
        Assert.False(scene.Picker.Is(ColorPicker.Expanded));
        Assert.Equal(Red, scene.Picker.Color);
        Assert.Equal(1, scene.Changes);
    }

    /// <summary>Enter in a box gives the color typed, as Done does.</summary>
    [Fact]
    public void EnterGivesTheColor()
    {
        var scene = new Scene();
        scene.Press(scene.Picker);
        scene.Panel.Entry = ColorEntry.Hex;
        scene.Draw();

        scene.Type(scene.Code, "#00FF00");
        scene.Workspace.Key(MyKeys.Enter, false, false);
        scene.Draw();

        Assert.False(scene.Picker.IsOpen);
        Assert.Equal(new Color(0, 255, 0), scene.Picker.Color);
        Assert.Equal(1, scene.Changes);
    }

    /// <summary>Enter where no box holds the keyboard gives the color too.</summary>
    [Fact]
    public void EnterAnywhereInThePanelGivesTheColor()
    {
        var scene = new Scene();
        scene.Press(scene.Picker);
        scene.Press(scene.Palette.Children[0]);
        scene.Workspace.Input.Focus(null);

        scene.Workspace.Key(MyKeys.Enter, false, false);
        scene.Draw();

        Assert.False(scene.Picker.IsOpen);
        Assert.Equal(Red, scene.Picker.Color);
    }

    /// <summary>Escape puts the panel away and gives nothing.</summary>
    [Fact]
    public void EscapeGivesNothing()
    {
        var scene = new Scene();
        scene.Press(scene.Picker);
        scene.Press(scene.Palette.Children[0]);
        scene.Workspace.Input.Focus(scene.Code);

        scene.Workspace.Key(MyKeys.Escape, false, false);
        scene.Draw();

        Assert.False(scene.Picker.IsOpen);
        Assert.Equal(Blue, scene.Picker.Color);
        Assert.Equal(0, scene.Changes);
    }

    /// <summary>A press outside the panel puts it away and gives nothing.</summary>
    [Fact]
    public void APressOutsideGivesNothing()
    {
        var scene = new Scene();
        scene.Press(scene.Picker);
        scene.Press(scene.Palette.Children[0]);

        scene.Press(new Vector2(780f, 980f));

        Assert.False(scene.Picker.IsOpen);
        Assert.Equal(Blue, scene.Picker.Color);
        Assert.Equal(0, scene.Changes);
    }

    /// <summary>Opened again, the panel shows the picker's color, not the one last made and put away.</summary>
    [Fact]
    public void OpenedAgainThePanelShowsThePickersColor()
    {
        var scene = new Scene();
        scene.Press(scene.Picker);
        scene.Press(scene.Palette.Children[0]);
        scene.Press(new Vector2(780f, 980f));

        scene.Press(scene.Picker);

        Assert.Equal(Blue, scene.Panel.Color);
    }

    /// <summary>Pressing the box while the panel is open puts it away.</summary>
    [Fact]
    public void PressingTheBoxAgainClosesThePanel()
    {
        var scene = new Scene();
        scene.Press(scene.Picker);

        scene.Press(scene.Picker);

        Assert.False(scene.Picker.IsOpen);
    }

    /// <summary>The tab the player shows in the panel is the picker's, and the application is told.</summary>
    [Fact]
    public void TheTabShownIsThePickers()
    {
        var scene = new Scene();
        var told = 0;
        scene.Picker.EntryChanged += () => told++;
        scene.Press(scene.Picker);

        scene.Press(scene.Entries.Tabs[1]);

        Assert.Equal(ColorEntry.Rgb, scene.Picker.Entry);
        Assert.Equal(1, told);
    }

    /// <summary>The box is as wide whatever its color, and draws the color inside its edge at the size its look gives.</summary>
    [Fact]
    public void TheBoxDrawsItsColor()
    {
        var recording = new Recording(Area);
        var scene = new Scene();
        var wide = scene.Picker.Bounds.Width;

        scene.Picker.Color = Color.Transparent;
        scene.Workspace.DrawScreen(recording, Area);

        Assert.Equal(wide, scene.Picker.Bounds.Width);
        scene.Picker.Color = Blue;
        recording = new Recording(Area);
        scene.Workspace.DrawScreen(recording, Area);
        Assert.Contains(recording.Fills, fill => fill.Width == 94f && fill.Height == 18f && scene.Picker.Bounds.Contains(fill.Center));
    }

    /// <summary>A color given is put first among the colors chosen last, and a picker sharing the history shows it when it opens.</summary>
    [Fact]
    public void AColorGivenIsRemembered()
    {
        var scene = new Scene();
        var other = new ColorPicker { Recent = scene.History };
        ((HStack)((Window)scene.Workspace.Windows.Top).Content).Children.Add(other);
        scene.Draw();
        scene.Press(scene.Picker);
        scene.Press(scene.Palette.Children[0]);
        scene.Press(scene.Done);

        scene.Press(other);

        Assert.Equal(new[] { Red }, scene.History.Colors);
        var row = (ColorPalette)other.Panel.Palettes[1];
        Assert.Equal(8, row.Children.Count);
        Assert.Equal(Red, ((Swatch)row.Children[0]).Color);
        Assert.True(row.Children[0].IsEnabled);
        Assert.False(row.Children[1].IsEnabled);
    }

    /// <summary>Done with the color the picker held puts the panel away, raises nothing and remembers nothing.</summary>
    [Fact]
    public void DoneWithTheSameColorGivesNothing()
    {
        var scene = new Scene();
        scene.Press(scene.Picker);

        scene.Press(scene.Done);

        Assert.False(scene.Picker.IsOpen);
        Assert.Equal(0, scene.Changes);
        Assert.Empty(scene.History.Colors);
    }

    /// <summary>An empty code given makes no color, which the box shows as its edge alone.</summary>
    [Fact]
    public void AnEmptyCodeGivesNoColor()
    {
        var scene = new Scene();
        scene.Press(scene.Picker);
        scene.Panel.Entry = ColorEntry.Hex;
        scene.Draw();

        scene.Type(scene.Code, "");
        scene.Type(scene.Code, " ");
        scene.Workspace.Key(MyKeys.Enter, false, false);
        var recording = new Recording(Area);
        scene.Workspace.DrawScreen(recording, Area);

        Assert.Null(scene.Picker.Color);
        Assert.Equal(1, scene.Changes);
        Assert.Empty(scene.History.Colors);
        Assert.DoesNotContain(recording.Fills, fill => fill.Width == 94f && fill.Height == 18f);
    }

    /// <summary>A picker holding no color opens on white, its color made showing nothing, and its code empty.</summary>
    [Fact]
    public void NoColorOpensOnWhite()
    {
        var scene = new Scene();
        scene.Picker.Color = null;

        scene.Press(scene.Picker);

        Assert.Null(scene.Panel.Color);
        Assert.Equal("", scene.Code.Text);
    }

    /// <summary>With no colors offered first, their row is not shown.</summary>
    [Fact]
    public void NoPaletteShowsNoRow()
    {
        var scene = new Scene();

        scene.Picker.SetPalette(new Color[0]);

        Assert.False(scene.Palette.IsVisible);
    }

    /// <summary>The box takes the keyboard, and Space opens the panel.</summary>
    [Fact]
    public void TheBoxTakesTheKeyboard()
    {
        var scene = new Scene();
        scene.Workspace.Input.Focus(scene.Picker);

        scene.Workspace.Key(MyKeys.Space, false, false);
        scene.Draw();

        Assert.True(scene.Picker.IsOpen);
    }

    /// <summary>A blue picker offering red, green and blue, with a history of eight, in a window on a workspace, counting the colors given.</summary>
    sealed class Scene
    {
        public readonly ColorPicker Picker;
        public readonly ColorHistory History = new ColorHistory(8);
        public readonly Workspace Workspace = new Workspace();

        public Scene()
        {
            Picker = new ColorPicker { Color = Blue, Recent = History };
            Picker.SetPalette(new[] { Red, new Color(0, 255, 0), Blue });
            Picker.ColorChanged += () => Changes++;
            Workspace.Windows.Open(new Window { Content = new HStack { Children = { Picker } } });
            Draw();
        }

        public int Changes { get; private set; }

        /// <summary>Gets what the popup the picker opens holds: the panel, and the Done button under it.</summary>
        VStack Popped => (VStack)((Popup)Workspace.Windows.Top).Content;

        /// <summary>Gets the panel the picker opens.</summary>
        public ColorPanel Panel => Picker.Panel;

        /// <summary>Gets the row of fixed colors.</summary>
        public ColorPalette Palette => (ColorPalette)Panel.Palettes[0];

        /// <summary>Gets the Done button under the panel.</summary>
        public Button Done => (Button)Popped.Children[1];

        /// <summary>Gets the tabs for typing the color.</summary>
        public TabView Entries => (TabView)((HStack)Panel.Children[1]).Children[1];

        /// <summary>Gets the box the code is typed in.</summary>
        public TextBox Code => (TextBox)Entries.Tabs[2].Content;

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
            Press(new Vector2(element.Bounds.X + element.Bounds.Width / 2f, element.Bounds.Y + element.Bounds.Height / 2f));
        }

        public void Press(Vector2 point)
        {
            Workspace.PointerDown(point, PointerButton.Left, 1, false, false);
            Workspace.PointerUp(point, PointerButton.Left);
            Draw();
        }
    }
}
