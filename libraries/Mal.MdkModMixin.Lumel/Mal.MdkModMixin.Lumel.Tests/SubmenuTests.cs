using Lumel.Input;
using Lumel.Menus;
using Lumel.Screens;
using Lumel.Text;
using VRage.Input;
using VRageMath;
using Xunit;

namespace Lumel.Tests;

/// <summary>
///     A submenu: opened beside its item by a press, a key or the pointer resting there, closed by the keys or the
///     pointer resting elsewhere, and closed with every menu it opened from as one of its items is pressed.
/// </summary>
public class SubmenuTests
{
    static readonly RectangleF Area = new RectangleF(0f, 0f, 800f, 600f);

    /// <summary>
    ///     Pressing the item opens its submenu at its right, with the first entry level with it, and the item stays lit.
    /// </summary>
    [Fact]
    public void PressingTheItemOpensItsSubmenuBesideIt()
    {
        var scene = new Scene();

        scene.Press(scene.Opener);

        Assert.True(scene.Submenu.IsOpen);
        Assert.True(scene.Menu.IsOpen);
        Assert.Same(scene.Submenu, scene.Menu.ShownSubmenu);
        Assert.Same(scene.Menu, scene.Submenu.ParentMenu);
        Assert.True(scene.Opener.IsHighlighted);
        Assert.True(scene.Submenu.Bounds.X >= scene.Opener.Bounds.Right - 4f);
        Assert.Equal(Middle(scene.Opener).Y, Middle(scene.Display).Y, 1);
    }

    /// <summary>
    ///     Pressing an item of the submenu tells the submenu, then the menu it opened from, and closes both before the
    ///     item's command runs, each once.
    /// </summary>
    [Fact]
    public void PressingAnItemClosesTheWholeChain()
    {
        var scene = new Scene();
        var heard = new List<string>();
        scene.Menu.ItemPressed += item => heard.Add("menu " + item.Text);
        scene.Submenu.ItemPressed += item => heard.Add("submenu " + item.Text);
        scene.Menu.Closed += _ => heard.Add("menu closed");
        scene.Submenu.Closed += _ => heard.Add("submenu closed");
        scene.Press(scene.Opener);

        scene.Press(scene.Keyboard);

        Assert.Equal(new[] { "submenu Keyboard", "menu Keyboard", "submenu closed", "menu closed", "ran Keyboard" }, heard.Concat(scene.Said).ToArray());
        Assert.False(scene.Menu.IsOpen);
    }

    /// <summary>Pressing the item that opens a submenu tells its menu nothing and leaves it open.</summary>
    [Fact]
    public void TheItemThatOpensASubmenuIsNoChoice()
    {
        var scene = new Scene();
        var pressed = 0;
        scene.Menu.ItemPressed += _ => pressed++;

        scene.Press(scene.Opener);

        Assert.Equal(0, pressed);
        Assert.True(scene.Menu.IsOpen);
    }

    /// <summary>The pointer resting on the item opens its submenu once the look's delay has passed, and not before.</summary>
    [Fact]
    public void ThePointerRestingOpensTheSubmenuAfterTheDelay()
    {
        var scene = new Scene();

        scene.Rest(scene.Opener, 100);
        scene.Frame(110);
        Assert.False(scene.Submenu.IsOpen);

        scene.Frame(124);
        Assert.True(scene.Submenu.IsOpen);
    }

    /// <summary>The menu closing while the pointer rests on the item cancels the wait, so the submenu never opens.</summary>
    [Fact]
    public void AMenuThatClosesStopsWaiting()
    {
        var scene = new Scene();
        scene.Rest(scene.Opener, 100);

        scene.Menu.Close();
        scene.Menu.OpenAt(scene.Workspace.Windows, new Vector2(100f, 100f));
        scene.Workspace.Pointer = new Vector2(float.NegativeInfinity);
        scene.Frame(200);

        Assert.False(scene.Submenu.IsOpen);
    }

    /// <summary>
    ///     The pointer crossing another item on its way into the open submenu leaves it open, and lit where it came from;
    ///     resting on another item closes it.
    /// </summary>
    [Fact]
    public void ThePointerCanCrossAnotherItemIntoTheSubmenu()
    {
        var scene = new Scene();
        scene.Press(scene.Opener);

        scene.Rest(scene.Paste, 200);
        scene.Rest(scene.Display, 210);
        scene.Frame(300);

        Assert.True(scene.Submenu.IsOpen);
        Assert.True(scene.Opener.IsHighlighted);
        Assert.False(scene.Paste.IsHighlighted);

        scene.Rest(scene.Paste, 400);
        scene.Frame(424);

        Assert.False(scene.Submenu.IsOpen);
        Assert.True(scene.Paste.IsHighlighted);
        Assert.Same(scene.Menu, scene.Workspace.Input.Focused);
    }

    /// <summary>
    ///     Right opens the submenu with its first entry lit and the keyboard in it; Left closes it and gives the keyboard
    ///     back, with its item still lit.
    /// </summary>
    [Fact]
    public void RightGoesInAndLeftComesBack()
    {
        var scene = new Scene();
        scene.Menu.Highlight = scene.Opener;

        Assert.True(scene.Workspace.Key(MyKeys.Right, false, false));
        Assert.True(scene.Submenu.IsOpen);
        Assert.Same(scene.Display, scene.Submenu.Highlight);
        Assert.Same(scene.Submenu, scene.Workspace.Input.Focused);

        scene.Workspace.Key(MyKeys.Down, false, false);
        Assert.Same(scene.Keyboard, scene.Submenu.Highlight);
        Assert.Same(scene.Opener, scene.Menu.Highlight);

        Assert.True(scene.Workspace.Key(MyKeys.Left, false, false));
        Assert.False(scene.Submenu.IsOpen);
        Assert.Same(scene.Menu, scene.Workspace.Input.Focused);
        Assert.Same(scene.Opener, scene.Menu.Highlight);
    }

    /// <summary>
    ///     Enter on the item opens the submenu as Right does; Escape in the submenu closes only it, and Escape again closes
    ///     the menu.
    /// </summary>
    [Fact]
    public void EscapeStepsOutOneMenuAtATime()
    {
        var scene = new Scene();
        scene.Menu.Highlight = scene.Opener;

        scene.Workspace.Key(MyKeys.Enter, false, false);
        Assert.Same(scene.Display, scene.Submenu.Highlight);

        scene.Workspace.Key(MyKeys.Escape, false, false);
        Assert.False(scene.Submenu.IsOpen);
        Assert.True(scene.Menu.IsOpen);

        scene.Workspace.Key(MyKeys.Escape, false, false);
        Assert.False(scene.Menu.IsOpen);
    }

    /// <summary>Enter on an item of the submenu presses it, closing every menu.</summary>
    [Fact]
    public void EnterInTheSubmenuPressesItsItem()
    {
        var scene = new Scene();
        scene.Menu.Highlight = scene.Opener;
        scene.Workspace.Key(MyKeys.Right, false, false);

        scene.Workspace.Key(MyKeys.Enter, false, false);

        Assert.Equal(new[] { "ran Large Display" }, scene.Said);
        Assert.False(scene.Menu.IsOpen);
        Assert.False(scene.Submenu.IsOpen);
    }

    /// <summary>Right on an item of the submenu that opens nothing reaches none of the menus beneath.</summary>
    [Fact]
    public void RightInTheSubmenuStaysThere()
    {
        var scene = new Scene();
        scene.Menu.Highlight = scene.Opener;
        scene.Workspace.Key(MyKeys.Right, false, false);

        Assert.True(scene.Workspace.Key(MyKeys.Right, false, false));

        Assert.Same(scene.Display, scene.Submenu.Highlight);
        Assert.Same(scene.Submenu, scene.Workspace.Input.Focused);
    }

    /// <summary>A press on another item of the menu closes the submenu and presses that item.</summary>
    [Fact]
    public void APressOnTheMenuBeneathGoesOn()
    {
        var scene = new Scene();
        scene.Press(scene.Opener);

        scene.Press(scene.Paste);

        Assert.Equal(new[] { "ran Paste" }, scene.Said);
        Assert.False(scene.Submenu.IsOpen);
        Assert.False(scene.Menu.IsOpen);
    }

    /// <summary>A press outside every menu closes them all.</summary>
    [Fact]
    public void APressOutsideClosesEveryMenu()
    {
        var scene = new Scene();
        scene.Press(scene.Opener);

        scene.Workspace.PointerDown(new Vector2(790f, 590f), PointerButton.Left, 1, false, false);

        Assert.False(scene.Submenu.IsOpen);
        Assert.False(scene.Menu.IsOpen);
    }

    /// <summary>Closing the menu closes the submenu open beside it.</summary>
    [Fact]
    public void ClosingTheMenuClosesItsSubmenu()
    {
        var scene = new Scene();
        scene.Press(scene.Opener);

        scene.Menu.Close();

        Assert.False(scene.Submenu.IsOpen);
    }

    /// <summary>
    ///     While one item opens a submenu, every item keeps room for its arrow at the right, so the shortcuts line up, and
    ///     the arrow is drawn on that item alone.
    /// </summary>
    [Fact]
    public void EveryItemKeepsRoomForTheArrow()
    {
        var scene = new Scene();
        scene.Paste.Shortcut = "Ctrl+V";
        var drawn = new Recording(Area);
        scene.Workspace.DrawScreen(drawn, Area);

        Assert.True(scene.Menu.HasSubmenus);
        Assert.Equal(8f + 16f, scene.Paste.ContentBounds.Right - Shortcut(scene.Paste).Bounds.Right, 1);
        Assert.Equal(2, drawn.Fills.Count(fill => fill.Height == 2f && fill.Y > scene.Opener.Bounds.Y && fill.Bottom < scene.Opener.Bounds.Bottom));
    }

    /// <summary>An item given a submenu while it has a command of its own throws, since pressing it opens the submenu.</summary>
    [Fact]
    public void AnItemWithACommandTakesNoSubmenu()
    {
        var item = new MenuItem { Text = "Copy", Command = new RelayCommand(() => { }) };

        Assert.Throws<InvalidOperationException>(() => item.Submenu = new Menu());
    }

    /// <summary>An item whose command is replaced after it was given a submenu throws as it is pressed.</summary>
    [Fact]
    public void AnItemWithASubmenuKeepsItsCommand()
    {
        var scene = new Scene();
        scene.Opener.Command = new RelayCommand(() => { });

        Assert.Throws<InvalidOperationException>(() => scene.Press(scene.Opener));
    }

    static Vector2 Middle(Element element)
    {
        return new Vector2(element.Bounds.X + element.Bounds.Width / 2f, element.Bounds.Y + element.Bounds.Height / 2f);
    }

    static Element Shortcut(MenuItem item)
    {
        for (var i = 0; i < item.VisualChildCount; i++)
        {
            var child = item.GetVisualChild(i);
            if (child is Label label && label.Text == item.Shortcut)
                return label;
        }

        throw new InvalidOperationException("The item shows no shortcut.");
    }

    /// <summary>A menu of Paste and Copy screen, whose submenu lists two screens, open at a point on a workspace.</summary>
    sealed class Scene
    {
        public Scene()
        {
            Submenu = new Menu { Items = { Item("Large Display").As(out Display), Item("Keyboard").As(out Keyboard) } };
            Menu = new Menu
            {
                Items =
                {
                    Item("Paste").As(out Paste),
                    new MenuItem { Text = "Copy screen", Submenu = Submenu }.As(out Opener)
                }
            };

            Workspace = new Workspace();
            Frame(0);
            Menu.OpenAt(Workspace.Windows, new Vector2(100f, 100f));
            Frame(0);
        }

        public readonly Workspace Workspace;
        public readonly Menu Menu;
        public readonly Menu Submenu;
        public readonly MenuItem Paste;
        public readonly MenuItem Opener;
        public readonly MenuItem Display;
        public readonly MenuItem Keyboard;
        public readonly List<string> Said = new List<string>();

        public void Frame(int now)
        {
            Workspace.DrawScreen(new Recording(Area) { Now = now }, Area);
        }

        public void Rest(Element element, int now)
        {
            Workspace.Pointer = Middle(element);
            Frame(now);
        }

        public void Press(Element element)
        {
            var middle = Middle(element);
            Workspace.PointerDown(middle, PointerButton.Left, 1, false, false);
            Workspace.PointerUp(middle, PointerButton.Left);
            Frame(0);
        }

        MenuItem Item(string text)
        {
            return new MenuItem { Text = text, Command = new RelayCommand(() => Said.Add("ran " + text)) };
        }
    }
}
