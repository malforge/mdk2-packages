using Lumel.Input;
using Lumel.Layout;
using Lumel.Text;
using VRage.Input;
using VRageMath;
using Xunit;

namespace Lumel.Tests;

/// <summary>A box that holds a number: its value, the bounds up and down keep to, and what it lets be typed.</summary>
public class NumberBoxTests
{
    static (NumberBox Box, InputRoot Input) Focused(NumberBox box)
    {
        var root = new VStack { Children = { box } };
        _ = new Easel(root, 400f, 200f);
        var input = new InputRoot(root);
        input.Focus(box);
        return (box, input);
    }

    /// <summary>The value is the number typed, not a number while what is typed is none, and is written as its format says when set.</summary>
    [Fact]
    public void TheValueIsTheNumberTyped()
    {
        var (box, input) = Focused(new NumberBox { FormatString = "0.0" });
        Assert.True(double.IsNaN(box.Value));

        input.Text('1');
        input.Text('2');
        Assert.Equal(12d, box.Value);

        box.Value = 3.25;
        Assert.Equal("3.3", box.Text);
        Assert.Equal(3.3d, box.Value, 6);
    }

    /// <summary>A unit in the format shows after the number, and the text and the value stay the number alone.</summary>
    [Theory]
    [InlineData("0.0# kg")]
    [InlineData("0.0# 'kg'")]
    public void AUnitInTheFormatIsNoPartOfTheNumber(string format)
    {
        var box = new NumberBox { FormatString = format, Value = 12.5 };
        var easel = new Easel(new VStack { Children = { box } }, 400f, 200f);

        Assert.Equal("12.5", box.Text);
        Assert.Equal(12.5, box.Value);
        var number = easel.Written.Single(c => c.Text == "12.5");
        var unit = easel.Written.Single(c => c.Text == " kg");
        Assert.True(unit.Position.X > number.Position.X);
    }

    /// <summary>Words before the number stay out of the text, for a negative number and for a step.</summary>
    [Fact]
    public void WordsBeforeTheNumberStandBeforeIt()
    {
        var (box, input) = Focused(new NumberBox { FormatString = "'x'0.0", Minimum = null, Value = -2 });

        Assert.Equal("-2.0", box.Text);
        box.Value = 1;
        input.Key(MyKeys.Up, false, false);
        Assert.Equal("2.0", box.Text);
    }

    /// <summary>A press lands on the digit under it, past the words before the number.</summary>
    [Fact]
    public void APressFindsTheDigitPastTheWordsBefore()
    {
        var box = new NumberBox { FormatString = "'Amount: '0", Value = 1234, Widest = "000000000", Focusable = true };
        var root = new VStack { Children = { box } };
        var easel = new Easel(root, 400f, 200f);
        var input = new InputRoot(root);
        var words = easel.Written.Single(c => c.Text == "Amount: ");
        var number = easel.Written.Single(c => c.Text == "1234");
        Assert.True(number.Position.X > words.Position.X);

        input.PointerDown(new Vector2(number.Position.X + 1f, number.Position.Y + 4f), PointerButton.Left, 1, false, false);

        Assert.Equal(0, box.CaretIndex);
    }

    /// <summary>A new unit shows with the box open, and the number and where it is typed stay.</summary>
    [Fact]
    public void ChangingTheFormatKeepsTheNumber()
    {
        var box = new NumberBox { FormatString = "0.0# kg", Value = 12.5 };
        var root = new VStack { Children = { box } };
        var easel = new Easel(root, 400f, 200f);
        new InputRoot(root).Focus(box);
        box.MoveCaret(2, false);
        var edited = 0;
        box.ValueChanged += () => edited++;

        box.FormatString = "0 pcs";
        easel.Draw(400f, 200f);

        Assert.Equal("12.5", box.Text);
        Assert.Equal(2, box.CaretIndex);
        Assert.Equal(0, edited);
        Assert.Contains(easel.Written, c => c.Text == " pcs");
        Assert.DoesNotContain(easel.Written, c => c.Text == " kg");
    }

    /// <summary>A format that changes the number's size, or writes none, is refused and a quoted percent sign is a word.</summary>
    [Fact]
    public void AFormatMustWriteTheNumberAtItsSize()
    {
        var box = new NumberBox();

        Assert.Throws<ArgumentException>(() => box.FormatString = "0 %");
        Assert.Throws<ArgumentException>(() => box.FormatString = "'none'");

        box.FormatString = "0 '%'";
        box.Value = 40;
        Assert.Equal("40", box.Text);
    }

    /// <summary>The box is wide enough for the widest number and the words around it.</summary>
    [Fact]
    public void TheWordsWidenTheBox()
    {
        var plain = new NumberBox();
        var united = new NumberBox { FormatString = "0 kilograms" };
        _ = new Easel(new VStack { Children = { plain, united } }, 400f, 200f);

        Assert.True(united.DesiredSize.X > plain.DesiredSize.X);
    }

    /// <summary>An empty box shows its hint alone, and no words around a number, with the keyboard or without it.</summary>
    [Fact]
    public void AnEmptyBoxShowsNoFormat()
    {
        var box = new NumberBox { FormatString = "'x'0 kg", Hint = "Amount" };
        var root = new VStack { Children = { box } };
        var easel = new Easel(root, 400f, 200f);
        Assert.Equal("Amount", Assert.Single(easel.Written).Text);

        new InputRoot(root).Focus(box);
        easel.Draw(400f, 200f);

        Assert.Empty(easel.Written);
    }

    /// <summary>The words show from the first character typed, and go with the last.</summary>
    [Fact]
    public void TheWordsComeAndGoWithTheText()
    {
        var box = new NumberBox { FormatString = "0 kg" };
        var root = new VStack { Children = { box } };
        var easel = new Easel(root, 400f, 200f);
        var input = new InputRoot(root);
        input.Focus(box);

        input.Text('5');
        easel.Draw(400f, 200f);
        Assert.Contains(easel.Written, c => c.Text == " kg");

        input.Key(MyKeys.Back, false, false);
        easel.Draw(400f, 200f);
        Assert.Empty(easel.Written);
    }

    /// <summary>An empty box keeps no room for words it does not show: a right-aligned caret stands at the box's edge.</summary>
    [Fact]
    public void AnEmptyBoxKeepsNoRoomForTheWords()
    {
        var plain = new NumberBox { Base = { TextAlignment = TextAlignment.Right }, Placement = FixedSize.Width(200f) };
        var united = new NumberBox { FormatString = "0 kg", Base = { TextAlignment = TextAlignment.Right }, Placement = FixedSize.Width(200f) };
        Assert.Equal(CaretOf(plain), CaretOf(united));
    }

    /// <summary>Returns where the caret of the empty, focused <paramref name="box" /> is drawn.</summary>
    static float CaretOf(NumberBox box)
    {
        var root = new VStack { Children = { box } };
        var easel = new Easel(root, 400f, 200f);
        new InputRoot(root).Focus(box);
        easel.Draw(400f, 200f);
        return easel.Sprites.Last().Destination.X;
    }

    /// <summary>The value tells whoever listens each time it changes, by typing, by setting it or by setting the text, and only then.</summary>
    [Fact]
    public void TheValueTellsWhenItChanges()
    {
        var (box, input) = Focused(new NumberBox());
        var changes = 0;
        box.ValueChanged += () => changes++;

        input.Text('5');
        box.Value = 7;
        box.Text = "7";
        box.Text = "8";
        input.Text('x');

        Assert.Equal(3, changes);
        Assert.Equal(8d, box.Value);
    }

    /// <summary>A minus sign can be typed only where the minimum allows the number to go below 0.</summary>
    [Theory]
    [InlineData(0d, false)]
    [InlineData(-10d, true)]
    [InlineData(null, true)]
    public void AMinusFollowsTheMinimum(double? minimum, bool takesMinus)
    {
        var (box, input) = Focused(new NumberBox { Minimum = minimum });

        input.Text('-');

        Assert.Equal(takesMinus ? "-" : "", box.Text);
    }

    /// <summary>Up and down step the number by its small change, and stop at its minimum and maximum.</summary>
    [Fact]
    public void UpAndDownStepWithinTheBounds()
    {
        var (box, input) = Focused(new NumberBox { SmallChange = 5, Minimum = -5, Maximum = 10, Text = "0" });

        input.Key(MyKeys.Up, false, false);
        Assert.Equal(5d, box.Value);
        input.Key(MyKeys.Up, false, false);
        input.Key(MyKeys.Up, false, false);
        Assert.Equal(10d, box.Value);

        for (var i = 0; i < 5; i++)
            input.Key(MyKeys.Down, false, false);
        Assert.Equal(-5d, box.Value);
    }

    /// <summary>A box for whole numbers takes no point.</summary>
    [Fact]
    public void AnIntegerBoxTakesNoPoint()
    {
        var (box, input) = Focused(new NumberBox { IsInteger = true });

        input.Text('1');
        input.Text('.');

        Assert.Equal("1", box.Text);
    }
}
