using Lumel.Drawing;
using Lumel.Images;
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
/// A codex tip: opened by resting on its element, standing edge to edge with it on the side with room, kept while the pointer
/// crosses onto it, gone between pages through its links, and closed by leaving, by a press elsewhere or by a window opening.
/// </summary>
public class CodexTipTests
{
    static readonly RectangleF Area = new RectangleF(0f, 0f, 1000f, 800f);

    // The tip's own geometry, with none of the room or gaps the game's look gives it.
    static readonly IStyle<CodexTip> Bare = new CodexTipStyle();

    const float Wide = 300f;

    static readonly string Long = string.Join(" ", Enumerable.Repeat("words", 800));

    /// <summary>The tip opens once the pointer has rested on its element for the delay, on its own page.</summary>
    [Fact]
    public void TheTipOpensAfterTheDelay()
    {
        var scene = new Scene(100f, 100f);
        scene.Tip.Base.ShowDelay = 10;

        scene.Frame(0, On(scene.Anchor));
        scene.Frame(9, On(scene.Anchor));
        Assert.False(scene.Tip.IsOpen, "the tip opened before the delay");

        scene.Frame(10, On(scene.Anchor));
        Assert.True(scene.Tip.IsOpen);
        Assert.Same(scene.Anchor, scene.Tip.Owner);
        Assert.Same(scene.Tip, scene.Tip.Page);
    }

    /// <summary>With room to the right, the tip stands there, its left edge on the element's right edge and their tops level.</summary>
    [Fact]
    public void TheTipStandsToTheRightWhereThereIsRoom()
    {
        var scene = Scene.Opened(100f, 100f);

        Assert.Equal(TipSide.Right, scene.Tip.Side);
        Assert.Equal(scene.Anchor.Bounds.X + scene.Anchor.Bounds.Width, scene.Tip.Bounds.X, 1);
        Assert.Equal(scene.Anchor.Bounds.Y, scene.Tip.Bounds.Y, 1);
        Assert.Equal(Wide, scene.Tip.Bounds.Width, 1);
    }

    /// <summary>Without room to the right the tip stands to the left; without room either side, over; and without room over, under.</summary>
    [Theory]
    [InlineData(800f, 100f, 100f, TipSide.Left)]
    [InlineData(0f, 600f, 1000f, TipSide.Above)]
    [InlineData(0f, 0f, 1000f, TipSide.Below)]
    public void TheTipTakesTheFirstSideWithRoom(float x, float y, float width, TipSide side)
    {
        var scene = Scene.Opened(x, y, width);
        var anchor = scene.Anchor.Bounds;
        var tip = scene.Tip.Bounds;

        Assert.Equal(side, scene.Tip.Side);
        switch (side)
        {
            case TipSide.Left:
                Assert.Equal(anchor.X, tip.X + tip.Width, 1);
                break;
            case TipSide.Above:
                Assert.Equal(anchor.Y, tip.Y + tip.Height, 1);
                break;
            default:
                Assert.Equal(anchor.Y + anchor.Height, tip.Y, 1);
                break;
        }
    }

    /// <summary>The side the tip took when it opened is kept while its pages change, though the page shown later would fit elsewhere.</summary>
    [Fact]
    public void TheSideIsKeptWhileThePagesChange()
    {
        var scene = Scene.Opened(0f, 600f, 1000f);
        Assert.Equal(TipSide.Above, scene.Tip.Side);

        scene.Tip.Navigate(new Tip(Long));
        scene.Frame(1, On(scene.Anchor));

        Assert.Equal(TipSide.Above, scene.Tip.Side);
    }

    /// <summary>A tip with no width of its own is as wide as its page asks, up to its widest, not as wide as the screen.</summary>
    [Fact]
    public void ATipWithNoWidthIsAsWideAsItsPageAsks()
    {
        var scene = new Scene(100f, 100f, page: new Tip("A few words."));
        scene.Tip.Placement = null;
        scene.Tip.Base.MaxWidth = 512f;

        scene.Frame(0, On(scene.Anchor));

        Assert.True(scene.Tip.Bounds.Width < 512f, "the tip is " + scene.Tip.Bounds.Width + " wide");
        Assert.Equal(TipSide.Right, scene.Tip.Side);

        var shortWidth = scene.Tip.Bounds.Width;
        scene.Tip.Navigate(new Tip(Long));
        scene.Frame(1, On(scene.Anchor));
        Assert.True(scene.Tip.Bounds.Width > shortWidth && scene.Tip.Bounds.Width <= 512f, "a long page is " + scene.Tip.Bounds.Width + " wide");
    }

    /// <summary>The tip never grows taller than its cap, however long its page: it is cut off there rather than scrolled.</summary>
    [Fact]
    public void ATipIsNeverTallerThanItsCap()
    {
        var scene = new Scene(100f, 100f, page: new Tip(Long));
        scene.Tip.Base.MaxHeight = 120f;

        scene.Frame(0, On(scene.Anchor));

        Assert.Equal(120f, scene.Tip.Bounds.Height, 1);
    }

    /// <summary>
    /// Off both the element and the tip, the tip waits out its grace before closing, and while it waits no other element's tip
    /// takes its place; reaching the tip in time keeps it.
    /// </summary>
    [Fact]
    public void ThePointerHasTimeToCrossOntoTheTip()
    {
        var scene = Scene.Pair();
        scene.Tip.Base.CloseDelay = 15;
        var between = new Vector2(scene.Tip.Bounds.Right + 50f, scene.Tip.Bounds.Bottom + 50f);

        scene.Frame(100, between);
        scene.Frame(110, On(scene.Other!));
        Assert.Same(scene.Tip, scene.Workspace.ShownTip);

        scene.Frame(114, On(scene.Tip));
        scene.Frame(500, On(scene.Tip));
        Assert.Same(scene.Tip, scene.Tip.Page);
        Assert.True(scene.Tip.IsOpen, "the tip closed with the pointer on it");
    }

    /// <summary>An element the pointer reaches during the grace opens its tip once it has rested its delay from when it was reached, not from when the grace ran out.</summary>
    [Fact]
    public void AnElementReachedDuringTheGraceRestsFromWhenItWasReached()
    {
        var scene = Scene.Pair();
        scene.Tip.Base.CloseDelay = 15;
        scene.OtherTip!.Base.ShowDelay = 10;

        scene.Frame(100, On(scene.Other!));
        scene.Frame(114, On(scene.Other!));
        Assert.Same(scene.Tip, scene.Workspace.ShownTip);

        scene.Frame(115, On(scene.Other!));
        Assert.Same(scene.OtherTip, scene.Workspace.ShownTip);
        Assert.Same(scene.Other, scene.OtherTip.Owner);
    }

    /// <summary>Once the pointer has been off both the element and the tip for the grace, the tip closes.</summary>
    [Fact]
    public void TheTipClosesWhenThePointerHasLeftBoth()
    {
        var scene = Scene.Pair();
        scene.Tip.Base.CloseDelay = 15;
        var away = new Vector2(990f, 790f);

        scene.Frame(100, away);
        scene.Frame(114, away);
        Assert.True(scene.Tip.IsOpen, "the tip closed before its grace was out");

        scene.Frame(115, away);
        Assert.False(scene.Tip.IsOpen);
    }

    /// <summary>
    /// A press outside the tip closes it, and its element shows it again only once the pointer has rested on something else; a
    /// press inside it leaves it open.
    /// </summary>
    [Fact]
    public void APressOutsideClosesTheTipAndOneInsideDoesNot()
    {
        var scene = Scene.Pair();

        scene.Press(On(scene.Tip));
        Assert.True(scene.Tip.IsOpen, "a press on the tip closed it");

        scene.Press(On(scene.Anchor));
        Assert.False(scene.Tip.IsOpen, "a press beside the tip left it open");

        scene.Frame(1, On(scene.Anchor));
        Assert.False(scene.Tip.IsOpen, "the tip came back over what was just pressed");

        scene.Frame(2, On(scene.Other!));
        scene.Frame(3, On(scene.Anchor));
        Assert.True(scene.Tip.IsOpen, "the tip did not come back once the pointer had rested elsewhere");
    }

    /// <summary>Kept by a key, the tip without the key is an ordinary tooltip: the pointer reaches through it, and leaving the element closes it at once.</summary>
    [Fact]
    public void WithoutItsKeyATipIsAnOrdinaryTooltip()
    {
        var scene = Scene.Pair();
        scene.Codex.IsPinnedByKey = true;
        scene.Codex.FnIsPinKeyHeld = () => false;
        scene.Tip.Base.CloseDelay = 15;

        Assert.False(scene.Tip.Covers(On(scene.Tip)), "the pointer cannot reach through the tip");
        scene.Frame(100, On(scene.Tip));
        Assert.False(scene.Tip.IsOpen, "the tip stayed with the pointer off its element");
    }

    /// <summary>Held, the key lets the pointer move onto the tip, which it then reaches and which stays.</summary>
    [Fact]
    public void HoldingTheKeyLetsThePointerOntoTheTip()
    {
        var scene = Scene.Pair();
        scene.Codex.IsPinnedByKey = true;
        scene.Codex.FnIsPinKeyHeld = () => true;
        scene.Tip.Base.CloseDelay = 15;

        scene.Frame(100, On(scene.Anchor));
        scene.Frame(101, On(scene.Tip));

        Assert.True(scene.Tip.IsOpen);
        Assert.True(scene.Tip.Covers(On(scene.Tip)));
    }

    /// <summary>Letting the key go closes the tip, unless the pointer is on the tip's own element, where it stays as an ordinary tooltip.</summary>
    [Fact]
    public void LettingTheKeyGoClosesTheTipAwayFromItsElement()
    {
        var scene = Scene.Pair();
        var held = true;
        scene.Codex.IsPinnedByKey = true;
        // ReSharper disable once AccessToModifiedClosure
        // The key is pressed and let go by the test setting what the stand-in answers.
        scene.Codex.FnIsPinKeyHeld = () => held;

        scene.Frame(100, On(scene.Anchor));
        held = false;
        scene.Frame(101, On(scene.Anchor));
        Assert.True(scene.Tip.IsOpen, "letting go on the element closed the tip");

        held = true;
        scene.Frame(102, On(scene.Tip));
        held = false;
        scene.Frame(103, On(scene.Tip));
        Assert.False(scene.Tip.IsOpen, "letting go on the tip left it open");
    }

    /// <summary>A tip open with no key held shows its text in its plain look, and holding the key lights it with the tip still open on its page.</summary>
    [Fact]
    public void HoldingTheKeyLightsTheOpenTip()
    {
        var scene = new Scene(100f, 100f);
        var held = false;
        scene.Codex.IsPinnedByKey = true;
        // ReSharper disable once AccessToModifiedClosure
        // The key is pressed and let go by the test setting what the stand-in answers.
        scene.Codex.FnIsPinKeyHeld = () => held;
        scene.Tip.Base.BodyStyle = new RichTextStyle
        {
            Base = { LinkColor = Color.Gray },
            [CodexTip.Pinned] = { LinkColor = Color.Orange }
        };
        scene.Frame(0, On(scene.Anchor));
        var page = scene.Tip.Page;
        var body = (RichText)((VStack)scene.Tip.GetVisualChild(0)).Children[1];

        Assert.False(scene.Tip.Is(CodexTip.Pinned));
        Assert.False(body.Is(CodexTip.Pinned));
        Assert.Equal(Color.Gray, body.LinkColor);

        held = true;
        scene.Frame(1, On(scene.Anchor));

        Assert.True(scene.Tip.Is(CodexTip.Pinned));
        Assert.True(body.Is(CodexTip.Pinned));
        Assert.Equal(Color.Orange, body.LinkColor);
        Assert.True(scene.Tip.IsOpen);
        Assert.Same(page, scene.Tip.Page);

        held = false;
        scene.Frame(2, On(scene.Anchor));

        Assert.Equal(Color.Gray, body.LinkColor);
    }

    /// <summary>A body style for the pinned state replaces the text's and the hint's at the frame the key goes down, with the tip where it was and as large.</summary>
    [Fact]
    public void APinnedBodyStyleSwapsWithTheTipOpen()
    {
        var plain = new RichTextStyle { Base = { LinkColor = Color.Gray } };
        var lit = new RichTextStyle { Base = { LinkColor = Color.Orange } };
        var scene = new Scene(100f, 100f);
        var held = false;
        scene.Codex.IsPinnedByKey = true;
        // ReSharper disable once AccessToModifiedClosure
        // The key is pressed and let go by the test setting what the stand-in answers.
        scene.Codex.FnIsPinKeyHeld = () => held;
        scene.Tip.Base.BodyStyle = plain;
        scene.Tip[CodexTip.Pinned].BodyStyle = lit;
        scene.Frame(0, On(scene.Anchor));
        var column = (VStack)scene.Tip.GetVisualChild(0);
        var body = (RichText)column.Children[1];
        var hint = (RichText)column.Children[2];
        var bounds = scene.Tip.Bounds;
        Assert.Same(plain, body.Style);

        held = true;
        scene.Frame(1, On(scene.Anchor));

        Assert.Same(lit, body.Style);
        Assert.Same(lit, hint.Style);
        Assert.Equal(Color.Orange, body.LinkColor);
        Assert.Equal(bounds, scene.Tip.Bounds);
    }

    /// <summary>The pin hint shows on a page with a link and not on one without, and follows the page shown.</summary>
    [Fact]
    public void ThePinHintShowsOnlyOnAPageWithALink()
    {
        var linked = new Tip("See [b](b).");
        var plain = new Tip("Only words.");
        var scene = new Scene(100f, 100f, page: linked);
        scene.Codex.IsPinnedByKey = true;
        scene.Codex.FnIsPinKeyHeld = () => true;
        scene.Codex.PinHint = new Tip("Hold to pin.").Document;
        scene.Codex.FnResolvePage = _ => plain;
        scene.Frame(0, On(scene.Anchor));
        var hint = ((VStack)scene.Tip.GetVisualChild(0)).Children[2];
        Assert.True(hint.IsVisible);

        scene.Tip.Follow("b");
        Assert.False(hint.IsVisible);

        scene.Tip.GoBack();
        Assert.True(hint.IsVisible);
    }

    /// <summary>A document knows it has a link, however deep it sits.</summary>
    [Theory]
    [InlineData("Only words.", false)]
    [InlineData("See [b](b).", true)]
    [InlineData("> Quoted [b](b).", true)]
    [InlineData("::: warning\nHeld [b](b).\n:::", true)]
    [InlineData("| A |\n|---|\n| [b](b) |", true)]
    public void ADocumentKnowsItHasALink(string markdown, bool has)
    {
        Assert.Equal(has, Markdown.Parse(markdown).Links.Count > 0);
    }

    /// <summary>A tip pinned all the time is in the state as soon as it is drawn.</summary>
    [Fact]
    public void ATipPinnedAllTheTimeIsPinnedOnceDrawn()
    {
        var scene = Scene.Opened(100f, 100f);

        Assert.True(scene.Tip.Is(CodexTip.Pinned));
    }

    /// <summary>A window opening closes the tip, so nothing that opens is covered by it.</summary>
    [Fact]
    public void AWindowOpeningClosesTheTip()
    {
        var scene = Scene.Pair();

        scene.Workspace.Open(new Window { Placement = FixedSize.Width(100f) });

        Assert.False(scene.Tip.IsOpen);
        Assert.Null(scene.Workspace.ShownTip);
    }

    /// <summary>A link leads to the page it names, back and forward step through what was read, and going on drops what was stepped back from.</summary>
    [Fact]
    public void LinksMoveThroughPagesWithBackAndForward()
    {
        var pages = new Dictionary<string, Tip> { ["b"] = new Tip("Page b."), ["c"] = new Tip("Page c.") };
        var scene = new Scene(100f, 100f, page: new Tip("See [b](b)."));
        scene.Codex.FnResolvePage = id => pages.TryGetValue(id, out var page) ? page : null;
        scene.Frame(0, On(scene.Anchor));
        Assert.False(scene.Tip.CanGoBack || scene.Tip.CanGoForward, "a fresh tip has somewhere to go");

        Assert.True(scene.Tip.Follow("b"));
        Assert.Same(pages["b"], scene.Tip.Page);
        Assert.True(scene.Tip.CanGoBack);

        scene.Tip.GoBack();
        Assert.Same(scene.Tip, scene.Tip.Page);
        Assert.True(scene.Tip.CanGoForward);

        scene.Tip.Follow("c");
        Assert.Same(pages["c"], scene.Tip.Page);
        Assert.False(scene.Tip.CanGoForward, "going on kept what was stepped back from");
    }

    /// <summary>A page is its document whole, a heading it opens with included, with no title row and no row of steps above it.</summary>
    [Fact]
    public void AFreshTipIsItsPageAlone()
    {
        var page = new Tip("# A heading\n\nA few words about it.");
        var scene = new Scene(100f, 100f, page: page);
        scene.Tip.Base.BackSymbol = "<";
        scene.Tip.Base.ForwardSymbol = ">";

        scene.Frame(0, On(scene.Anchor));

        Assert.Equal(Alone(page), scene.Tip.Bounds.Height, 1);
    }

    /// <summary>The row of steps back and forward shows above the page once a link was followed, and stays while there is a page to step forward to.</summary>
    [Fact]
    public void TheStepsShowOnceALinkWasFollowed()
    {
        var first = new Tip("See [b](b).");
        var second = new Tip("Page b.");
        var scene = new Scene(100f, 100f, page: first);
        scene.Tip.Base.BackSymbol = "<";
        scene.Tip.Base.ForwardSymbol = ">";
        scene.Codex.FnResolvePage = id => id == "b" ? second : null;
        scene.Frame(0, On(scene.Anchor));

        scene.Tip.Follow("b");
        scene.Frame(1, On(scene.Anchor));
        Assert.True(scene.Tip.Bounds.Height > Alone(second) + 1f, "the steps did not show after a link was followed");

        scene.Tip.GoBack();
        scene.Frame(2, On(scene.Anchor));
        Assert.True(scene.Tip.Bounds.Height > Alone(first) + 1f, "the steps went with a page still to step forward to");
    }

    /// <summary>A link to a page there is none of goes nowhere: the page shown stays.</summary>
    [Fact]
    public void ALinkToAMissingPageFails()
    {
        var scene = new Scene(100f, 100f, page: new Tip("See [nowhere](nowhere)."));
        scene.Codex.FnResolvePage = _ => null;
        scene.Frame(0, On(scene.Anchor));

        Assert.False(scene.Tip.Follow("nowhere"));
        Assert.Same(scene.Tip, scene.Tip.Page);
        Assert.False(scene.Tip.CanGoBack);
    }

    /// <summary>A tip opens on its own page again, the pages read the time before gone.</summary>
    [Fact]
    public void ATipOpensFreshEachTime()
    {
        var scene = Scene.Pair();
        scene.Codex.FnResolvePage = _ => new Tip("Elsewhere.");
        scene.Tip.Follow("anywhere");

        scene.Frame(1, new Vector2(990f, 790f));
        Assert.False(scene.Tip.IsOpen);

        scene.Frame(2, On(scene.Anchor));
        Assert.Same(scene.Tip, scene.Tip.Page);
        Assert.False(scene.Tip.CanGoBack);
    }

    /// <summary>A tip given a shadow draws it before itself, reaching out past its edge.</summary>
    [Fact]
    public void ATipCastsItsShadow()
    {
        var scene = new Scene(100f, 100f);
        scene.Tip.Base.Shadow = new Shadow(new NineSlice(VRage.Utils.MyStringId.GetOrCompute("Shade"), 64, 24), 8f, new Vector2(2f, 4f), Color.Black);

        var graphics = scene.Frame(0, On(scene.Anchor));

        var shadow = graphics.Pieces.Where(piece => piece.Material.String == "Shade").Select(piece => piece.Destination).ToList();
        Assert.NotEmpty(shadow);
        Assert.Equal(scene.Tip.Bounds.X + 2f - 8f, shadow.Min(r => r.X), 1);
    }

    /// <summary>A tip whose element is taken off the screen closes.</summary>
    [Fact]
    public void ATipClosesWhenItsElementLeavesTheScreen()
    {
        var scene = Scene.Opened(100f, 100f);

        scene.Root.Children.Remove(scene.Anchor.Parent!);
        scene.Frame(1, new Vector2(990f, 790f));

        Assert.False(scene.Tip.IsOpen);
    }

    /// <summary>A tip given no style takes the game's: the chrome's color with a hairline round it, room inside the edge, and its parts a little apart.</summary>
    [Fact]
    public void AnUnstyledTipTakesTheGamesLook()
    {
        var tip = new CodexTip(new Codex(), new Tip("Words."));
        tip.Restyle();

        Assert.Equal(GameColors.Chrome, tip.Background);
        Assert.Equal(GameColors.Line, tip.Border);
        Assert.Equal(new Thickness(12f, 8f), tip.Padding);
        Assert.Equal(8f, tip.Spacing);
    }

    /// <summary>A tip's style sets its room, the gap between its parts, and the looks and symbols of its own words and steps.</summary>
    [Fact]
    public void ItsStyleDressesItsOwnParts()
    {
        var body = new RichTextStyle { Base = { FontSize = 20f } };
        var steps = new GlyphStyle { Base = { FontSize = 16f } };
        var page = new Tip("A few [words](words) about it.");
        var scene = new Scene(100f, 100f, page: page);
        scene.Tip.Style = new CodexTipStyle
        {
            Base = { Padding = new Thickness(12f, 8f), Spacing = 8f, BodyStyle = body, StepStyle = steps, BackSymbol = "<", ForwardSymbol = ">" }
        };
        scene.Codex.IsPinnedByKey = true;
        scene.Codex.FnIsPinKeyHeld = () => false;
        scene.Codex.PinHint = new Tip("Hold to pin.").Document;

        scene.Frame(0, On(scene.Anchor));

        Assert.Same(body, scene.Tip.BodyStyle);
        Assert.Same(steps, scene.Tip.StepStyle);
        Assert.Equal("<", scene.Tip.BackSymbol);
        Assert.Equal(">", scene.Tip.ForwardSymbol);
        Assert.Equal(8f, scene.Tip.Spacing);
        var words = Alone(page, body, Wide - 24f);
        var hint = Alone(new Tip("Hold to pin."), body, Wide - 24f);
        Assert.Equal(8f + words + 8f + hint + 8f, scene.Tip.Bounds.Height, 1);
    }

    /// <summary>Returns the height of <paramref name="page" /> shown alone as rich text, at the tip's width.</summary>
    static float Alone(Tip page) => Alone(page, null, Wide);

    /// <summary>
    ///     Returns the height of <paramref name="page" /> shown alone as rich text in <paramref name="style" />,
    ///     <paramref name="wide" /> across.
    /// </summary>
    static float Alone(Tip page, IStyle<RichText>? style, float wide)
    {
        var text = new RichText { Document = page.Document, Style = style };
        text.Measure(new Recording(Area), new Vector2(wide, Area.Height));
        return text.DesiredSize.Y;
    }

    static Vector2 On(Element element) => new Vector2(element.Bounds.X + element.Bounds.Width / 2f, element.Bounds.Y + element.Bounds.Height / 2f);

    /// <summary>
    ///     A workspace whose one window fills the area, holding an element at a given place with a codex tip, and
    ///     optionally a second element below it with a tip of its own.
    /// </summary>
    sealed class Scene
    {
        public Scene(float x, float y, float width = 100f, Tip? page = null, bool pair = false)
        {
            Anchor = new VStack { Placement = FixedSize.Size(width, 40f), Align = Align.Left | Align.Top };
            Tip = new CodexTip(Codex, page ?? new Tip("A few words about it.")) { Style = Bare, Placement = FixedSize.Width(Wide) };
            Anchor.Tip = Tip;
            Root = new VStack { Children = { new VStack { Base = { Padding = new Thickness(x, y, 0f, 0f) }, Children = { Anchor } } } };

            if (pair)
            {
                Other = new VStack { Placement = FixedSize.Size(100f, 40f), Align = Align.Left | Align.Top };
                OtherTip = new CodexTip(Codex, new Tip("Other.")) { Style = Bare, Placement = FixedSize.Width(Wide) };
                Other.Tip = OtherTip;
                Root.Children.Add(new VStack { Base = { Padding = new Thickness(100f, 400f - y - 40f, 0f, 0f) }, Children = { Other } });
            }

            Workspace.Open(new Window { Style = Showing.Bare, Placement = FixedSize.Size(Area.Width, Area.Height), Content = Root });
            Frame(-1000, new Vector2(float.NegativeInfinity));
        }

        public Codex Codex { get; } = new Codex();

        public Workspace Workspace { get; } = new Workspace();

        public VStack Root { get; }

        public VStack Anchor { get; }

        public CodexTip Tip { get; }

        public VStack? Other { get; }

        public CodexTip? OtherTip { get; }

        /// <summary>Creates a scene with the tip open on its element and drawn.</summary>
        public static Scene Opened(float x, float y, float width = 100f)
        {
            var scene = new Scene(x, y, width);
            scene.Frame(0, On(scene.Anchor));
            return scene;
        }

        /// <summary>Creates two separate elements on one screen, with the first one's tip open and drawn.</summary>
        public static Scene Pair()
        {
            var scene = new Scene(100f, 100f, pair: true);
            scene.Frame(0, On(scene.Anchor));
            return scene;
        }

        /// <summary>Draws a frame at <paramref name="now" /> with the pointer at <paramref name="pointer" />, and returns what was drawn.</summary>
        public Recording Frame(int now, Vector2 pointer)
        {
            Workspace.Pointer = pointer;
            var graphics = new Recording(Area) { Now = now };
            Workspace.DrawScreen(graphics, Area);
            return graphics;
        }

        /// <summary>Presses and releases the left button at <paramref name="point" />.</summary>
        public void Press(Vector2 point)
        {
            Workspace.PointerDown(point, PointerButton.Left, 1, false, false);
            Workspace.PointerUp(point, PointerButton.Left);
        }
    }
}
