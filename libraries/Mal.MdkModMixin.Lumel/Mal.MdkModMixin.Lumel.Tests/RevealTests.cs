using Lumel.Layout;
using Lumel.Motion;
using Lumel.Text;
using Lumel.Viewing;
using Xunit;

namespace Lumel.Tests;

/// <summary>A reveal opens and closes over its own ticks, laid out a little more or less open each frame.</summary>
public class RevealTests
{
    const float Wide = 400f;
    const float High = 400f;

    /// <summary>A reveal starts closed, and opened, it shows its content a share at a time over its ticks, asking for each frame.</summary>
    [Fact]
    public void OpenedItShowsItsContentOverItsTicks()
    {
        Label content;
        var reveal = new Reveal { Content = new Label { Text = "held" }.As(out content) };
        var easel = new Easel(new VStack { Children = { reveal } }, Wide, High);
        Assert.True(reveal.IsClosed);
        Assert.Equal(0f, reveal.DesiredSize.Y);

        reveal.IsOpen = true;
        var start = easel.Now;
        easel.Draw(Wide, High, start);
        easel.Draw(Wide, High, start + Reveal.OpenTicks / 2);
        Assert.Equal(Easing.EaseOut(0.5f), reveal.Progress, 3);
        Assert.True(easel.IsFrameRequested);

        easel.Draw(Wide, High, start + Reveal.OpenTicks / 2 + 1);
        Assert.InRange(reveal.DesiredSize.Y, 1f, content.DesiredSize.Y - 1f);

        easel.Draw(Wide, High, start + Reveal.OpenTicks);
        easel.Draw(Wide, High, start + Reveal.OpenTicks + 1);
        Assert.Equal(1f, reveal.Progress);
        Assert.Equal(content.DesiredSize.Y, reveal.DesiredSize.Y, 3);
        Assert.False(easel.IsFrameRequested);
    }

    /// <summary>Closed partway open, it closes from where it had got to, and says it is closed once none of it shows.</summary>
    [Fact]
    public void ClosedPartwayItClosesFromThere()
    {
        var reveal = new Reveal { Content = new Label { Text = "held" } };
        var easel = new Easel(new VStack { Children = { reveal } }, Wide, High);

        reveal.IsOpen = true;
        var start = easel.Now;
        easel.Draw(Wide, High, start);
        easel.Draw(Wide, High, start + Reveal.OpenTicks / 2);
        var partway = reveal.Progress;

        reveal.IsOpen = false;
        var back = easel.Now;
        easel.Draw(Wide, High, back);
        Assert.Equal(partway, reveal.Progress, 3);
        Assert.False(reveal.IsClosed);

        easel.Draw(Wide, High, back + Reveal.OpenTicks);
        Assert.True(reveal.IsClosed);
    }
}
