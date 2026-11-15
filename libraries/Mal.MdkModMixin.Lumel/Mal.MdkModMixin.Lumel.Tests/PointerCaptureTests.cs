using Lumel.Controls;
using Lumel.Input;
using Lumel.Layout;
using Lumel.Screens;
using Lumel.Text;
using Lumel.Viewing;
using VRageMath;
using Xunit;

namespace Lumel.Tests;

/// <summary>
/// A press whose release never comes: it ends as another press begins, or as the screen opens or closes, and what it
/// began on lets go of whatever the press was doing.
/// </summary>
public class PointerCaptureTests
{
    const float Wide = 400f;
    const float High = 400f;

    /// <summary>A button held down stops being pressed when another press begins, and its command is not carried out.</summary>
    [Fact]
    public void AButtonHeldStopsBeingPressedWhenAnotherPressBegins()
    {
        var pressed = 0;
        var button = new Button { Text = "Go", Command = new RelayCommand(() => pressed++), Placement = FixedSize.Size(100f, 40f) };
        var other = new Button { Text = "Stop", Placement = FixedSize.Size(100f, 40f) };
        var root = new VStack { Children = { button, other } };
        _ = new Easel(root, Wide, High);
        var input = new InputRoot(root);

        input.PointerDown(Middle(button), PointerButton.Left, 1, false, false);
        Assert.True(button.IsPressed);

        input.PointerDown(Middle(other), PointerButton.Right, 1, false, false);

        Assert.False(button.IsPressed);
        Assert.Equal(0, pressed);
    }

    /// <summary>A button held down as its screen closes, or opens again, is no longer pressed, and a release after it reaches nothing.</summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AButtonHeldAsTheScreenClosesOrOpensIsLetGo(bool closing)
    {
        var pressed = 0;
        var button = new Button { Text = "Go", Command = new RelayCommand(() => pressed++), Placement = FixedSize.Size(100f, 40f) };
        var workspace = new Workspace().With(new VStack { Children = { button } });
        var area = new RectangleF(0f, 0f, Wide, High);
        workspace.DrawScreen(new Recording(area), area);
        var on = Middle(button);

        workspace.PointerDown(on, PointerButton.Left, 1, false, false);
        Assert.True(button.IsPressed);

        if (closing)
            ((IScreenLifetime)workspace).Closed(false);
        else
            ((IScreenLifetime)workspace).Opened();

        Assert.False(button.IsPressed);

        workspace.PointerUp(on, PointerButton.Left);
        Assert.Equal(0, pressed);
    }

    /// <summary>A dial being turned stops being dragged when the press is lost.</summary>
    [Fact]
    public void ADialTurnedStopsBeingDraggedWhenThePressIsLost()
    {
        var dial = new Dial();
        var root = new VStack { Children = { dial } };
        _ = new Easel(root, Wide, High);
        var input = new InputRoot(root);

        input.PointerDown(dial.ToScreen(new Vector2(dial.ContentBounds.Right - 2f, dial.ContentBounds.Center.Y)), PointerButton.Left, 1, false, false);
        Assert.True(dial.IsDragging);

        input.ReleasePointerCapture();

        Assert.False(dial.IsDragging);
    }

    /// <summary>A scroll view's thumb held stops being dragged when the press is lost, and moving the pointer then moves nothing.</summary>
    [Fact]
    public void AScrollThumbHeldStopsBeingDraggedWhenThePressIsLost()
    {
        var content = new VStack { Children = { new VStack { Placement = FixedSize.Height(1000f) } } };
        var scroll = new ScrollView
        {
            Style = new ScrollViewStyle { Base = { ScrollBarWidth = 12f, Thumb = Color.White } },
            Content = content,
            Placement = FixedSize.Size(Wide, High)
        };
        var root = new VStack { Children = { scroll } };
        _ = new Easel(root, Wide, High);
        var input = new InputRoot(root);
        var top = new Vector2(Wide - 6f, 10f);

        input.PointerDown(scroll.HitTest(top), top, PointerButton.Left, 1, false, false);
        Assert.True(scroll.IsDragging);

        input.ReleasePointerCapture();
        input.PointerMove(top + new Vector2(0f, 100f));

        Assert.False(scroll.IsDragging);
        Assert.Equal(0f, scroll.VerticalOffset);
    }

    /// <summary>A text area's bar held stops being dragged when the press is lost.</summary>
    [Fact]
    public void ATextAreasBarHeldStopsBeingDraggedWhenThePressIsLost()
    {
        var listing = new TextArea
        {
            IsReadOnly = true,
            Text = string.Join("\n", Enumerable.Range(1, 500).Select(i => "line " + i)),
            Base = { ScrollBarWidth = 20f, Thumb = Color.White }
        };
        var root = new VStack { Children = { listing } };
        _ = new Easel(root, Wide, High);
        var input = new InputRoot(root);

        input.PointerDown(listing, new Vector2(Wide - 10f, 10f), PointerButton.Left, 1, false, false);
        Assert.True(listing.IsDragging);

        input.ReleasePointerCapture();

        Assert.False(listing.IsDragging);
        Assert.False(listing.IsPanning);
    }

    /// <summary>A row dragged part way when the press is lost goes back where it stood: nothing is reordered, and the list comes to rest.</summary>
    [Fact]
    public void ARowDraggedPartWayGoesBackWhenThePressIsLost()
    {
        var list = new ReorderList();
        var rows = new[] { new Row("one"), new Row("two"), new Row("three") };
        foreach (var row in rows)
            list.Children.Add(row);

        var root = new VStack { Children = { list } };
        var easel = new Easel(root, Wide, High);
        var input = new InputRoot(root);
        var reordered = 0;
        list.Reordered += _ => reordered++;
        var height = rows[0].Bounds.Height;

        var at = rows[0].DragHandle.ToScreen(new Vector2(rows[0].DragHandle.Bounds.X + 4f, rows[0].DragHandle.Bounds.Y + 4f));
        input.PointerDown(at, PointerButton.Left, 1, false, false);
        input.PointerMove(at + new Vector2(0f, height * 1.2f));
        easel.Draw(Wide, High, easel.Now + 1);
        Assert.True(list.IsDragging);

        input.ReleasePointerCapture();

        Assert.False(rows[0].DragHandle.IsDragging);
        Assert.False(rows[0].IsDragging);
        Assert.False(list.IsDragging);
        Assert.Equal(0, reordered);

        easel.Draw(Wide, High);
        Assert.False(easel.IsFrameRequested);
        Assert.All(rows, row => Assert.Equal(0f, row.RenderTransform.Translate.Y));
    }

    static Vector2 Middle(Element element) => element.ToScreen(element.Bounds.Center);

    /// <summary>A row of a list: a handle and some words.</summary>
    sealed class Row : HStack, IReorderable, IDragged
    {
        public Row(string words)
        {
            Children.Add(DragHandle);
            Children.Add(new Label { Text = words });
        }

        public DragHandle DragHandle { get; } = new DragHandle { CanDrag = true, Placement = FixedSize.Size(24f, 24f) };

        public bool IsDragging { get; private set; }

        public int DraggedWithCount => 0;

        bool IDragged.IsDragging
        {
            set { IsDragging = value; }
        }
    }
}
