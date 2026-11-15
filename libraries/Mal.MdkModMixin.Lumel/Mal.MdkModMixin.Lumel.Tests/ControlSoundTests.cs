using Lumel.Controls;
using Lumel.Input;
using Lumel.Layout;
using Lumel.Screens;
using Lumel.Styling;
using Lumel.Text;
using Lumel.Windows;
using VRage.Input;
using VRageMath;
using Xunit;

namespace Lumel.Tests;

/// <summary>
///     A workspace's controls are heard as the pointer comes onto something pressable and as a press, by pointer or key,
///     is about to run its command, each with the sound its looks or its own logic give it.
/// </summary>
public class ControlSoundTests
{
    static readonly RectangleF Area = new RectangleF(0f, 0f, 800f, 600f);
    static readonly Sound Chime = new Sound("Chime");

    /// <summary>The pointer coming onto a button plays the game's hover once, and staying on it plays nothing more.</summary>
    [Fact]
    public void ComingOntoAButtonIsHeardOnce()
    {
        var (workspace, button, _, heard) = Laid(new RelayCommand(() => { }));

        Rest(workspace, button);
        Rest(workspace, button);

        Assert.Equal(new[] { GameTheme.HoverSound }, heard);
    }

    /// <summary>Coming onto something with no command, or one that cannot be used, is not heard.</summary>
    [Fact]
    public void ComingOntoWhatCannotBePressedIsNotHeard()
    {
        var (workspace, button, label, heard) = Laid(new RelayCommand(() => { }) { CanExecute = false });

        Rest(workspace, label);
        Rest(workspace, button);

        Assert.Empty(heard);
    }

    /// <summary>A press that carries out a command plays the game's click.</summary>
    [Fact]
    public void APressCarryingOutACommandClicks()
    {
        var pressed = 0;
        var (workspace, button, _, heard) = Laid(new RelayCommand(() => pressed++));

        Press(workspace, button);

        Assert.Equal(1, pressed);
        Assert.Equal(new[] { GameTheme.ClickSound }, heard);
    }

    /// <summary>A sound a look sets replaces the style's, and <see cref="Sound.None" /> silences it.</summary>
    [Fact]
    public void ALooksSoundReplacesTheStylesAndNoneSilencesIt()
    {
        var (workspace, button, _, heard) = Laid(new RelayCommand(() => { }));
        button.Base.ClickSound = Chime;
        button.Base.HoverSound = Sound.None;

        Rest(workspace, button);
        Press(workspace, button);

        Assert.Equal(new[] { Chime }, heard);
    }

    /// <summary>An element's own logic picks its sound, and returning null leaves the sound to its looks.</summary>
    [Fact]
    public void AnElementsLogicPicksItsSound()
    {
        var (workspace, button, _, heard) = Laid(new RelayCommand(() => { }));
        button.FnSound = sound => sound == ControlSound.Click ? Chime : null;

        Rest(workspace, button);
        Press(workspace, button);

        Assert.Equal(new[] { GameTheme.HoverSound, Chime }, heard);
    }

    /// <summary>The click plays before the command runs, so a sound the command plays follows it.</summary>
    [Fact]
    public void TheClickComesBeforeWhatTheCommandDoes()
    {
        var (workspace, button, _, heard) = Laid(new RelayCommand(() => { }));
        button.Command = new RelayCommand(() => heard.Add(Chime));

        Press(workspace, button);

        Assert.Equal(new[] { GameTheme.ClickSound, Chime }, heard);
    }

    /// <summary>Enter clicks as the default answer's button does, and Escape as the Cancel button does, shown or not.</summary>
    [Fact]
    public void KeysClickAsTheButtonsTheyPress()
    {
        var heard = new Heard();
        var said = new List<string>();
        var box = new MessageBox
        {
            YesCommand = new RelayCommand(() => said.Add("yes")),
            CancelCommand = new RelayCommand(() => said.Add("cancel")),
            DefaultButton = MessageBoxAnswer.Yes,
            ShowsCancel = false,
            FnAnswerSound = (answer, _) => answer == MessageBoxAnswer.Cancel ? Chime : null
        };
        var workspace = new Workspace { Sounds = heard };
        workspace.Windows.Open(box);
        workspace.DrawScreen(new Recording(Area), Area);

        Assert.True(workspace.Key(MyKeys.Enter, false, false));
        Assert.True(workspace.Key(MyKeys.Escape, false, false));

        Assert.Equal(new[] { "yes", "cancel" }, said);
        Assert.Equal(new[] { GameTheme.ClickSound, Chime }, heard.Played);
    }

    /// <summary>A key whose answer has nothing to run is silent, and Escape is then left to the workspace.</summary>
    [Fact]
    public void AKeyWithNothingToRunIsSilent()
    {
        var heard = new Heard();
        var box = new MessageBox { YesCommand = new RelayCommand(() => { }) { CanExecute = false }, DefaultButton = MessageBoxAnswer.Yes };
        var workspace = new Workspace { Sounds = heard };
        workspace.Windows.Open(box);
        workspace.DrawScreen(new Recording(Area), Area);

        Assert.False(workspace.Key(MyKeys.Enter, false, false));
        Assert.True(workspace.Key(MyKeys.Escape, false, false), "Escape did not close the box");

        Assert.Empty(heard.Played);
        Assert.Equal(0, workspace.Windows.Count);
    }

    /// <summary>A message box picks each answer's sound, and an answer it leaves alone plays its button's own.</summary>
    [Fact]
    public void AMessageBoxPicksEachAnswersSound()
    {
        var heard = new Heard();
        var yes = 0;
        var box = new MessageBox
        {
            YesCommand = new RelayCommand(() => yes++),
            NoCommand = new RelayCommand(() => { }),
            FnAnswerSound = (answer, _) => answer == MessageBoxAnswer.Yes ? Sound.None : null
        };
        var workspace = new Workspace { Sounds = heard };
        workspace.DrawScreen(new Recording(Area), Area);
        workspace.Windows.Open(box);
        workspace.DrawScreen(new Recording(Area), Area);
        var buttons = Buttons(box);

        Press(workspace, buttons.Single(b => b.Text == "Yes"));
        Press(workspace, buttons.Single(b => b.Text == "No"));

        Assert.Equal(1, yes);
        Assert.Equal(new[] { GameTheme.ClickSound }, heard.Played);
    }

    /// <summary>A workspace told not to play sounds is silent.</summary>
    [Fact]
    public void ToldNotToPlaySoundsItIsSilent()
    {
        var (workspace, button, _, heard) = Laid(new RelayCommand(() => { }));
        workspace.PlaysSounds = false;

        Rest(workspace, button);
        Press(workspace, button);

        Assert.Empty(heard);
    }

    /// <summary>A workspace with nothing to play sounds is silent.</summary>
    [Fact]
    public void WithNoSoundsItIsSilent()
    {
        var (workspace, button, _, _) = Laid(new RelayCommand(() => { }));
        workspace.Sounds = null;

        Rest(workspace, button);
        Press(workspace, button);
    }

    static (Workspace, Button, Label, List<Sound>) Laid(ICommand command)
    {
        var button = new Button { Text = "Go", Command = command };
        var label = new Label { Text = "Words" };
        var heard = new Heard();
        var workspace = new Workspace { Sounds = heard }.With(new VStack { Children = { label, button } });
        workspace.DrawScreen(new Recording(Area), Area);
        return (workspace, button, label, heard.Played);
    }

    /// <summary>Rests the pointer on the middle of <paramref name="element" /> for a frame.</summary>
    static void Rest(Workspace workspace, Element element)
    {
        workspace.Pointer = Middle(element);
        workspace.DrawScreen(new Recording(Area), Area);
    }

    /// <summary>Presses and releases the left button on the middle of <paramref name="element" />.</summary>
    static void Press(Workspace workspace, Element element)
    {
        workspace.PointerDown(Middle(element), PointerButton.Left, 1, false, false);
        workspace.PointerUp(Middle(element), PointerButton.Left);
    }

    static Vector2 Middle(Element element)
    {
        return new Vector2(element.Bounds.X + element.Bounds.Width / 2f, element.Bounds.Y + element.Bounds.Height / 2f);
    }

    static List<Button> Buttons(Element element)
    {
        var found = new List<Button>();
        if (element is Button button && button.IsVisible)
            found.Add(button);

        for (var i = 0; i < element.VisualChildCount; i++)
            found.AddRange(Buttons(element.GetVisualChild(i)));

        return found;
    }

    sealed class Heard : IControlSounds
    {
        public List<Sound> Played { get; } = new List<Sound>();

        public void Play(Sound sound) => Played.Add(sound);
    }
}
