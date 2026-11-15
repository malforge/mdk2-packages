using Lumel.Layout;
using Lumel.Text;
using Xunit;

namespace Lumel.Tests;

/// <summary>A container's children kept by key: each made once, kept while its key is shown, and moved only when out of place.</summary>
public class KeyedChildrenTests
{
    readonly VStack _list = new VStack();
    readonly List<string> _made = new List<string>();
    readonly KeyedChildren<string, Label> _rows;

    public KeyedChildrenTests()
    {
        _rows = new KeyedChildren<string, Label>(_list.Children, key =>
        {
            _made.Add(key);
            return new Label { Text = key };
        });
    }

    /// <summary>Each key gets an element, in the order given.</summary>
    [Fact]
    public void EachKeyGetsAnElementInOrder()
    {
        _rows.Show(new[] { "a", "b", "c" });

        Assert.Equal(new[] { "a", "b", "c" }, Shown());
        Assert.Equal(new[] { "a", "b", "c" }, _made);
    }

    /// <summary>Showing the same keys again makes nothing and keeps every element where it is.</summary>
    [Fact]
    public void TheSameKeysChangeNothing()
    {
        _rows.Show(new[] { "a", "b", "c" });
        var before = _list.Children.ToList();
        _made.Clear();

        _rows.Show(new[] { "a", "b", "c" });

        Assert.Empty(_made);
        Assert.Equal(before, _list.Children.ToList());
    }

    /// <summary>A new key in the middle gets the one new element, and the others are the same ones.</summary>
    [Fact]
    public void ANewKeyAddsOneElement()
    {
        _rows.Show(new[] { "a", "c" });
        var a = _rows["a"];
        var c = _rows["c"];
        _made.Clear();

        _rows.Show(new[] { "a", "b", "c" });

        Assert.Equal(new[] { "b" }, _made);
        Assert.Equal(new[] { "a", "b", "c" }, Shown());
        Assert.Same(a, _rows["a"]);
        Assert.Same(c, _rows["c"]);
    }

    /// <summary>A key no longer shown loses its element, and a later showing of it makes a new one.</summary>
    [Fact]
    public void AKeyGoneLosesItsElement()
    {
        _rows.Show(new[] { "a", "b", "c" });
        var b = _rows["b"];

        _rows.Show(new[] { "a", "c" });

        Assert.Equal(new[] { "a", "c" }, Shown());
        Assert.Null(_rows["b"]);
        Assert.Null(b!.Parent);
        Assert.Equal(2, _rows.Count);

        _rows.Show(new[] { "a", "b", "c" });
        Assert.NotSame(b, _rows["b"]);
    }

    /// <summary>A new order moves the elements, and makes none.</summary>
    [Fact]
    public void ANewOrderMovesTheSameElements()
    {
        _rows.Show(new[] { "a", "b", "c" });
        var elements = _list.Children.ToList();
        _made.Clear();

        _rows.Show(new[] { "c", "a", "b" });

        Assert.Empty(_made);
        Assert.Equal(new[] { "c", "a", "b" }, Shown());
        Assert.Equal(new[] { elements[2], elements[0], elements[1] }, _list.Children.ToList());
    }

    /// <summary>A key given twice is shown once, where it first stands.</summary>
    [Fact]
    public void AKeyGivenTwiceIsShownOnce()
    {
        _rows.Show(new[] { "a", "b", "a" });

        Assert.Equal(new[] { "a", "b" }, Shown());
    }

    /// <summary>No keys empties the container.</summary>
    [Fact]
    public void NoKeysEmptiesIt()
    {
        _rows.Show(new[] { "a", "b" });
        _rows.Show(new string[0]);

        Assert.Empty(_list.Children);
        Assert.Equal(0, _rows.Count);
    }

    string[] Shown() => _list.Children.Select(child => ((Label)child).Text).ToArray();
}
