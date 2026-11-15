using Lumel.Input;
using Lumel.Styling;
using Lumel.Text;
using VRage.Input;
using VRageMath;
using Xunit;

namespace Lumel.Tests;

/// <summary>A text area's scroll bars in the scroll view's look model, its window moved only within what it holds, and the looks of its line numbers and selection.</summary>
public class TextAreaLookTests
{
    const float Width = 800f;
    const float Height = 600f;

    static readonly Color Dragged = new Color(200, 100, 50);

    static string Lines(int count) => string.Join("\n", Enumerable.Range(0, count).Select(i => "line " + i));

    /// <summary>A thumb held by the pointer puts the box in its dragging state, which colors its thumbs, until it is let go.</summary>
    [Fact]
    public void AHeldThumbTakesTheDraggingLook()
    {
        var listing = new TextArea
        {
            IsReadOnly = true,
            Text = Lines(5000),
            Style = new TextAreaStyle { Base = { ScrollBarWidth = 20f, Thumb = Color.White }, Dragging = { Thumb = Dragged } }
        };
        var easel = new Easel(listing, Width, Height);

        ((IInputTarget)listing).PointerDown(new Vector2(Width - 10f, 10f), PointerButton.Left, 1, false, false);
        Assert.True(listing.IsDragging);
        Assert.Equal(Dragged, listing.Thumb);
        easel.Draw(Width, Height);
        Assert.Single(easel.Sprites, s => s.Color == Dragged);

        ((IInputTarget)listing).PointerUp(new Vector2(Width - 10f, 10f), PointerButton.Left);
        Assert.False(listing.IsDragging);
        Assert.Equal(Color.White, listing.Thumb);
    }

    /// <summary>With both bars showing, only the thumb held takes the dragging look; the other stays as it rests.</summary>
    [Fact]
    public void OnlyTheHeldThumbTakesTheDraggingLook()
    {
        var listing = new TextArea
        {
            IsReadOnly = true,
            Text = new string('x', 400) + "\n" + Lines(5000),
            Style = new TextAreaStyle { Base = { ScrollBarWidth = 20f, Thumb = Color.White }, Dragging = { Thumb = Dragged } }
        };
        var easel = new Easel(listing, Width, Height);
        Assert.Equal(2, easel.Sprites.Count(s => s.Color == Color.White));

        ((IInputTarget)listing).PointerDown(new Vector2(Width - 10f, 10f), PointerButton.Left, 1, false, false);
        easel.Draw(Width, Height);

        Assert.Single(easel.Sprites, s => s.Color == Dragged);
        Assert.Single(easel.Sprites, s => s.Color == Color.White);
    }

    /// <summary>Dragging the text about is not dragging a thumb.</summary>
    [Fact]
    public void PanningIsNotDragging()
    {
        var listing = new TextArea { IsReadOnly = true, Text = Lines(5000), Base = { ScrollBarWidth = 20f, Thumb = Color.White } };
        _ = new Easel(listing, Width, Height);

        ((IInputTarget)listing).PointerDown(new Vector2(100f, 100f), PointerButton.Left, 1, false, false);

        Assert.True(listing.IsPanning);
        Assert.False(listing.IsDragging);
    }

    /// <summary>The window moves only within what the box holds, however far it is asked to go.</summary>
    [Fact]
    public void TheWindowStaysWithinTheText()
    {
        var listing = new TextArea { IsReadOnly = true, Text = Lines(100) + "\n" + new string('x', 400) };
        var easel = new Easel(listing, Width, Height);
        listing.ScrollToVerticalOffset(float.MaxValue);
        easel.Draw(Width, Height);

        listing.ScrollToVerticalOffset(-50f);
        Assert.Equal(0f, listing.VerticalOffset);

        listing.ScrollToVerticalOffset(1000000f);
        Assert.Equal(listing.ExtentHeight - Height, listing.VerticalOffset, 1);

        listing.ScrollToHorizontalOffset(1000000f);
        Assert.Equal(listing.ExtentWidth - Width, listing.HorizontalOffset, 1);
    }

    /// <summary>The gap beside the bars is taken from the window the text is seen through.</summary>
    [Fact]
    public void TheGapBesideTheBarsNarrowsTheWindow()
    {
        var listing = new TextArea
        {
            IsReadOnly = true,
            Text = Lines(100) + "\n" + new string('x', 400),
            Base = { ScrollBarWidth = 8f, ScrollBarGap = 8f, Thumb = Color.White }
        };
        var easel = new Easel(listing, Width, Height);

        listing.ScrollToVerticalOffset(1000000f);
        easel.Draw(Width, Height);
        listing.ScrollToHorizontalOffset(1000000f);
        easel.Draw(Width, Height);

        Assert.Equal(listing.ExtentHeight - (Height - 16f), listing.VerticalOffset, 1);
        Assert.Equal(listing.ExtentWidth - (Width - 16f), listing.HorizontalOffset, 1);
    }

    /// <summary>The room either side of the line numbers is a look, its right side standing between them and the words.</summary>
    [Fact]
    public void TheLineNumbersPaddingIsALook()
    {
        var listing = new TextArea
        {
            IsReadOnly = true,
            Text = "one\ntwo",
            ShowLineNumbers = true,
            Base = { LineNumberColor = Color.White, LineNumbersPadding = new Thickness(0f, 0f, 40f, 0f) }
        };
        var easel = new Easel(listing, Width, Height);

        var number = easel.Written.First(c => c.Text == "1");
        var words = easel.Written.First(c => c.Text == "one");
        Assert.Equal(easel.WidthOf("1") + 40f * easel.Pixels, words.Position.X - number.Position.X, 1f);
    }

    /// <summary>A selection carried on past the end of a line runs as far past it as its look says.</summary>
    [Fact]
    public void TheSelectionRunsPastALineAsItsLookSays()
    {
        var box = new TextArea { Text = "one\ntwo", Base = { SelectionColor = Color.Orange, LineBreakSelectionWidth = 30f } };
        var easel = new Easel(box, Width, Height);
        var input = new InputRoot(box);
        input.Focus(box);
        input.Key(MyKeys.A, false, true);
        easel.Draw(Width, Height);

        var first = easel.Sprites.Where(s => s.Color == Color.Orange).OrderBy(s => s.Destination.Y).First();
        Assert.Equal(easel.WidthOf("one") + 30f * easel.Pixels, first.Destination.Width, 1f);
    }

    /// <summary>The game's text area is dim-thumbed, its thumbs lit while dragged, and has no bars until a style gives it some.</summary>
    [Fact]
    public void TheGamesTextAreaLook()
    {
        var listing = new TextArea();
        listing.Restyle();

        Assert.Equal(0f, listing.ScrollBarWidth);
        Assert.Equal(GameColors.Dim, listing.Thumb);
        Assert.Equal(32f, listing.MinThumbLength);
        Assert.Equal(GameColors.Dim, listing.LineNumberColor);
    }
}
