using Lumel.Controls;
using Lumel.Input;
using Lumel.Windows;
using VRageMath;
using Xunit;
using Lumel.Styling;
using Lumel.Text;

namespace Lumel.Tests;

/// <summary>A message box: its standard buttons in the game's order, each with whatever words it is given, and Enter and Escape doing what it says.</summary>
public class MessageBoxTests
{
    /// <summary>The buttons stand Yes, No, Cancel from left to right, each with its own words.</summary>
    [Fact]
    public void TheButtonsStandInTheGamesOrder()
    {
        var box = Box(out _);
        box.YesText = "Replace it";
        box.NoText = "Leave it alone";
        box.CancelText = "Not now";
        Laid(box);

        var buttons = Buttons(box);

        Assert.Equal(new[] { "Replace it", "Leave it alone", "Not now" }, buttons.Select(b => b.Text));
        Assert.True(buttons[0].Bounds.X < buttons[1].Bounds.X && buttons[1].Bounds.X < buttons[2].Bounds.X);
    }

    /// <summary>A button with nothing to do is not shown, and Cancel can be left to Escape alone.</summary>
    [Fact]
    public void OnlyButtonsWithSomethingToDoAreShown()
    {
        var box = Box(out _);
        box.NoCommand = null;
        box.ShowsCancel = false;
        Laid(box);

        Assert.Equal(new[] { "Yes" }, Buttons(box).Select(b => b.Text));
        Assert.True(box.PerformCancel(), "Escape no longer cancels");
    }

    /// <summary>Enter gives the default answer, and with no default Enter gives nothing.</summary>
    [Theory]
    [InlineData(MessageBoxAnswer.Yes, "yes")]
    [InlineData(MessageBoxAnswer.No, "no")]
    [InlineData(MessageBoxAnswer.Cancel, "cancel")]
    [InlineData(null, "")]
    public void EnterGivesTheDefault(MessageBoxAnswer? given, string answered)
    {
        var box = Box(out var said);
        box.DefaultButton = given;

        Assert.Equal(given.HasValue, box.PerformAccept());
        Assert.Equal(answered, said.ToString());
    }

    /// <summary>The default answer looks like the default; the others look alike.</summary>
    [Fact]
    public void TheDefaultStandsOut()
    {
        var plain = new ButtonStyle { Base = { Border = Color.Gray } };
        var standing = new ButtonStyle { Base = { Background = Color.White } };
        var box = Box(out _);
        box.Base.ButtonStyle = plain;
        box.Base.DefaultButtonStyle = standing;
        box.DefaultButton = MessageBoxAnswer.No;
        Laid(box);

        var buttons = Buttons(box);

        Assert.Same(plain, buttons[0].Style);
        Assert.Same(standing, buttons[1].Style);
        Assert.Same(plain, buttons[2].Style);
    }

    /// <summary>In the game's look the default answer is the primary button and the others the host's own; a style says otherwise.</summary>
    [Fact]
    public void TheDefaultsLookComesFromTheBoxsStyle()
    {
        var game = Box(out _);
        game.DefaultButton = MessageBoxAnswer.Yes;
        Laid(game);
        var gameButtons = Buttons(game);
        Assert.Same(GameTheme.PrimaryButton, gameButtons[0].Style);
        Assert.Null(gameButtons[1].Style);

        var standing = new ButtonStyle { Base = { Background = Color.White } };
        var styled = Box(out _);
        styled.Style = new MessageBoxStyle(GameTheme.Dialog) { Base = { DefaultButtonStyle = standing } };
        styled.DefaultButton = MessageBoxAnswer.Yes;
        Laid(styled);
        Assert.Same(standing, Buttons(styled)[0].Style);
    }

    /// <summary>A short message makes a box no wider than it needs, with the buttons centered beneath the message.</summary>
    [Fact]
    public void AShortMessageMakesANarrowBox()
    {
        var box = Box(out _);
        Laid(box);

        var buttons = Buttons(box);
        var middle = (buttons[0].Bounds.X + buttons[2].Bounds.Right) / 2f;

        Assert.True(box.Bounds.Width < 500f, $"the box is {box.Bounds.Width} wide in a room of 1000");
        Assert.Equal(box.Bounds.Center.X, middle, 1);
    }

    /// <summary>A message longer than the box's widest width wraps there.</summary>
    [Fact]
    public void ALongMessageWrapsAtTheWidestWidth()
    {
        var box = Box(out _);
        box.Message = Document.Plain(string.Join(" ", Enumerable.Repeat("Every reference to this timer stops working.", 8)));
        box.Base.MaxWidth = 300f;
        Laid(box);

        var message = Messages(box).Single();

        Assert.True(message.Bounds.Width <= 300f, $"the message is {message.Bounds.Width} wide");
        Assert.True(message.Bounds.Height > message.FontSize * 2f, "the message did not wrap");
    }

    static List<RichText> Messages(Element element)
    {
        var found = new List<RichText>();
        if (element is RichText text)
            found.Add(text);

        for (var i = 0; i < element.VisualChildCount; i++)
            found.AddRange(Messages(element.GetVisualChild(i)));

        return found;
    }

    static MessageBox Box(out System.Text.StringBuilder said)
    {
        var heard = new System.Text.StringBuilder();
        said = heard;

        return new MessageBox
        {
            Message = Document.Plain("Replace the program?"),
            YesCommand = new RelayCommand(() => heard.Append("yes")),
            NoCommand = new RelayCommand(() => heard.Append("no")),
            CancelCommand = new RelayCommand(() => heard.Append("cancel"))
        };
    }

    static void Laid(MessageBox box)
    {
        var layer = new WindowLayer();
        layer.Open(box);
        layer.Draw(new Counting(), new RectangleF(0f, 0f, 1000f, 800f));
    }

    static List<Button> Buttons(MessageBox box)
    {
        var found = new List<Button>();
        Collect(box, found);
        return found;
    }

    static void Collect(Element element, List<Button> found)
    {
        if (!element.IsVisible)
            return;

        if (element is Button button)
            found.Add(button);

        for (var i = 0; i < element.VisualChildCount; i++)
            Collect(element.GetVisualChild(i), found);
    }
}
