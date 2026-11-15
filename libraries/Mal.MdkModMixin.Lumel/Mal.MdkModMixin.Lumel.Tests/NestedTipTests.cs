using Lumel.Input;
using Lumel.Layout;
using Lumel.Screens;
using Lumel.Styling;
using Lumel.Text;
using Lumel.Windows;
using VRageMath;
using Xunit;

namespace Lumel.Tests;

/// <summary>
/// A codex tip that nests: resting on a link inside it opens the link's page in a second tip beside it, the first staying
/// open; each stays while the pointer is anywhere on them, resting on another link replaces it, and leaving them all
/// closes all, however many stand, unless a pin key holds them, which keeps them wherever the pointer goes.
/// </summary>
public class NestedTipTests
{
    static readonly RectangleF Area = new RectangleF(0f, 0f, 1600f, 800f);

    // The tip's own geometry, with none of the room or gaps the game's look gives it.
    static readonly IStyle<CodexTip> Bare = new CodexTipStyle();

    const float Wide = 300f;

    const int Delay = 10;

    const int Grace = 5;

    /// <summary>Resting on a link for the delay opens its page in a tip of its own beside the first, which stays open.</summary>
    [Fact]
    public void ALinkRestedOnOpensItsPageBeside()
    {
        var scene = new Scene();
        var tip = scene.Tip;

        scene.Hover(Link(tip), 0);
        scene.Hover(Link(tip), Delay - 1);
        Assert.Null(tip.NestedTip);

        scene.Hover(Link(tip), Delay);
        Assert.True(tip.IsOpen);
        Assert.NotNull(tip.NestedTip);
        Assert.Same(scene.Pages["b"], tip.NestedTip!.Page);

        scene.Hover(Link(tip), Delay + 1);
        Assert.True(tip.NestedTip.Bounds.X >= tip.Bounds.X + tip.Bounds.Width - 1f || tip.NestedTip.Bounds.X + tip.NestedTip.Bounds.Width <= tip.Bounds.X + 1f,
            "the tip beside stood over the first");
    }

    /// <summary>The tip beside takes the pinned state and the look that answers it as the first does.</summary>
    [Fact]
    public void TheTipBesideIsPinnedToo()
    {
        var scene = Scene.Beside();

        var beside = scene.Tip.NestedTip!;
        var body = (RichText)((VStack)beside.GetVisualChild(0)).Children[1];

        Assert.True(beside.Is(CodexTip.Pinned));
        Assert.True(body.Is(CodexTip.Pinned));
    }

    /// <summary>The pointer crossing onto the tip beside keeps both open, and a press there closes neither.</summary>
    [Fact]
    public void ThePointerOnTheTipBesideKeepsBoth()
    {
        var scene = Scene.Beside();
        var tip = scene.Tip;

        var on = On(tip.NestedTip!);
        scene.Hover(on, Delay + 1);
        scene.Hover(on, Delay + 1 + Grace * 3);
        Assert.True(tip.Covers(on));
        scene.Press(on);

        Assert.True(tip.IsOpen);
        Assert.NotNull(tip.NestedTip);
    }

    /// <summary>A press on the tip beside reaches it through the first tip, held as any element.</summary>
    [Fact]
    public void APressOnTheTipBesideReachesItThroughTheFirst()
    {
        var scene = Scene.Beside();
        var tip = scene.Tip;
        var nested = tip.NestedTip!;
        Element held = tip;
        tip.Base.BackSymbol = "<";
        nested.Navigate(new Tip("Further words."));
        scene.Hover(On(nested), Delay + 1);

        var points =
            from x in Enumerable.Range(0, (int)(nested.Bounds.Width / 4f))
            from y in Enumerable.Range(0, (int)(nested.Bounds.Height / 4f))
            select new Vector2(nested.Bounds.X + x * 4f + 2f, nested.Bounds.Y + y * 4f + 2f);
        var point = points.First(p => nested.HitTest(p) != null);

        Assert.Same(nested.HitTest(point), held.HitTest(point));
    }

    /// <summary>The tip beside takes the first tip's own looks as well as its style, when it is made and whenever they change.</summary>
    [Fact]
    public void TheTipBesideTakesTheFirstTipsOwnLooks()
    {
        var scene = new Scene();
        var tip = scene.Tip;
        tip.Base.Spacing = 12f;
        tip.Base.MaxHeight = 400f;
        scene.Hover(Link(tip), -1);

        scene.Hover(Link(tip), 0);
        scene.Hover(Link(tip), Delay);
        var nested = tip.NestedTip!;

        Assert.Same(tip.Style, nested.Style);
        Assert.Equal(12f, nested.Spacing);
        Assert.Equal(400f, nested.MaxHeight);

        tip.Base.Spacing = 20f;
        scene.Hover(Link(tip), Delay + 1);

        Assert.Equal(20f, nested.Spacing);
    }

    /// <summary>A change of the workspace's theme reaches a tip beside made under the theme before.</summary>
    [Fact]
    public void AThemeChangeReachesTheTipBeside()
    {
        var scene = Scene.Beside();
        var tip = scene.Tip;
        var nested = tip.NestedTip!;
        tip.Style = null;

        scene.Workspace.Theme = new Theme { new CodexTipStyle { Base = { Spacing = 8f } } };
        scene.Hover(On(nested), Delay + 1);
        Assert.Equal(8f, nested.Spacing);

        scene.Workspace.Theme = new Theme { new CodexTipStyle { Base = { Spacing = 16f } } };
        scene.Hover(On(nested), Delay + 2);
        Assert.Equal(16f, nested.Spacing);
    }

    /// <summary>The first tip's plain words keep the tip beside open, however long the pointer stays on them.</summary>
    [Fact]
    public void OnTheFirstsWordsTheTipBesideStays()
    {
        var scene = Scene.Beside();
        var tip = scene.Tip;

        var plain = Plain(tip);
        scene.Hover(plain, Delay + 1);
        scene.Hover(plain, Delay + 1 + Grace * 10);

        Assert.True(tip.IsOpen);
        Assert.Same(scene.Pages["b"], tip.NestedTip?.Page);
    }

    /// <summary>A hand walks from a link across the card to the tip beside, frame by frame, and on to a third.</summary>
    [Fact]
    public void WalkingAcrossTheCardReachesTheTipBeside()
    {
        var scene = Scene.Beside();
        var tip = scene.Tip;
        var second = tip.NestedTip!;

        var now = scene.Walk(Link(tip), Link(second), Delay + 1, Grace * 4);
        Assert.Same(second, tip.NestedTip);

        scene.Hover(Link(second), now);
        scene.Hover(Link(second), now + Delay);

        Assert.NotNull(second.NestedTip);
    }

    /// <summary>Walking back from the third tip across the second keeps all three.</summary>
    [Fact]
    public void WalkingBackKeepsTheChain()
    {
        var scene = new Scene();
        var third = Third(scene);
        var second = scene.Tip.NestedTip!;

        scene.Walk(On(third), Plain(scene.Tip), Delay * 2 + 2, Grace * 4);

        Assert.Same(second, scene.Tip.NestedTip);
        Assert.Same(third, second.NestedTip);
    }

    /// <summary>Crossing another link on the way leaves the tip beside as it is.</summary>
    [Fact]
    public void CrossingAnotherLinkKeepsTheTipBeside()
    {
        var scene = TwoLinks();
        var tip = scene.Tip;

        scene.Hover(Plain(tip), Delay + 1);
        scene.Hover(Plain(tip), Delay + Delay);

        Assert.Same(scene.Pages["b"], tip.NestedTip?.Page);
    }

    /// <summary>A longer delay holds a page in place through a pause that would open a closed tip.</summary>
    [Fact]
    public void ReplacingWaitsForItsOwnDelay()
    {
        var scene = new Scene(first: "[c](c)\n\n[b](b)");
        var tip = scene.Tip;
        tip.Base.ReplaceDelay = Delay * 2;
        scene.Hover(Link(tip), 0);
        scene.Hover(Link(tip), Delay);
        scene.Hover(Link(tip), Delay);
        Assert.Equal(Delay, tip.ShowDelay);
        Assert.Same(scene.Pages["b"], tip.NestedTip?.Page);

        scene.Hover(Plain(tip), Delay + 1);
        scene.Hover(Plain(tip), Delay * 3);
        Assert.Same(scene.Pages["b"], tip.NestedTip?.Page);

        scene.Hover(Plain(tip), Delay * 3 + 1);
        Assert.Same(scene.Pages["c"], tip.NestedTip?.Page);
    }

    /// <summary>Resting on another link replaces the page beside, and the tip beside that closes.</summary>
    [Fact]
    public void RestingOnAnotherLinkReplacesTheTipBeside()
    {
        var scene = TwoLinks();
        var tip = scene.Tip;
        var second = tip.NestedTip!;
        scene.Hover(Link(second), Delay + 1);
        scene.Hover(Link(second), Delay * 2 + 1);
        Assert.NotNull(second.NestedTip);

        scene.Hover(Plain(tip), Delay * 2 + 2);
        scene.Hover(Plain(tip), Delay * 3 + 2);

        Assert.Same(scene.Pages["c"], tip.NestedTip?.Page);
        Assert.Null(second.NestedTip);
    }

    /// <summary>A rest on a link ends as the pointer leaves its card, so coming back to the link waits again.</summary>
    [Fact]
    public void ComingBackToALinkWaitsAgain()
    {
        var scene = Scene.Beside();
        var tip = scene.Tip;
        var second = tip.NestedTip!;
        scene.Hover(Link(second), Delay + 1);
        scene.Hover(Link(second), Delay + 2);

        scene.Hover(Plain(tip), Delay + 3);
        scene.Hover(Link(second), Delay * 4);
        Assert.Null(second.NestedTip);

        scene.Hover(Link(second), Delay * 5);
        Assert.NotNull(second.NestedTip);
    }

    /// <summary>The pointer walking off every tip onto the empty screen leaves them all open for as long as the key is held.</summary>
    [Fact]
    public void ThePinKeyKeepsTheTipsWhereverThePointerGoes()
    {
        var scene = PinnedBeside(out _);
        var tip = scene.Tip;
        var away = new Vector2(Area.Width - 5f, Area.Height - 5f);

        var now = scene.Walk(Link(tip), away, Delay + 1, Grace * 4);
        scene.Hover(away, now + Grace * 100);

        Assert.True(tip.IsOpen);
        Assert.NotNull(tip.NestedTip);
    }

    /// <summary>Releasing the pin key away from every tip closes them all at once.</summary>
    [Fact]
    public void ReleasingThePinKeyAwayClosesAll()
    {
        var scene = PinnedBeside(out var held);
        var tip = scene.Tip;
        var away = new Vector2(Area.Width - 5f, Area.Height - 5f);
        var now = scene.Walk(Link(tip), away, Delay + 1, Grace);
        scene.Hover(away, now + Grace * 10);

        held[0] = false;
        scene.Hover(away, now + Grace * 10 + 1);

        Assert.False(tip.IsOpen);
        Assert.Null(tip.NestedTip);
    }

    /// <summary>Resting on another element with a tip of its own, with the key held, opens nothing and closes nothing.</summary>
    [Fact]
    public void ThePinKeyKeepsTheTipsOverAnotherElementsTip()
    {
        var scene = PinnedBeside(out _);
        var tip = scene.Tip;

        scene.Hover(On(scene.Other), Delay + 1);
        scene.Hover(On(scene.Other), Delay * 3);

        Assert.True(tip.IsOpen);
        Assert.NotNull(tip.NestedTip);
        Assert.False(scene.Other.Tip!.IsOpen);
    }

    /// <summary>Releasing the key over another element closes the tips, and that element's own tip opens.</summary>
    [Fact]
    public void ReleasingOverAnotherElementOpensItsTip()
    {
        var scene = PinnedBeside(out var held);

        scene.Hover(On(scene.Other), Delay + 1);
        held[0] = false;
        scene.Hover(On(scene.Other), Delay * 3);
        scene.Hover(On(scene.Other), Delay * 3 + 1);

        Assert.False(scene.Tip.IsOpen);
        Assert.True(scene.Other.Tip!.IsOpen);
    }

    /// <summary>A tip with no pin key, always pinned, still closes once the pointer has left it.</summary>
    [Fact]
    public void WithNoPinKeyLeavingCloses()
    {
        var scene = Scene.Beside();
        var tip = scene.Tip;
        var away = new Vector2(Area.Width - 5f, Area.Height - 5f);

        var now = scene.Walk(Link(tip), away, Delay + 1, Grace);
        scene.Hover(away, now + Grace);

        Assert.False(tip.IsOpen);
    }

    /// <summary>Releasing the pin key on any tip closes them all.</summary>
    [Fact]
    public void ReleasingThePinKeyOnATipClosesAll()
    {
        var scene = PinnedBeside(out var held);
        var tip = scene.Tip;

        held[0] = false;
        scene.Hover(On(tip.NestedTip!), Delay + 1);

        Assert.False(tip.IsOpen);
    }

    /// <summary>Releasing the pin key with the pointer back on the element leaves the first tip, as an ordinary tip, and closes the one beside it.</summary>
    [Fact]
    public void ReleasingThePinKeyOnTheElementClosesTheTipBeside()
    {
        var scene = PinnedBeside(out var held);
        var tip = scene.Tip;

        held[0] = false;
        scene.Hover(new Vector2(50f, 110f), Delay + 1);

        Assert.True(tip.IsOpen);
        Assert.Null(tip.NestedTip);
    }

    /// <summary>Leaving every tip closes them all once the grace is over.</summary>
    [Fact]
    public void LeavingThemAllClosesAll()
    {
        var scene = Scene.Beside();
        var tip = scene.Tip;

        var away = new Vector2(Area.Width - 5f, Area.Height - 5f);
        scene.Hover(away, Delay + 1);
        scene.Hover(away, Delay + 1 + Grace);

        Assert.False(tip.IsOpen);
        Assert.Null(tip.NestedTip);
    }

    /// <summary>Any number of tips can stand, on screen however far they run, and leaving them closes them all.</summary>
    [Fact]
    public void AnyNumberOfTipsStand()
    {
        var scene = new Scene();
        scene.Pages["e"] = new Tip("Words of e.\n\n[f](f)");
        scene.Pages["f"] = new Tip("Words of f.\n\n[g](g)");
        scene.Pages["g"] = new Tip("Words of g.\n\n[h](h)");
        scene.Pages["h"] = new Tip("Words of h.");

        var tip = scene.Tip;
        var now = 0;
        for (var page = 0; page < 7; page++)
        {
            scene.Hover(Link(tip), now);
            now += Delay;
            scene.Hover(Link(tip), now);
            scene.Hover(Link(tip), now);
            tip = tip.NestedTip!;
            now++;
        }

        Assert.Same(scene.Pages["h"], tip.Page);
        Assert.True(tip.Bounds.X >= 0f && tip.Bounds.X + tip.Bounds.Width <= Area.Width, $"the last tip stands at {tip.Bounds.X}");

        var away = new Vector2(Area.Width - 5f, Area.Height - 5f);
        scene.Hover(away, now);
        scene.Hover(away, now + Grace);
        Assert.False(scene.Tip.IsOpen);
        Assert.False(tip.IsOpen);
    }

    /// <summary>A link to a page open further up is plain words: it has no link there and opens nothing.</summary>
    [Fact]
    public void ALinkToAPageOpenAboveIsPlainWords()
    {
        var scene = new Scene();
        scene.Pages["b"] = new Tip("[c](c)");
        scene.Pages["c"] = new Tip("[b](b)");

        var third = Third(scene);

        Assert.Null(Text(third).LinkAt(Link(third)));
        scene.Hover(Link(third), 200);
        scene.Hover(Link(third), 200 + Delay);
        Assert.Null(third.NestedTip);
    }

    /// <summary>The same page keeps its link in a chain that does not have the page it leads to open.</summary>
    [Fact]
    public void TheSamePageIsLiveInAnotherChain()
    {
        var first = new Scene();
        first.Pages["b"] = new Tip("[c](c)");
        first.Pages["c"] = new Tip("[b](b)");
        var third = Third(first);
        Assert.Null(Text(third).LinkAt(Link(third)));

        var second = new Scene(first: "Words first.\n\n[c](c)", pages: first.Pages);
        second.Hover(Link(second.Tip), 0);
        second.Hover(Link(second.Tip), Delay);
        second.Hover(Link(second.Tip), Delay);

        var beside = second.Tip.NestedTip!;
        Assert.Same(first.Pages["c"], beside.Page);
        Assert.Equal("b", Text(beside).LinkAt(Link(beside))?.Target);
    }

    /// <summary>A tip made apart from the codex from the same words counts as that page, and with other arguments it does not.</summary>
    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public void ATipFromTheSameWordsIsTheSamePage(bool otherArguments, bool plain)
    {
        var codex = new Codex { OpensNestedTips = true };
        var a = new Arguments();
        var words = new Tip("Words of a {x}.\n\n[b](b)", a);
        var other = otherArguments ? new Arguments() : a;
        var pages = new Dictionary<string, Tip> { ["a"] = words, ["b"] = new Tip("[a](a)") };
        codex.FnResolvePage = id => pages.TryGetValue(id, out var page) ? page : null;
        var made = new Tip(words.Text, other);
        var scene = new Scene(codex, made);

        scene.Hover(Link(scene.Tip), 0);
        scene.Hover(Link(scene.Tip), Delay);
        scene.Hover(Link(scene.Tip), Delay);

        var beside = scene.Tip.NestedTip!;
        Assert.Equal(plain, Text(beside).LinkAt(new Vector2(beside.Bounds.X + 4f, beside.Bounds.Y + 4f)) == null);
    }

    /// <summary>A tip beside another stands against its edge, past its padding, level with its top, where its look gives no stagger.</summary>
    [Fact]
    public void TheTipBesideStandsAgainstTheEdge()
    {
        var beside = Padded(0f, out var tip);

        Assert.Equal(tip.Bounds.X + tip.Bounds.Width, beside.Bounds.X, 1);
        Assert.Equal(tip.Bounds.Y, beside.Bounds.Y, 1);
    }

    /// <summary>A stagger steps a tip beside another down its edge, touching it, whatever the padding.</summary>
    [Fact]
    public void AStaggerStepsTheTipBeside()
    {
        var beside = Padded(16f, out var tip);

        Assert.Equal(tip.Bounds.X + tip.Bounds.Width, beside.Bounds.X, 1);
        Assert.Equal(tip.Bounds.Y + 16f, beside.Bounds.Y, 1);
    }

    /// <summary>On a tip's left, a stagger steps the tip beside down its edge, and they touch.</summary>
    [Fact]
    public void AStaggerStepsDownOnTheLeft()
    {
        var scene = new Scene(across: 900f, wide: 400f);

        var beside = Stepped(scene);

        Assert.Equal(TipSide.Left, beside.Side);
        Assert.Equal(scene.Tip.Bounds.X, beside.Bounds.X + beside.Bounds.Width, 1);
        Assert.Equal(scene.Tip.Bounds.Y + 16f, beside.Bounds.Y, 1);
    }

    /// <summary>Above a tip, a stagger steps the tip beside along its edge to the right, and they touch.</summary>
    [Fact]
    public void AStaggerStepsAcrossAbove()
    {
        var scene = new Scene(wide: 900f);

        var beside = Stepped(scene);

        Assert.Equal(TipSide.Above, beside.Side);
        Assert.Equal(scene.Tip.Bounds.X + 16f, beside.Bounds.X, 1);
        Assert.Equal(scene.Tip.Bounds.Y, beside.Bounds.Y + beside.Bounds.Height, 1);
    }

    /// <summary>Below a tip, a stagger steps the tip beside along its edge to the right, and they touch.</summary>
    [Fact]
    public void AStaggerStepsAcrossBelow()
    {
        var scene = new Scene(wide: 900f);
        scene.Pages["b"] = new Tip(string.Join("\n\n", Enumerable.Repeat("Words of a tall page.", 20)));

        var beside = Stepped(scene);

        Assert.Equal(TipSide.Below, beside.Side);
        Assert.Equal(scene.Tip.Bounds.X + 16f, beside.Bounds.X, 1);
        Assert.Equal(scene.Tip.Bounds.Y + scene.Tip.Bounds.Height, beside.Bounds.Y, 1);
    }

    /// <summary>Opens the tip beside the first with a stagger of 16, and returns it.</summary>
    static CodexTip Stepped(Scene scene)
    {
        scene.Tip.Base.Stagger = 16f;
        scene.Hover(Link(scene.Tip), 0);
        scene.Hover(Link(scene.Tip), Delay);
        scene.Hover(Link(scene.Tip), Delay);
        return scene.Tip.NestedTip!;
    }

    /// <summary>A first tip on its element takes no stagger.</summary>
    [Fact]
    public void AFirstTipTakesNoStagger()
    {
        var plain = new Scene();
        var stepped = new Scene();
        stepped.Tip.Base.Stagger = 16f;
        stepped.Hover(Link(stepped.Tip), -100 + Delay + 2);

        Assert.Equal(plain.Tip.Bounds, stepped.Tip.Bounds);
    }

    /// <summary>Opens a tip of 12 padding with <paramref name="stagger" /> beside <paramref name="tip" />, and returns it.</summary>
    static CodexTip Padded(float stagger, out CodexTip tip)
    {
        var scene = new Scene();
        tip = scene.Tip;
        tip.Base.Padding = new Thickness(12f);
        tip.Base.Stagger = stagger;
        var near = new Vector2(scene.Tip.Bounds.X + 16f, scene.Tip.Bounds.Y + scene.Tip.Bounds.Height - 18f);
        scene.Hover(near, 0);
        scene.Hover(near, 0);
        near = new Vector2(scene.Tip.Bounds.X + 16f, scene.Tip.Bounds.Y + scene.Tip.Bounds.Height - 18f);
        scene.Hover(near, 0);
        scene.Hover(near, Delay);
        scene.Hover(near, Delay);
        return tip.NestedTip!;
    }

    /// <summary>A link to the page it stands on is plain words as well, and a page with only such links takes no pin hint.</summary>
    [Fact]
    public void APageWithNothingNewTakesNoPinHint()
    {
        var scene = new Scene();
        scene.Codex.IsPinnedByKey = true;
        scene.Codex.FnIsPinKeyHeld = () => true;
        scene.Codex.PinHint = new Tip("Hold to pin.").Document;
        scene.Pages["b"] = new Tip("[b](b)");
        scene.Hover(Link(scene.Tip), 0);
        scene.Hover(Link(scene.Tip), Delay);
        scene.Hover(Link(scene.Tip), Delay);

        var beside = scene.Tip.NestedTip!;

        Assert.Null(Text(beside).LinkAt(new Vector2(beside.Bounds.X + 4f, beside.Bounds.Y + 4f)));
        Assert.False(((VStack)beside.GetVisualChild(0)).Children[2].IsVisible);
    }

    /// <summary>A tip that does not nest keeps a link to the page it shows.</summary>
    [Fact]
    public void ATipThatDoesNotNestKeepsItsLinks()
    {
        var scene = new Scene(false, "[b](b)");
        scene.Pages["b"] = new Tip("[b](b)");

        scene.Tip.Follow("b");
        scene.Hover(new Vector2(40f, 100f), 0);

        Assert.Same(scene.Pages["b"], scene.Tip.Page);
        Assert.Equal("b", Text(scene.Tip).LinkAt(Link(scene.Tip))?.Target);
    }

    /// <summary>A tip that does not nest shows a link's page in itself, and opens nothing beside it.</summary>
    [Fact]
    public void ATipThatDoesNotNestOpensNothingBeside()
    {
        var scene = new Scene(false);

        scene.Hover(Link(scene.Tip), 0);
        scene.Hover(Link(scene.Tip), Delay * 2);

        Assert.Null(scene.Tip.NestedTip);
    }

    /// <summary>Opens the tip beside a first tip with a link to c above its link to b, resting on the link to b.</summary>
    static Scene TwoLinks()
    {
        var scene = new Scene(first: "[c](c)\n\n[b](b)");
        scene.Hover(Link(scene.Tip), 0);
        scene.Hover(Link(scene.Tip), Delay);
        scene.Hover(Link(scene.Tip), Delay);
        return scene;
    }

    /// <summary>Opens the tip beside the first with the tips pinned while <paramref name="held" />'s only value is true.</summary>
    static Scene PinnedBeside(out bool[] held)
    {
        var key = new[] { true };
        held = key;
        var scene = new Scene();
        scene.Codex.IsPinnedByKey = true;
        scene.Codex.FnIsPinKeyHeld = () => key[0];
        scene.Hover(Link(scene.Tip), 0);
        scene.Hover(Link(scene.Tip), Delay);
        scene.Hover(Link(scene.Tip), Delay);
        return scene;
    }

    /// <summary>Returns the text of the page <paramref name="tip" /> shows.</summary>
    static RichText Text(CodexTip tip) => (RichText)((VStack)tip.GetVisualChild(0)).Children[1];

    /// <summary>Opens the third tip of a chain by resting on the link of each one before it, and returns it.</summary>
    static CodexTip Third(Scene scene)
    {
        scene.Hover(Link(scene.Tip), 0);
        scene.Hover(Link(scene.Tip), Delay);
        scene.Hover(Link(scene.Tip), Delay);
        var second = scene.Tip.NestedTip!;
        scene.Hover(Link(second), Delay + 1);
        scene.Hover(Link(second), Delay * 2 + 1);
        scene.Hover(Link(second), Delay * 2 + 1);
        return second.NestedTip!;
    }

    /// <summary>Returns a point on the link at the start of the last line of <paramref name="tip" />.</summary>
    static Vector2 Link(CodexTip tip) => new Vector2(tip.Bounds.X + 4f, tip.Bounds.Y + tip.Bounds.Height - 4f);

    /// <summary>Returns a point on the plain words at the start of the first line of <paramref name="tip" />.</summary>
    static Vector2 Plain(CodexTip tip) => new Vector2(tip.Bounds.X + 4f, tip.Bounds.Y + 4f);

    static Vector2 On(Element element) => new Vector2(element.Bounds.X + element.Bounds.Width / 2f, element.Bounds.Y + element.Bounds.Height / 2f);

    /// <summary>
    ///     A workspace with an element at the left of a wide screen whose tip, open and drawn, is plain words followed by a
    ///     link, and every page it leads to the same.
    /// </summary>
    sealed class Scene
    {
        readonly float _across = 40f;

        public Scene(bool nests = true, string first = "Plain words first.\n\n[b](b)", Dictionary<string, Tip>? pages = null, float across = 40f, float wide = Wide)
        {
            _across = across;
            Pages = pages ?? new Dictionary<string, Tip>
            {
                ["b"] = new Tip("Words of b.\n\n[c](c)"),
                ["c"] = new Tip("Words of c.\n\n[d](d)"),
                ["d"] = new Tip("Words of d.\n\n[e](e)"),
                ["e"] = new Tip("Words of e.")
            };
            var codex = new Codex { FnResolvePage = id => Pages.TryGetValue(id, out var page) ? page : null, OpensNestedTips = nests };
            Codex = codex;
            Tip = new CodexTip(codex, new Tip(first))
            {
                Style = Bare,
                Placement = FixedSize.Width(wide),
                Base = { ShowDelay = Delay, CloseDelay = Grace }
            };
            Open();
        }

        void Open()
        {
            var anchor = new VStack { Placement = FixedSize.Size(100f, 40f), Align = Align.Left | Align.Top, Tip = Tip };
            Other = new VStack
            {
                Placement = FixedSize.Size(100f, 40f),
                Align = Align.Left | Align.Top,
                Tip = new Tip("Another element's tip.") { Style = new TipStyle { Base = { ShowDelay = Delay } } }
            };
            var root = new VStack { Children = { new VStack { Base = { Padding = new Thickness(_across, 100f, 0f, 0f), Spacing = 500f }, Children = { anchor, Other } } } };
            Workspace.Open(new Window { Style = Showing.Bare, Placement = FixedSize.Size(Area.Width, Area.Height), Content = root });

            Hover(Vector2.Zero, -200);
            Hover(On(anchor), -100);
            Hover(On(anchor), -100 + Delay);
            Hover(On(anchor), -100 + Delay + 1);
        }

        public Workspace Workspace { get; } = new Workspace();

        /// <summary>An element below the first with a tip of its own.</summary>
        public VStack Other { get; private set; } = null!;

        public Dictionary<string, Tip> Pages { get; }

        public Codex Codex { get; }

        public CodexTip Tip { get; }

        public Scene(Codex codex, Tip first)
        {
            Pages = new Dictionary<string, Tip>();
            Codex = codex;
            Tip = new CodexTip(codex, first)
            {
                Style = Bare,
                Placement = FixedSize.Width(Wide),
                Base = { ShowDelay = Delay, CloseDelay = Grace }
            };
            Open();
        }

        /// <summary>Creates a scene with a second tip open beside the first on its link's page.</summary>
        public static Scene Beside()
        {
            var scene = new Scene();
            scene.Hover(Link(scene.Tip), 0);
            scene.Hover(Link(scene.Tip), Delay);
            scene.Hover(Link(scene.Tip), Delay);
            return scene;
        }

        /// <summary>Rests the pointer at <paramref name="point" /> at <paramref name="now" /> and draws the frame.</summary>
        public void Hover(Vector2 point, int now)
        {
            Workspace.Pointer = point;
            Workspace.DrawScreen(new Recording(Area) { Now = now }, Area);
        }

        /// <summary>
        ///     Moves the pointer in a straight line from <paramref name="from" /> to <paramref name="to" /> over
        ///     <paramref name="frames" /> frames starting at <paramref name="now" />, and returns the frame after the last.
        /// </summary>
        public int Walk(Vector2 from, Vector2 to, int now, int frames)
        {
            for (var frame = 0; frame <= frames; frame++)
                Hover(Vector2.Lerp(from, to, (float)frame / frames), now + frame);

            return now + frames + 1;
        }

        /// <summary>Presses and releases the left button at <paramref name="point" />.</summary>
        public void Press(Vector2 point)
        {
            Workspace.PointerDown(point, PointerButton.Left, 1, false, false);
            Workspace.PointerUp(point, PointerButton.Left);
        }
    }
}
