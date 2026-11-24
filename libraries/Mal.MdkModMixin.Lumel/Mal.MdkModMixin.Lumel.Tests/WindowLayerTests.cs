using VRage.Utils;
using Lumel.Drawing;
using Lumel.Controls;
using Lumel.Input;
using Lumel.Layout;
using Lumel.Text;
using Lumel.Viewing;
using Lumel.Windows;
using VRageMath;
using Xunit;

namespace Lumel.Tests;

/// <summary>Windows over a screen: placed by the layer, drawn over what is beneath, keeping what is on them to themselves, and a modal one keeping all input.</summary>
public class WindowLayerTests
{
    static readonly RectangleF Area = new RectangleF(0f, 0f, 1000f, 800f);

    /// <summary>A window stands in the middle of the area, as wide as its placement says.</summary>
    [Fact]
    public void AWindowStandsInTheMiddle()
    {
        var (layer, window, _) = Opened(modal: false);

        Assert.Equal(300f, window.Bounds.Width);
        Assert.Equal(Area.Width / 2f, window.Bounds.X + window.Bounds.Width / 2f, 1);
        Assert.Equal(Area.Height / 2f, window.Bounds.Y + window.Bounds.Height / 2f, 1);
        Assert.Equal(1, layer.Count);
    }

    /// <summary>A press on what a window holds reaches it, and the screen beneath is told to stay out of it.</summary>
    [Fact]
    public void APressOnAWindowReachesWhatItHolds()
    {
        var (layer, _, button) = Opened(modal: false);

        var hit = layer.HitTest(Middle(button), out var stopped);

        Assert.Same(button, hit);
        Assert.True(stopped);
    }

    /// <summary>Beside a modeless window, a press is left for the screen beneath; beside a modal one it reaches nothing at all.</summary>
    [Theory]
    [InlineData(false, false)]
    [InlineData(true, true)]
    public void AModalWindowKeepsPressesFromTheScreen(bool modal, bool kept)
    {
        var (layer, _, _) = Opened(modal);

        var hit = layer.HitTest(new Vector2(5f, 5f), out var stopped);

        Assert.Null(hit);
        Assert.Equal(kept, stopped);
        Assert.Equal(modal, layer.Modal != null);
    }

    /// <summary>A modal window's backdrop is painted over the whole area before the window is drawn.</summary>
    [Fact]
    public void AModalWindowPaintsItsBackdropFirst()
    {
        var graphics = new Recording(Area);
        var layer = new WindowLayer();
        layer.Open(new Dialog { Base = { Backdrop = Color.Black, Background = Color.White }, Content = new Button { Text = "Go" } });

        layer.Draw(graphics, Area);

        Assert.Equal(Area, graphics.Fills[0]);
        Assert.True(graphics.Fills.Count > 1, "the window itself was not drawn");
    }

    /// <summary>Closing a window takes it away, and the screen has the input back.</summary>
    [Fact]
    public void ClosingGivesTheInputBack()
    {
        var (layer, window, _) = Opened(modal: true);

        layer.Close(window);
        layer.HitTest(new Vector2(5f, 5f), out var stopped);

        Assert.False(stopped);
        Assert.Equal(0, layer.Count);
    }

    /// <summary>A long list hanging under something near the top stops at the foot of the area and scrolls, rather than running off it.</summary>
    [Fact]
    public void ALongListHangingOpenStopsAtTheEdgeAndScrolls()
    {
        var (layer, popup, scroll, rows) = Hanging(73, 100f);

        Assert.True(popup.Bounds.Y >= 140f, "the list is not under what it hangs from");
        Assert.True(popup.Bounds.Y + popup.Bounds.Height <= Area.Y + Area.Height, "the list runs off the area");
        Assert.True(scroll.CanScroll, "a list longer than its room does not scroll");
        Assert.Equal(1, layer.Count);
        Assert.True(rows[72].Bounds.Y > popup.Bounds.Y + popup.Bounds.Height, "the last word is on show without scrolling");
    }

    /// <summary>A short list is as tall as its words, with nothing to scroll.</summary>
    [Fact]
    public void AShortListIsAsTallAsItsWords()
    {
        var (_, popup, scroll, rows) = Hanging(3, 100f);

        Assert.False(scroll.CanScroll);
        Assert.Equal(rows[2].Bounds.Y + rows[2].Bounds.Height + popup.Padding.Bottom, popup.Bounds.Y + popup.Bounds.Height, 1);
    }

    /// <summary>Asked to show one of its words, a list scrolls just far enough to bring it into view.</summary>
    [Fact]
    public void AListBringsAWordIntoView()
    {
        var (layer, popup, scroll, rows) = Hanging(73, 100f);

        scroll.BringIntoView(rows[60]);
        layer.Draw(new Recording(Area), Area);

        Assert.True(rows[60].Bounds.Y >= popup.Bounds.Y, "the word is above what is on show");
        Assert.True(rows[60].Bounds.Y + rows[60].Bounds.Height <= popup.Bounds.Y + popup.Bounds.Height,
            "the word is below what is on show");
    }

    /// <summary>A list opened on something near the foot hangs over it instead, in the room above.</summary>
    [Fact]
    public void AListNearTheFootHangsAboveInTheRoomThere()
    {
        var (_, popup, scroll, _) = Hanging(73, 700f);

        Assert.True(popup.Bounds.Y + popup.Bounds.Height <= 700f, "the list is not over what it hangs from");
        Assert.True(popup.Bounds.Y >= Area.Y, "the list runs off the top");
        Assert.True(scroll.CanScroll);
    }

    /// <summary>A long list under a box of its own at its head, as a list of kinds with its search is, stays hung from what opened it, on the area, its rows scrolling.</summary>
    [Fact]
    public void ALongListUnderAHeadStaysOnItsAnchor()
    {
        var (_, popup, scroll, _) = Hanging(73, 100f, headed: true);

        Assert.True(popup.Bounds.Y >= 140f, "the list left what it hangs from");
        Assert.True(popup.Bounds.Y + popup.Bounds.Height <= Area.Y + Area.Height, "the list runs off the area");
        Assert.True(scroll.CanScroll, "a list longer than its room does not scroll");
    }

    /// <summary>A list that fits under what opened it opens there, though there is more room above.</summary>
    [Fact]
    public void AListThatFitsBelowOpensBelow()
    {
        var (_, popup, scroll, _) = Hanging(3, 600f);

        Assert.True(popup.Bounds.Y >= 640f, "the list opened above though it fits below");
        Assert.False(scroll.CanScroll);
    }

    /// <summary>A list too tall for the room under what opened it, but not for the room above, opens above whole.</summary>
    [Fact]
    public void AListThatFitsOnlyAboveOpensAboveWhole()
    {
        var (_, popup, scroll, _) = Hanging(8, 700f);

        Assert.True(popup.Bounds.Y + popup.Bounds.Height <= 700f, "the list is not over what it hangs from");
        Assert.False(scroll.CanScroll, "a list with room for all of it above was cut");
    }

    static (WindowLayer, Popup, ScrollView, List<Button>) Hanging(int count, float at, bool headed = false)
    {
        var anchor = new Button { Text = "Anchor" };
        anchor.Measure(new Recording(Area), new Vector2(200f, 40f));
        anchor.Arrange(new RectangleF(0f, at, 200f, 40f));

        var rows = new List<Button>();
        var stack = new VStack();
        for (var i = 0; i < count; i++)
        {
            var row = new Button { Text = "Word " + i, Command = new RelayCommand(() => { }) };
            rows.Add(row);
            stack.Children.Add(row);
        }

        var scroll = new ScrollView { Base = { ScrollBarWidth = 12f, Thumb = Color.White }, FitsContent = true, Content = stack };
        Element content = scroll;
        if (headed)
        {
            scroll.Placement = Flex.Grow;
            content = new VStack { Children = { new Button { Text = "Head" }, scroll } };
        }

        var popup = new Popup { Anchor = anchor, Content = content };
        var layer = new WindowLayer();
        layer.Covers(new VStack { Children = { anchor } });
        layer.Open(popup);
        layer.Draw(new Recording(Area), Area);
        return (layer, popup, scroll, rows);
    }

    /// <summary>
    ///     An element whose look follows the background opacity, a window or anything in one, paints its background at
    ///     the layer's opacity, and one whose look does not paints it as its look says.
    /// </summary>
    [Theory]
    [InlineData(true, 100)]
    [InlineData(false, 200)]
    public void AnElementFollowingTheUserBackgroundOpacityFadesItsBackground(bool follows, int alpha)
    {
        var label = new Label { Base = { Background = new Color(40, 50, 60, 200), FollowsUserBackgroundOpacity = follows }, Text = "Words" };
        var window = new Window
        {
            Base = { Background = new Color(10, 20, 30, 200), FollowsUserBackgroundOpacity = true },
            Placement = FixedSize.Size(300f, 200f),
            Content = label
        };
        var layer = new WindowLayer { UserBackgroundOpacity = 0.5f };
        layer.Open(window);
        var recording = new Recording(Area);

        layer.Draw(recording, Area);

        Assert.Equal(new Color(10, 20, 30, 100), FillOf(recording, window));
        Assert.Equal(new Color(40, 50, 60, alpha), FillOf(recording, label));
    }

    /// <summary>Returns the color <paramref name="element" />'s background was painted in on <paramref name="recording" />.</summary>
    static Color FillOf(Recording recording, Element element)
    {
        var index = recording.Fills.IndexOf(element.Bounds);
        Assert.True(index >= 0, "the background was not painted");
        return recording.FillColors[index];
    }

    static (WindowLayer, Window, Button) Opened(bool modal)
    {
        var button = new Button { Text = "Go", Command = new RelayCommand(() => { }) };
        var window = new Window { IsModal = modal, Placement = FixedSize.Width(300f), Content = new VStack { Children = { button } } };
        var layer = new WindowLayer();
        layer.Open(window);
        layer.Draw(new Recording(Area), Area);
        return (layer, window, button);
    }

    /// <summary>Over a window holding nothing that takes input, a press, a resting pointer and a wheel all stop at the window; beside it, none do.</summary>
    [Fact]
    public void AWindowKeepsWhatIsOnItToItself()
    {
        var layer = new WindowLayer();
        var window = new FloatingWindow { Placement = FixedSize.Size(200f, 100f), Content = new Label { Text = "Words" } };
        layer.Open(window);
        layer.Draw(new Recording(Area), Area);

        var on = new Vector2(window.Bounds.X + 20f, window.Bounds.Y + 20f);
        var beside = new Vector2(window.Bounds.X - 20f, window.Bounds.Y - 20f);

        layer.HitTest(on, out var pressed);
        layer.TipOwnerAt(on, out var said);
        layer.WheelAt(on, out var turned);
        Assert.True(pressed, "a press went through");
        Assert.True(said, "the pointer's note came from beneath");
        Assert.True(turned, "the wheel went through");

        layer.HitTest(beside, out pressed);
        layer.TipOwnerAt(beside, out said);
        layer.WheelAt(beside, out turned);
        Assert.False(pressed || said || turned, "something beside the window stopped at it");
    }

    /// <summary>A floating window is kept on the screen its layer gives it, and a move by its title bar ends with it brought back onto it and said.</summary>
    [Fact]
    public void AFloatingWindowMovedOffTheScreenIsBroughtBack()
    {
        var layer = new WindowLayer();
        var bar = new TitleBar { Content = new Label { Text = "Title" } };
        var window = new FloatingWindow { Placement = FixedSize.Size(200f, 100f), Content = bar, MinimumVisible = 50f };
        bar.Window = window;
        layer.Open(window);
        layer.Draw(new Recording(Area), Area);
        Assert.Equal(Area, window.Screen);
        var moved = 0;
        window.Moved += () => moved++;
        var input = new InputRoot(window);

        var on = bar.ToScreen(new Vector2(bar.Bounds.X + 4f, bar.Bounds.Y + 4f));
        var off = new Vector2(Area.Width + 500f, on.Y);
        input.PointerDown(on, PointerButton.Left, 1, false, false);
        input.PointerMove(off);
        input.PointerUp(off, PointerButton.Left);

        Assert.Equal(1, moved);
        Assert.Equal(Area.Width - 50f, window.Position!.Value.X, 1);
    }

    /// <summary>Anything on a screen a layer covers finds that layer through what holds it, and anything loose finds none.</summary>
    [Fact]
    public void WhatIsOnACoveredScreenFindsItsLayer()
    {
        var button = new Button { Text = "Go" };
        var screen = new VStack { Children = { new HStack { Children = { button } } } };
        var layer = new WindowLayer();

        Assert.Null(button.Windows);

        layer.Covers(screen);

        Assert.Same(layer, button.Windows);
        Assert.Null(new Button().Windows);
    }

    /// <summary>A popup opens over the layer its anchor finds, what it holds finds that layer while it is open, and closing it lets go of both.</summary>
    [Fact]
    public void APopupOpensWhereItsAnchorIs()
    {
        var anchor = new Button { Text = "Open" };
        var inside = new Button { Text = "Inside" };
        var popup = new Popup { Anchor = anchor, Content = new VStack { Children = { inside } } };
        var layer = new WindowLayer();
        layer.Covers(new VStack { Children = { anchor } });

        popup.Toggle();

        Assert.True(layer.IsOpen(popup));
        Assert.True(popup.IsOpen);
        Assert.Same(layer, inside.Windows);

        popup.Toggle();

        Assert.False(layer.IsOpen(popup));
        Assert.False(popup.IsOpen);
        Assert.Null(inside.Windows);
    }

    /// <summary>A popup hung beside its anchor opens to its right with its first entry level with it, and to its left where the right has no room.</summary>
    [Theory]
    [InlineData(100f, true)]
    [InlineData(900f, false)]
    public void APopupBesideItsAnchorSetsItsFirstEntryLevelWithIt(float at, bool right)
    {
        var anchor = new Button { Text = "Add" };
        var first = new Button { Text = "First" };
        var popup = new Popup
        {
            Anchor = anchor,
            Side = PopupSide.Right,
            Base = { Gap = 4f, Padding = new Thickness(8f) },
            Placement = FixedSize.Width(150f),
            Content = new VStack { Children = { first, new Button { Text = "Second" }, new Button { Text = "Third" } } }
        };
        var screen = new VStack { Children = { anchor } };
        var layer = new WindowLayer();
        layer.Covers(screen);
        screen.Measure(new Recording(Area), new Vector2(Area.Width, Area.Height));
        anchor.Arrange(new RectangleF(at, 300f, 80f, 30f));

        popup.Open();
        layer.Draw(new Recording(Area), Area);

        Assert.Equal(Middle(anchor).Y, Middle(first).Y, 1);
        if (right)
            Assert.Equal(anchor.Bounds.X + anchor.Bounds.Width + 4f, popup.Bounds.X, 1);
        else
            Assert.Equal(anchor.Bounds.X - 4f, popup.Bounds.X + popup.Bounds.Width, 1);
    }

    /// <summary>A drop-down's list hangs no narrower than its line, however short its words; any other popup is as wide as what it holds.</summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ADropDownsListIsNeverNarrowerThanItsLine(bool flush)
    {
        var anchor = new Button { Text = "Line" };
        var popup = new Popup
        {
            Anchor = anchor,
            MatchAnchorWidth = flush,
            Placement = FixedSize.Width(40f),
            Content = new VStack { Children = { new Button { Text = "A" }, new Button { Text = "B" } } }
        };
        var screen = new VStack { Children = { anchor } };
        var layer = new WindowLayer();
        layer.Covers(screen);
        screen.Measure(new Recording(Area), new Vector2(Area.Width, Area.Height));
        anchor.Arrange(new RectangleF(100f, 300f, 200f, 30f));

        popup.Open();
        layer.Draw(new Recording(Area), Area);

        Assert.Equal(anchor.Bounds.X, popup.Bounds.X, 1);
        Assert.Equal(flush ? 200f : 40f, popup.Bounds.Width, 1);
    }

    /// <summary>A window whose style casts a shadow draws it before itself, reaching out past its edge; one whose style casts none draws nothing.</summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AWindowCastsTheShadowItsStyleGivesIt(bool casts)
    {
        var shade = new NineSlice(MyStringId.GetOrCompute("Shade"), 64, 24);
        var style = new WindowStyle { Base = { Shadow = casts ? new Shadow(shade, 8f, new Vector2(2f, 4f), Color.Black) : null } };
        var window = new Window { Style = style, Placement = FixedSize.Size(200f, 100f), Content = new Label { Text = "Words" } };
        var layer = new WindowLayer();
        var graphics = new Recording(Area);
        layer.Open(window);

        layer.Draw(graphics, Area);

        var shadow = graphics.Pieces.Where(piece => piece.Material.String == "Shade").Select(piece => piece.Destination).ToList();
        if (!casts)
        {
            Assert.Empty(shadow);
            return;
        }

        Assert.NotEmpty(shadow);
        Assert.Equal(window.Bounds.X + 2f - 8f, shadow.Min(r => r.X), 1);
        Assert.Equal(window.Bounds.Y + window.Bounds.Height + 4f + 8f, shadow.Max(r => r.Y + r.Height), 1);
    }

    /// <summary>A popup whose anchor is on no screen, or that has no anchor, refuses to open, and stays closed.</summary>
    [Fact]
    public void APopupWithNowhereToOpenThrows()
    {
        var loose = new Popup { Anchor = new Button(), Content = new Label { Text = "Words" } };
        var unanchored = new Popup { Content = new Label { Text = "Words" } };

        Assert.Throws<InvalidOperationException>(() => loose.Open());
        Assert.Throws<InvalidOperationException>(() => unanchored.Open());
        Assert.False(loose.IsOpen);
        Assert.False(unanchored.IsOpen);
    }

    /// <summary>
    ///     A hung popup waits until what it hangs from is on a screen, opens there, and follows it when it is rebuilt;
    ///     with nothing to find its anchor, hanging it throws.
    /// </summary>
    [Fact]
    public void AHungPopupWaitsForItsAnchorAndFollowsItRebuilt()
    {
        Button? found = null;
        // ReSharper disable once AccessToModifiedClosure - the finder is meant to see the anchor as the test rebuilds it.
        var popup = new Popup { FnAnchor = () => found!, Content = new Label { Text = "Words" } };
        var row = new VStack();
        var layer = new WindowLayer();
        layer.Covers(new VStack { Children = { row } });

        popup.Hang();
        Assert.False(popup.IsOpen);

        found = new Button { Text = "First" };
        popup.Hang();
        Assert.False(popup.IsOpen);

        row.Children.Add(found);
        popup.Hang();
        Assert.True(layer.IsOpen(popup));
        Assert.Same(found, popup.Anchor);

        row.Children.Remove(found);
        found = new Button { Text = "Rebuilt" };
        row.Children.Add(found);
        popup.Hang();
        Assert.True(popup.IsOpen);
        Assert.Same(found, popup.Anchor);

        Assert.Throws<InvalidOperationException>(() => new Popup().Hang());
    }

    /// <summary>A window accepted closes saying so, and one dismissed by a press closes as plainly closed.</summary>
    [Fact]
    public void AnAcceptedWindowClosesSayingSo()
    {
        var (layer, popup, _, _) = Hanging(3, 100f);
        var reasons = new List<CloseReason>();
        popup.Closed += reasons.Add;

        popup.Accept();
        layer.Open(popup);
        layer.Dismiss(new Vector2(Area.Width - 1f, Area.Height - 1f));

        Assert.Equal(new[] { CloseReason.Accepted, CloseReason.Closed }, reasons);
        Assert.False(popup.IsOpen);
    }

    /// <summary>A popup a press closes by missing it is closed as far as it knows too.</summary>
    [Fact]
    public void APopupDismissedByAPressKnowsItIsClosed()
    {
        var (layer, popup, _, _) = Hanging(3, 100f);

        Assert.True(popup.IsOpen);
        Assert.True(layer.Dismiss(new Vector2(Area.Width - 1f, Area.Height - 1f)));
        Assert.False(popup.IsOpen);
    }

    /// <summary>
    ///     A popup closes, as plainly closed, at the first draw after its anchor is taken out of the tree or something
    ///     holding it is hidden, and a popup hanging from one in it closes with it.
    /// </summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void APopupClosesOnceItsAnchorIsNoLongerShown(bool removed)
    {
        var anchor = new Button { Text = "Anchor" };
        var row = new VStack { Children = { anchor } };
        var screen = new VStack { Children = { row } };
        var layer = new WindowLayer();
        layer.Covers(screen);
        var item = new Button { Text = "Item" };
        var popup = new Popup { Anchor = anchor, Content = item };
        var submenu = new Popup { Anchor = item, Content = new Label { Text = "Words" } };
        var reasons = new List<CloseReason>();
        popup.Closed += reasons.Add;
        popup.Open();
        submenu.Open();

        layer.Draw(new Recording(Area), Area);
        Assert.True(popup.IsOpen);
        Assert.True(submenu.IsOpen);

        if (removed)
            screen.Children.Remove(row);
        else
            row.IsVisible = false;

        Assert.True(popup.IsOpen);
        layer.Draw(new Recording(Area), Area);

        Assert.False(popup.IsOpen);
        Assert.False(submenu.IsOpen);
        Assert.Equal(0, layer.Count);
        Assert.Equal(new[] { CloseReason.Closed }, reasons);
    }

    /// <summary>A popup with a finder for its anchor moves to the one it finds when its anchor is rebuilt, and closes once it finds none.</summary>
    [Fact]
    public void APopupWithAFinderMovesToItsRebuiltAnchorAndClosesWithoutOne()
    {
        var anchor = new Button { Text = "First" };
        var row = new VStack { Children = { anchor } };
        var layer = new WindowLayer();
        layer.Covers(new VStack { Children = { row } });
        // ReSharper disable once AccessToModifiedClosure - the finder is meant to see the anchor as the test rebuilds it.
        var popup = new Popup { Anchor = anchor, FnAnchor = () => anchor, Content = new Label { Text = "Words" } };
        popup.Open();

        row.Children.Remove(anchor);
        anchor = new Button { Text = "Rebuilt" };
        row.Children.Add(anchor);
        layer.Draw(new Recording(Area), Area);

        Assert.True(popup.IsOpen);
        Assert.Same(anchor, popup.Anchor);

        row.Children.Remove(anchor);
        layer.Draw(new Recording(Area), Area);

        Assert.False(popup.IsOpen);
    }

    static Vector2 Middle(Element element) =>
        new Vector2(element.Bounds.X + element.Bounds.Width / 2f, element.Bounds.Y + element.Bounds.Height / 2f);
}
