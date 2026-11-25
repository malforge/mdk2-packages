using Lumel.Controls;
using Lumel.Input;
using Lumel.Layout;
using Lumel.Screens;
using Lumel.Viewing;
using VRage.Input;
using VRageMath;
using Xunit;

namespace Lumel.Tests;

/// <summary>Dragging data from a handle to drop targets that take it or refuse it.</summary>
public class DragDropTests
{
    static readonly RectangleF Area = new RectangleF(0f, 0f, 800f, 600f);

    static readonly Color Lit = new Color(58, 204, 0);
    static readonly Color Dim = new Color(60, 60, 60);
    static readonly Color Aimed = new Color(220, 245, 253);
    static readonly Color Carried = new Color(200, 100, 0);

    /// <summary>
    ///     A grab lights each target that takes the data and dims each that refuses it, the accepting one under the
    ///     pointer is dragged over, and a release there drops the data on it and puts every target back.
    /// </summary>
    [Fact]
    public void ADragLightsTheTargetsAndDropsOnTheOneUnderThePointer()
    {
        var scene = new Scene();
        scene.Grab();

        Assert.True(scene.Drag.IsDragging);
        Assert.True(scene.Drag.FitsAnywhere);
        Assert.Equal(Lit, scene.Takes.Background);
        Assert.Equal(Dim, scene.Refuses.Background);

        scene.MoveTo(scene.Takes);
        Assert.Same(scene.Takes, scene.Drag.Target);
        Assert.Equal(Aimed, scene.Takes.Background);

        scene.MoveTo(scene.Refuses);
        Assert.Null(scene.Drag.Target);
        Assert.Equal(Lit, scene.Takes.Background);

        scene.MoveTo(scene.Takes);
        scene.Release(scene.Takes);
        Assert.Equal(new object[] { "piece" }, scene.Taken.Dropped);
        Assert.Same(scene.Takes, scene.Ended);
        Assert.False(scene.Drag.IsDragging);
        Assert.Equal(0, scene.Takes.Background.A);
        Assert.Equal(0, scene.Refuses.Background.A);
    }

    /// <summary>A release off every accepting target drops nothing and ends the drag as canceled.</summary>
    [Fact]
    public void AReleaseOffTheTargetsDropsNothing()
    {
        var scene = new Scene();
        scene.Grab();
        scene.MoveTo(scene.Refuses);
        scene.Release(scene.Refuses);

        Assert.Empty(scene.Taken.Dropped);
        Assert.Empty(scene.Refused.Dropped);
        Assert.True(scene.EndedRaised);
        Assert.Null(scene.Ended);
        Assert.Equal(0, scene.Takes.Background.A);
    }

    /// <summary>A target scrolled out of its scroll view is passed over, though it is lit as taking the data.</summary>
    [Fact]
    public void ATargetScrolledOutOfViewIsPassedOver()
    {
        var taken = new Target();
        var hidden = new HStack { Placement = FixedSize.Size(200f, 80f), DropTarget = taken };
        var handle = new DragHandle { CanDrag = true, DragData = "piece", Placement = FixedSize.Size(24f, 24f) };
        var scroll = new ScrollView
        {
            Placement = FixedSize.Size(200f, 100f),
            Content = new VStack { Children = { new HStack { Placement = FixedSize.Size(200f, 300f) }, hidden } }
        };
        // The filler under the scroll view keeps the pointer on the window where the scrolled-out target lies.
        var filler = new HStack { Placement = FixedSize.Size(200f, 400f) };
        var workspace = new Workspace().With(new VStack { Children = { handle, scroll, filler } });
        workspace.DrawScreen(new Recording(Area), Area);

        workspace.PointerDown(Middle(handle), PointerButton.Left, 1, false, false);
        Assert.True(hidden.Is(DragDrop.Accepting));

        workspace.PointerMove(Middle(hidden));
        workspace.PointerUp(Middle(hidden), PointerButton.Left);
        Assert.Empty(taken.Dropped);
    }

    /// <summary>Escape during a drag cancels it, and the release that follows drops nothing.</summary>
    [Fact]
    public void EscapeCancelsADrag()
    {
        var scene = new Scene();
        scene.Grab();
        scene.MoveTo(scene.Takes);

        Assert.True(scene.Workspace.Key(MyKeys.Escape, false, false));
        Assert.False(scene.Drag.IsDragging);
        Assert.False(scene.Handle.IsDragging);
        Assert.Equal(0, scene.Takes.Background.A);

        scene.Release(scene.Takes);
        Assert.Empty(scene.Taken.Dropped);
    }

    /// <summary>The ghost is drawn over the window, moved by as far as the pointer has gone since the grab.</summary>
    [Fact]
    public void TheGhostFollowsThePointer()
    {
        var scene = new Scene();
        scene.Grab();
        var from = Middle(scene.Handle);
        var to = from + new Vector2(40f, 120f);
        scene.Workspace.PointerMove(to);
        scene.Workspace.Pointer = to;

        var g = new Recording(Area);
        scene.Workspace.DrawScreen(g, Area);

        var (moved, fillsBefore) = g.Transforms[g.Transforms.Count - 1];
        Assert.Equal(new Vector2(40f, 120f), moved.Translate);
        Assert.Equal(1f, moved.Scale);
        Assert.Equal(Carried, g.FillColors[fillsBefore]);
        Assert.Equal(scene.Piece.Bounds, g.Fills[fillsBefore]);
    }

    /// <summary>While a press holds the pointer, an element the pointer passes over is not lit as under the pointer.</summary>
    [Fact]
    public void WhileThePointerIsHeldOnlyTheElementHoldingItIsUnderThePointer()
    {
        var pressed = new Button { Text = "Held", Command = new RelayCommand(() => { }) };
        var passed = new Button { Text = "Passed", Command = new RelayCommand(() => { }) };
        var workspace = new Workspace().With(new VStack { Children = { pressed, passed } });
        workspace.DrawScreen(new Recording(Area), Area);

        Rest(workspace, passed);
        Assert.True(passed.IsPointerOver);

        Rest(workspace, pressed);
        workspace.PointerDown(Middle(pressed), PointerButton.Left, 1, false, false);
        Rest(workspace, passed);
        Assert.False(passed.IsPointerOver);
        Assert.False(pressed.IsPointerOver);

        Rest(workspace, pressed);
        Assert.True(pressed.IsPointerOver);

        workspace.PointerUp(Middle(pressed), PointerButton.Left);
        Rest(workspace, passed);
        Assert.True(passed.IsPointerOver);
    }

    static void Rest(Workspace workspace, Element element)
    {
        workspace.Pointer = Middle(element);
        workspace.DrawScreen(new Recording(Area), Area);
    }

    static Vector2 Middle(Element element)
    {
        var middle = new Vector2(element.Bounds.X + element.Bounds.Width / 2f, element.Bounds.Y + element.Bounds.Height / 2f);
        return element.Parent?.ToScreen(middle) ?? middle;
    }

    /// <summary>A drop target that takes what <see cref="FnTakes" /> says, keeping what is dropped on it.</summary>
    sealed class Target : IDropTarget
    {
        public Func<object, bool> FnTakes = _ => true;

        public List<object> Dropped { get; } = new List<object>();

        public bool CanDrop(object data) => FnTakes(data);

        public void Drop(object data) => Dropped.Add(data);
    }

    /// <summary>A piece with a handle carrying "piece", a target that takes it and one that refuses it, laid out on a workspace.</summary>
    sealed class Scene
    {
        public readonly Workspace Workspace;
        public readonly DragHandle Handle;
        public readonly HStack Piece;
        public readonly HStack Takes;
        public readonly HStack Refuses;
        public readonly Target Taken = new Target();
        public readonly Target Refused = new Target { FnTakes = _ => false };
        public Element? Ended;
        public bool EndedRaised;

        public Scene()
        {
            Handle = new DragHandle { CanDrag = true, DragData = "piece", Placement = FixedSize.Size(24f, 24f) };
            Piece = new HStack { Base = { Background = Carried }, Children = { Handle } };
            Handle.DragGhost = Piece;
            Takes = Box(Taken);
            Refuses = Box(Refused);
            Workspace = new Workspace().With(new VStack { Children = { Piece, Takes, Refuses } });
            Workspace.DrawScreen(new Recording(Area), Area);
            Drag.Ended += target =>
            {
                EndedRaised = true;
                Ended = target;
            };
        }

        public DragDrop Drag => Piece.Windows!.DragDrop;

        public void Grab()
        {
            Workspace.PointerDown(Middle(Handle), PointerButton.Left, 1, false, false);
        }

        public void MoveTo(Element element)
        {
            Workspace.PointerMove(Middle(element));
        }

        public void Release(Element element)
        {
            Workspace.PointerUp(Middle(element), PointerButton.Left);
        }

        static HStack Box(Target target)
        {
            return new HStack
            {
                Placement = FixedSize.Size(200f, 80f),
                DropTarget = target,
                [DragDrop.Accepting] = { Background = Lit },
                [DragDrop.Refusing] = { Background = Dim },
                [DragDrop.DraggedOver] = { Background = Aimed }
            };
        }
    }
}
