using Lumel.Controls;
using Lumel.Input;
using Lumel.Layout;
using Lumel.Styling;
using Lumel.Text;
using VRageMath;
using Xunit;

namespace Lumel.Tests;

/// <summary>A drag handle and a list whose rows are reordered by theirs.</summary>
public class DragTests
{
    const float Wide = 400f;
    const float High = 400f;

    static readonly Color Faint = new Color(133, 161, 169);
    static readonly Color Ink = new Color(220, 245, 253);
    static readonly Color Live = new Color(58, 204, 0);

    static readonly DragHandleStyle Dots = new DragHandleStyle
    {
        Base = { Color = Faint, DotSize = 4f, DotSpacing = 2f },
        PointerOver = { Color = Ink },
        Dragging = { Color = Live }
    };

    /// <summary>A handle's dots are as big and as far apart as its look says, two across and three down.</summary>
    [Fact]
    public void AHandleIsAsBigAsItsDotsSay()
    {
        var handle = new DragHandle { Style = Dots };
        _ = new Easel(new HStack { Children = { handle } }, Wide, High);

        Assert.Equal(new Vector2(2 * 4f + 2f, 3 * 4f + 2 * 2f), handle.DesiredSize);
        Assert.Equal(4f, handle.DotSize);
        Assert.Equal(2f, handle.DotSpacing);
    }

    /// <summary>A handle takes its hover look on hover and its dragging look while dragged.</summary>
    [Fact]
    public void AHandleShowsItsLooksUnderThePointerAndWhileDragged()
    {
        var handle = new DragHandle { Style = Dots, CanDrag = true, Placement = FixedSize.Size(24f, 24f) };
        var root = new VStack { Children = { handle } };
        _ = new Easel(root, Wide, High);
        var input = new InputRoot(root);
        Assert.Equal(Faint, handle.Color);

        ((IInputTarget)handle).IsPointerOver = true;
        Assert.Equal(Ink, handle.Color);

        var at = handle.ToScreen(new Vector2(handle.Bounds.X + 4f, handle.Bounds.Y + 4f));
        input.PointerDown(at, PointerButton.Left, 1, false, false);
        Assert.True(handle.IsDragging);
        Assert.Equal(Live, handle.Color);

        input.PointerUp(at, PointerButton.Left);
        Assert.False(handle.IsDragging);
        Assert.Equal(Ink, handle.Color);
    }

    /// <summary>The game's handle is faint, and is highlighted to ink on hover and while dragged.</summary>
    [Fact]
    public void TheGamesHandleIsFaintAndLights()
    {
        var handle = new DragHandle();
        handle.Restyle();
        Assert.Equal(GameColors.Faint, handle.Color);

        handle.Set(DragHandle.Dragging, true);
        Assert.Equal(GameColors.Ink, handle.Color);
    }

    /// <summary>A list holds only rows: a child that is not one is refused as it is added.</summary>
    [Fact]
    public void AListRefusesAChildThatIsNotARow()
    {
        var list = new ReorderList();

        Assert.Throws<ArgumentException>(() => list.Children.Add(new Label()));
        Assert.Equal(0, list.Children.Count);
    }

    /// <summary>A row dragged down past the next tells it is dragged, makes the next slide up over the frames that follow, and on its drop says where it went.</summary>
    [Fact]
    public void ARowDraggedDownIsReorderedAndTheOthersSlide()
    {
        var (list, rows, easel, input) = Laid(3);
        ReorderedEventArgs? heard = null;
        list.Reordered += args => heard = args;
        var height = rows[0].Bounds.Height;

        var at = Handle(rows[0]);
        input.PointerDown(at, PointerButton.Left, 1, false, false);
        Assert.True(rows[0].IsDragging);
        Assert.True(list.IsDragging);

        input.PointerMove(at + new Vector2(0f, height * 1.2f));
        var start = easel.Now;
        easel.Draw(Wide, High, start);
        easel.Draw(Wide, High, start + 1);
        Assert.True(easel.IsFrameRequested);
        var partway = rows[1].RenderTransform.Translate.Y;
        Assert.InRange(partway, -height, -1f);

        easel.Draw(Wide, High, start + 120);
        Assert.Equal(-height, rows[1].RenderTransform.Translate.Y, 1);
        Assert.Equal(height * 1.2f, rows[0].RenderTransform.Translate.Y, 1);

        input.PointerUp(at + new Vector2(0f, height * 1.2f), PointerButton.Left);
        Assert.False(rows[0].IsDragging);
        Assert.False(list.IsDragging);
        Assert.NotNull(heard);
        Assert.Equal(0, heard!.From);
        Assert.Equal(1, heard.To);
        Assert.False(heard.IsIndented);
    }

    /// <summary>Once every row is home and none is dragged, the list asks for no more frames.</summary>
    [Fact]
    public void AListAtRestAsksForNoFrames()
    {
        var (_, rows, easel, input) = Laid(2);
        Assert.False(easel.IsFrameRequested);

        var at = Handle(rows[0]);
        input.PointerDown(at, PointerButton.Left, 1, false, false);
        easel.Draw(Wide, High, easel.Now + 1);
        Assert.True(easel.IsFrameRequested);

        input.PointerUp(at, PointerButton.Left);
        easel.Draw(Wide, High);
        Assert.False(easel.IsFrameRequested);
    }

    /// <summary>A row dragged where it may go in is drawn in by the list's indent, and its drop says it went in.</summary>
    [Fact]
    public void ARowDraggedRightWhereItMayGoInIsIndented()
    {
        var (list, rows, easel, input) = Laid(2);
        list.Base.IndentWidth = 24f;
        list.FnCanIndent = delegate { return true; };
        ReorderedEventArgs? heard = null;
        list.Reordered += args => heard = args;

        var at = Handle(rows[1]);
        input.PointerDown(at, PointerButton.Left, 1, false, false);
        input.PointerMove(at + new Vector2(20f, 0f));
        easel.Draw(Wide, High, easel.Now + 1);
        Assert.Equal(24f, rows[1].RenderTransform.Translate.X, 1);

        input.PointerUp(at + new Vector2(20f, 0f), PointerButton.Left);
        Assert.NotNull(heard);
        Assert.True(heard!.IsIndented);
    }

    static (ReorderList, Row[], Easel, InputRoot) Laid(int count)
    {
        var list = new ReorderList();
        var rows = new Row[count];
        for (var i = 0; i < count; i++)
        {
            rows[i] = new Row(i.ToString());
            list.Children.Add(rows[i]);
        }

        var root = new VStack { Children = { list } };
        var easel = new Easel(root, Wide, High);
        return (list, rows, easel, new InputRoot(root));
    }

    static Vector2 Handle(Row row)
    {
        var handle = row.DragHandle;
        return handle.ToScreen(new Vector2(handle.Bounds.X + 4f, handle.Bounds.Y + 4f));
    }

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
