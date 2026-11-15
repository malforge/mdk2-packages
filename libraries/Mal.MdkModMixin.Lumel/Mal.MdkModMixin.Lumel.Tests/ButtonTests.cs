using Lumel.Input;
using Lumel.Controls;
using Lumel.Layout;
using Lumel.Text;
using VRageMath;
using Xunit;
using Lumel.Sim;

namespace Lumel.Tests;

/// <summary>Tests of a button's text or content, its fill, its style and its presses.</summary>
public class ButtonTests
{
    const float Wide = 400f;
    const float High = 200f;

    static readonly Color Accent = new Color(142, 188, 207);
    static readonly Color Chrome = new Color(27, 36, 42);
    static readonly Color Ink = new Color(220, 245, 253);
    static readonly Color Well = new Color(55, 72, 81);
    static readonly Color Faint = new Color(133, 161, 169);

    static readonly ButtonStyle Go = new ButtonStyle
    {
        Base = { Background = Accent, Color = Chrome, Padding = new Thickness(16f, 8f), FontSize = 24f },
        PointerOver = { Background = Ink },
        Disabled = { Background = Well, Color = Faint }
    };

    /// <summary>A button's style fills the button and colors its text.</summary>
    [Fact]
    public void ItsStyleFillsItAndColorsItsWords()
    {
        var button = new Button { Text = "Done", Style = Go };
        var easel = new Easel(new VStack { Children = { button } }, Wide, High);

        Assert.Equal(Accent, button.Background);
        Assert.Equal(Chrome, Words(easel).Color);
    }

    /// <summary>Its text sits inside its padding, and the button is as big as the text plus the padding.</summary>
    [Fact]
    public void ItsWordsSitInsideItsPadding()
    {
        var button = new Button { Text = "Done", Style = Go };
        var easel = new Easel(new HStack { Children = { button } }, Wide, High);

        Assert.Equal(24f + 16f, button.DesiredSize.Y, 3);
        Assert.Equal(button.Bounds.X + 16f, Words(easel).Position.X / easel.Pixels, 2);
    }

    /// <summary>Given more room than its text needs, it centers the text.</summary>
    [Fact]
    public void ItsWordsSitInTheMiddleOfMoreRoom()
    {
        var button = new Button { Text = "Done", Style = Go };
        var easel = new Easel(new VStack { Children = { button } }, Wide, High);

        var words = button.DesiredSize.X - 32f;
        Assert.Equal(button.Bounds.X + (button.Bounds.Width - words) / 2f, Words(easel).Position.X / easel.Pixels, 2);
    }

    /// <summary>On hover, it takes its hover fill; when the pointer leaves, it goes back.</summary>
    [Fact]
    public void UnderThePointerItTakesItsFill()
    {
        var button = new Button { Text = "Done", Style = Go };
        button.Restyle();

        ((IInputTarget)button).IsPointerOver = true;
        Assert.Equal(Ink, button.Background);

        ((IInputTarget)button).IsPointerOver = false;
        Assert.Equal(Accent, button.Background);
    }

    /// <summary>When disabled, it looks disabled whether or not the pointer is over it, text included.</summary>
    [Fact]
    public void DisabledItLooksDisabledEvenUnderThePointer()
    {
        var button = new Button { Text = "Done", Style = Go };
        var easel = new Easel(new VStack { Children = { button } }, Wide, High);

        ((IInputTarget)button).IsPointerOver = true;
        button.IsEnabled = false;
        easel.Draw(Wide, High);

        Assert.Equal(Well, button.Background);
        Assert.Equal(Faint, Words(easel).Color);
    }

    /// <summary>A press on its text reaches the button, not the text.</summary>
    [Fact]
    public void APressOnItsWordsReachesTheButton()
    {
        var button = new Button { Text = "Done", Style = Go, Command = new RelayCommand(() => { }) };
        _ = new Easel(new VStack { Children = { button } }, Wide, High);

        var middle = new Vector2(button.Bounds.X + button.Bounds.Width / 2f, button.Bounds.Y + button.Bounds.Height / 2f);
        Assert.Same(button, button.HitTest(middle));
    }

    /// <summary>The pointer entering and leaving measures no text, since only colors change.</summary>
    [Fact]
    public void ThePointerComingMeasuresNoText()
    {
        var button = new Button { Text = "Done", Style = Go };
        var easel = new Easel(new VStack { Children = { button } }, Wide, High);
        var settled = easel.Measured;

        ((IInputTarget)button).IsPointerOver = true;
        easel.Draw(Wide, High);
        ((IInputTarget)button).IsPointerOver = false;
        easel.Draw(Wide, High);

        Assert.Equal(settled, easel.Measured);
    }

    /// <summary>Content given to it replaces its text, and its style leaves that content as given.</summary>
    [Fact]
    public void GivenContentTakesThePlaceOfItsWords()
    {
        var given = new Label { Text = "Given", Base = { Color = Faint } };
        var button = new Button { Text = "Done", Style = Go };
        button.Content = given;
        var easel = new Easel(new VStack { Children = { button } }, Wide, High);

        Assert.Equal("", button.Text);
        Assert.Equal("Given", Words(easel).Text);
        Assert.Equal(Faint, Words(easel).Color);
    }

    /// <summary>Given text again, it shows its own text in place of the content it was given.</summary>
    [Fact]
    public void GivenWordsItShowsItsOwnAgain()
    {
        var button = new Button { Style = Go, Content = new Label { Text = "Given" } };
        button.Text = "Done";
        var easel = new Easel(new VStack { Children = { button } }, Wide, High);

        Assert.Equal("Done", button.Text);
        Assert.Equal("Done", Words(easel).Text);
        Assert.Equal(Chrome, Words(easel).Color);
    }

    /// <summary>A press on content given to it reaches the button, not the content.</summary>
    [Fact]
    public void APressOnGivenContentReachesTheButton()
    {
        var given = new HStack { Children = { new Label { Text = "Given" } } };
        var button = new Button { Style = Go, Content = given, Command = new RelayCommand(() => { }) };
        _ = new Easel(new VStack { Children = { button } }, Wide, High);

        var middle = new Vector2(given.Bounds.X + given.Bounds.Width / 2f, given.Bounds.Y + given.Bounds.Height / 2f);
        Assert.Same(button, button.HitTest(middle));
    }

    /// <summary>Content given to it is centered in the room inside its padding.</summary>
    [Fact]
    public void GivenContentSitsInTheMiddle()
    {
        var given = new Label { Text = "Given" };
        var button = new Button { Style = Go, Content = given };
        _ = new Easel(new VStack { Children = { button } }, Wide, High);

        Assert.Equal(button.Bounds.X + button.Bounds.Width / 2f, given.Bounds.X + given.Bounds.Width / 2f, 2);
        Assert.Equal(button.Bounds.Y + button.Bounds.Height / 2f, given.Bounds.Y + given.Bounds.Height / 2f, 2);
    }

    /// <summary>A button narrower than its words cuts them short with an ellipsis, inside its padding.</summary>
    [Fact]
    public void ItsWordsAreCutShortToItsWidth()
    {
        var button = new Button { Text = "the quick brown fox jumps over the lazy dog", Style = Go, Placement = FixedSize.Width(200f) };
        var easel = new Easel(new VStack { Children = { button } }, Wide, High);
        var words = Words(easel);

        Assert.EndsWith("…", words.Text);
        Assert.True(easel.WidthOf(words.Text, 24f) <= 200f - 2f * 16f + 0.01f, "the words reach past the padding");
    }

    static DrawCommand Words(Easel easel)
    {
        return Assert.Single(easel.Written);
    }
}
