using Lumel.Drawing;
using Lumel.Windows;
using Lumel.Text;
using Lumel.Controls;
using Lumel.Layout;
using Lumel.Screens;
using Lumel.Styling;
using VRageMath;
using Xunit;

namespace Lumel.Tests;

/// <summary>
///     A workspace: its tip, chosen by the workspace, timed by the clock it is drawn with and looking as its theme says;
///     the world dimmed as its top window asked; and what it tells its windows and elements as they come and go.
/// </summary>
public class WorkspaceTests
{
    static readonly RectangleF Area = new RectangleF(0f, 0f, 800f, 600f);

    // A terminal's box off the middle of the screen, so a layout in one is told from a layout in the other.
    static readonly RectangleF Terminal = new RectangleF(200f, 120f, 400f, 300f);

    /// <summary>The tip opens once the pointer has rested for its delay by the clock of the graphics the workspace is drawn with.</summary>
    [Fact]
    public void TheTipIsTimedByTheClockItIsDrawnWith()
    {
        var (workspace, button) = Laid();
        button.Tip!.Base.ShowDelay = 10;
        workspace.Pointer = Middle(button);

        workspace.DrawScreen(new Recording(Area) { Now = 100 }, Area);
        workspace.DrawScreen(new Recording(Area) { Now = 109 }, Area);
        Assert.Null(workspace.ShownTip);

        workspace.DrawScreen(new Recording(Area) { Now = 110 }, Area);
        Assert.Same(button.Tip, workspace.ShownTip);
        Assert.Same(button, button.Tip.Owner);
    }

    /// <summary>A tip takes its look from the workspace's theme, given before it opens or after.</summary>
    [Fact]
    public void TheTipTakesTheWorkspacesTheme()
    {
        var (workspace, button) = Laid();
        workspace.Theme = new Theme { new TipStyle { Base = { Background = Color.Red } } };
        Rest(workspace, button);
        Assert.Equal(Color.Red, button.Tip!.Background);

        workspace.Theme = new Theme { new TipStyle { Base = { Background = Color.Blue } } };
        workspace.DrawScreen(new Recording(Area) { Now = 111 }, Area);
        Assert.Equal(Color.Blue, button.Tip.Background);
    }

    /// <summary>
    ///     A tip stands below and to the right of the pointer, which passes through it to what lies beneath, and closes as
    ///     soon as the pointer leaves its element.
    /// </summary>
    [Fact]
    public void ATipStandsByThePointerAndLetsItThrough()
    {
        var (workspace, button) = Laid();
        var tip = button.Tip!;
        Rest(workspace, button);
        var at = Middle(button);

        Assert.Equal(at.X + 16f, tip.Bounds.X, 1);
        Assert.Equal(at.Y + 16f, tip.Bounds.Y, 1);

        var onTip = Middle(tip);
        Assert.False(tip.Covers(onTip));
        Assert.NotSame(tip, workspace.HitTest(onTip));

        workspace.Pointer = new Vector2(Area.Right - 4f, Area.Bottom - 4f);
        workspace.DrawScreen(new Recording(Area) { Now = 111 }, Area);
        Assert.False(tip.IsOpen);
    }

    /// <summary>A tip given no style takes the game's: a short box on the chrome, a hairline round it, that waits half a second.</summary>
    [Fact]
    public void AnUnstyledTipTakesTheGamesLook()
    {
        var tip = new Tip("Words.");
        tip.Restyle();

        Assert.Equal(GameColors.Chrome, tip.Background);
        Assert.Equal(GameColors.Line, tip.Border);
        Assert.Equal(400f, tip.MaxWidth);
        Assert.Equal(30, tip.ShowDelay);
    }

    /// <summary>The world behind is faded as the window on top asked as it opened.</summary>
    [Fact]
    public void TheWorldIsDimmedAsTheTopWindowAsked()
    {
        var workspace = new Workspace();
        var dimming = new Window();
        var clear = new Window();

        workspace.Open(dimming, dimsWorld: true);
        Assert.True(workspace.DimsWorld);

        workspace.Open(clear);
        Assert.False(workspace.DimsWorld);

        clear.Close();
        Assert.True(workspace.DimsWorld);

        dimming.Close();
        Assert.False(workspace.DimsWorld);
    }

    /// <summary>
    ///     Disposing a workspace closes its windows, top first, raising Closed on each, and unloads what its layers show,
    ///     raising Unloaded through it.
    /// </summary>
    [Fact]
    public void DisposingClosesTheWindowsAndUnloadsTheLayers()
    {
        var said = new List<string>();
        var label = new Label { Text = "Hello" };
        label.Unloaded += () => said.Add("label unloaded");
        var below = new Window();
        below.Closed += reason => said.Add("below closed, " + reason);
        var above = new Window();
        above.Closed += reason => said.Add("above closed, " + reason);
        var workspace = new Workspace { BelowHud = label };
        workspace.Open(below);
        workspace.Open(above);

        Assert.True(label.IsLoaded);

        workspace.Dispose();

        Assert.Equal(new[] { "above closed, WorldUnloading", "below closed, WorldUnloading", "label unloaded" }, said);
        Assert.Equal(0, workspace.Windows.Count);
        Assert.False(label.IsLoaded);
    }

    /// <summary>
    ///     An element in a window is loaded while the window is open, and is told as it comes and goes, its holder
    ///     before it on the way in and after it on the way out.
    /// </summary>
    [Fact]
    public void AnElementInAWindowIsLoadedWhileItIsOpen()
    {
        var said = new List<string>();
        var label = new Label { Text = "Hello" };
        var column = new VStack { Children = { label } };
        column.Loaded += () => said.Add("column loaded");
        label.Loaded += () => said.Add("label loaded");
        column.Unloaded += () => said.Add("column unloaded");
        label.Unloaded += () => said.Add("label unloaded");
        var window = new Window { Content = column };
        var workspace = new Workspace();

        Assert.False(label.IsLoaded);

        workspace.Open(window);
        Assert.True(label.IsLoaded);

        window.Close();
        Assert.False(label.IsLoaded);

        Assert.Equal(new[] { "column loaded", "label loaded", "label unloaded", "column unloaded" }, said);
    }

    /// <summary>An element added to an open window is loaded, and one taken out of it unloaded.</summary>
    [Fact]
    public void AddingAndRemovingLoadsAndUnloads()
    {
        var column = new VStack();
        var workspace = new Workspace();
        workspace.Open(new Window { Content = column });
        var label = new Label { Text = "Hello" };

        column.Children.Add(label);
        Assert.True(label.IsLoaded);

        column.Children.Remove(label);
        Assert.False(label.IsLoaded);
    }

    /// <summary>Each HUD layer is laid out across the whole screen, as the game's own HUD is, unless it is given the terminal's box.</summary>
    [Fact]
    public void EachLayerTakesTheAreaItIsGiven()
    {
        var below = new Label { Text = "Below", Align = Align.Right | Align.Top };
        var above = new Label { Text = "Above", Align = Align.Right | Align.Top };
        var workspace = new Workspace
        {
            BelowHud = below,
            AboveHud = above,
            AboveHudArea = ScreenArea.Terminal
        };

        workspace.DrawHud(new Recording(Area), Area, Terminal);

        Assert.Equal(Area.Right, below.Bounds.Right);
        Assert.Equal(Terminal.Right, above.Bounds.Right);
    }

    /// <summary>A window is laid out in the middle of the terminal's box unless it is given the whole screen, and a popup takes the whole screen.</summary>
    [Fact]
    public void EachWindowTakesTheAreaItIsGiven()
    {
        var boxed = new Window { Style = Showing.Bare, Placement = FixedSize.Size(100f, 40f) };
        var whole = new Window { Style = Showing.Bare, Placement = FixedSize.Size(100f, 40f), Area = ScreenArea.Window };
        var workspace = new Workspace();
        workspace.Open(boxed);
        workspace.Open(whole);

        workspace.DrawScreen(new Recording(Area), Area, Terminal);

        Assert.Equal(Terminal.Center.X, boxed.Bounds.Center.X, 1);
        Assert.Equal(Terminal.Center.Y, boxed.Bounds.Center.Y, 1);
        Assert.Equal(Area.Center.X, whole.Bounds.Center.X, 1);
        Assert.Equal(Area.Center.Y, whole.Bounds.Center.Y, 1);
        Assert.Equal(ScreenArea.Window, new Popup().Area);
    }

    /// <summary>A window the game's screen takes with it as the world unloads is told so, and one it takes otherwise is not.</summary>
    [Theory]
    [InlineData(true, CloseReason.WorldUnloading)]
    [InlineData(false, CloseReason.Closed)]
    public void AWindowKnowsWhenTheWorldUnloads(bool unloading, CloseReason expected)
    {
        var heard = new List<CloseReason>();
        var window = new Window();
        window.Closed += heard.Add;
        var workspace = new Workspace();
        workspace.Open(window);

        ((IScreenLifetime)workspace).Closed(unloading);

        Assert.Equal(new[] { expected }, heard);
    }

    /// <summary>A window is told as it opens and as it closes, by its own Close or by being dismissed.</summary>
    [Fact]
    public void AWindowIsToldAsItOpensAndCloses()
    {
        var said = new List<string>();
        var window = new Window();
        window.Opened += () => said.Add("opened");
        window.Closed += reason => said.Add("closed, " + reason);
        var workspace = new Workspace();

        workspace.Open(window);
        window.Close();

        Assert.Equal(new[] { "opened", "closed, Closed" }, said);
    }

    /// <summary>The tip that opens is the one the workspace chooses for the element's own.</summary>
    [Fact]
    public void TheWorkspaceChoosesTheTipThatOpens()
    {
        var shorter = new Tip("Vents.") { Base = { ShowDelay = 10 } };
        var (workspace, button) = Laid(root => new Choosing(_ => shorter).With(root));
        Rest(workspace, button);

        Assert.Same(shorter, workspace.ShownTip);
    }

    /// <summary>No tip opens where the workspace chooses none.</summary>
    [Fact]
    public void NoTipOpensWhereTheWorkspaceChoosesNone()
    {
        var (workspace, button) = Laid(root => new Choosing(_ => null).With(root));
        Rest(workspace, button);

        Assert.Null(workspace.ShownTip);
    }

    static (Workspace Workspace, Button Button) Laid() => Laid(root => new Workspace().With(root));

    static (Workspace Workspace, Button Button) Laid(Func<Element, Workspace> create)
    {
        var button = new Button { Text = "Vent", Tip = new Tip("Opens the vents."), Placement = FixedSize.Size(100f, 40f) };
        var workspace = create(new VStack { Children = { button } });
        workspace.DrawScreen(new Recording(Area), Area);
        return (workspace, button);
    }

    /// <summary>Rests the pointer on <paramref name="button" /> for as long as the tip waits before it opens.</summary>
    static void Rest(Workspace workspace, Button button)
    {
        button.Tip!.Base.ShowDelay = 10;
        workspace.Pointer = Middle(button);
        workspace.DrawScreen(new Recording(Area) { Now = 100 }, Area);
        workspace.DrawScreen(new Recording(Area) { Now = 110 }, Area);
    }

    static Vector2 Middle(Element element) =>
        new Vector2(element.Bounds.X + element.Bounds.Width / 2f, element.Bounds.Y + element.Bounds.Height / 2f);

    /// <summary>A workspace that chooses each tip with <c>choose</c>.</summary>
    sealed class Choosing(Func<Tip, Tip?> choose) : Workspace
    {
        protected override Tip TipFor(Element owner, Tip tip) => choose(tip)!;
    }
}
