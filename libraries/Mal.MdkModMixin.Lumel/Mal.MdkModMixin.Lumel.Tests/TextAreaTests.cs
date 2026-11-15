using Lumel.Input;
using System.Text;
using VRageMath;
using Xunit;
using Lumel.Text;

namespace Lumel.Tests;

/// <summary>
/// A listing of thousands of lines costs a window's worth of drawing, not a listing's worth.
/// </summary>
/// <remarks>
/// The defect this guards against draws the right picture: every line issued as a call, the scissor
/// throwing away all but a screenful. Nothing on screen says it is happening, so it is counted.
/// </remarks>
public class TextAreaTests
{
    const float Width = 800f;
    const float Height = 600f;

    static string Lines(int count)
    {
        var text = new StringBuilder();
        for (var i = 0; i < count; i++)
            text.Append("line ").Append(i).Append('\n');

        return text.ToString(0, text.Length - 1);
    }

    static TextArea Long(int count)
    {
        return new TextArea { IsReadOnly = true, Text = Lines(count) };
    }

    /// <summary>Lays out and draws <paramref name="box" />, for a test that is about neither.</summary>
    /// <remarks>
    /// A control has no bounds until it has been arranged, so anything that presses at one has to
    /// have drawn it first even where the frame is not what is being asked about.
    /// </remarks>
    static void Laid(TextArea box)
    {
        new Easel(box, Width, Height).Draw(Width, Height);
    }

    [Fact]
    public void ItHoldsEveryLineItWasGiven()
    {
        Assert.Equal(5000, Long(5000).LineCount);
    }

    /// <summary>Its height is the line height times the count, which is arithmetic, not measurement.</summary>
    [Fact]
    public void ItStandsAsTallAsItsLines()
    {
        TextArea listing = Long(200);
        var easel = new Easel(listing, Width, Height);

        Assert.Equal(200 * easel.LineHeight, listing.ExtentHeight, 2);
    }

    /// <summary>It takes the room it is given rather than growing to the text.</summary>
    [Fact]
    public void ItIsAWindowAndNotAColumn()
    {
        TextArea listing = Long(5000);
        var easel = new Easel(listing, Width, Height);

        Assert.Equal(Height, listing.DesiredSize.Y, 2);
        Assert.True(listing.ExtentHeight > Height, "the listing is shorter than its own window, so this proves nothing");
        Assert.True(easel.Written.Count() * easel.LineHeight <= Height + easel.LineHeight,
            "it drew more lines than the window is tall");
    }

    /// <summary>A window held still measures nothing, however many frames go past.</summary>
    [Fact]
    public void AWindowHeldStillMeasuresNothing()
    {
        var easel = new Easel(Long(5000), Width, Height);

        int settled = easel.Measured;

        for (var i = 0; i < 60; i++)
            easel.Draw(Width, Height);

        Assert.Equal(settled, easel.Measured);
    }

    /// <summary>
    /// Only the lines somebody has actually looked at are ever measured.
    /// </summary>
    /// <remarks>
    /// The number is the point, and it is the whole reason the sideways reach is learned rather than
    /// swept: measuring a thousand lines up front was timed at 22ms, which is a frame and a half of
    /// frozen game for text nobody has scrolled to yet.
    /// </remarks>
    [Fact]
    public void ItMeasuresOnlyTheLinesItHasShown()
    {
        var easel = new Easel(Long(1000), Width, Height);

        var fits = (int)(Height / easel.LineHeight);

        Assert.InRange(easel.Measured, fits - 1, fits + 2);
    }

    /// <summary>
    /// Moving down one line measures one line, because one line is all that came into view.
    /// </summary>
    /// <remarks>
    /// What it remembers is the band it showed last frame, not a note per line, so this is the number
    /// that says the remembering works.
    /// </remarks>
    [Fact]
    public void ScrollingMeasuresOnlyWhatComesIntoView()
    {
        TextArea listing = Long(1000);
        var easel = new Easel(listing, Width, Height);

        int settled = easel.Measured;

        listing.ScrollToVerticalOffset(easel.LineHeight);
        easel.Draw(Width, Height);

        Assert.Equal(settled + 1, easel.Measured);
    }

    /// <summary>The reach grows as longer lines are met, and never shrinks back.</summary>
    [Fact]
    public void TheReachGrowsAsLongerLinesAreMet()
    {
        var listing = new TextArea { IsReadOnly = true, Text = Lines(500) + "\n" + new string('x', 400) + "\n" + Lines(500) };
        var easel = new Easel(listing, Width, Height);

        float known = listing.ExtentWidth;
        Assert.False(listing.CanScrollHorizontally, "the short lines at the top already reach past the window");

        listing.ScrollToVerticalOffset(500f * easel.LineHeight);
        easel.Draw(Width, Height);

        Assert.True(listing.ExtentWidth > known, "meeting the long line taught it nothing");
        Assert.True(listing.CanScrollHorizontally, "the long line is on screen and still cannot be reached");

        float met = listing.ExtentWidth;

        listing.ScrollToVerticalOffset(0f);
        easel.Draw(Width, Height);

        Assert.Equal(met, listing.ExtentWidth);
    }

    /// <summary>The whole point: the calls are what the window shows and nothing more.</summary>
    [Fact]
    public void ItDrawsOnlyWhatIsOnShow()
    {
        var easel = new Easel(Long(5000), Width, Height);

        var fits = (int)(Height / easel.LineHeight);

        Assert.InRange(easel.Written.Count(), fits - 1, fits + 2);
    }

    /// <summary>Scrolling moves the window rather than growing it.</summary>
    [Fact]
    public void ScrollingShowsOtherLinesAndNoMoreOfThem()
    {
        TextArea listing = Long(5000);
        var easel = new Easel(listing, Width, Height);

        int before = easel.Written.Count();
        Assert.Contains(easel.Written, c => c.Text == "line 0");

        listing.ScrollToVerticalOffset(4000f * easel.LineHeight);
        easel.Draw(Width, Height);

        Assert.DoesNotContain(easel.Written, c => c.Text == "line 0");
        Assert.Contains(easel.Written, c => c.Text == "line 4000");
        Assert.InRange(easel.Written.Count(), before - 1, before + 1);
    }

    /// <summary>A line longer than the room is reachable, which is what the sideways axis is for.</summary>
    [Fact]
    public void ALineTooWideForTheRoomCanBeReached()
    {
        var listing = new TextArea { IsReadOnly = true, Text = new string('x', 400) };
        var easel = new Easel(listing, Width, Height);

        Assert.True(listing.ExtentWidth > Width, "the line fits, so there is nothing to reach");
        Assert.True(listing.CanScrollHorizontally, "it cannot be moved sideways");

        float first = easel.Written.First().Position.X;

        listing.ScrollHorizontallyBy(listing.ExtentWidth);
        easel.Draw(Width, Height);

        Assert.True(easel.Written.First().Position.X < first, "moving sideways drew it in the same place");
    }

    /// <summary>Sideways stops at the end of the longest line rather than running on into nothing.</summary>
    [Fact]
    public void SidewaysStopsAtTheLongestLine()
    {
        var listing = new TextArea { IsReadOnly = true, Text = new string('x', 400) };
        var easel = new Easel(listing, Width, Height);

        listing.ScrollHorizontallyBy(100000f);
        easel.Draw(Width, Height);

        Assert.Equal(listing.ExtentWidth - Width, listing.HorizontalOffset, 1);
    }

    /// <summary>A thumb is drawn for each direction that has something out of sight, and no others.</summary>
    [Fact]
    public void AThumbIsDrawnForEachDirectionThatHasSomewhereToGo()
    {
        var listing = new TextArea
        {
            IsReadOnly = true,
            Text = Lines(500),
            Base = { ScrollBarWidth = 8f, Thumb = Color.White }
        };

        var easel = new Easel(listing, Width, Height);

        Assert.Single(easel.Sprites);

        // Put a line longer than the window at the top, where it is met straight away. Down at the
        // bottom it would not be, and that is the point of the test above rather than this one.
        listing.Text = new string('x', 400) + "\n" + Lines(500);
        easel.Draw(Width, Height);

        Assert.Equal(2, easel.Sprites.Count());
    }

    /// <summary>
    /// The text can be taken hold of and dragged, and goes where the hand goes.
    /// </summary>
    /// <remarks>
    /// Dragging upwards pushes the sheet up, which brings what is below into view - so the window moves
    /// down. Getting this the wrong way round is the classic bug and reads as the screen fighting you.
    /// </remarks>
    [Fact]
    public void TheTextCanBeDraggedAbout()
    {
        TextArea listing = Long(5000);
        var easel = new Easel(listing, Width, Height);

        ((IInputTarget)listing).PointerDown(new Vector2(100f, 100f), PointerButton.Left, 1, false, false);
        Assert.True(listing.IsPanning);

        ((IInputTarget)listing).PointerMove(new Vector2(100f, 40f), PointerButton.Left);
        easel.Draw(Width, Height);

        Assert.Equal(60f, listing.VerticalOffset, 1);

        ((IInputTarget)listing).PointerUp(new Vector2(100f, 40f), PointerButton.Left);
        Assert.False(listing.IsPanning);

        ((IInputTarget)listing).PointerMove(new Vector2(100f, 10f), PointerButton.Left);
        Assert.Equal(60f, listing.VerticalOffset, 1);
    }

    /// <summary>Dragging sideways moves it sideways, and stops where the longest line stops.</summary>
    [Fact]
    public void DraggingSidewaysMovesItSideways()
    {
        var listing = new TextArea { IsReadOnly = true, Text = new string('x', 400) };
        Laid(listing);

        ((IInputTarget)listing).PointerDown(new Vector2(100f, 10f), PointerButton.Left, 1, false, false);
        ((IInputTarget)listing).PointerMove(new Vector2(40f, 10f), PointerButton.Left);

        Assert.Equal(60f, listing.HorizontalOffset, 1);

        ((IInputTarget)listing).PointerMove(new Vector2(-100000f, 10f), PointerButton.Left);
        Assert.Equal(listing.ExtentWidth - Width, listing.HorizontalOffset, 1);
    }

    /// <summary>A press on a thumb is the thumb being dragged, not the text.</summary>
    [Fact]
    public void APressOnAThumbTakesTheThumb()
    {
        var listing = new TextArea { IsReadOnly = true, Text = Lines(5000), Base = { ScrollBarWidth = 20f, Thumb = Color.White } };
        Laid(listing);

        ((IInputTarget)listing).PointerDown(new Vector2(Width - 10f, 10f), PointerButton.Left, 1, false, false);
        Assert.False(listing.IsPanning);

        // Carried down, a thumb goes down with the hand, where the text would have gone the other way.
        ((IInputTarget)listing).PointerMove(new Vector2(Width - 10f, 30f), PointerButton.Left);
        Assert.True(listing.VerticalOffset > 0f, "the thumb did not move the listing down");
    }

    /// <summary>Text with nowhere to go is not dragged about.</summary>
    [Fact]
    public void TextThatFitsIsNotDraggedAbout()
    {
        var listing = new TextArea { IsReadOnly = true, Text = "one\ntwo" };
        _ = new Easel(listing, Width, Height);

        ((IInputTarget)listing).PointerDown(new Vector2(100f, 10f), PointerButton.Left, 1, false, false);
        Assert.False(listing.IsPanning);
    }

    static TextArea Numbered(string text)
    {
        return new TextArea { IsReadOnly = true, Text = text, ShowLineNumbers = true, Base = { LineNumberColor = Color.White } };
    }

    static float WhereIs(Easel easel, string said)
    {
        return easel.Written.First(c => c.Text == said).Position.X;
    }

    /// <summary>Lines are numbered from one, in a gutter, for the lines on show.</summary>
    [Fact]
    public void TheGutterNumbersTheLinesOnShow()
    {
        var easel = new Easel(Numbered(Lines(5000)), Width, Height);

        Assert.Contains(easel.Written, c => c.Text == "1");
        Assert.Contains(easel.Written, c => c.Text == "2");
        Assert.DoesNotContain(easel.Written, c => c.Text == "0");
        Assert.DoesNotContain(easel.Written, c => c.Text == "4000");
    }

    /// <summary>Nothing is numbered unless somebody asked for it.</summary>
    [Fact]
    public void ThereIsNoGutterUnlessItWasAskedFor()
    {
        var easel = new Easel(Long(5000), Width, Height);

        Assert.DoesNotContain(easel.Written, c => c.Text == "1");
    }

    /// <summary>
    /// The numbers are right against the words, whatever they run to.
    /// </summary>
    /// <remarks>
    /// Ragged on the left, flush on the right, which is what makes a column of numbers readable as a
    /// column rather than as a list of different-length things.
    /// </remarks>
    [Fact]
    public void TheNumbersAreFlushAgainstTheWords()
    {
        TextArea listing = Numbered(Lines(5000));
        var easel = new Easel(listing, Width, Height);

        float nine = WhereIs(easel, "9");
        float digit = nine - WhereIs(easel, "10");

        Assert.True(digit > 0f, "a two-digit number did not start further left than a one-digit one");

        // Line 100 is not on the first screenful, so go and find it.
        listing.ScrollToVerticalOffset(95f * easel.LineHeight);
        easel.Draw(Width, Height);

        Assert.Equal(nine - 2f * digit, WhereIs(easel, "100"), 1);
    }

    /// <summary>
    /// The numbers end on one line, in a face whose digits are not all one width.
    /// </summary>
    /// <remarks>
    /// The game's own face has a <c>1</c> less than half the width of a <c>0</c>, so a number placed by
    /// counting its digits ends short of the margin by however many narrow ones it contains. The
    /// stand-in face cannot show that - its digits are all one width - so this asks for the real one,
    /// and says so rather than quietly passing where it cannot be had.
    /// </remarks>
    [Fact]
    public void TheNumbersEndOnOneLineWhateverDigitsTheyHold()
    {
        var easel = new Easel(Numbered(Lines(40)), Width, Height, true);

        if (!easel.IsExact)
            Assert.Skip("the game's own font is not here, and the stand-in's digits are all one width");

        float Ends(string said) => WhereIs(easel, said) + easel.WidthOf(said);

        // The narrowest digit alone, two of them, and one beside a wide one.
        Assert.Equal(Ends("9"), Ends("1"), 1);
        Assert.Equal(Ends("9"), Ends("11"), 1);
        Assert.Equal(Ends("9"), Ends("10"), 1);
    }

    /// <summary>
    /// Dragging the words sideways leaves the gutter where it is.
    /// </summary>
    /// <remarks>
    /// The one arrangement in which numbers are any use: somebody following a long line across is
    /// exactly the person who needs to know which line they are on.
    /// </remarks>
    [Fact]
    public void TheGutterHoldsStillWhileTheWordsMove()
    {
        TextArea listing = Numbered(new string('x', 400) + "\n" + Lines(500));
        var easel = new Easel(listing, Width, Height);

        float gutter = WhereIs(easel, "2");
        float words = WhereIs(easel, "line 0");

        listing.ScrollHorizontallyBy(120f);
        easel.Draw(Width, Height);

        Assert.Equal(gutter, WhereIs(easel, "2"), 2);
        Assert.Equal(words - 120f * easel.Pixels, WhereIs(easel, "line 0"), 2);
    }

    /// <summary>The gutter takes its room from the words, so a long line cannot run under it.</summary>
    [Fact]
    public void TheGutterTakesItsRoomFromTheWords()
    {
        var wide = new string('x', 400);

        var plain = new TextArea { IsReadOnly = true, Text = wide };
        var withGutter = Numbered(wide);

        var first = new Easel(plain, Width, Height);
        var second = new Easel(withGutter, Width, Height);

        Assert.True(WhereIs(second, wide) > WhereIs(first, wide), "the words did not move over for it");
        Assert.True(withGutter.CanScrollHorizontally);
    }

    /// <summary>An empty one draws nothing and still stands a line tall, so a box is not zero high.</summary>
    [Fact]
    public void EmptyItHasOneLineOfExtent()
    {
        var listing = new TextArea { IsReadOnly = true };
        var easel = new Easel(listing, Width, Height);

        Assert.Empty(easel.Written);
        Assert.Equal(easel.LineHeight, listing.ExtentHeight, 2);
    }
}
