using Lumel.Controls;
using Lumel.Layout;
using Lumel.Styling;
using Lumel.Text;
using VRageMath;
using Xunit;

namespace Lumel.Tests;

/// <summary>
///     Trees laid out on several threads at once, as the game does with text panels while a world loads, share nothing
///     that breaks.
/// </summary>
public class ThreadingTests
{
    const int Threads = 8;
    const int Rounds = 200;

    /// <summary>Trees whose elements have looks of their own work their looks out on several threads at once.</summary>
    [Fact]
    public void TreesWorkOutTheirOwnLooksOnSeveralThreadsAtOnce()
    {
        var trees = Enumerable.Range(0, Threads).Select(_ => Tree()).ToArray();

        RunTogether(i =>
        {
            for (var round = 0; round < Rounds; round++)
                trees[i].Restyle();
        });

        foreach (var tree in trees)
            Assert.All(tree.Children.Cast<Label>(), label => Assert.Equal(Color.Red, label.Color));
    }

    /// <summary>A style shared by every thread works out each set of states once per thread at most, and each comes out whole.</summary>
    [Fact]
    public void AStyleWorksOutLooksOnSeveralThreadsAtOnce()
    {
        var hot = State.PointerOver | State.Focused;
        var style = new LabelStyle
        {
            Base = { Color = Color.Gray },
            PointerOver = { Color = Color.White },
            [State.Focused] = { FontSize = 20f },
            [hot] = { Color = Color.Red },
            Disabled = { Color = Color.Black }
        };
        var states = new[] { State.None, State.PointerOver, State.Focused, hot, State.Disabled, State.Disabled | hot };

        RunTogether(_ =>
        {
            for (var round = 0; round < Rounds; round++)
            {
                foreach (var set in states)
                    style.Lay(new LabelLook(), set);
            }
        });

        var look = new LabelLook();
        style.Lay(look, hot);
        Assert.Equal(Color.Red, look.Color);
        Assert.Equal(20f, look.FontSize);
    }

    /// <summary>A theme shared by every thread finds the style for each kind of control on several threads at once.</summary>
    [Fact]
    public void AThemeFindsStylesOnSeveralThreadsAtOnce()
    {
        var labels = new LabelStyle { Base = { Color = Color.Red } };
        var theme = new Theme(GameTheme.Theme) { labels };

        RunTogether(_ =>
        {
            for (var round = 0; round < Rounds; round++)
            {
                Assert.Same(labels, theme.StyleFor(new Label()));
                Assert.NotNull(theme.StyleFor(new Button()));
                Assert.NotNull(theme.StyleFor(new CheckBox()));
                Assert.NotNull(theme.StyleFor(new Slider()));
            }
        });
    }

    /// <summary>A tree of labels, each with a look of its own for its base and for hover.</summary>
    static VStack Tree()
    {
        var tree = new VStack();
        for (var i = 0; i < 20; i++)
        {
            var label = new Label { Base = { Color = Color.Red }, PointerOver = { Color = Color.Blue }, Text = "Label " + i };
            label.Set(State.Focused, i % 2 == 0);
            tree.Children.Add(label);
        }

        _ = new Easel(tree, 400f, 800f);
        return tree;
    }

    /// <summary>Runs <paramref name="work" /> on <see cref="Threads" /> threads, started together, and rethrows what any threw.</summary>
    static void RunTogether(Action<int> work)
    {
        var start = new Barrier(Threads);
        var failures = new Exception[Threads];
        var threads = Enumerable.Range(0, Threads).Select(i => new Thread(() =>
        {
            start.SignalAndWait();
            try
            {
                work(i);
            }
            catch (Exception failure)
            {
                failures[i] = failure;
            }
        })).ToArray();

        foreach (var thread in threads)
            thread.Start();

        foreach (var thread in threads)
            thread.Join();

        var first = failures.FirstOrDefault(f => f != null);
        if (first != null)
            throw new AggregateException(first);
    }
}
