using Lumel.Controls;
using Lumel.Images;
using Lumel.Input;
using Lumel.Layout;
using Lumel.Text;
using Xunit;

namespace Lumel.Tests;

/// <summary>Pages under a strip of headers: only the page of the selected tab is laid out and drawn, and its header takes no press.</summary>
public class TabViewTests
{
    /// <summary>Presses the header of <paramref name="tab" />.</summary>
    static void Press(Tab tab) => tab.Command.Execute();

    static TabView Two(out Label first, out Label second)
    {
        return new TabView
        {
            Tabs =
            {
                new Tab { Header = "One", Content = new Label { Text = "first page" }.As(out first) },
                new Tab { Header = "Two", Content = new Label { Text = "second page" }.As(out second) }
            }
        };
    }

    /// <summary>The first tab's page shows, and only it: the other is held in the view, and is not drawn.</summary>
    [Fact]
    public void OnlyTheSelectedPageIsDrawn()
    {
        var tabs = Two(out var first, out var second);
        var easel = new Easel(new VStack { Children = { tabs } }, 400f, 200f);

        var written = easel.Written.Select(run => run.Text).ToArray();
        Assert.Contains("first page", written);
        Assert.DoesNotContain("second page", written);
        Assert.Same(first.Parent, second.Parent);
        Assert.Same(tabs, first.Parent!.Parent);
    }

    /// <summary>A tab view is as large as its largest page, so choosing another tab never changes its size.</summary>
    [Fact]
    public void ATabViewIsAsLargeAsItsLargestPage()
    {
        var tabs = new TabView
        {
            Tabs =
            {
                new Tab { Header = "Short", Content = new Label { Text = "short", Placement = FixedSize.Size(80f, 40f) } },
                new Tab { Header = "Tall", Content = new Label { Text = "tall", Placement = FixedSize.Size(120f, 160f) } }
            }
        };
        var easel = new Easel(new VStack { Children = { new HStack { Children = { tabs } } } }, 400f, 400f);
        var first = tabs.Bounds;

        tabs.SelectedIndex = 1;
        easel.Draw(400f, 400f);

        Assert.Equal(first, tabs.Bounds);
        Assert.True(first.Height >= 160f, $"the view stands {first.Height} tall");
    }

    /// <summary>Pressing another header shows its page in place of the one there, says so, and leaves the pressed header with no press of its own.</summary>
    [Fact]
    public void PressingAHeaderShowsItsPage()
    {
        var tabs = Two(out _, out _);
        var switched = -1;
        tabs.SelectionChanged += at => switched = at;
        var easel = new Easel(new VStack { Children = { tabs } }, 400f, 200f);
        Assert.Null(tabs.Tabs[0].Command);

        Press(tabs.Tabs[1]);
        easel.Draw(400f, 200f);

        var written = easel.Written.Select(run => run.Text).ToArray();
        Assert.Equal(1, switched);
        Assert.Contains("second page", written);
        Assert.DoesNotContain("first page", written);
        Assert.True(tabs.Tabs[1].IsSelected);
        Assert.False(tabs.Tabs[0].IsSelected);
        Assert.Null(tabs.Tabs[1].Command);
        Assert.NotNull(tabs.Tabs[0].Command);
    }

    /// <summary>A badge hidden when the header was laid out is given its room once it is shown, on the header of the page shown as on any other.</summary>
    [Fact]
    public void ABadgeShownLaterIsGivenItsRoom()
    {
        var mark = new Glyph { Symbol = "!", IsVisible = false };
        var tabs = new TabView { Tabs = { new Tab { Header = "Sequences", Badge = mark, Content = new Label { Text = "page" } } } };
        var easel = new Easel(new VStack { Children = { tabs } }, 400f, 200f);
        Assert.True(tabs.Tabs[0].IsSelected);

        mark.IsVisible = true;
        easel.Draw(400f, 200f);

        var words = easel.Written.Single(run => run.Text == "Sequences");
        Assert.True(mark.Bounds.Width > 0f, "the badge was given no room");
        Assert.True(mark.Bounds.X * easel.Pixels >= words.Position.X + easel.WidthOf("Sequences") - 0.5f);
    }

    /// <summary>A header's badge is drawn after its words, and leaves the header's words alone while it is hidden.</summary>
    [Fact]
    public void ABadgeIsDrawnAfterTheWords()
    {
        var mark = new Glyph { Symbol = "!" };
        var tabs = new TabView { Tabs = { new Tab { Header = "Sequences", Badge = mark, Content = new Label { Text = "page" } } } };
        var easel = new Easel(new VStack { Children = { tabs } }, 400f, 200f);

        var words = easel.Written.Single(run => run.Text == "Sequences");
        Assert.True(mark.Bounds.X * easel.Pixels >= words.Position.X + easel.WidthOf("Sequences") - 0.5f);

        mark.IsVisible = false;
        easel.Draw(400f, 200f);
        Assert.Contains(easel.Written, run => run.Text == "Sequences");
        Assert.DoesNotContain(easel.Written, run => run.Text == "!");
    }

    /// <summary>Changing the page of the selected tab shows the new page in place of the old.</summary>
    [Fact]
    public void ChangingTheSelectedTabsPageShowsTheNewOne()
    {
        var tabs = Two(out var first, out _);
        var easel = new Easel(new VStack { Children = { tabs } }, 400f, 200f);
        var replaced = new Label { Text = "replaced page" };

        tabs.Tabs[0].Content = replaced;
        easel.Draw(400f, 200f);

        var written = easel.Written.Select(run => run.Text).ToArray();
        Assert.Contains("replaced page", written);
        Assert.DoesNotContain("first page", written);
        Assert.Null(first.Parent);
    }

    /// <summary>Taking the selected tab out shows the one that takes its place and says so; taking one before it keeps the same page shown.</summary>
    [Fact]
    public void TakingTabsOutKeepsAPageShown()
    {
        var tabs = new TabView
        {
            Tabs =
            {
                new Tab { Header = "One", Content = new Label { Text = "one" } },
                new Tab { Header = "Two", Content = new Label { Text = "two" } },
                new Tab { Header = "Three", Content = new Label { Text = "three" } }
            }
        };
        var easel = new Easel(new VStack { Children = { tabs } }, 400f, 200f);
        var switched = -1;
        tabs.SelectionChanged += at => switched = at;
        tabs.SelectedIndex = 1;
        switched = -1;

        var one = tabs.Tabs[0];
        tabs.Tabs.RemoveAt(0);
        easel.Draw(400f, 200f);
        Assert.Equal(0, tabs.SelectedIndex);
        Assert.Equal(-1, switched);
        Assert.Contains(easel.Written, run => run.Text == "two");
        Assert.Null(one.Command);
        Assert.False(one.IsSelected);

        tabs.Tabs.RemoveAt(0);
        easel.Draw(400f, 200f);
        Assert.Equal(0, switched);
        Assert.Contains(easel.Written, run => run.Text == "three");
        Assert.DoesNotContain(easel.Written, run => run.Text == "Two");
    }

    /// <summary>A tab taken out of the view has no parent left, the others stay in the view, and clearing leaves none with one.</summary>
    [Fact]
    public void ATabTakenOutHasNoParent()
    {
        var tabs = Two(out _, out _);
        _ = new Easel(new VStack { Children = { tabs } }, 400f, 200f);
        var one = tabs.Tabs[0];
        var two = tabs.Tabs[1];

        tabs.Tabs.Remove(one);

        Assert.Null(one.Parent);
        Assert.NotNull(two.Parent);
        Assert.Same(tabs, two.Parent!.Parent);

        tabs.Tabs.Clear();
        Assert.Null(two.Parent);
    }

    /// <summary>A tab put in before the selected one leaves the same page shown, its header standing where it was put.</summary>
    [Fact]
    public void ATabInsertedBeforeTheSelectedOneLeavesThePageShown()
    {
        var tabs = Two(out _, out _);
        var easel = new Easel(new VStack { Children = { tabs } }, 400f, 200f);

        tabs.Tabs.Insert(0, new Tab { Header = "Zero", Content = new Label { Text = "zero page" } });
        easel.Draw(400f, 200f);

        Assert.Equal(1, tabs.SelectedIndex);
        Assert.Contains(easel.Written, run => run.Text == "first page");
        Assert.True(tabs.Tabs[0].Bounds.X < tabs.Tabs[1].Bounds.X);
    }

    /// <summary>Clearing the tabs leaves no header and no page.</summary>
    [Fact]
    public void ClearingTheTabsLeavesNothingShown()
    {
        var tabs = Two(out var first, out _);
        var easel = new Easel(new VStack { Children = { tabs } }, 400f, 200f);

        tabs.Tabs.Clear();
        easel.Draw(400f, 200f);

        Assert.Equal(0, tabs.Tabs.Count);
        Assert.Empty(easel.Written);
        Assert.Null(first.Parent);
    }

    /// <summary>A tab's command is its view's: a tab given one of its own is refused, and an index with no tab is refused.</summary>
    [Fact]
    public void ATabWithACommandAndAMissingIndexAreRefused()
    {
        var tabs = Two(out _, out _);
        var own = new Tab { Header = "Own", Command = new RelayCommand(() => { }) };

        Assert.Throws<ArgumentException>(() => tabs.Tabs.Add(own));
        Assert.Equal(2, tabs.Tabs.Count);
        Assert.Throws<ArgumentException>(() => tabs.SelectedIndex = 2);
    }

    /// <summary>A header's words sit where its look puts them across it: in the middle unless told otherwise.</summary>
    [Fact]
    public void AHeadersWordsSitWhereItsLookPutsThem()
    {
        var tabs = new TabView { Tabs = { new Tab { Header = "One", Content = new Label { Text = "page" } } } };
        var easel = new Easel(new VStack { Children = { tabs } }, 400f, 200f);
        var tab = tabs.Tabs[0];
        var middle = easel.Written.Single(run => run.Text == "One").Position.X;
        Assert.True(middle > tab.ContentBounds.X * easel.Pixels + 10f, "the words were not in the middle");

        tab.Base.ContentAlign = Align.Left;
        easel.Draw(400f, 200f);

        Assert.Equal(tab.ContentBounds.X * easel.Pixels, easel.Written.Single(run => run.Text == "One").Position.X, 1);
    }
}
