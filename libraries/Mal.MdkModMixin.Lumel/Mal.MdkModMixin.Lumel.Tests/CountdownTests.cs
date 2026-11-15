using Lumel.Text;
using Lumel.Windows;
using Xunit;

namespace Lumel.Tests;

/// <summary>A countdown: run once when it is due, never once canceled, and canceled as its element stops being shown.</summary>
public class CountdownTests
{
    /// <summary>The action runs on the first tick at or after its time, and once only.</summary>
    [Fact]
    public void ItRunsOnceWhenDue()
    {
        var (layer, window) = Opened();
        var runs = 0;
        layer.Tick(100);
        var countdown = layer.After(window, 10, () => runs++);

        layer.Tick(109);
        Assert.Equal(0, runs);

        layer.Tick(112);
        layer.Tick(130);
        Assert.Equal(1, runs);
        Assert.False(countdown.IsPending);
    }

    /// <summary>A canceled countdown never runs.</summary>
    [Fact]
    public void ACanceledCountdownNeverRuns()
    {
        var (layer, window) = Opened();
        var runs = 0;
        var countdown = layer.After(window, 10, () => runs++);

        countdown.Cancel();
        layer.Tick(100);

        Assert.Equal(0, runs);
        Assert.False(countdown.IsPending);
    }

    /// <summary>
    ///     The window its element is in closing cancels the countdown, so it runs neither then nor once the window opens
    ///     again.
    /// </summary>
    [Fact]
    public void ClosingTheWindowCancelsIt()
    {
        var (layer, window) = Opened();
        var inside = new Label { Text = "Waiting" };
        window.Content = inside;
        var runs = 0;
        var countdown = layer.After(inside, 10, () => runs++);

        window.Close();
        layer.Open(window);
        layer.Tick(100);

        Assert.Equal(0, runs);
        Assert.False(countdown.IsPending);
    }

    /// <summary>An action that cancels another countdown due on the same tick keeps that one from running.</summary>
    [Fact]
    public void AnActionCanCancelOneDueWithIt()
    {
        var (layer, window) = Opened();
        var said = new List<string>();
        var later = new List<ICountdown>();
        layer.After(window, 5, () =>
        {
            said.Add("first");
            later[0].Cancel();
        });
        later.Add(layer.After(window, 5, () => said.Add("second")));

        layer.Tick(10);

        Assert.Equal(new[] { "first" }, said);
    }

    /// <summary>An element that is not shown cannot wait.</summary>
    [Fact]
    public void ALooseElementCannotWait()
    {
        var layer = new WindowLayer();

        Assert.Throws<InvalidOperationException>(() => layer.After(new Label(), 10, () => { }));
    }

    static (WindowLayer, Window) Opened()
    {
        var layer = new WindowLayer();
        var window = new Window();
        layer.Open(window);
        return (layer, window);
    }
}
