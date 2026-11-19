using Lumel.Controls;
using Lumel.Input;
using Lumel.Layout;
using Lumel.Screens;
using Lumel.Windows;
using VRage.Input;
using VRageMath;
using Xunit;

namespace Lumel.Tests;

/// <summary>
///     A coach mark: a bubble hanging from an element with its tail aimed at it, dismissed by a press on it or, where
///     asked, anywhere else, closed without being dismissed when its element goes, and never in the way of the window
///     beneath.
/// </summary>
public class CoachMarkTests
{
    static readonly RectangleF Area = new RectangleF(0f, 0f, 1000f, 800f);

    /// <summary>
    ///     A coach mark stands below its anchor, centered across it at the gap its look gives, with its tail on its top
    ///     edge level with the anchor's middle.
    /// </summary>
    [Fact]
    public void ACoachMarkStandsBelowItsAnchorWithItsTailAimedAtIt()
    {
        var scene = new Scene();
        var graphics = new Recording(Area);
        scene.Workspace.DrawScreen(graphics, Area);

        var anchor = scene.Anchor.Bounds;
        var bubble = scene.Mark.Bounds;
        var middle = anchor.X + anchor.Width / 2f;
        Assert.Equal(anchor.Y + anchor.Height + 12f, bubble.Y, 1);
        Assert.Equal(middle, bubble.X + bubble.Width / 2f, 1);

        var side = 12f * 1.41421356f;
        Assert.Contains(graphics.Fills, fill => Near(fill.Width, side) && Near(fill.X + fill.Width / 2f, middle) && Near(fill.Y + fill.Height / 2f, bubble.Y));
    }

    /// <summary>Beside its anchor, a coach mark is centered on it from top to bottom, with its tail on the edge facing it.</summary>
    [Fact]
    public void ACoachMarkBesideItsAnchorIsCenteredOnIt()
    {
        var scene = new Scene(mark => mark.Side = PopupSide.Right);
        var graphics = new Recording(Area);
        scene.Workspace.DrawScreen(graphics, Area);

        var anchor = scene.Anchor.Bounds;
        var bubble = scene.Mark.Bounds;
        var middle = anchor.Y + anchor.Height / 2f;
        Assert.Equal(anchor.X + anchor.Width + 12f, bubble.X, 1);
        Assert.Equal(middle, bubble.Y + bubble.Height / 2f, 1);
        Assert.Contains(graphics.Fills, fill => Near(fill.X + fill.Width / 2f, bubble.X) && Near(fill.Y + fill.Height / 2f, middle));
    }

    /// <summary>Its text wraps at the widest its look gives.</summary>
    [Fact]
    public void ItsTextWrapsAtItsWidest()
    {
        var scene = new Scene(text: string.Join(" ", Enumerable.Repeat("Words that run on.", 20)));
        scene.Workspace.DrawScreen(new Recording(Area), Area);

        Assert.InRange(scene.Mark.Bounds.Width, 1f, 320f);
        Assert.True(scene.Mark.Bounds.Height > 60f, "the text did not wrap");
    }

    /// <summary>A press on the coach mark dismisses it, raising Dismissed once it has closed.</summary>
    [Fact]
    public void APressOnItDismissesIt()
    {
        var scene = new Scene();
        scene.Workspace.DrawScreen(new Recording(Area), Area);

        scene.Press(Middle(scene.Mark.Bounds));

        Assert.False(scene.Mark.IsOpen);
        Assert.Equal(new[] { "closed", "dismissed" }, scene.Heard);
    }

    /// <summary>By default a press elsewhere leaves the coach mark open and goes on to what it lands on.</summary>
    [Fact]
    public void APressElsewhereGoesByIt()
    {
        var scene = new Scene();
        scene.Workspace.DrawScreen(new Recording(Area), Area);

        scene.Press(Middle(scene.Other.Bounds));

        Assert.True(scene.Mark.IsOpen);
        Assert.Equal(new[] { "other" }, scene.Heard);
    }

    /// <summary>
    ///     A coach mark that a press elsewhere dismisses closes on it, raising Dismissed, and the press goes on to what it
    ///     lands on unless the coach mark keeps it.
    /// </summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void APressElsewhereDismissesItWhereAsked(bool passes)
    {
        var scene = new Scene(mark =>
        {
            mark.DismissesOnPressOutside = true;
            mark.PassesPressOutside = passes;
        });
        scene.Workspace.DrawScreen(new Recording(Area), Area);

        scene.Press(Middle(scene.Other.Bounds));

        Assert.False(scene.Mark.IsOpen);
        Assert.Equal(passes ? new[] { "closed", "dismissed", "other" } : new[] { "closed", "dismissed" }, scene.Heard);
    }

    /// <summary>A coach mark whose anchor is hidden closes at the next draw without being dismissed.</summary>
    [Fact]
    public void ItClosesWithoutBeingDismissedWhenItsAnchorGoes()
    {
        var scene = new Scene();
        scene.Workspace.DrawScreen(new Recording(Area), Area);

        scene.Anchor.IsVisible = false;
        scene.Workspace.DrawScreen(new Recording(Area), Area);

        Assert.False(scene.Mark.IsOpen);
        Assert.Equal(new[] { "closed" }, scene.Heard);
    }

    /// <summary>A coach mark whose anchor is on a card of a deck closes without being dismissed when another card is turned to.</summary>
    [Fact]
    public void ItClosesWithoutBeingDismissedWhenItsAnchorsCardIsCovered()
    {
        var heard = new List<string>();
        var workspace = new Workspace();
        var anchor = new Button { Text = "New", Placement = FixedSize.Size(100f, 40f) };
        var other = new HStack();
        var deck = new Deck { Children = { new VStack { Children = { anchor } }, other } };
        workspace.Open(Showing.Window(deck));
        workspace.DrawScreen(new Recording(Area), Area);
        var mark = new CoachMark("This is new.") { Anchor = anchor };
        mark.Closed += _ => heard.Add("closed");
        mark.Dismissed += () => heard.Add("dismissed");
        mark.Open();
        workspace.DrawScreen(new Recording(Area), Area);
        Assert.True(mark.IsOpen);

        deck.Top = other;
        workspace.DrawScreen(new Recording(Area), Area);

        Assert.False(mark.IsOpen);
        Assert.Equal(new[] { "closed" }, heard);
    }

    /// <summary>
    ///     Over a window, a coach mark leaves the world dimmed as the window asked, and Escape closes the window, which
    ///     takes the coach mark with it without dismissing it.
    /// </summary>
    [Fact]
    public void EscapeAndTheWorldGoByIt()
    {
        var scene = new Scene();
        scene.Workspace.DrawScreen(new Recording(Area), Area);

        Assert.True(scene.Workspace.DimsWorld);

        Assert.True(scene.Workspace.Key(MyKeys.Escape, false, false));
        Assert.False(scene.Window.IsOpen);
        scene.Workspace.DrawScreen(new Recording(Area), Area);

        Assert.False(scene.Mark.IsOpen);
        Assert.Equal(new[] { "closed" }, scene.Heard);
    }

    static bool Near(float a, float b) => Math.Abs(a - b) < 0.5f;

    static Vector2 Middle(RectangleF bounds) => new Vector2(bounds.X + bounds.Width / 2f, bounds.Y + bounds.Height / 2f);

    /// <summary>A window holding a button the coach mark hangs from and another, with the coach mark open over it.</summary>
    sealed class Scene
    {
        public readonly Workspace Workspace = new Workspace();
        public readonly Window Window;
        public readonly Button Anchor;
        public readonly Button Other;
        public readonly CoachMark Mark;
        public readonly List<string> Heard = new List<string>();

        public Scene(Action<CoachMark>? set = null, string text = "This is new.")
        {
            Anchor = new Button { Text = "New", Command = new RelayCommand(() => Heard.Add("anchor")), Placement = FixedSize.Size(100f, 40f) };
            Other = new Button { Text = "Other", Command = new RelayCommand(() => Heard.Add("other")), Placement = FixedSize.Size(100f, 40f) };
            Window = Showing.Window(new VStack { Children = { Anchor, new VStack { Placement = FixedSize.Height(300f) }, Other } });
            Workspace.Open(Window, true);
            Workspace.DrawScreen(new Recording(Area), Area);

            Mark = new CoachMark(text) { Anchor = Anchor };
            set?.Invoke(Mark);
            Mark.Closed += _ => Heard.Add("closed");
            Mark.Dismissed += () => Heard.Add("dismissed");
            Mark.Open();
        }

        public void Press(Vector2 point)
        {
            Workspace.PointerDown(point, PointerButton.Left, 1, false, false);
            Workspace.PointerUp(point, PointerButton.Left);
        }
    }
}
