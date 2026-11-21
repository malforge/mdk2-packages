using Lumel.Controls;
using Lumel.Input;
using Lumel.Layout;
using Lumel.Menus;
using Lumel.Screens;
using Lumel.Styling;
using Lumel.Text;
using Lumel.Viewing;
using Lumel.Windows;
using VRage.Input;
using VRageMath;
using Xunit;

namespace Lumel.Tests;

/// <summary>
///     A workspace used from the keyboard as the game's screens are: anything pressable takes the keyboard, Enter and
///     Space press it, moving sounds, and a focused control looks hovered.
/// </summary>
public class KeyboardTests
{
    static readonly RectangleF Area = new RectangleF(0f, 0f, 800f, 600f);

    /// <summary>Tab with nothing holding the keyboard gives it to the first button, and Enter and Space press it.</summary>
    [Fact]
    public void TabReachesAButtonAndEnterAndSpacePressIt()
    {
        var pressed = 0;
        var (workspace, first, _, heard) = Laid(new RelayCommand(() => pressed++));

        Assert.True(workspace.Key(MyKeys.Tab, false, false));
        Assert.Same(first, workspace.Input.Focused);

        Assert.True(workspace.Key(MyKeys.Enter, false, false));
        Assert.True(workspace.Key(MyKeys.Space, false, false));

        Assert.Equal(2, pressed);
        Assert.Equal(new[] { GameTheme.HoverSound, GameTheme.ClickSound, GameTheme.ClickSound }, heard);
    }

    /// <summary>Down with nothing holding the keyboard goes to the first element, Up to the last, and the arrows move on from there.</summary>
    [Fact]
    public void TheArrowsStartAtAnEndAndMoveOn()
    {
        var (workspace, first, second, _) = Laid(new RelayCommand(() => { }));

        workspace.Key(MyKeys.Up, false, false);
        Assert.Same(second, workspace.Input.Focused);

        workspace.Key(MyKeys.Up, false, false);
        Assert.Same(first, workspace.Input.Focused);

        Assert.False(workspace.Key(MyKeys.Up, false, false));
        Assert.Same(first, workspace.Input.Focused);
    }

    /// <summary>
    ///     A click focuses the button it presses without showing it, so its look goes back to rest once the pointer
    ///     leaves; the next key carries on from it, and the focus it moves shows.
    /// </summary>
    [Fact]
    public void AClickFocusesWithoutShowingAndKeysCarryOn()
    {
        var (workspace, first, second, _) = Laid(new RelayCommand(() => { }));
        var middle = new Vector2(first.Bounds.Center.X, first.Bounds.Center.Y);

        workspace.PointerDown(middle, PointerButton.Left, 1, false, false);
        workspace.PointerUp(middle, PointerButton.Left);
        workspace.Pointer = new Vector2(float.NegativeInfinity);
        workspace.DrawScreen(new Recording(Area), Area);

        Assert.Same(first, workspace.Input.Focused);
        Assert.False(first.IsFocusVisible);
        Assert.Equal(GameColors.Panel, first.Background);

        workspace.Key(MyKeys.Down, false, false);
        workspace.DrawScreen(new Recording(Area), Area);

        Assert.Same(second, workspace.Input.Focused);
        Assert.True(second.IsFocusVisible);
        Assert.Equal(GameColors.PanelHot, second.Background);
    }

    /// <summary>A key used after a click shows the focus the click gave.</summary>
    [Fact]
    public void AKeyShowsTheFocusAClickGave()
    {
        var (workspace, first, _, _) = Laid(new RelayCommand(() => { }));
        var middle = new Vector2(first.Bounds.Center.X, first.Bounds.Center.Y);
        workspace.PointerDown(middle, PointerButton.Left, 1, false, false);
        workspace.PointerUp(middle, PointerButton.Left);

        workspace.Key(MyKeys.Left, false, false);

        Assert.Same(first, workspace.Input.Focused);
        Assert.True(first.IsFocusVisible);
    }

    /// <summary>A click on a text box gives it the keyboard.</summary>
    [Fact]
    public void AClickFocusesATextBox()
    {
        var box = new TextBox { Placement = FixedSize.Width(200f) };
        var workspace = new Workspace().With(new VStack { Children = { box } });
        workspace.DrawScreen(new Recording(Area), Area);
        var middle = new Vector2(box.Bounds.Center.X, box.Bounds.Center.Y);

        workspace.PointerDown(middle, PointerButton.Left, 1, false, false);
        workspace.PointerUp(middle, PointerButton.Left);

        Assert.Same(box, workspace.Input.Focused);
        Assert.True(box.IsFocusVisible);
    }

    /// <summary>Space while typing in a dialog types, and never gives the dialog its answer.</summary>
    [Fact]
    public void SpaceInATextBoxDoesNotAnswerTheDialog()
    {
        var accepted = 0;
        var box = new TextBox();
        var dialog = new Dialog { Content = box, AcceptCommand = new RelayCommand(() => accepted++) };
        var workspace = new Workspace();
        workspace.Windows.Open(dialog);
        workspace.DrawScreen(new Recording(Area), Area);
        workspace.Input.Focus(box);

        Assert.False(workspace.Key(MyKeys.Space, false, false));

        Assert.Equal(0, accepted);
    }

    /// <summary>A focused slider takes Left and Right as steps of its value, and keeps the keyboard.</summary>
    [Fact]
    public void LeftAndRightStepAFocusedSlider()
    {
        var slider = new Slider { Value = 0.5f, Placement = FixedSize.Width(200f) };
        var changes = 0;
        slider.ValueChanged += () => changes++;
        var workspace = new Workspace().With(new VStack { Children = { new Button { Text = "Go", Command = new RelayCommand(() => { }) }, slider } });
        workspace.DrawScreen(new Recording(Area), Area);
        workspace.Input.Focus(slider);

        workspace.Key(MyKeys.Right, false, false);
        workspace.Key(MyKeys.Right, false, false);
        workspace.Key(MyKeys.Left, false, false);

        Assert.Equal(0.6f, slider.Value, 4);
        Assert.Equal(3, changes);
        Assert.Same(slider, workspace.Input.Focused);
    }

    /// <summary>Focus moved by the keys onto something out of a scroll view's sight scrolls it into view.</summary>
    [Fact]
    public void FocusIsScrolledIntoView()
    {
        var column = new VStack();
        for (var i = 0; i < 40; i++)
            column.Children.Add(new Button { Text = "Word " + i, Command = new RelayCommand(() => { }) });

        var scroll = new ScrollView { Content = column, Placement = FixedSize.Size(300f, 200f) };
        var workspace = new Workspace().With(new VStack { Children = { scroll } });
        workspace.DrawScreen(new Recording(Area), Area);

        workspace.Key(MyKeys.Up, false, false);
        workspace.DrawScreen(new Recording(Area), Area);

        var last = workspace.Input.Focused!;
        Assert.Same(column.Children[39], last);
        Assert.True(last.Bounds.Bottom <= scroll.Bounds.Bottom + 0.01f, "the focused button is out of view");
    }

    /// <summary>
    ///     Focus taken by a box in a popup leaves the scroll view its anchor is in where it is, though the box lies
    ///     past the edge of that view.
    /// </summary>
    [Fact]
    public void FocusInAPopupLeavesTheScrollViewAroundItsAnchor()
    {
        var column = new VStack();
        for (var i = 0; i < 40; i++)
            column.Children.Add(new Button { Text = "Word " + i, Command = new RelayCommand(() => { }) });

        var scroll = new ScrollView { Content = column, Placement = FixedSize.Size(300f, 200f) };
        var workspace = new Workspace().With(new VStack { Children = { scroll } });
        workspace.DrawScreen(new Recording(Area), Area);

        var anchor = column.Children[0];
        foreach (var child in column.Children)
        {
            if (child.Bounds.Bottom <= scroll.Bounds.Bottom)
                anchor = child;
        }

        var box = new TextBox { Placement = FixedSize.Width(200f) };
        new Popup { Anchor = anchor, Content = box }.Open();
        workspace.DrawScreen(new Recording(Area), Area);
        Assert.True(box.Bounds.Bottom > scroll.Bounds.Bottom, "the box is inside the scroll view's sight");

        workspace.Input.Focus(box);
        workspace.DrawScreen(new Recording(Area), Area);

        Assert.Same(box, workspace.Input.Focused);
        Assert.Equal(0f, scroll.VerticalOffset);
    }

    /// <summary>
    ///     A menu opened from a focused button takes the keyboard, so the arrows move through it, and gives the keyboard
    ///     back to the button once it closes.
    /// </summary>
    [Fact]
    public void AMenuTakesTheKeyboardAndGivesItBack()
    {
        var copied = 0;
        var item = new MenuItem { Text = "Copy", Command = new RelayCommand(() => copied++) };
        var menu = new Menu { Items = { item } };
        var opener = new Button { Text = "Open", Command = new RelayCommand(() => menu.Open()) };
        menu.Anchor = opener;
        var workspace = new Workspace().With(new VStack { Children = { opener } });
        workspace.DrawScreen(new Recording(Area), Area);

        workspace.Key(MyKeys.Tab, false, false);
        workspace.Key(MyKeys.Enter, false, false);
        Assert.Same(menu, workspace.Input.Focused);

        workspace.DrawScreen(new Recording(Area), Area);
        workspace.Key(MyKeys.Down, false, false);
        workspace.Key(MyKeys.Enter, false, false);
        workspace.DrawScreen(new Recording(Area), Area);

        Assert.Equal(1, copied);
        Assert.False(menu.IsOpen);
        Assert.Same(opener, workspace.Input.Focused);
    }

    /// <summary>Moving through a menu with the keys plays each item's hover sound.</summary>
    [Fact]
    public void MovingThroughAMenuSounds()
    {
        var heard = new Heard();
        var menu = new Menu { Items = { new MenuItem { Text = "Copy", Command = new RelayCommand(() => { }) } } };
        var workspace = new Workspace { Sounds = heard };
        workspace.DrawScreen(new Recording(Area), Area);
        menu.OpenAt(workspace.Windows, new Vector2(100f, 100f));
        workspace.DrawScreen(new Recording(Area), Area);

        workspace.Key(MyKeys.Down, false, false);

        Assert.Equal(new[] { GameTheme.HoverSound }, heard.Played);
    }

    static (Workspace, Button, Button, List<Sound>) Laid(ICommand command)
    {
        var first = new Button { Text = "First", Command = command };
        var second = new Button { Text = "Second", Command = command };
        var heard = new Heard();
        var workspace = new Workspace { Sounds = heard }.With(new VStack { Children = { first, new Label { Text = "Words" }, second } });
        workspace.DrawScreen(new Recording(Area), Area);
        return (workspace, first, second, heard.Played);
    }

    sealed class Heard : IControlSounds
    {
        public List<Sound> Played { get; } = new List<Sound>();

        public void Play(Sound sound) => Played.Add(sound);
    }
}
