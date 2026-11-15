using Lumel.Input;
using Lumel.Menus;
using Lumel.Screens;
using Lumel.Styling;
using Lumel.Text;
using VRage.Input;
using VRageMath;
using Xunit;

namespace Lumel.Tests;

/// <summary>
///     A menu: its items pressed by pointer or key, its separators shown only between items, where it opens, and how an
///     item lays out its words and shortcut.
/// </summary>
public class MenuTests
{
    static readonly RectangleF Area = new RectangleF(0f, 0f, 800f, 600f);

    /// <summary>Pressing an item closes the menu before the item's command runs, and clicks.</summary>
    [Fact]
    public void PressingAnItemClosesTheMenuFirst()
    {
        var heard = new Heard();
        var openWhenRun = true;
        var menu = new Menu();
        var item = new MenuItem { Text = "Paste", Command = new RelayCommand(() => openWhenRun = menu.IsOpen) };
        menu.Items.Add(item);
        var workspace = Opened(menu, heard);

        Press(workspace, item);

        Assert.False(openWhenRun, "the menu was still open as the command ran");
        Assert.False(menu.IsOpen);
        Assert.Equal(new[] { GameTheme.ClickSound }, heard.Played);
    }

    /// <summary>
    ///     A pressed item is announced while the menu is still open, so its owner can tell a press from a dismissal,
    ///     which announces nothing.
    /// </summary>
    [Fact]
    public void APressIsToldApartFromADismissal()
    {
        var said = new List<string>();
        var item = Item("Copy", said);
        var menu = new Menu { Items = { item } };
        var pressed = new List<(MenuItem, bool)>();
        menu.ItemPressed += pressedItem => pressed.Add((pressedItem, menu.IsOpen));
        var workspace = Opened(menu, new Heard());

        Press(workspace, item);
        Assert.Equal(new[] { (item, true) }, pressed);

        menu.OpenAt(workspace.Windows, new Vector2(100f, 100f));
        workspace.DrawScreen(new Recording(Area), Area);
        workspace.Key(MyKeys.Escape, false, false);

        Assert.False(menu.IsOpen);
        Assert.Single(pressed);
    }

    /// <summary>An item whose command cannot run looks disabled, and a press on it runs nothing and leaves the menu open.</summary>
    [Fact]
    public void AnItemThatCannotRunIsDisabled()
    {
        var item = new MenuItem { Text = "Paste", Command = new RelayCommand(() => { }) { CanExecute = false } };
        var menu = new Menu { Items = { item } };
        var workspace = Opened(menu, new Heard());

        Press(workspace, item);

        Assert.True(menu.IsOpen);
        Assert.Equal(GameColors.Dim, item.Color);
    }

    /// <summary>Down and Up move between the items that can be used, around the ends, and Enter presses the one they rest on.</summary>
    [Fact]
    public void KeysMoveBetweenUsableItemsAndEnterPressesOne()
    {
        var said = new List<string>();
        var copy = Item("Copy", said);
        var paste = new MenuItem { Text = "Paste", Command = new RelayCommand(() => { }) { CanExecute = false } };
        var delete = Item("Delete", said);
        var menu = new Menu { Items = { copy, paste, new MenuSeparator(), delete } };
        var workspace = Opened(menu, new Heard());

        Assert.True(workspace.Key(MyKeys.Down, false, false));
        Assert.Same(copy, menu.Highlight);
        workspace.Key(MyKeys.Down, false, false);
        Assert.Same(delete, menu.Highlight);
        workspace.Key(MyKeys.Down, false, false);
        Assert.Same(copy, menu.Highlight);
        workspace.Key(MyKeys.Up, false, false);
        Assert.Same(delete, menu.Highlight);

        Assert.True(workspace.Key(MyKeys.Enter, false, false));
        Assert.Equal(new[] { "Delete" }, said);
        Assert.False(menu.IsOpen);
        Assert.Null(menu.Highlight);
    }

    /// <summary>Up with nothing highlighted goes to the last usable item.</summary>
    [Fact]
    public void UpFirstGoesToTheLastItem()
    {
        var said = new List<string>();
        var first = Item("Copy", said);
        var last = Item("Delete", said);
        var menu = new Menu { Items = { first, last } };
        var workspace = Opened(menu, new Heard());

        workspace.Key(MyKeys.Up, false, false);

        Assert.Same(last, menu.Highlight);
    }

    /// <summary>
    ///     The pointer and the keys share one highlight: the pointer lights the item it comes onto, the keys move on from
    ///     there and leave only their item lit, and the pointer leaving puts its item out.
    /// </summary>
    [Fact]
    public void ThePointerAndTheKeysShareOneHighlight()
    {
        var said = new List<string>();
        var first = Item("Copy", said);
        var second = Item("Paste", said);
        var third = Item("Delete", said);
        var menu = new Menu { Items = { first, second, third } };
        var workspace = Opened(menu, new Heard());

        Rest(workspace, second);
        Assert.Same(second, menu.Highlight);

        workspace.Key(MyKeys.Down, false, false);
        Assert.Same(third, menu.Highlight);
        Assert.Equal(GameColors.Line, third.Background);
        Assert.Equal(Color.Transparent, second.Background);

        Rest(workspace, first);
        Assert.Same(first, menu.Highlight);

        workspace.Pointer = new Vector2(float.NegativeInfinity);
        workspace.DrawScreen(new Recording(Area), Area);
        Assert.Null(menu.Highlight);
    }

    /// <summary>A menu taller than the screen scrolls, and the lit item is scrolled into view.</summary>
    [Fact]
    public void TheLitItemIsScrolledIntoView()
    {
        var said = new List<string>();
        var menu = new Menu();
        for (var i = 0; i < 40; i++)
            menu.Items.Add(Item("Word " + i, said));

        var workspace = Opened(menu, new Heard());
        var last = (MenuItem)menu.Items[39];
        Assert.True(last.Bounds.Y > Area.Bottom, "the menu did not run past the screen");

        menu.Highlight = last;
        workspace.DrawScreen(new Recording(Area), Area);

        Assert.True(last.Bounds.Bottom <= menu.Bounds.Bottom, "the lit item is out of view");
    }

    /// <summary>An item the pointer lights stays where it is, so a half shown item under a resting pointer does not scroll the menu.</summary>
    [Fact]
    public void AnItemThePointerLightsDoesNotScroll()
    {
        var said = new List<string>();
        var menu = new Menu();
        for (var i = 0; i < 40; i++)
            menu.Items.Add(Item("Word " + i, said));

        var workspace = Opened(menu, new Heard());
        var cut = menu.Items.Cast<MenuItem>().First(item => item.Bounds.Bottom > menu.Bounds.Bottom);
        var top = cut.Bounds.Y;

        workspace.Pointer = new Vector2(cut.Bounds.Center.X, top + 2f);
        workspace.DrawScreen(new Recording(Area), Area);
        workspace.DrawScreen(new Recording(Area), Area);

        Assert.Same(cut, menu.Highlight);
        Assert.Equal(top, cut.Bounds.Y);
    }

    /// <summary>A menu's looks style the scroll that holds its items.</summary>
    [Fact]
    public void TheMenusLooksStyleItsScroll()
    {
        var thin = new Viewing.ScrollViewStyle { Base = { ScrollBarWidth = 4f } };
        var menu = new Menu { Style = new MenuStyle(GameTheme.Menu) { Base = { ScrollStyle = thin } }, Items = { Item("Copy", new List<string>()) } };
        Opened(menu, new Heard());

        Assert.Same(thin, menu.ScrollStyle);
        Assert.Equal(4f, ((Viewing.ScrollView)menu.Content).ScrollBarWidth);
    }

    /// <summary>Escape closes the menu and nothing more.</summary>
    [Fact]
    public void EscapeClosesTheMenu()
    {
        var menu = new Menu { Items = { Item("Copy", new List<string>()) } };
        var workspace = Opened(menu, new Heard());

        Assert.True(workspace.Key(MyKeys.Escape, false, false));

        Assert.False(menu.IsOpen);
        Assert.False(workspace.Key(MyKeys.Escape, false, false));
    }

    /// <summary>A separator shows only between visible items: never first, last, twice in a row, or beside a hidden item's place.</summary>
    [Fact]
    public void SeparatorsShowOnlyBetweenItems()
    {
        var said = new List<string>();
        var leading = new MenuSeparator();
        var copy = Item("Copy", said);
        var between = new MenuSeparator();
        var twice = new MenuSeparator();
        var paste = Item("Paste", said);
        var trailing = new MenuSeparator();
        var menu = new Menu { Items = { leading, copy, between, twice, paste, trailing } };
        var workspace = Opened(menu, new Heard());

        Assert.Equal(new[] { false, true, false, false }, new[] { leading.IsVisible, between.IsVisible, twice.IsVisible, trailing.IsVisible });

        paste.IsVisible = false;
        workspace.DrawScreen(new Recording(Area), Area);

        Assert.False(between.IsVisible);
    }

    /// <summary>A separator leaves the menu as wide as its items and runs across all of it.</summary>
    [Fact]
    public void ASeparatorDoesNotWidenTheMenu()
    {
        var said = new List<string>();
        var copy = Item("Copy", said);
        var menu = new Menu { Items = { copy } };
        var workspace = Opened(menu, new Heard());
        var wide = menu.Bounds.Width;

        var separator = new MenuSeparator();
        menu.Items.Add(separator);
        menu.Items.Add(Item("Cut", said));
        workspace.DrawScreen(new Recording(Area), Area);

        Assert.Equal(wide, menu.Bounds.Width);
        Assert.Equal(copy.Bounds.Width, separator.Bounds.Width);
    }

    /// <summary>A menu opened at a point has its top left corner there, and opens to the left and above where it would run off.</summary>
    [Fact]
    public void AMenuOpensAtAPointAndStaysOnTheScreen()
    {
        var menu = new Menu { Items = { Item("Copy", new List<string>()) } };
        var workspace = new Workspace();
        workspace.DrawScreen(new Recording(Area), Area);

        menu.OpenAt(workspace.Windows, new Vector2(100f, 120f));
        workspace.DrawScreen(new Recording(Area), Area);
        Assert.Equal(new Vector2(100f, 120f), new Vector2(menu.Bounds.X, menu.Bounds.Y));

        menu.Close();
        menu.OpenAt(workspace.Windows, new Vector2(790f, 590f));
        workspace.DrawScreen(new Recording(Area), Area);
        Assert.Equal(new Vector2(790f, 590f), new Vector2(menu.Bounds.Right, menu.Bounds.Bottom));
    }

    /// <summary>A menu with an anchor refuses to open at a point.</summary>
    [Fact]
    public void AnAnchoredMenuDoesNotOpenAtAPoint()
    {
        var menu = new Menu { Anchor = new Label() };

        Assert.Throws<InvalidOperationException>(() => menu.OpenAt(new Windows.WindowLayer(), Vector2.Zero));
    }

    /// <summary>An item's words sit at its left and its shortcut at its right, inside its padding.</summary>
    [Fact]
    public void TheWordsSitLeftAndTheShortcutRight()
    {
        var item = new MenuItem { Text = "Paste", Shortcut = "Fire", Placement = Layout.FixedSize.Width(400f) };
        var easel = new Easel(new Layout.VStack { Children = { item } }, 600f, 200f, true);

        var words = easel.Written.Single(c => c.Text == "Paste");
        var shortcut = easel.Written.Single(c => c.Text == "Fire");

        Assert.Equal(item.Bounds.X + 12f, words.Position.X / easel.Pixels, 1);
        Assert.Equal(item.Bounds.Right - 12f, (shortcut.Position.X + easel.WidthOf("Fire", 16f)) / easel.Pixels, 1);
    }

    /// <summary>An item with content shows the content in place of words, at its left.</summary>
    [Fact]
    public void AnItemCanHoldContent()
    {
        var mark = new Label { Text = "Template" };
        var item = new MenuItem { Content = mark, Placement = Layout.FixedSize.Width(400f) };
        _ = new Easel(new Layout.VStack { Children = { item } }, 600f, 200f);

        Assert.Equal("", item.Text);
        Assert.Equal(item.Bounds.X + 12f, mark.Bounds.X, 1);
    }

    /// <summary>
    ///     A chosen item shows a mark before its words and no plate of its own, and while one item is chosen every item
    ///     keeps the mark's column, so their words line up.
    /// </summary>
    [Fact]
    public void AChosenItemIsMarkedAndTheWordsLineUp()
    {
        var said = new List<string>();
        var plain = Item("Copy", said);
        var chosen = Item("Copy all", said);
        var menu = new Menu { Items = { plain, chosen } };
        var workspace = Opened(menu, new Heard());
        var unmarked = Words(plain).Bounds.X;

        chosen.IsChosen = true;
        var drawn = new Recording(Area);
        workspace.DrawScreen(drawn, Area);

        Assert.Equal(unmarked + 8f + 12f, Words(plain).Bounds.X);
        Assert.Equal(Words(plain).Bounds.X, Words(chosen).Bounds.X);
        Assert.Equal(Color.Transparent, chosen.Background);
        Assert.Contains(drawn.Fills, fill => fill.Width == 8f && fill.Height == 8f && fill.X == chosen.ContentBounds.X);

        chosen.IsChosen = false;
        workspace.DrawScreen(new Recording(Area), Area);

        Assert.Equal(unmarked, Words(plain).Bounds.X);
    }

    /// <summary>A chosen item that is lit shows the lit plate with its mark.</summary>
    [Fact]
    public void AChosenItemCanBeLit()
    {
        var chosen = Item("Copy all", new List<string>());
        chosen.IsChosen = true;
        var menu = new Menu { Items = { chosen } };
        Opened(menu, new Heard());

        menu.Highlight = chosen;

        Assert.Equal(GameColors.Line, chosen.Background);
        Assert.Equal(GameColors.Accent, chosen.Mark);
    }

    static Element Words(MenuItem item)
    {
        return item.Content;
    }

    static MenuItem Item(string text, List<string> said)
    {
        return new MenuItem { Text = text, Command = new RelayCommand(() => said.Add(text)) };
    }

    static Workspace Opened(Menu menu, Heard heard)
    {
        var workspace = new Workspace { Sounds = heard };
        workspace.DrawScreen(new Recording(Area), Area);
        menu.OpenAt(workspace.Windows, new Vector2(100f, 100f));
        workspace.DrawScreen(new Recording(Area), Area);
        return workspace;
    }

    static void Rest(Workspace workspace, Element element)
    {
        workspace.Pointer = new Vector2(element.Bounds.X + element.Bounds.Width / 2f, element.Bounds.Y + element.Bounds.Height / 2f);
        workspace.DrawScreen(new Recording(Area), Area);
    }

    static void Press(Workspace workspace, Element element)
    {
        var middle = new Vector2(element.Bounds.X + element.Bounds.Width / 2f, element.Bounds.Y + element.Bounds.Height / 2f);
        workspace.PointerDown(middle, PointerButton.Left, 1, false, false);
        workspace.PointerUp(middle, PointerButton.Left);
    }

    sealed class Heard : IControlSounds
    {
        public List<Sound> Played { get; } = new List<Sound>();

        public void Play(Sound sound) => Played.Add(sound);
    }
}
