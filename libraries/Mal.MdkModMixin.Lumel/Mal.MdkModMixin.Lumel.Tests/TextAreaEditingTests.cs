using Lumel.Input;
using Lumel.Text;
using VRage.Input;
using VRageMath;
using Xunit;

namespace Lumel.Tests;

/// <summary>
/// A text area written in: typing, lines broken and joined, the caret moved about, and edits taken back.
/// </summary>
/// <remarks>
/// Every test types through an input root holding the box, as a player's keyboard reaches it, and draws a frame after
/// anything that settles in the measure pass: a press, or a move to another line.
/// </remarks>
public class TextAreaEditingTests
{
    const float Width = 800f;
    const float Height = 600f;

    sealed class Typist
    {
        public Typist(string text)
        {
            Box = new TextArea { Text = text };
            Easel = new Easel(Box, Width, Height);
            Input = new InputRoot(Box);
            Input.Focus(Box);
        }

        public TextArea Box { get; }

        public Easel Easel { get; }

        public InputRoot Input { get; }

        public Typist Type(string words)
        {
            foreach (var c in words)
                Input.Text(c);

            return Frame();
        }

        public Typist Press(MyKeys key, bool shift = false, bool ctrl = false)
        {
            Input.Key(key, shift, ctrl);
            return Frame();
        }

        public Typist Frame()
        {
            Easel.Draw(Width, Height);
            return this;
        }
    }

    [Fact]
    public void TypingGoesInAtTheCaret()
    {
        var typist = new Typist("one two").Press(MyKeys.End).Type(" three");

        Assert.Equal("one two three", typist.Box.Text);
        Assert.Equal(13, typist.Box.CaretColumn);
    }

    [Fact]
    public void EnterBreaksTheLineAtTheCaret()
    {
        var typist = new Typist("onetwo");
        typist.Box.MoveCaret(0, 3, false);
        typist.Press(MyKeys.Enter);

        Assert.Equal("one\ntwo", typist.Box.Text);
        Assert.Equal(2, typist.Box.LineCount);
        Assert.Equal(1, typist.Box.CaretLine);
        Assert.Equal(0, typist.Box.CaretColumn);
    }

    [Fact]
    public void BackspaceAtTheStartOfALineJoinsItToTheOneAbove()
    {
        var typist = new Typist("one\ntwo");
        typist.Box.MoveCaret(1, 0, false);
        typist.Press(MyKeys.Back);

        Assert.Equal("onetwo", typist.Box.Text);
        Assert.Equal(0, typist.Box.CaretLine);
        Assert.Equal(3, typist.Box.CaretColumn);
    }

    [Fact]
    public void DeleteAtTheEndOfALineJoinsTheNextOneToIt()
    {
        var typist = new Typist("one\ntwo");
        typist.Box.MoveCaret(0, 3, false);
        typist.Press(MyKeys.Delete);

        Assert.Equal("onetwo", typist.Box.Text);
    }

    /// <summary>A selection running over several lines is replaced whole by what is typed.</summary>
    [Fact]
    public void TypingReplacesASelectionAcrossLines()
    {
        var typist = new Typist("one\ntwo\nthree");
        typist.Box.MoveCaret(0, 1, false);
        typist.Box.MoveCaret(2, 2, true);
        typist.Type("X");

        Assert.Equal("oXree", typist.Box.Text);
        Assert.Equal(1, typist.Box.LineCount);
    }

    /// <summary>Up and Down keep the column the run of them began in, across a line too short to hold it.</summary>
    [Fact]
    public void UpAndDownKeepTheirColumnAcrossAShortLine()
    {
        var typist = new Typist("aaaaaaaaaaaa\naa\naaaaaaaaaaaa");
        typist.Box.MoveCaret(0, 10, false);
        typist.Frame();

        typist.Press(MyKeys.Down);
        Assert.Equal(1, typist.Box.CaretLine);
        Assert.Equal(2, typist.Box.CaretColumn);

        typist.Press(MyKeys.Down);
        Assert.Equal(2, typist.Box.CaretLine);
        Assert.Equal(10, typist.Box.CaretColumn);
    }

    [Fact]
    public void PastedLinesBecomeLines()
    {
        var typist = new Typist("[]");
        typist.Box.MoveCaret(0, 1, false);
        typist.Input.Clipboard.Set("a\r\nb\nc");
        typist.Press(MyKeys.V, ctrl: true);

        Assert.Equal("[a\nb\nc]", typist.Box.Text);
        Assert.Equal(2, typist.Box.CaretLine);
        Assert.Equal(1, typist.Box.CaretColumn);
    }

    /// <summary>A word typed is taken back whole, and made again whole.</summary>
    [Fact]
    public void AWordTypedIsTakenBackInOne()
    {
        var typist = new Typist("one").Press(MyKeys.End).Type(" two");

        typist.Press(MyKeys.Z, ctrl: true);
        Assert.Equal("one", typist.Box.Text);
        Assert.Equal(3, typist.Box.CaretColumn);

        typist.Press(MyKeys.Y, ctrl: true);
        Assert.Equal("one two", typist.Box.Text);
    }

    /// <summary>A line broken is taken back as its own edit, apart from the typing either side of it.</summary>
    [Fact]
    public void ALineBreakIsItsOwnEdit()
    {
        var typist = new Typist("").Type("ab").Press(MyKeys.Enter).Type("cd");

        typist.Press(MyKeys.Z, ctrl: true);
        Assert.Equal("ab\n", typist.Box.Text);

        typist.Press(MyKeys.Z, ctrl: true);
        Assert.Equal("ab", typist.Box.Text);
    }

    /// <summary>Setting the text is a new document, and nothing before it can be taken back.</summary>
    [Fact]
    public void SettingTheTextForgetsWhatCouldBeUndone()
    {
        var typist = new Typist("one").Type("x");
        typist.Box.Text = "fresh";

        Assert.False(typist.Box.CanUndo);
    }

    /// <summary>A press on a line puts the caret on that line.</summary>
    [Fact]
    public void APressPutsTheCaretOnTheLinePressed()
    {
        var typist = new Typist("one\ntwo\nthree");
        var line = typist.Easel.LineHeight;

        typist.Input.PointerDown(new Vector2(1f, line * 2.5f), PointerButton.Left, 1, false, false);
        typist.Input.PointerUp(new Vector2(1f, line * 2.5f), PointerButton.Left);
        typist.Frame();

        Assert.Equal(2, typist.Box.CaretLine);
        Assert.Equal(0, typist.Box.CaretColumn);
    }

    /// <summary>Going to the end of a long text brings the end into view.</summary>
    [Fact]
    public void TheWindowFollowsTheCaret()
    {
        var lines = new System.Text.StringBuilder();
        for (var i = 0; i < 500; i++)
            lines.Append("line ").Append(i).Append('\n');

        var typist = new Typist(lines.ToString());
        typist.Press(MyKeys.End, ctrl: true);

        Assert.Equal(500, typist.Box.CaretLine);
        Assert.True(typist.Box.VerticalOffset > 0f, "the window stayed at the top");
        Assert.True(typist.Box.VerticalOffset + Height >= typist.Box.ExtentHeight - 1f, "the window stopped short of the end");
    }

    /// <summary>A frame where nothing has changed measures nothing, caret and selection included.</summary>
    [Fact]
    public void AnIdleFrameMeasuresNothing()
    {
        var typist = new Typist("one\ntwo\nthree");
        typist.Box.MoveCaret(0, 1, false);
        typist.Box.MoveCaret(2, 2, true);
        typist.Frame().Frame();

        var before = typist.Easel.Measured;
        typist.Frame();

        Assert.Equal(before, typist.Easel.Measured);
    }

    [Fact]
    public void ReadOnlyTakesNoTyping()
    {
        var typist = new Typist("one");
        typist.Box.IsReadOnly = true;
        typist.Type("x").Press(MyKeys.Back);

        Assert.Equal("one", typist.Box.Text);
    }
}
