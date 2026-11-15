using Lumel.Input;
using Lumel.Layout;
using Lumel.Text;
using VRage.Input;
using VRageMath;
using Xunit;

namespace Lumel.Tests;

/// <summary>A box of editable text: typed into, selected by the pointer as well as the keys, and measured only when its text changes.</summary>
public class TextBoxTests
{
    const float Wide = 400f;
    const float High = 200f;

    static readonly Color Focus = new Color(69, 128, 157);

    /// <summary>A press inside the box puts the caret at the nearest boundary between characters.</summary>
    [Fact]
    public void APressPutsTheCaretAtTheNearestBoundary()
    {
        var (box, easel, input) = Laid("Airlock door");

        input.PointerDown(At(box, easel, "Air"), PointerButton.Left, 1, false, false);
        input.PointerUp(At(box, easel, "Air"), PointerButton.Left);

        Assert.Equal(3, box.CaretIndex);
        Assert.False(box.HasSelection);
    }

    /// <summary>A drag from a press selects the run it crosses.</summary>
    [Fact]
    public void ADragSelectsTheRunItCrosses()
    {
        var (box, easel, input) = Laid("Airlock door");

        input.PointerDown(At(box, easel, "Air"), PointerButton.Left, 1, false, false);
        input.PointerMove(At(box, easel, "Airlock"));
        input.PointerUp(At(box, easel, "Airlock"), PointerButton.Left);

        Assert.Equal("lock", box.SelectedText);
    }

    /// <summary>A second press takes the word under it.</summary>
    [Fact]
    public void ASecondPressTakesTheWord()
    {
        var (box, easel, input) = Laid("Airlock door");

        input.PointerDown(At(box, easel, "Airlock do"), PointerButton.Left, 2, false, false);
        input.PointerUp(At(box, easel, "Airlock do"), PointerButton.Left);

        Assert.Equal("door", box.SelectedText);
    }

    /// <summary>Shift with the arrows extends a selection, and typing replaces it.</summary>
    [Fact]
    public void TypingReplacesASelectionMadeWithTheKeys()
    {
        var (box, _, input) = Laid("Airlock");

        input.Focus(box);
        input.Key(MyKeys.End, false, false);
        input.Key(MyKeys.Left, true, false);
        input.Key(MyKeys.Left, true, false);
        input.Key(MyKeys.Left, true, false);
        input.Key(MyKeys.Left, true, false);
        input.Text('!');

        Assert.Equal("Air!", box.Text);
    }

    /// <summary>A box with a limit takes as much of what is put in as fits, and no typing once it is full.</summary>
    [Fact]
    public void ABoxHoldsToItsLimit()
    {
        var (box, _, input) = Laid("Air");
        box.MaxLength = 7;

        input.Focus(box);
        input.Key(MyKeys.End, false, false);
        box.Insert("lockdoor");
        Assert.Equal("Airlock", box.Text);

        input.Text('!');
        Assert.Equal("Airlock", box.Text);
    }

    /// <summary>What is cut can be pasted back, through the root's clipboard.</summary>
    [Fact]
    public void CutAndPasteGoThroughTheClipboard()
    {
        var (box, _, input) = Laid("Airlock");

        input.Focus(box);
        input.Key(MyKeys.A, false, true);
        input.Key(MyKeys.X, false, true);
        Assert.Equal("", box.Text);

        input.Key(MyKeys.V, false, true);
        input.Key(MyKeys.V, false, true);
        Assert.Equal("AirlockAirlock", box.Text);
    }

    /// <summary>Enter carries out the box's submit command.</summary>
    [Fact]
    public void EnterSubmits()
    {
        var (box, _, input) = Laid("Airlock");
        var submitted = 0;
        box.Submit = new RelayCommand(() => submitted++);

        input.Focus(box);
        input.Key(MyKeys.Enter, false, false);

        Assert.Equal(1, submitted);
    }

    /// <summary>Escape carries out the box's cancel command, and the box keeps the keyboard.</summary>
    [Fact]
    public void EscapeCancels()
    {
        var (box, _, input) = Laid("Airlock");
        var canceled = 0;
        box.Cancel = new RelayCommand(() => canceled++);

        input.Focus(box);
        input.Key(MyKeys.Escape, false, false);

        Assert.Equal(1, canceled);
        Assert.Same(box, input.Focused);
    }

    /// <summary>Escape lets the keyboard go from a box with no cancel command.</summary>
    [Fact]
    public void EscapeWithNothingToCancelLetsTheKeyboardGo()
    {
        var (box, _, input) = Laid("Airlock");

        input.Focus(box);
        input.Key(MyKeys.Escape, false, false);

        Assert.Null(input.Focused);
    }

    /// <summary>Typing says the text was edited; setting it does not.</summary>
    [Fact]
    public void TypingSaysItEditedAndSettingDoesNot()
    {
        var (box, _, input) = Laid("Airlock");
        var edits = 0;
        box.Edited += () => edits++;

        box.Text = "Hangar";
        Assert.Equal(0, edits);

        input.Focus(box);
        input.Text('s');
        Assert.Equal(1, edits);
    }

    /// <summary>A box being edited measures no text on a frame where nothing about it changed, and moving the caret measures none either.</summary>
    [Fact]
    public void AnEditedBoxMeasuresNothingItHasNotChanged()
    {
        var (box, easel, input) = Laid("Airlock door");

        var from = At(box, easel, "Air");
        var to = At(box, easel, "Airlock");

        input.Focus(box);
        easel.Draw(Wide, High);
        var settled = easel.Measured;

        easel.Draw(Wide, High);
        input.Key(MyKeys.Left, false, false);
        input.Key(MyKeys.Home, true, false);
        input.PointerDown(from, PointerButton.Left, 1, false, false);
        input.PointerMove(to);
        input.PointerUp(to, PointerButton.Left);
        easel.Draw(Wide, High);

        Assert.Equal(settled, easel.Measured);
    }

    /// <summary>An empty box is as wide as it is tall, so there is still somewhere to type.</summary>
    [Fact]
    public void AnEmptyBoxIsAsWideAsItIsTall()
    {
        var box = new TextBox { Base = { FontSize = 32f } };
        _ = new Easel(new HStack { Children = { box } }, Wide, High);

        Assert.Equal(32f, box.ContentBounds.Width);
    }

    /// <summary>Holding the keyboard takes the style's focused layer.</summary>
    [Fact]
    public void HoldingTheKeyboardTakesTheFocusedLayer()
    {
        var (box, _, input) = Laid("Airlock");
        box.Style = new TextBoxStyle { Focused = { Border = Focus } };

        input.Focus(box);
        Assert.Equal(Focus, box.Border);

        input.Focus(null);
        Assert.Equal(Color.Transparent, box.Border);
    }

    /// <summary>
    /// An edit the box does not allow is not made, whether typed, pasted or deleted, with the caret where
    /// it was, and nothing is told it was edited.
    /// </summary>
    [Fact]
    public void AnEditItDoesNotAllowIsNotMade()
    {
        var (box, _, _) = Laid("12");
        box.FnIsTextAllowed = said => said.Length == 0 || said.All(char.IsDigit);
        box.MoveCaret(1, false);

        var edits = 0;
        box.Edited += () => edits++;

        ((IInputTarget)box).Text('x');
        box.Insert("3a");
        Assert.Equal("12", box.Text);
        Assert.Equal(1, box.CaretIndex);
        Assert.Equal(0, edits);

        ((IInputTarget)box).Text('5');
        Assert.Equal("152", box.Text);
        Assert.Equal(1, edits);
    }

    /// <summary>A box accepting only something non-empty refuses to be emptied.</summary>
    [Fact]
    public void ABoxThatMustHoldSomethingCannotBeEmptied()
    {
        var (box, _, _) = Laid("7");
        box.FnIsTextAllowed = said => said.Length > 0;
        box.MoveCaret(1, false);

        box.Backspace();

        Assert.Equal("7", box.Text);
    }

    static (TextBox, Easel, InputRoot) Laid(string text)
    {
        var box = new TextBox { Text = text, Placement = FixedSize.Width(300f) };
        var root = new VStack { Children = { box } };
        var easel = new Easel(root, Wide, High);
        return (box, easel, new InputRoot(root));
    }

    /// <summary>
    ///     Returns the point in <paramref name="box" /> just after <paramref name="run" />, with the text starting at
    ///     the start of the box.
    /// </summary>
    static Vector2 At(TextBox box, Easel easel, string run)
    {
        var x = box.ContentBounds.X + easel.WidthOf(run) / easel.Pixels;
        return new Vector2(x, box.Bounds.Y + box.Bounds.Height / 2f);
    }
}

