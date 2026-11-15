using Lumel.Controls;
using Lumel.Input;
using Lumel.Layout;
using Lumel.Screens;
using Lumel.Text;
using Lumel.Windows;
using VRage.Input;
using Xunit;

namespace Lumel.Tests;

/// <summary>
///     A frame's keys handed to a workspace: Enter, Tab and the arrows always, typing and editing while something holds
///     the keyboard, and Escape closing what nothing took it for.
/// </summary>
public class KeyFeedTests
{
    /// <summary>Enter reaches the dialog on top with nothing holding the keyboard.</summary>
    [Fact]
    public void EnterReachesADialogWithNothingFocused()
    {
        var workspace = new Workspace().With(new VStack());
        var accepted = 0;
        workspace.Windows.Open(new Dialog { AcceptCommand = new RelayCommand(() => accepted++) });

        new KeyFeed().Dispatch(new Keyboard { Pressed = { MyKeys.Enter } }, workspace, 0);

        Assert.Equal(1, accepted);
    }

    /// <summary>What is typed goes to the focused box, and to nothing while nothing holds the keyboard.</summary>
    [Fact]
    public void TypingReachesOnlyAFocusedBox()
    {
        var box = new TextBox();
        var workspace = new Workspace().With(new VStack { Children = { box } });
        var feed = new KeyFeed();

        Assert.False(feed.Dispatch(new Keyboard { Text = "ab" }, workspace, 0));
        Assert.Equal("", box.Text);

        workspace.Input.Focus(box);
        Assert.True(feed.Dispatch(new Keyboard { Text = "ab" }, workspace, 1));
        Assert.Equal("ab", box.Text);
    }

    /// <summary>Tab with nothing holding the keyboard gives it to the first element that can take it, and then moves it on.</summary>
    [Fact]
    public void TabStartsAtTheFirstAndMovesOn()
    {
        var first = new TextBox();
        var second = new TextBox();
        var workspace = new Workspace().With(new VStack { Children = { first, second } });
        var feed = new KeyFeed();

        feed.Dispatch(new Keyboard { Pressed = { MyKeys.Tab } }, workspace, 0);
        Assert.Same(first, workspace.Input.Focused);

        feed.Dispatch(new Keyboard { Pressed = { MyKeys.Tab } }, workspace, 1);
        Assert.Same(second, workspace.Input.Focused);
    }

    /// <summary>Space reaches what holds the keyboard, and presses it.</summary>
    [Fact]
    public void SpacePressesWhatHoldsTheKeyboard()
    {
        var pressed = 0;
        var button = new Button { Text = "Go", Command = new RelayCommand(() => pressed++) };
        var workspace = new Workspace().With(new VStack { Children = { button } });
        workspace.Input.Focus(button);

        new KeyFeed().Dispatch(new Keyboard { Pressed = { MyKeys.Space } }, workspace, 0);

        Assert.Equal(1, pressed);
    }

    /// <summary>Tab with Shift held moves the keyboard back.</summary>
    [Fact]
    public void TabWithShiftMovesBack()
    {
        var first = new TextBox();
        var second = new TextBox();
        var workspace = new Workspace().With(new VStack { Children = { first, second } });
        workspace.Input.Focus(second);

        new KeyFeed().Dispatch(new Keyboard { Pressed = { MyKeys.Tab }, IsShiftHeld = true }, workspace, 0);

        Assert.Same(first, workspace.Input.Focused);
    }

    /// <summary>With Ctrl held, the shortcut goes to the box and the letter is not typed.</summary>
    [Fact]
    public void WithCtrlHeldAShortcutGoesInPlaceOfTyping()
    {
        var box = new TextBox { Text = "abc" };
        var workspace = new Workspace().With(new VStack { Children = { box } });
        workspace.Input.Focus(box);

        new KeyFeed().Dispatch(new Keyboard { IsCtrlHeld = true, Pressed = { MyKeys.A }, Text = "a" }, workspace, 0);

        Assert.Equal("abc", box.Text);
        Assert.Equal("abc", box.SelectedText);
    }

    /// <summary>An editing key reaches the focused box, and counts as typing.</summary>
    [Fact]
    public void AnEditingKeyReachesTheBox()
    {
        var box = new TextBox { Text = "abc" };
        var workspace = new Workspace().With(new VStack { Children = { box } });
        workspace.Input.Focus(box);
        box.MoveCaret(3, false);

        var typed = new KeyFeed().Dispatch(new Keyboard { Pressed = { MyKeys.Back } }, workspace, 0);

        Assert.True(typed);
        Assert.Equal("ab", box.Text);
    }

    /// <summary>Escape closes the window on top and keeps the screen, one window at a time; with nothing open, the screen may close.</summary>
    [Fact]
    public void EscapeClosesTheTopWindowAndThenTheScreen()
    {
        var workspace = new Workspace().With(new VStack());
        var window = new Window();
        workspace.Open(window);
        var feed = new KeyFeed();
        var keyboard = new Keyboard();

        Assert.False(feed.Escape(keyboard, workspace));
        Assert.False(window.IsOpen);
        Assert.False(feed.Escape(keyboard, workspace));
        Assert.Equal(0, workspace.Windows.Count);
        Assert.True(feed.Escape(keyboard, workspace));
    }

    /// <summary>Any key that went down reaches the window on top, F1 and a letter with nothing holding the keyboard among them.</summary>
    [Fact]
    public void EveryKeyReachesTheWindow()
    {
        var workspace = new Workspace().With(new VStack());
        var window = new Listening();
        workspace.Open(window);

        new KeyFeed().Dispatch(new Keyboard { Pressed = { MyKeys.F1, MyKeys.Q } }, workspace, 0);

        Assert.Equal(new[] { MyKeys.F1, MyKeys.Q }, window.Heard);
    }

    /// <summary>A key held since an earlier frame is not passed again, nor are Escape, the pointer's buttons and the modifiers.</summary>
    [Fact]
    public void OnlyKeysJustDownArePassed()
    {
        var workspace = new Workspace().With(new VStack());
        var window = new Listening();
        workspace.Open(window);

        new KeyFeed().Dispatch(new Keyboard { Held = { MyKeys.F1 }, Pressed = { MyKeys.Escape, MyKeys.LeftButton, MyKeys.LeftShift, MyKeys.RightControl, MyKeys.Alt } }, workspace, 0);

        Assert.Empty(window.Heard);
    }

    /// <summary>A window that records the keys offered to it.</summary>
    sealed class Listening : Window
    {
        public List<MyKeys> Heard { get; } = new List<MyKeys>();

        protected override bool OnKeyDown(InputRoot input, MyKeys key, bool shift, bool ctrl)
        {
            Heard.Add(key);
            return true;
        }
    }

    /// <summary>A keyboard whose keys and typing are whatever the test says.</summary>
    sealed class Keyboard : IKeyboard
    {
        public HashSet<MyKeys> Pressed { get; } = new HashSet<MyKeys>();

        public string Text { get; set; } = "";

        public bool IsShiftHeld { get; set; }

        public bool IsCtrlHeld { get; set; }

        public int TypedCount => Text.Length;

        public bool IsNewKeyPressed(MyKeys key) => Pressed.Contains(key);

        public HashSet<MyKeys> Held { get; } = new HashSet<MyKeys>();

        public bool IsKeyPressed(MyKeys key) => Pressed.Contains(key) || Held.Contains(key);

        public void ListHeldKeys(List<MyKeys> keys)
        {
            keys.Clear();
            keys.AddRange(Pressed);
            keys.AddRange(Held);
        }

        public char Typed(int index) => Text[index];
    }
}
