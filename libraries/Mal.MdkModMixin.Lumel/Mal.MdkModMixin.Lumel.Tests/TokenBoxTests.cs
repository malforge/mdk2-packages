using System;
using Lumel.Input;
using Lumel.Layout;
using Lumel.Text;
using VRage.Input;
using VRageMath;
using Xunit;

namespace Lumel.Tests;

/// <summary>
///     Words with tokens among them: each token one unit to the caret, the selection and the delete keys, put in and taken
///     out by code, pressable, and the line wrapping where the box is narrow.
/// </summary>
public class TokenBoxTests
{
    /// <summary>What a box is given comes back as the same runs and tokens, with the tokens its children.</summary>
    [Fact]
    public void ItHoldsTheRunsAndTokensItIsGiven()
    {
        var (box, tanks, margin, _, _) = Laid();

        var parts = box.Parts;

        Assert.Equal(4, parts.Count);
        Assert.Equal("( ", parts[0].Text);
        Assert.Same(tanks, parts[1].Token);
        Assert.Equal(" × 10 ) + ", parts[2].Text);
        Assert.Same(margin, parts[3].Token);
        Assert.Same(box, tanks.Parent);
        Assert.Equal("( Tanks × 10 ) + Margin", box.Words);
    }

    /// <summary>The caret steps over a token in one move, and Backspace takes it out whole, which an undo brings back.</summary>
    [Fact]
    public void ATokenIsOneUnitToTheCaretAndTheDeleteKeys()
    {
        var (box, tanks, _, _, input) = Laid();
        input.Focus(box);

        box.MoveCaret(2, false);
        input.Key(MyKeys.Right, false, false);
        Assert.Equal(3, box.CaretIndex);

        input.Key(MyKeys.Back, false, false);
        Assert.Equal(-1, box.IndexOf(tanks));
        Assert.Null(tanks.Parent);
        Assert.Equal("(  × 10 ) + Margin", box.Words);

        input.Key(MyKeys.Z, false, true);
        Assert.Equal(2, box.IndexOf(tanks));
        Assert.Same(box, tanks.Parent);
    }

    /// <summary>A selection takes a token whole, marks it selected, and copies it as its words.</summary>
    [Fact]
    public void ASelectionTakesATokenWhole()
    {
        var (box, tanks, margin, _, input) = Laid();
        input.Focus(box);

        box.Select(0, 3);
        input.Key(MyKeys.C, false, true);

        Assert.True(tanks.IsSelected);
        Assert.False(margin.IsSelected);
        Assert.Equal("( Tanks", input.Clipboard.Get());
    }

    /// <summary>Pasted and typed words never become tokens, and a token is put in, swapped and taken out by code.</summary>
    [Fact]
    public void TokensComeFromCodeAlone()
    {
        var (box, tanks, margin, _, input) = Laid();
        input.Focus(box);
        box.MoveCaret(0, false);

        input.Clipboard.Set("x");
        input.Key(MyKeys.V, false, true);
        Assert.Equal("x( Tanks × 10 ) + Margin", box.Words);

        var fill = new Token("Fill", 3);
        box.Insert(fill);
        Assert.Equal(1, box.IndexOf(fill));
        Assert.Equal(2, box.CaretIndex);

        var level = new Token("Level", 4);
        box.Replace(tanks, level);
        Assert.Null(tanks.Parent);
        Assert.Equal("xFill( Level × 10 ) + Margin", box.Words);

        box.Remove(margin);
        Assert.Equal("xFill( Level × 10 ) + ", box.Words);
        Assert.Throws<ArgumentException>(() => box.Insert(level));
    }

    /// <summary>A press on a token raises the box's TokenPressed with it.</summary>
    [Fact]
    public void APressOnATokenRaisesTokenPressed()
    {
        var (box, tanks, _, easel, input) = Laid();
        Token pressed = null;
        box.TokenPressed += token => pressed = token;
        easel.Draw(400f, 200f);

        var middle = new Vector2(tanks.Bounds.X + tanks.Bounds.Width / 2f, tanks.Bounds.Y + tanks.Bounds.Height / 2f);
        input.PointerDown(middle, PointerButton.Left, 1, false, false);
        input.PointerUp(middle, PointerButton.Left);

        Assert.Same(tanks, pressed);
    }

    /// <summary>Where the box is too narrow for the line, it breaks beside a token or after a space, and grows a row.</summary>
    [Fact]
    public void ANarrowBoxWrapsTheLine()
    {
        var (wide, _, _, wideEasel, _) = Laid();
        var (narrow, tanks, margin, narrowEasel, _) = Laid(160f);
        wideEasel.Draw(400f, 200f);
        narrowEasel.Draw(400f, 200f);

        Assert.True(narrow.Bounds.Height > wide.Bounds.Height, "the line did not wrap");
        Assert.True(margin.Bounds.Y > tanks.Bounds.Y, "the last token is on the first row");
        Assert.InRange(margin.Bounds.X, narrow.ContentBounds.X, narrow.ContentBounds.X + 40f);
    }

    static (TokenBox, Token, Token, Easel, InputRoot) Laid(float width = 400f)
    {
        var tanks = new Token("Tanks", 1);
        var margin = new Token("Margin", 2);
        var box = new TokenBox { Placement = FixedSize.Width(width) };
        box.SetParts(new[] { new TokenBoxPart("( "), new TokenBoxPart(tanks), new TokenBoxPart(" × 10 ) + "), new TokenBoxPart(margin) });
        var root = new VStack { Children = { box } };
        var easel = new Easel(root, 400f, 200f);
        return (box, tanks, margin, easel, new InputRoot(root));
    }
}
