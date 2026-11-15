using Lumel.Motion;
using VRageMath;
using Xunit;

namespace Lumel.Tests;

/// <summary>A transition: at rest until started, then from 0 to 1 over its ticks from the next frame drawn, asking for frames only while it runs.</summary>
public class TransitionTests
{
    static readonly RectangleF Area = new RectangleF(0f, 0f, 400f, 300f);

    /// <summary>One never started is at its end and asks for nothing.</summary>
    [Fact]
    public void NeverStartedItIsAtRestAtItsEnd()
    {
        var g = new Recording(Area);

        Assert.Equal(1f, new Transition(10).Progress(g));
        Assert.Equal(0, g.FramesRequested);
    }

    /// <summary>Started, it runs from the next frame drawn to its end over its ticks, asking for frames until it gets there.</summary>
    [Fact]
    public void StartedItRunsFromTheNextFrameOverItsTicks()
    {
        var g = new Recording(Area) { Now = 100 };
        var fade = new Transition(10);
        fade.Start();

        Assert.Equal(0f, fade.Progress(g));
        g.Now = 105;
        Assert.Equal(0.5f, fade.Progress(g), 3);
        Assert.Equal(2, g.FramesRequested);

        g.Now = 110;
        Assert.Equal(1f, fade.Progress(g));
        Assert.False(fade.IsRunning);
        Assert.Equal(2, g.FramesRequested);
    }

    /// <summary>Started with a delay, it stays at 0 for the delay, still asking for frames, and then runs.</summary>
    [Fact]
    public void ADelayHoldsItAtTheStart()
    {
        var g = new Recording(Area);
        var pop = new Transition(10);
        pop.Start(20);

        Assert.Equal(0f, pop.Progress(g));
        g.Now = 20;
        Assert.Equal(0f, pop.Progress(g));
        g.Now = 25;
        Assert.Equal(0.5f, pop.Progress(g), 3);
        Assert.Equal(3, g.FramesRequested);
    }

    /// <summary>Its curve eases how far it has run.</summary>
    [Fact]
    public void ItsCurveEasesItsProgress()
    {
        var g = new Recording(Area);
        var slide = new Transition(10, Easing.EaseOut);
        slide.Start();
        slide.Progress(g);

        g.Now = 5;
        Assert.Equal(Easing.EaseOut(0.5f), slide.Progress(g), 3);
    }

    /// <summary>Finished, it is at its end at once.</summary>
    [Fact]
    public void FinishedItIsAtItsEnd()
    {
        var g = new Recording(Area);
        var fade = new Transition(10);
        fade.Start();
        fade.Progress(g);

        fade.Finish();

        Assert.Equal(1f, fade.Progress(g));
        Assert.False(fade.IsRunning);
    }

    /// <summary>A clock that goes back ends it.</summary>
    [Fact]
    public void AClockGoingBackEndsIt()
    {
        var g = new Recording(Area) { Now = 500 };
        var fade = new Transition(10);
        fade.Start();
        fade.Progress(g);

        g.Now = 3;

        Assert.Equal(1f, fade.Progress(g));
        Assert.False(fade.IsRunning);
    }

    /// <summary>Every curve starts at 0 and ends at 1; ease out is ahead of even at the middle, and the overshoot passes the end before it settles.</summary>
    [Fact]
    public void TheCurvesKeepTheirEndsAndTheirShapes()
    {
        foreach (var curve in new[] { Easing.EaseOut, Easing.EaseInOut, Easing.EaseOutBack })
        {
            Assert.Equal(0f, curve(0f), 4);
            Assert.Equal(1f, curve(1f), 4);
        }

        Assert.True(Easing.EaseOut(0.5f) > 0.5f);
        Assert.Equal(0.5f, Easing.EaseInOut(0.5f), 4);
        Assert.True(Easing.EaseOutBack(0.7f) > 1f);
    }
}
