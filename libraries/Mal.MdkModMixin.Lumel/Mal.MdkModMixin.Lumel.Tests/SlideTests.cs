using Lumel.Controls;
using Lumel.Input;
using Lumel.Layout;
using Lumel.Motion;
using Lumel.Text;
using Lumel.Viewing;
using VRageMath;
using Xunit;

namespace Lumel.Tests;

/// <summary>One content sliding in from the right as another slides out to the left, moved as it is drawn and laid out once.</summary>
public class SlideTests
{
    /// <summary>Partway through, the leaving content is drawn that far to the left and the coming one the rest of the way to the right.</summary>
    [Fact]
    public void PartwayEachIsDrawnItsShareOfTheWayAcross()
    {
        var from = new Label { Text = "leaving" };
        var to = new Label { Text = "coming" };
        var slide = new SlideTransition { From = from, To = to, Placement = FixedSize.Size(200f, 40f) };
        var easel = new Easel(new HStack { Children = { slide } }, 400f, 200f);

        Partway(slide, easel, 0.25f);
        var x = slide.Bounds.X * easel.Pixels;
        var gone = Easing.EaseOut(0.25f);

        Assert.Equal(gone, slide.Progress, 3);
        Assert.Equal(x - 200f * gone * easel.Pixels, easel.Written.Single(run => run.Text == "leaving").Position.X, 2);
        Assert.Equal(x + 200f * (1f - gone) * easel.Pixels, easel.Written.Single(run => run.Text == "coming").Position.X, 2);
    }

    /// <summary>Travel sets how far apart the two are drawn in place of the slide's width.</summary>
    [Fact]
    public void TravelSetsHowFarApartTheyAre()
    {
        var from = new Label { Text = "leaving" };
        var to = new Label { Text = "coming" };
        var slide = new SlideTransition { From = from, To = to, Placement = FixedSize.Size(200f, 40f), Travel = 300f };
        var easel = new Easel(new HStack { Children = { slide } }, 400f, 200f);

        Partway(slide, easel, 0.5f);
        var x = slide.Bounds.X * easel.Pixels;
        var gone = Easing.EaseOut(0.5f);

        Assert.Equal(x - 300f * gone * easel.Pixels, easel.Written.Single(run => run.Text == "leaving").Position.X, 2);
        Assert.Equal(x + 300f * (1f - gone) * easel.Pixels, easel.Written.Single(run => run.Text == "coming").Position.X, 2);
    }

    /// <summary>Once done, only the coming content is drawn, in place, and nothing is laid out again on the way.</summary>
    [Fact]
    public void DoneOnlyTheComingContentIsDrawnInPlace()
    {
        var slide = new SlideTransition { From = new Label { Text = "leaving" }, To = new Label { Text = "coming" } };
        var easel = new Easel(new HStack { Children = { slide } }, 400f, 200f);
        var measured = easel.Measured;

        slide.Start();
        var start = easel.Now;
        easel.Draw(400f, 200f, start);
        Assert.Equal(new[] { "leaving", "coming" }, easel.Written.Select(run => run.Text).ToArray());

        easel.Draw(400f, 200f, start + SlideTransition.SlideTicks);

        Assert.Equal(1f, slide.Progress);
        Assert.Equal(new[] { "coming" }, easel.Written.Select(run => run.Text).ToArray());
        Assert.Equal(slide.Bounds.X * easel.Pixels, easel.Written.Single().Position.X, 2);
        Assert.Equal(measured, easel.Measured);
    }

    /// <summary>While it slides it asks for every frame, and once done it asks for none.</summary>
    [Fact]
    public void ItAsksForFramesOnlyWhileItSlides()
    {
        var slide = new SlideTransition { From = new Label { Text = "leaving" }, To = new Label { Text = "coming" } };
        var easel = new Easel(new HStack { Children = { slide } }, 400f, 200f);
        Assert.False(easel.IsFrameRequested);

        slide.Start();
        var start = easel.Now;
        easel.Draw(400f, 200f, start);
        Assert.True(easel.IsFrameRequested);

        easel.Draw(400f, 200f, start + SlideTransition.SlideTicks);
        Assert.False(easel.IsFrameRequested);
    }

    /// <summary>A press reaches the coming content only once it is in place.</summary>
    [Fact]
    public void APressReachesTheComingContentOnlyInPlace()
    {
        var coming = new Button { Placement = FixedSize.Size(200f, 40f), Command = new RelayCommand(() => { }) };
        var slide = new SlideTransition { From = new Label { Text = "leaving" }, To = coming };
        var easel = new Easel(new HStack { Children = { slide } }, 400f, 200f);
        var middle = new Vector2(slide.Bounds.X + 10f, slide.Bounds.Y + 10f);
        Assert.Same(coming, slide.HitTest(middle));

        Partway(slide, easel, 0.5f);
        Assert.Null(slide.HitTest(middle));

        slide.Finish();
        Assert.Same(coming, slide.HitTest(middle));
    }

    /// <summary>The slide holds its two contents itself: what it holds is only what it is given as them.</summary>
    [Fact]
    public void ItHoldsOnlyItsTwoContents()
    {
        var first = new Label { Text = "first" };
        var second = new Label { Text = "second" };
        var slide = new SlideTransition { From = first, To = second };

        slide.From = null;

        Assert.Null(first.Parent);
        Assert.Same(slide, second.Parent);
        Assert.Equal(1, slide.VisualChildCount);
    }

    /// <summary>
    ///     Starts <paramref name="slide" /> and draws it <paramref name="share" /> of the way through its ticks on
    ///     <paramref name="easel" />.
    /// </summary>
    static void Partway(SlideTransition slide, Easel easel, float share)
    {
        slide.Start();
        var start = easel.Now;
        easel.Draw(400f, 200f, start);
        easel.Draw(400f, 200f, start + (int)(SlideTransition.SlideTicks * share));
    }
}
