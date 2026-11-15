using Lumel.Input;
using Lumel.Layout;
using Lumel.Text;
using VRage.Input;
using VRageMath;
using Xunit;

namespace Lumel.Tests;

/// <summary>What every box of editable text does alike, one line or many: its caret and selection, what can be undone, the clipboard's keys and being only read.</summary>
public class EditableTextTests
{
    const float Wide = 400f;
    const float High = 200f;

    public static TheoryData<string> Kinds => new() { "TextBox", "TextArea" };

    static (IEditableText Box, InputRoot Input) Focused(string kind, string text)
    {
        Element box = kind == "TextBox"
            ? new TextBox { Text = text, Placement = FixedSize.Width(300f) }
            : new TextArea { Text = text, Placement = FixedSize.Size(300f, 100f) };
        var root = new VStack { Children = { box } };
        _ = new Easel(root, Wide, High);
        var input = new InputRoot(root);
        input.Focus(box);
        return ((IEditableText)box, input);
    }

    /// <summary>Characters typed one after another are taken back as one edit, and made again as one.</summary>
    [Theory]
    [MemberData(nameof(Kinds))]
    public void TypingIsUndoneAsOneEditAndRedone(string kind)
    {
        var (box, input) = Focused(kind, "Air");
        input.Key(MyKeys.End, false, false);
        input.Text('l');
        input.Text('o');
        input.Text('c');
        input.Text('k');
        Assert.Equal("Airlock", box.Text);

        input.Key(MyKeys.Z, false, true);
        Assert.Equal("Air", box.Text);
        Assert.Equal(3, box.CaretIndex);

        input.Key(MyKeys.Y, false, true);
        Assert.Equal("Airlock", box.Text);
        Assert.Equal(7, box.CaretIndex);
    }

    /// <summary>Undoing puts back the selection the edit replaced, and every undo and redo is an edit.</summary>
    [Theory]
    [MemberData(nameof(Kinds))]
    public void UndoPutsTheSelectionBack(string kind)
    {
        var (box, input) = Focused(kind, "Airlock door");
        var edits = 0;
        box.Edited += () => edits++;

        box.Select(8, 4);
        Assert.Equal("door", box.SelectedText);
        Assert.Equal(8, box.SelectionStart);
        Assert.Equal(4, box.SelectionLength);
        Assert.Equal(12, box.CaretIndex);

        input.Text('x');
        Assert.Equal("Airlock x", box.Text);

        box.Undo();
        Assert.Equal("Airlock door", box.Text);
        Assert.Equal("door", box.SelectedText);

        box.Redo();
        Assert.Equal("Airlock x", box.Text);
        Assert.Equal(3, edits);
    }

    /// <summary>Cut, copy and paste go through the input's clipboard, and a pasted line break is one character however it was written.</summary>
    [Theory]
    [MemberData(nameof(Kinds))]
    public void TheClipboardKeysCutCopyAndPaste(string kind)
    {
        var (box, input) = Focused(kind, "Airlock");

        box.Select(0, 3);
        input.Key(MyKeys.C, false, true);
        Assert.Equal("Air", input.Clipboard.Get());

        input.Key(MyKeys.X, false, true);
        Assert.Equal("lock", box.Text);

        input.Clipboard.Set("a\r\nb");
        input.Key(MyKeys.V, false, true);
        Assert.Equal("a\nblock", box.Text);
    }

    /// <summary>A box only read takes no edit of any kind, and its words can still be selected and copied.</summary>
    [Theory]
    [MemberData(nameof(Kinds))]
    public void ABoxOnlyReadCanBeSelectedAndCopiedButNotEdited(string kind)
    {
        var (box, input) = Focused(kind, "Airlock");
        box.IsReadOnly = true;

        input.Text('x');
        input.Key(MyKeys.Back, false, false);
        input.Key(MyKeys.A, false, true);
        input.Key(MyKeys.X, false, true);
        input.Clipboard.Set("pasted");
        input.Key(MyKeys.V, false, true);
        Assert.Equal("Airlock", box.Text);
        Assert.Equal("Airlock", box.SelectedText);

        input.Key(MyKeys.C, false, true);
        Assert.Equal("Airlock", input.Clipboard.Get());
    }

    /// <summary>Being only read leaves the box able to take the keyboard.</summary>
    [Theory]
    [MemberData(nameof(Kinds))]
    public void ABoxOnlyReadStaysFocusable(string kind)
    {
        var (box, _) = Focused(kind, "Airlock");

        box.IsReadOnly = true;

        Assert.True(((Element)box).Focusable);
    }

    /// <summary>Setting the text forgets what could be undone.</summary>
    [Theory]
    [MemberData(nameof(Kinds))]
    public void SettingTheTextForgetsTheUndo(string kind)
    {
        var (box, input) = Focused(kind, "Air");
        input.Text('x');

        box.Text = "Door";
        box.Undo();

        Assert.Equal("Door", box.Text);

        input.Key(MyKeys.End, false, false);
        input.Text('s');
        box.Text = box.Text;
        box.Undo();

        Assert.Equal("Doors", box.Text);
    }

    /// <summary>Setting the text puts the caret at the start with nothing selected, even where the text is what it held.</summary>
    [Theory]
    [MemberData(nameof(Kinds))]
    public void SettingTheTextPutsTheCaretAtTheStart(string kind)
    {
        var (box, _) = Focused(kind, "Airlock");
        box.Select(3, 4);

        box.Text = "Airlock";

        Assert.Equal(0, box.CaretIndex);
        Assert.Equal(0, box.SelectionLength);
    }

    /// <summary>A box only read draws no caret, and its caret keys still move and, with Shift, select, so its words can be copied.</summary>
    [Theory]
    [MemberData(nameof(Kinds))]
    public void ABoxOnlyReadDrawsNoCaretAndItsKeysStillSelect(string kind)
    {
        var (box, input) = Focused(kind, "Airlock");
        var editable = Fills(box);
        box.IsReadOnly = true;

        input.Key(MyKeys.End, false, false);
        input.Key(MyKeys.Left, true, false);
        input.Key(MyKeys.Left, true, false);
        Assert.Equal("ck", box.SelectedText);

        input.Key(MyKeys.Home, true, false);
        Assert.Equal("Airlock", box.SelectedText);

        input.Key(MyKeys.Right, false, false);
        Assert.Equal(7, box.CaretIndex);
        Assert.Equal(0, box.SelectionLength);

        Assert.Equal(editable - 1, Fills(box));
    }

    /// <summary>Returns how many fills one frame of the whole tree of <paramref name="box" /> issues.</summary>
    static int Fills(IEditableText box)
    {
        var g = new Recording(new RectangleF(0f, 0f, Wide, High));
        var root = ((Element)box).Parent!;
        root.Measure(g, new Vector2(Wide, High));
        root.Arrange(new RectangleF(0f, 0f, Wide, High));
        root.Draw(g);
        return g.Fills.Count;
    }

    /// <summary>A box keeps no more edits to take back than its undo limit.</summary>
    [Fact]
    public void ABoxKeepsNoMoreEditsThanItsLimit()
    {
        var box = new TextBox { UndoLimit = 2 };

        box.Insert("a");
        box.Insert("b");
        box.Insert("c");
        box.Undo();
        box.Undo();
        box.Undo();

        Assert.Equal("a", box.Text);
        Assert.False(box.CanUndo);
    }

    /// <summary>The caret is drawn as wide as the box's look says.</summary>
    [Fact]
    public void TheCaretIsAsWideAsItsLook()
    {
        var box = new TextBox { Text = "Air", Base = { CaretWidth = 4f, CaretColor = Color.Orange }, Placement = FixedSize.Width(300f) };
        var root = new VStack { Children = { box } };
        var input = new InputRoot(root);
        input.Focus(box);
        var easel = new Easel(root, Wide, High);

        var caret = Assert.Single(easel.Sprites, s => s.Color == Color.Orange);
        Assert.Equal(4f * easel.Pixels, caret.Destination.Width, 1f);
    }
}
