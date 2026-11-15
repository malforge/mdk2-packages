using Lumel.Controls;
using Lumel.Input;
using Lumel.Layout;
using Lumel.Screens;
using Lumel.Styling;
using Lumel.Text;
using Lumel.Windows;
using VRageMath;
using Xunit;

namespace Lumel.Tests;

/// <summary>How an element's looks, its style's and its own, give it the values it draws with.</summary>
public class StyleTests
{
    static readonly Color Panel = new Color(41, 54, 62);
    static readonly Color PanelHot = new Color(60, 76, 82);
    static readonly Color Ink = new Color(220, 245, 253);
    static readonly Color Dim = new Color(95, 114, 120);
    static readonly Color Light = new Color(200, 160, 40);
    static readonly Color LightPointerOver = new Color(240, 200, 80);

    static readonly State Uncarried = State.Declare("Uncarried", 500);
    static readonly State Light1 = State.Declare("Light1", 150);
    static readonly State Light2 = State.Declare("Light2", 150);

    static readonly RectangleF Area = new RectangleF(0f, 0f, 400f, 200f);

    /// <summary>A value an element sets in its own look outlasts every change of state and every restyle.</summary>
    [Fact]
    public void AnOwnValueOutlastsChangesOfState()
    {
        var label = new Label { Base = { Color = Light }, Style = new LabelStyle { Base = { Color = Ink }, Disabled = { Background = Dim } } };
        label.Restyle();

        label.IsEnabled = false;
        label.IsEnabled = true;
        label.Restyle();

        Assert.Equal(Light, label.Color);
    }

    /// <summary>What an initializer names first makes no difference: nothing a style does withdraws what was set before it.</summary>
    [Fact]
    public void TheOrderOfAnInitializerMakesNoDifference()
    {
        var style = new LabelStyle { Base = { Color = Ink, FontSize = 20f } };
        var before = new Label { Base = { Font = "Monospace", Padding = new Thickness(5f) }, Style = style };
        var after = new Label { Style = style, Base = { Font = "Monospace", Padding = new Thickness(5f) } };
        before.Restyle();
        after.Restyle();

        foreach (var label in new[] { before, after })
        {
            Assert.Equal(Ink, label.Color);
            Assert.Equal("Monospace", label.Font);
            Assert.Equal(20f, label.FontSize);
            Assert.Equal(new Thickness(5f), label.Padding);
        }
    }

    /// <summary>A state that has ended takes back what it set, where nothing beneath it sets the same.</summary>
    [Fact]
    public void AnEndedStateTakesBackWhatItSet()
    {
        var row = new HStack { Style = Row() };
        row.Restyle();

        row.IsEnabled = false;
        Assert.Equal(Dim, row.Border);

        row.IsEnabled = true;
        Assert.Equal(Color.Transparent, row.Border);
        Assert.Equal(Panel, row.Background);
    }

    /// <summary>A style's look for a state wins over the element's own base look, and the element's own look for that state wins over both.</summary>
    [Fact]
    public void AStatesLookWinsOverTheBaseLooksBeneathIt()
    {
        var row = new HStack { Style = Row(), Base = { Background = Light } };
        row.Restyle();
        Assert.Equal(Light, row.Background);

        ((IInputTarget)row).IsPointerOver = true;
        Assert.Equal(PanelHot, row.Background);

        row.PointerOver.Background = LightPointerOver;
        Assert.Equal(LightPointerOver, row.Background);
    }

    /// <summary>Of two states setting the same thing, the heavier wins, whichever was entered first.</summary>
    [Fact]
    public void TheHeavierStateWins()
    {
        var label = new Label
        {
            Style = new LabelStyle
            {
                [Uncarried] = { Color = Dim },
                PointerOver = { Color = Ink }
            }
        };
        label.Restyle();

        label.Set(Uncarried, true);
        ((IInputTarget)label).IsPointerOver = true;

        Assert.Equal(Dim, label.Color);
    }

    /// <summary>A look for a joining of states beats the looks for the states it is made of, and a heavier state still beats it.</summary>
    [Fact]
    public void AJoiningOfStatesBeatsItsParts()
    {
        var label = new Label
        {
            Style = new LabelStyle
            {
                PointerOver = { Color = Ink },
                [Uncarried] = { Color = Dim },
                [Uncarried | State.PointerOver] = { Color = Light },
                Disabled = { Color = Panel }
            }
        };
        label.Restyle();

        label.Set(Uncarried, true);
        ((IInputTarget)label).IsPointerOver = true;
        Assert.Equal(Light, label.Color);

        label.IsEnabled = false;
        Assert.Equal(Panel, label.Color);
    }

    /// <summary>Of two states of one weight, the one declared first is laid first, so the later declared wins.</summary>
    [Fact]
    public void AtOneWeightTheLaterDeclaredWins()
    {
        var label = new Label { Style = new LabelStyle { [Light2] = { Color = Dim }, [Light1] = { Color = Ink } } };
        label.Restyle();

        label.Set(Light2, true);
        label.Set(Light1, true);

        Assert.Equal(Dim, label.Color);
    }

    /// <summary>An element that cannot be used is never in the pointer-over state, whatever the pointer rests on.</summary>
    [Fact]
    public void ADisabledElementIsNeverPointerOver()
    {
        var row = new HStack { Style = new StackStyle { PointerOver = { Background = PanelHot } } };
        row.Restyle();

        row.IsEnabled = false;
        ((IInputTarget)row).IsPointerOver = true;

        Assert.False(row.Is(State.PointerOver));
        Assert.Equal(Color.Transparent, row.Background);
    }

    /// <summary>A state an application declares can be set on a stock control, and any style naming it shows it.</summary>
    [Fact]
    public void AnApplicationsStateWorksOnAStockControl()
    {
        var words = new Label { Style = new LabelStyle { Base = { Color = Ink }, [Uncarried] = { Color = Dim } } };
        words.Restyle();

        words.Set(Uncarried, true);
        Assert.Equal(Dim, words.Color);
        Assert.True(words.Is(Uncarried));

        words.Set(Uncarried, false);
        Assert.Equal(Ink, words.Color);
    }

    /// <summary>A control in a state by facts of its own shows that state's look, and leaves it when the facts change.</summary>
    [Fact]
    public void AControlsOwnFactsGiveItAState()
    {
        var chip = new Chip { Style = new ChipStyle { Base = { Background = Ink }, [Chip.Still] = { Background = Dim } } };
        chip.Restyle();
        Assert.Equal(Dim, chip.Background);

        chip.Command = new RelayCommand(() => { });
        Assert.Equal(Ink, chip.Background);

        chip.Command = null;
        Assert.Equal(Dim, chip.Background);
    }

    /// <summary>A swatch's look fades the color it shows, which stays its own.</summary>
    [Fact]
    public void ASwatchsLookFadesItsColor()
    {
        var swatch = new Swatch
        {
            Color = Light,
            Placement = FixedSize.Size(40f, 20f),
            Style = new SwatchStyle { [Uncarried] = { Opacity = 0.4f } }
        };
        var easel = new Easel(new VStack { Children = { swatch } }, 400f, 200f);
        Assert.Equal(255, easel.Sprites.Last().Color.A);

        swatch.Set(Uncarried, true);
        easel.Draw(400f, 200f);

        Assert.Equal(Light, swatch.Color);
        Assert.Equal(102, easel.Sprites.Last().Color.A);
    }

    /// <summary>A style based on another is everything the other is, with its own looks laid over it, and leaves the other as it was.</summary>
    [Fact]
    public void AStyleBasedOnAnotherLaysItsOwnOverIt()
    {
        var plain = Row();
        var chosen = new StackStyle(plain) { Base = { Background = Ink } };

        var first = new HStack { Style = plain };
        var second = new HStack { Style = chosen };
        first.Restyle();
        second.Restyle();
        Assert.Equal(Panel, first.Background);
        Assert.Equal(Ink, second.Background);

        ((IInputTarget)second).IsPointerOver = true;
        Assert.Equal(PanelHot, second.Background);
    }

    /// <summary>A style based on nothing is refused, which is what a style declared above its base receives.</summary>
    [Fact]
    public void AStyleBasedOnNothingIsRefused()
    {
        Assert.Throws<ArgumentNullException>(() => new ElementStyle(null!));
    }

    /// <summary>A style for another kind of control is refused when it is set through a reference to a kind that takes any style.</summary>
    [Fact]
    public void AStyleForAnotherKindIsRefused()
    {
        Element words = new Label();

        Assert.Throws<ArgumentException>(() => words.Style = new ElementStyle());
    }

    /// <summary>A control's constructor works out no look, so a derived control is never reached before its own constructor has run.</summary>
    [Fact]
    public void AConstructorWorksOutNoLook()
    {
        var chip = new Chip();

        Assert.Equal(0, chip.Applied);
        chip.Restyle();
        Assert.Equal(1, chip.Applied);
    }

    /// <summary>An element's look is worked out at its first measure, from the theme of the host it is in.</summary>
    [Fact]
    public void AnElementTakesItsHostsThemeAtItsFirstMeasure()
    {
        var button = new Button { Text = "Go" };
        var stage = new Stage { Content = new VStack { Children = { button } }, Theme = Theme(Light) };

        stage.Draw(new Recording(Area), Area);

        Assert.Equal(Light, button.Background);
    }

    /// <summary>The same kind of control looks as each host's theme says, side by side.</summary>
    [Fact]
    public void EachHostShowsItsOwnTheme()
    {
        var first = new Button { Text = "Go" };
        var second = new Button { Text = "Go" };
        var screen = new Stage { Content = new VStack { Children = { first } }, Theme = Theme(Light) };
        var panel = new Stage { Content = new VStack { Children = { second } }, Theme = Theme(Ink) };

        screen.Draw(new Recording(Area), Area);
        panel.Draw(new Recording(Area), Area);

        Assert.Equal(Light, first.Background);
        Assert.Equal(Ink, second.Background);
    }

    /// <summary>A control moved to another host looks as that host's theme says.</summary>
    [Fact]
    public void AMovedControlTakesItsNewHostsTheme()
    {
        var button = new Button { Text = "Go" };
        var screen = new Stage { Content = new VStack { Children = { button } }, Theme = Theme(Light) };
        screen.Draw(new Recording(Area), Area);

        var panel = new Stage { Content = new VStack(), Theme = Theme(Ink) };
        ((VStack)panel.Content).Children.Add(button);
        panel.Draw(new Recording(Area), Area);

        Assert.Equal(Ink, button.Background);
    }

    /// <summary>A control with no style takes the theme's style for the nearest kind it is, and its own kind's where the theme has one.</summary>
    [Fact]
    public void AThemeGivesTheNearestKind()
    {
        var withButton = Theme(Light);
        var withChip = new Theme(withButton) { new ChipStyle { Base = { Background = Ink } } };
        var first = new Chip();
        var second = new Chip();
        var plain = new Button();
        var a = new Stage { Content = new VStack { Children = { first } }, Theme = withButton };
        var b = new Stage { Content = new VStack { Children = { second, plain } }, Theme = withChip };

        a.Draw(new Recording(Area), Area);
        b.Draw(new Recording(Area), Area);

        Assert.Equal(Light, first.Background);
        Assert.Equal(Ink, second.Background);
        Assert.Equal(Light, plain.Background);
    }

    /// <summary>A window opened over a workspace takes the workspace's theme, as what it covers does.</summary>
    [Fact]
    public void AWindowTakesItsWorkspacesTheme()
    {
        var button = new Button { Text = "Go" };
        var workspace = new Workspace { Theme = Theme(Light) };
        workspace.Windows.Open(new Window { Content = button });

        workspace.DrawScreen(new Recording(Area), Area);

        Assert.Equal(Light, button.Background);
    }

    /// <summary>A window open before its workspace is given a theme takes the theme when it is given.</summary>
    [Fact]
    public void AnOpenWindowTakesALaterTheme()
    {
        var button = new Button { Text = "Go" };
        var workspace = new Workspace();
        workspace.Windows.Open(new Window { Content = button });
        workspace.DrawScreen(new Recording(Area), Area);

        workspace.Theme = Theme(Ink);
        workspace.DrawScreen(new Recording(Area), Area);

        Assert.Equal(Ink, button.Background);
    }

    /// <summary>A control's own value, set where it is placed, wins over its host theme's base.</summary>
    [Fact]
    public void AnOwnValueWinsOverTheHostsTheme()
    {
        var button = new Button { Text = "Go", Base = { Background = Dim } };
        var stage = new Stage { Content = new VStack { Children = { button } }, Theme = Theme(Light) };

        stage.Draw(new Recording(Area), Area);

        Assert.Equal(Dim, button.Background);
    }

    /// <summary>A label with a font size of 24 is one line 24 units tall.</summary>
    [Fact]
    public void AFontSizeIsTheHeightOfALine()
    {
        var label = new Label { Text = "Airlock", Base = { FontSize = 24f } };
        _ = new Easel(new VStack { Children = { label } }, 400f, 200f);

        Assert.Equal(24f, label.DesiredSize.Y, 3);
    }

    /// <summary>A change of state that only changes colors measures no text.</summary>
    [Fact]
    public void AChangeOfStateInColorOnlyMeasuresNoText()
    {
        var row = new HStack { Style = Row(), Children = { new Label { Text = "Done" } } };
        var easel = new Easel(new VStack { Children = { row } }, 400f, 200f);
        var settled = easel.Measured;

        ((IInputTarget)row).IsPointerOver = true;
        easel.Draw(400f, 200f);
        ((IInputTarget)row).IsPointerOver = false;
        easel.Draw(400f, 200f);

        Assert.Equal(settled, easel.Measured);
    }

    static StackStyle Row()
    {
        return new StackStyle
        {
            Base = { Background = Panel },
            PointerOver = { Background = PanelHot },
            Disabled = { Background = Color.Transparent, Border = Dim }
        };
    }

    static ITheme Theme(Color button)
    {
        return new Theme(GameTheme.Theme) { new ButtonStyle(GameTheme.Button) { Base = { Background = button } } };
    }

    /// <summary>A control derived from a button, which counts how often it works out its look from a list its constructor makes.</summary>
    class Chip : Button
    {
        public static readonly State Still = State.Declare("Still", 150);

        readonly List<int> _applied;

        public override State States => Command == null ? base.States | Still : base.States;

        public Chip()
        {
            _applied = new List<int>();
        }

        public int Applied => _applied.Count;

        protected override bool Fits(IStyle style)
        {
            return style is IStyle<Chip>;
        }

        protected override void Apply(ElementLook look)
        {
            base.Apply(look);
            _applied.Add(1);
        }
    }

    /// <summary>A chip's own kind of style, so a theme can hold one apart from a button's.</summary>
    class ChipStyle : Style<Chip, TextLook>
    {
    }
}
