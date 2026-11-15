using Lumel.Layout;
using Lumel.Text;
using Xunit;

namespace Lumel.Tests;

/// <summary>Cards one over another: the deck is the size of the largest, and only the card on top is drawn, pressed or reached.</summary>
public class DeckTests
{
    /// <summary>The deck takes the largest card's size whichever card is on top.</summary>
    [Fact]
    public void ADeckIsAsLargeAsItsLargestCardWhicheverIsOnTop()
    {
        var wide = new HStack { Placement = FixedSize.Size(100f, 20f) };
        var tall = new HStack { Placement = FixedSize.Size(60f, 40f) };
        var deck = new Deck { Children = { wide, tall } };
        var easel = new Easel(new HStack { Children = { deck } }, 400f, 200f);

        Assert.Equal(new VRageMath.Vector2(100f, 40f), deck.DesiredSize);

        deck.Top = tall;
        easel.Draw(400f, 200f);
        Assert.Equal(new VRageMath.Vector2(100f, 40f), deck.DesiredSize);
    }

    /// <summary>A z-stack holds every child in one place: as large as the largest, each placed by its own alignment, all drawn, the uppermost pressed.</summary>
    [Fact]
    public void TheZStackShowsEveryChildWhereItIsAligned()
    {
        var under = new Label { Text = "under", Placement = FixedSize.Size(200f, 80f) };
        var over = new Label { Text = "over", Placement = FixedSize.Size(50f, 20f), Align = Align.Right | Align.Bottom };
        var stack = new ZStack { Children = { under, over } };
        var easel = new Easel(new HStack { Children = { stack } }, 400f, 200f);

        Assert.Equal(new VRageMath.Vector2(200f, 80f), stack.DesiredSize);
        Assert.Equal(stack.Bounds.Right - 50f, over.Bounds.X, 3);
        Assert.Equal(stack.Bounds.Bottom - 20f, over.Bounds.Y, 3);
        Assert.Equal(new[] { "under", "over" }, easel.Written.Select(run => run.Text).ToArray());
        Assert.Equal(2, stack.VisualChildCount);
    }

    /// <summary>Only the card on top is drawn, responds to a press and counts as a child to walk.</summary>
    [Fact]
    public void OnlyTheTopCardIsDrawnPressedAndWalked()
    {
        var one = new Label { Text = "one" };
        var two = new Label { Text = "two" };
        var deck = new Deck { Children = { one, two } };
        var easel = new Easel(new HStack { Children = { deck } }, 400f, 200f);

        Assert.Same(one, deck.Top);
        Assert.Equal(new[] { "one" }, easel.Written.Select(run => run.Text).ToArray());
        Assert.Equal(1, deck.VisualChildCount);
        Assert.Same(one, deck.GetVisualChild(0));

        deck.Top = two;
        easel.Draw(400f, 200f);
        Assert.Equal(new[] { "two" }, easel.Written.Select(run => run.Text).ToArray());
        Assert.Same(two, deck.GetVisualChild(0));
    }
}
