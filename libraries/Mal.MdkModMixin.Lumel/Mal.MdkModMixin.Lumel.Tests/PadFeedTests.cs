using Lumel.Controls;
using Lumel.Input;
using Lumel.Layout;
using Lumel.Screens;
using Lumel.Styling;
using VRageMath;
using Xunit;

namespace Lumel.Tests;

/// <summary>
///     A frame's gamepad handed to a workspace as the game's screens take it: something always has the keyboard, the
///     directions move it and repeat, A presses, and the triggers switch tabs.
/// </summary>
public class PadFeedTests
{
    static readonly RectangleF Area = new RectangleF(0f, 0f, 800f, 600f);

    /// <summary>A gamepad that is not the last thing used passes nothing.</summary>
    [Fact]
    public void APadNotInUsePassesNothing()
    {
        var (workspace, _, _, _) = Laid();
        var pad = new Pad { IsLastUsed = false, Pressed = { PadControl.Down } };

        Assert.False(new PadFeed().Dispatch(pad, workspace, 0));
        Assert.Null(workspace.Input.Focused);
    }

    /// <summary>Using the gamepad gives the keyboard to the first element without a sound, and the directions move it on with one.</summary>
    [Fact]
    public void ThePadStartsAtTheFirstAndMovesOn()
    {
        var (workspace, first, second, heard) = Laid();
        var feed = new PadFeed();

        feed.Dispatch(new Pad(), workspace, 0);
        Assert.Same(first, workspace.Input.Focused);
        Assert.Empty(heard.Played);

        feed.Dispatch(new Pad { Pressed = { PadControl.Down } }, workspace, 1);
        Assert.Same(second, workspace.Input.Focused);
        Assert.Equal(new[] { GameTheme.HoverSound }, heard.Played);
    }

    /// <summary>A held direction repeats after half a second, and then every tenth of a second.</summary>
    [Fact]
    public void AHeldDirectionRepeatsAsTheGamesDoes()
    {
        var column = new VStack();
        for (var i = 0; i < 10; i++)
            column.Children.Add(new Button { Text = "Word " + i, Command = new RelayCommand(() => { }) });

        var workspace = new Workspace().With(column);
        workspace.DrawScreen(new Recording(Area), Area);
        var feed = new PadFeed();
        feed.Dispatch(new Pad(), workspace, 0);

        feed.Dispatch(new Pad { Pressed = { PadControl.Down } }, workspace, 0);
        for (var tick = 1; tick <= 42; tick++)
            feed.Dispatch(new Pad { Held = { PadControl.Down } }, workspace, tick);

        // The press, then repeats at 30, 36 and 42.
        Assert.Same(column.Children[4], workspace.Input.Focused);
    }

    /// <summary>A presses what has the keyboard.</summary>
    [Fact]
    public void APresses()
    {
        var pressed = 0;
        var button = new Button { Text = "Go", Command = new RelayCommand(() => pressed++) };
        var workspace = new Workspace().With(new VStack { Children = { button } });
        workspace.DrawScreen(new Recording(Area), Area);

        new PadFeed().Dispatch(new Pad { Pressed = { PadControl.Accept } }, workspace, 0);

        Assert.Equal(1, pressed);
    }

    /// <summary>The triggers switch to the next and the previous tab, around the ends.</summary>
    [Fact]
    public void TheTriggersSwitchTabs()
    {
        var view = new TabView
        {
            Tabs =
            {
                new Tab { Header = "One", Content = new Button { Text = "A", Command = new RelayCommand(() => { }) } },
                new Tab { Header = "Two" },
                new Tab { Header = "Three" }
            }
        };
        var workspace = new Workspace().With(view);
        workspace.DrawScreen(new Recording(Area), Area);
        var feed = new PadFeed();
        feed.Dispatch(new Pad(), workspace, 0);

        feed.Dispatch(new Pad { Pressed = { PadControl.NextPage } }, workspace, 1);
        Assert.Equal(1, view.SelectedIndex);

        feed.Dispatch(new Pad { Pressed = { PadControl.PreviousPage } }, workspace, 2);
        feed.Dispatch(new Pad { Pressed = { PadControl.PreviousPage } }, workspace, 3);
        Assert.Equal(2, view.SelectedIndex);
    }

    /// <summary>While the tips follow the keyboard, the element that has it shows its tip once it has rested there.</summary>
    [Fact]
    public void TheTipFollowsTheKeyboard()
    {
        var (workspace, first, _, _) = Laid();
        first.Tip = new Tip("Opens the vents.");
        first.Tip.Base.ShowDelay = 10;
        workspace.TipsFollowFocus = true;
        workspace.Input.Focus(first);

        workspace.DrawScreen(new Recording(Area) { Now = 100 }, Area);
        workspace.DrawScreen(new Recording(Area) { Now = 110 }, Area);

        Assert.Same(first.Tip, workspace.ShownTip);
        Assert.Same(first, first.Tip.Owner);
    }

    static (Workspace, Button, Button, Heard) Laid()
    {
        var first = new Button { Text = "First", Command = new RelayCommand(() => { }) };
        var second = new Button { Text = "Second", Command = new RelayCommand(() => { }) };
        var heard = new Heard();
        var workspace = new Workspace { Sounds = heard }.With(new VStack { Children = { first, second } });
        workspace.DrawScreen(new Recording(Area), Area);
        return (workspace, first, second, heard);
    }

    /// <summary>A gamepad whose controls are whatever the test says, last used unless it says otherwise.</summary>
    sealed class Pad : IGamepad
    {
        public HashSet<PadControl> Pressed { get; } = new HashSet<PadControl>();

        public HashSet<PadControl> Held { get; } = new HashSet<PadControl>();

        public bool IsLastUsed { get; set; } = true;

        public bool IsNewPressed(PadControl control) => Pressed.Contains(control);

        public bool IsHeld(PadControl control) => Pressed.Contains(control) || Held.Contains(control);
    }

    sealed class Heard : IControlSounds
    {
        public List<Sound> Played { get; } = new List<Sound>();

        public void Play(Sound sound) => Played.Add(sound);
    }
}
