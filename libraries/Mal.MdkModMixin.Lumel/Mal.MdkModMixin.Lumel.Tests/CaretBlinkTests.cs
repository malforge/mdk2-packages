using Lumel.Input;
using Lumel.Layout;
using Lumel.Styling;
using Lumel.Text;
using VRage.Input;
using VRageMath;
using Xunit;

namespace Lumel.Tests;

/// <summary>The caret of a box that holds the keyboard blinks by the clock it is drawn with, solid again from every keystroke.</summary>
public class CaretBlinkTests
{
    static readonly RectangleF Area = new RectangleF(0f, 0f, 400f, 300f);

    /// <summary>A text box shows its caret as it takes the keyboard, hides it a blink later, and shows it again a blink after that.</summary>
    [Fact]
    public void ATextBoxCaretBlinksByTheClock()
    {
        var (box, _) = Focused(new TextBox { Style = new TextBoxStyle(), Text = "Door" });

        Assert.True(CaretAt(box, 100));
        Assert.True(CaretAt(box, 100 + EditableTextCore.CaretBlinkTicks - 1));
        Assert.False(CaretAt(box, 100 + EditableTextCore.CaretBlinkTicks));
        Assert.True(CaretAt(box, 100 + 2 * EditableTextCore.CaretBlinkTicks));
    }

    /// <summary>A keystroke makes the caret solid at once, and the blink runs on from it.</summary>
    [Fact]
    public void AKeystrokeMakesTheCaretSolidAgain()
    {
        var (box, input) = Focused(new TextBox { Style = new TextBoxStyle(), Text = "Door" });
        CaretAt(box, 0);
        Assert.False(CaretAt(box, EditableTextCore.CaretBlinkTicks));

        input.Text('s');

        Assert.True(CaretAt(box, EditableTextCore.CaretBlinkTicks + 1));
        Assert.True(CaretAt(box, 2 * EditableTextCore.CaretBlinkTicks));
        Assert.False(CaretAt(box, 2 * EditableTextCore.CaretBlinkTicks + 1));
    }

    /// <summary>A box of many lines blinks its caret the same way.</summary>
    [Fact]
    public void ATextAreaCaretBlinksByTheClock()
    {
        var (box, input) = Focused(new TextArea { Style = new TextAreaStyle(), Text = "one" });

        Assert.True(CaretAt(box, 0));
        Assert.False(CaretAt(box, EditableTextCore.CaretBlinkTicks));

        input.Key(MyKeys.End, false, false);

        Assert.True(CaretAt(box, EditableTextCore.CaretBlinkTicks + 1));
    }

    /// <summary>
    /// A box that holds the keyboard and can be edited asks for every frame, its caret blinking by the clock; one only
    /// read, or not holding the keyboard, asks for none.
    /// </summary>
    [Theory]
    [InlineData("TextBox")]
    [InlineData("TextArea")]
    public void AFocusedBoxAsksForFramesWhileItsCaretBlinks(string kind)
    {
        Element box = kind == "TextBox"
            ? new TextBox { Style = new TextBoxStyle(), Text = "Door" }
            : new TextArea { Style = new TextAreaStyle(), Text = "Door" };
        var (_, input) = Focused(box);

        Assert.True(FramesAskedAt(box, 0) > 0);

        ((IEditableText)box).IsReadOnly = true;
        Assert.Equal(0, FramesAskedAt(box, 1));

        ((IEditableText)box).IsReadOnly = false;
        input.Focus(null);
        Assert.Equal(0, FramesAskedAt(box, 2));
    }

    /// <summary>A focused area made editable after being only read draws its caret where the caret stands in its line.</summary>
    [Fact]
    public void AnAreaMadeEditableDrawsItsCaretInPlace()
    {
        var area = new TextArea { Style = new TextAreaStyle(), Text = "Airlock", IsReadOnly = true };
        var (_, input) = Focused(area);
        input.Key(MyKeys.End, false, false);
        CaretAt(area, 0);

        area.IsReadOnly = false;
        var g = Drawn(area, 0);

        var caret = Assert.Single(g.Fills, fill => fill.Width == 2f);
        Assert.Equal(area.ContentBounds.X + 7 * TextLook.NeutralFontSize * 0.5f, caret.X, 1f);
    }

    static (Element Box, InputRoot Input) Focused(Element box)
    {
        var root = new VStack { Children = { box } };
        var input = new InputRoot(root);
        input.Focus(box);
        return (box, input);
    }

    /// <summary>
    ///     Returns true if the caret is drawn in the frame drawn at <paramref name="now" />, that is, if the frame
    ///     contains a fill as narrow as a caret.
    /// </summary>
    static bool CaretAt(Element box, int now) => Drawn(box, now).Fills.Exists(fill => fill.Width == 2f);

    /// <summary>Returns how many times the frame drawn at <paramref name="now" /> requests the next frame.</summary>
    static int FramesAskedAt(Element box, int now) => Drawn(box, now).FramesRequested;

    /// <summary>
    ///     Draws a frame of the whole tree of <paramref name="box" /> at <paramref name="now" /> and returns the
    ///     recording.
    /// </summary>
    static Recording Drawn(Element box, int now)
    {
        var g = new Recording(Area) { Now = now };
        var root = box.Parent!;
        root.Measure(g, new Vector2(Area.Width, Area.Height));
        root.Arrange(Area);
        root.Draw(g);
        return g;
    }
}
