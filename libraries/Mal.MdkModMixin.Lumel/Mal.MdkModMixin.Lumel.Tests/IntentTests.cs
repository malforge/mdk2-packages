using Lumel.Input;
using Lumel.Layout;
using Lumel.Screens;
using Lumel.Text;
using Lumel.Windows;
using VRage.Input;
using Xunit;

namespace Lumel.Tests;

/// <summary>A key goes first to what holds the keyboard as the key, and then, where nothing took it, as what it means.</summary>
public class IntentTests
{
    /// <summary>With nothing holding the keyboard, Enter accepts the dialog on top and Escape cancels it.</summary>
    [Fact]
    public void EnterAndEscapeAnswerTheDialogOnTop()
    {
        var workspace = new Workspace().With(new VStack());
        var said = "";
        workspace.Windows.Open(new Dialog { AcceptCommand = new RelayCommand(() => said += "yes"), CancelCommand = new RelayCommand(() => said += "no") });

        Assert.True(workspace.Key(MyKeys.Enter, false, false));
        Assert.True(workspace.Key(MyKeys.Escape, false, false));
        Assert.Equal("yesno", said);
    }

    /// <summary>A box that takes Enter itself keeps it from the dialog around it; a box with nothing to submit lets it through.</summary>
    [Theory]
    [InlineData(true, "submitted")]
    [InlineData(false, "accepted")]
    public void TheKeyComesBeforeWhatItMeans(bool submits, string expected)
    {
        var workspace = new Workspace().With(new VStack());
        var said = "";
        var box = new TextBox();
        if (submits)
            box.Submit = new RelayCommand(() => said = "submitted");

        workspace.Windows.Open(new Dialog { AcceptCommand = new RelayCommand(() => said = "accepted"), Content = box });
        workspace.Input.Focus(box);

        workspace.Key(MyKeys.Enter, false, false);

        Assert.Equal(expected, said);
    }

    /// <summary>With nothing holding the keyboard, the window on top hears what a key means.</summary>
    [Fact]
    public void TheWindowOnTopHearsWhatAKeyMeans()
    {
        var window = new ListeningWindow();
        var workspace = new Workspace().With(new VStack());
        workspace.Open(window);

        Assert.True(workspace.Key(MyKeys.Escape, false, false));
        Assert.Equal(new[] { Intent.Cancel }, window.Heard);
    }

    /// <summary>
    ///     A cancel nobody takes closes the window on top, one at a time, from a box that holds the keyboard too; with
    ///     none open, nothing took it, which leaves the screen to close.
    /// </summary>
    [Fact]
    public void ACancelNobodyTakesClosesTheTopWindowAndThenTheScreen()
    {
        var box = new TextBox();
        var workspace = new Workspace().With(new VStack { Children = { box } });
        var below = workspace.Windows.Top;
        var window = new Window();
        workspace.Open(window);

        Assert.True(workspace.Key(MyKeys.Escape, false, false));
        Assert.False(workspace.Windows.IsOpen(window));

        workspace.Input.Focus(box);
        Assert.True(workspace.Key(MyKeys.Escape, false, false));
        Assert.False(workspace.Windows.IsOpen(below));
        Assert.Null(workspace.Input.Focused);

        Assert.False(workspace.Key(MyKeys.Escape, false, false));
    }

    /// <summary>A modal window opening takes the keyboard from what lies beneath it.</summary>
    [Fact]
    public void AModalWindowTakesTheKeyboardFromBeneath()
    {
        var box = new TextBox();
        var workspace = new Workspace().With(new VStack { Children = { box } });
        workspace.Input.Focus(box);

        workspace.Windows.Open(new Dialog());

        Assert.Null(workspace.Input.Focused);
    }

    /// <summary>What a key means is the host's to change.</summary>
    [Fact]
    public void WhatAKeyMeansCanBeChanged()
    {
        var workspace = new Workspace().With(new VStack());
        var accepted = 0;
        workspace.Windows.Open(new Dialog { AcceptCommand = new RelayCommand(() => accepted++) });

        workspace.Input.Intents[MyKeys.Space] = Intent.Accept;
        workspace.Input.Intents.Remove(MyKeys.Enter);
        workspace.Key(MyKeys.Space, false, false);
        workspace.Key(MyKeys.Enter, false, false);

        Assert.Equal(1, accepted);
    }

    sealed class ListeningWindow : Window
    {
        public List<Intent> Heard { get; } = new List<Intent>();

        protected override bool OnIntent(InputRoot input, Intent intent)
        {
            Heard.Add(intent);
            return true;
        }
    }
}
