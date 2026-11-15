using Lumel.Controls;
using Lumel.Input;
using Lumel.Layout;
using Lumel.Menus;
using Lumel.Screens;
using Lumel.Styling;
using Lumel.Windows;
using VRage.Input;
using VRageMath;
using Xunit;

namespace Lumel.Tests;

/// <summary>
///     A drop-down: its list opened and chosen from by pointer and keys, its keys while closed, its width, and a choice
///     only the application makes.
/// </summary>
public class DropDownTests
{
    static readonly RectangleF Area = new RectangleF(0f, 0f, 800f, 600f);

    /// <summary>Pressing the box opens its list under it, at least as wide, with the chosen item marked and lit.</summary>
    [Fact]
    public void PressingTheBoxOpensItsListUnderIt()
    {
        var scene = new Scene();
        scene.DropDown.Chosen = scene.Normal;

        scene.Press(scene.DropDown);

        Assert.True(scene.DropDown.IsOpen);
        Assert.True(scene.DropDown.Is(DropDown.Expanded));
        var list = scene.Normal.Menu;
        Assert.True(list.Bounds.Y >= scene.DropDown.Bounds.Y + scene.DropDown.Bounds.Height);
        Assert.True(list.Bounds.Width >= scene.DropDown.Bounds.Width);
        Assert.True(scene.Normal.IsChosen);
        Assert.False(scene.Easy.IsChosen);
        Assert.Same(scene.Normal, list.Highlight);
    }

    /// <summary>Pressing a choice in the list chooses it, closes the list, shows its words and tells the application.</summary>
    [Fact]
    public void PressingAChoiceChoosesIt()
    {
        var scene = new Scene();
        scene.DropDown.Chosen = scene.Easy;
        scene.Press(scene.DropDown);

        scene.Press(scene.Hard);

        Assert.False(scene.DropDown.IsOpen);
        Assert.False(scene.DropDown.Is(DropDown.Expanded));
        Assert.Same(scene.Hard, scene.DropDown.Chosen);
        Assert.Equal(1, scene.Changes);
        Assert.Equal("Hard", Shown(scene.DropDown));
    }

    /// <summary>The list opens with its search box above the choices, holding the keyboard, and the box keeps showing the choice.</summary>
    [Fact]
    public void AListOpensWithItsSearchBox()
    {
        var scene = new Scene();
        scene.DropDown.IsSearchable = true;
        scene.DropDown.SearchHint = "Search";
        scene.DropDown.Chosen = scene.Hard;

        scene.Press(scene.DropDown);
        scene.Type("e");

        var search = Assert.IsType<SearchBox>(scene.Hard.Menu.Header);
        Assert.Same(search.Keyboard, scene.Workspace.Input.Focused);
        Assert.Equal("Search", search.Hint);
        Assert.True(search.Bounds.Y + search.Bounds.Height <= scene.Easy.Bounds.Y);
        Assert.Equal("Hard", Shown(scene.DropDown));
    }

    /// <summary>Typing leaves the choices with every word, in the order typed and any case, and lights the first.</summary>
    [Fact]
    public void TypingNarrowsASearchableList()
    {
        var scene = new Scene();
        scene.DropDown.IsSearchable = true;
        scene.Press(scene.DropDown);

        scene.Type("M");

        Assert.False(scene.Easy.IsVisible);
        Assert.True(scene.Normal.IsVisible);
        Assert.False(scene.Hard.IsVisible);
        Assert.True(scene.Custom.IsVisible);
        Assert.Same(scene.Normal, scene.Normal.Menu.Highlight);
        Assert.Equal("M", scene.DropDown.Search);

        scene.Workspace.Text(' ');
        scene.Type("cu");
        Assert.False(scene.Normal.IsVisible);
        Assert.False(scene.Custom.IsVisible);
    }

    /// <summary>The arrows move among the choices found while the search box holds the keyboard.</summary>
    [Fact]
    public void TheArrowsMoveAmongWhatTheSearchFound()
    {
        var scene = new Scene();
        scene.DropDown.IsSearchable = true;
        scene.Press(scene.DropDown);
        scene.Type("m");

        scene.Workspace.Key(MyKeys.Down, false, false);

        Assert.Same(scene.Custom, scene.Custom.Menu.Highlight);
    }

    /// <summary>The application is told of each change of the search, and its callback decides what stays.</summary>
    [Fact]
    public void AHostDecidesWhatIsFound()
    {
        var scene = new Scene();
        var told = new List<string>();
        scene.DropDown.IsSearchable = true;
        scene.DropDown.SearchChanged += () => told.Add(scene.DropDown.Search);
        scene.DropDown.FnIsFound = (item, search) => item == scene.Hard && search == "zz";
        scene.Press(scene.DropDown);

        scene.Type("zz");

        Assert.Equal(new[] { "z", "zz" }, told);
        Assert.True(scene.Hard.IsVisible);
        Assert.False(scene.Easy.IsVisible);
    }

    /// <summary>A handler of the search can change the choices, and what it adds is searched as well.</summary>
    [Fact]
    public void AHandlerCanChangeTheChoices()
    {
        var scene = new Scene();
        var added = new DropDownItem { Text = "Harder" };
        scene.DropDown.IsSearchable = true;
        scene.DropDown.SearchChanged += delegate
        {
            scene.DropDown.Items.Remove(scene.Custom);
            if (added.Parent == null)
                scene.DropDown.Items.Add(added);
        };
        scene.Press(scene.DropDown);

        scene.Type("harder");

        Assert.True(added.IsVisible);
        Assert.False(scene.Hard.IsVisible);
        Assert.Null(scene.Custom.Parent);
        Assert.Same(added, added.Menu.Highlight);
    }

    /// <summary>The words at a choice's right take no part in the search.</summary>
    [Fact]
    public void AChoicesShortcutIsNotSearched()
    {
        var scene = new Scene();
        scene.Normal.Shortcut = "12";
        scene.DropDown.IsSearchable = true;
        scene.Press(scene.DropDown);

        scene.Type("12");

        Assert.False(scene.Normal.IsVisible);
    }

    /// <summary>The cross empties the search box and brings every choice back, leaving the keyboard in the box.</summary>
    [Fact]
    public void TheCrossEmptiesTheSearch()
    {
        var scene = new Scene();
        scene.DropDown.IsSearchable = true;
        scene.Press(scene.DropDown);
        scene.Type("ha");
        var search = (SearchBox)scene.Hard.Menu.Header;
        var cross = search.GetVisualChild(1);
        Assert.True(cross.IsVisible);

        scene.Press(cross);

        Assert.Equal("", search.Text);
        Assert.False(cross.IsVisible);
        Assert.True(scene.Easy.IsVisible);
        Assert.True(scene.DropDown.IsOpen);
        Assert.Same(search.Keyboard, scene.Workspace.Input.Focused);
    }

    /// <summary>A long list scrolls below its search box, which stays on the screen.</summary>
    [Fact]
    public void ALongListKeepsItsSearchBoxInView()
    {
        var scene = new Scene();
        scene.DropDown.IsSearchable = true;
        for (var i = 0; i < 60; i++)
            scene.DropDown.Items.Add(new DropDownItem { Text = "Row " + i });

        scene.Press(scene.DropDown);

        var list = scene.Hard.Menu;
        var rows = list.Items[0].Parent!.Parent!;
        Assert.True(list.Bounds.Y + list.Bounds.Height <= Area.Height);
        Assert.True(list.Header!.Bounds.Y >= list.Bounds.Y);
        Assert.True(rows.Bounds.Y + rows.Bounds.Height <= list.Bounds.Y + list.Bounds.Height);
    }

    /// <summary>A list as short as its choices stays that short under its search box, and shrinks with them.</summary>
    [Fact]
    public void TheListIsAsTallAsItsChoices()
    {
        var scene = new Scene();
        scene.DropDown.IsSearchable = true;
        scene.Press(scene.DropDown);
        var list = scene.Hard.Menu;
        var tall = list.Bounds.Height;
        Assert.True(tall < Area.Height / 2f, $"the list stands {tall} tall");

        scene.Type("har");

        Assert.True(list.Bounds.Height < tall, $"the list went from {tall} to {list.Bounds.Height}");
    }

    /// <summary>The search box takes no more width than the list: the list stays as wide as the box it hangs from.</summary>
    [Fact]
    public void TheSearchBoxLeavesTheListItsWidth()
    {
        var plain = new Scene();
        plain.Press(plain.DropDown);
        var searching = new Scene();
        searching.DropDown.IsSearchable = true;
        searching.Press(searching.DropDown);

        Assert.Equal(plain.Hard.Menu.Bounds.Width, searching.Hard.Menu.Bounds.Width);
        Assert.Equal(searching.Hard.Menu.Header!.Bounds.Width, searching.Hard.Bounds.Width, 1);
    }

    /// <summary>A drop-down's style names the style of its search box, and of the cross with it.</summary>
    [Fact]
    public void TheStyleReachesTheSearchBox()
    {
        var scene = new Scene();
        var searching = new SearchBoxStyle { Base = { ClearSymbol = "z", ClearSize = 20f } };
        scene.DropDown.Style = new DropDownStyle { Base = { SearchStyle = searching } };
        scene.DropDown.IsSearchable = true;

        scene.Press(scene.DropDown);

        var search = (SearchBox)scene.Hard.Menu.Header!;
        Assert.Same(searching, search.Style);
    }

    /// <summary>The header goes as it is taken away, and the menu lists its items as it did.</summary>
    [Fact]
    public void TheSearchBoxCanBeTakenAway()
    {
        var scene = new Scene();
        scene.DropDown.IsSearchable = true;
        scene.DropDown.IsSearchable = false;

        scene.Press(scene.DropDown);

        Assert.Null(scene.Easy.Menu.Header);
        Assert.Same(scene.Easy.Menu, scene.Workspace.Input.Focused);
    }

    /// <summary>Space is a letter of the search and presses nothing, and Enter chooses the lit choice and ends the search.</summary>
    [Fact]
    public void EnterChoosesWhatTheSearchFound()
    {
        var scene = new Scene();
        scene.DropDown.IsSearchable = true;
        scene.Press(scene.DropDown);

        scene.Workspace.Key(MyKeys.Space, false, false);
        Assert.True(scene.DropDown.IsOpen);

        scene.Type("har");
        scene.Workspace.Key(MyKeys.Enter, false, false);
        scene.Draw();

        Assert.False(scene.DropDown.IsOpen);
        Assert.Same(scene.Hard, scene.DropDown.Chosen);
        Assert.True(scene.Easy.IsVisible);
        Assert.Equal("", scene.DropDown.Search);
        Assert.Equal("Hard", Shown(scene.DropDown));
    }

    /// <summary>Backspace takes back a letter, and the last one brings every choice back.</summary>
    [Fact]
    public void BackspaceWidensTheSearch()
    {
        var scene = new Scene();
        scene.DropDown.IsSearchable = true;
        scene.Press(scene.DropDown);
        scene.Type("ha");
        Assert.False(scene.Easy.IsVisible);

        scene.Workspace.Key(MyKeys.Back, false, false);
        Assert.False(scene.Easy.IsVisible);
        scene.Workspace.Key(MyKeys.Back, false, false);

        Assert.True(scene.Easy.IsVisible);
        Assert.True(scene.DropDown.IsOpen);
    }

    /// <summary>Escape closes the list and ends the search, and a choice the application hid stays hidden.</summary>
    [Fact]
    public void EscapeEndsTheSearch()
    {
        var scene = new Scene();
        scene.DropDown.IsSearchable = true;
        scene.DropDown.Hint = "Pick";
        scene.Hard.IsVisible = false;
        scene.Press(scene.DropDown);
        scene.Type("no");

        scene.Workspace.Key(MyKeys.Escape, false, false);
        scene.Draw();

        Assert.False(scene.DropDown.IsOpen);
        Assert.True(scene.Easy.IsVisible);
        Assert.False(scene.Hard.IsVisible);
        Assert.Equal("Pick", Shown(scene.DropDown));
    }

    /// <summary>A search that finds nothing lights nothing, and Enter chooses nothing.</summary>
    [Fact]
    public void ASearchThatFindsNothingChoosesNothing()
    {
        var scene = new Scene();
        scene.DropDown.IsSearchable = true;
        scene.DropDown.Chosen = scene.Easy;
        scene.Press(scene.DropDown);

        scene.Type("zz");
        scene.Workspace.Key(MyKeys.Enter, false, false);

        Assert.Same(scene.Easy, scene.DropDown.Chosen);
        Assert.Equal(0, scene.Changes);
    }

    /// <summary>A list that is not searchable has no search box and ignores typing.</summary>
    [Fact]
    public void TypingLeavesAPlainListAlone()
    {
        var scene = new Scene();
        scene.Press(scene.DropDown);

        scene.Type("ha");

        Assert.True(scene.Easy.IsVisible);
    }

    /// <summary>Setting the choice in code shows it and tells nobody.</summary>
    [Fact]
    public void SettingTheChoiceTellsNobody()
    {
        var scene = new Scene();

        scene.DropDown.Chosen = scene.Hard;

        Assert.Equal(0, scene.Changes);
        Assert.Equal("Hard", Shown(scene.DropDown));
    }

    /// <summary>Choosing the item already chosen tells nobody.</summary>
    [Fact]
    public void ChoosingTheSameItemTellsNobody()
    {
        var scene = new Scene();
        scene.DropDown.Chosen = scene.Easy;
        scene.Press(scene.DropDown);

        scene.Press(scene.Easy);

        Assert.False(scene.DropDown.IsOpen);
        Assert.Equal(0, scene.Changes);
    }

    /// <summary>
    ///     A choice only the application makes, hidden from the list, still shows in the box, and the keys step from it
    ///     to the choices the player can make.
    /// </summary>
    [Fact]
    public void AHiddenChoiceShowsInTheBoxOnly()
    {
        var scene = new Scene();
        scene.Custom.IsVisible = false;
        scene.DropDown.Chosen = scene.Custom;
        scene.Draw();

        Assert.Equal("Custom", Shown(scene.DropDown));

        scene.Press(scene.DropDown);
        Assert.Null(scene.Normal.Menu.Highlight);

        scene.Workspace.Key(MyKeys.Escape, false, false);
        scene.Workspace.Key(MyKeys.Left, false, false);
        Assert.Same(scene.Hard, scene.DropDown.Chosen);
    }

    /// <summary>A disabled choice shows in the list and cannot be chosen there or with the keys, but the application can choose it.</summary>
    [Fact]
    public void ADisabledChoiceIsChosenOnlyByTheApplication()
    {
        var scene = new Scene();
        scene.Custom.IsEnabled = false;
        scene.DropDown.Chosen = scene.Hard;
        scene.Press(scene.DropDown);

        scene.Press(scene.Custom);
        Assert.True(scene.DropDown.IsOpen);

        scene.Workspace.Key(MyKeys.Escape, false, false);
        scene.Workspace.Key(MyKeys.Right, false, false);
        Assert.Same(scene.Hard, scene.DropDown.Chosen);

        scene.DropDown.Chosen = scene.Custom;
        Assert.Equal("Custom", Shown(scene.DropDown));
    }

    /// <summary>Left and Right step through the choices while the list is closed, stopping at the ends, and tell the application.</summary>
    [Fact]
    public void LeftAndRightStepThroughTheChoices()
    {
        var scene = new Scene();
        scene.Workspace.Input.Focus(scene.DropDown);

        scene.Workspace.Key(MyKeys.Right, false, false);
        Assert.Same(scene.Easy, scene.DropDown.Chosen);

        scene.Workspace.Key(MyKeys.Right, false, false);
        scene.Workspace.Key(MyKeys.Right, false, false);
        scene.Workspace.Key(MyKeys.Right, false, false);
        scene.Workspace.Key(MyKeys.Right, false, false);
        Assert.Same(scene.Custom, scene.DropDown.Chosen);

        scene.Workspace.Key(MyKeys.Left, false, false);
        Assert.Same(scene.Hard, scene.DropDown.Chosen);
        Assert.Equal(5, scene.Changes);
        Assert.False(scene.DropDown.IsOpen);
    }

    /// <summary>Up and Down step through the choices while the list is closed, as Left and Right do.</summary>
    [Fact]
    public void UpAndDownStepThroughTheChoices()
    {
        var scene = new Scene();
        scene.Workspace.Input.Focus(scene.DropDown);

        scene.Workspace.Key(MyKeys.Down, false, false);
        scene.Workspace.Key(MyKeys.Down, false, false);
        Assert.Same(scene.Normal, scene.DropDown.Chosen);

        scene.Workspace.Key(MyKeys.Up, false, false);
        Assert.Same(scene.Easy, scene.DropDown.Chosen);
        Assert.Equal(3, scene.Changes);
        Assert.False(scene.DropDown.IsOpen);
    }

    /// <summary>While the list is open, Down moves through the list and chooses nothing.</summary>
    [Fact]
    public void DownInTheOpenListChoosesNothing()
    {
        var scene = new Scene();
        scene.DropDown.Chosen = scene.Easy;
        scene.Press(scene.DropDown);

        scene.Workspace.Key(MyKeys.Down, false, false);

        Assert.True(scene.DropDown.IsOpen);
        Assert.Same(scene.Easy, scene.DropDown.Chosen);
        Assert.Equal(0, scene.Changes);
    }

    /// <summary>A typed letter chooses the next choice starting with it, going round.</summary>
    [Fact]
    public void ALetterChoosesTheNextChoiceStartingWithIt()
    {
        var scene = new Scene();
        scene.DropDown.Items.Add(new DropDownItem { Text = "Harder" }.As(out var harder));
        scene.DropDown.Chosen = scene.Normal;
        scene.Workspace.Input.Focus(scene.DropDown);

        scene.Workspace.Text('h');
        Assert.Same(scene.Hard, scene.DropDown.Chosen);

        scene.Workspace.Text('H');
        Assert.Same(harder, scene.DropDown.Chosen);

        scene.Workspace.Text('h');
        Assert.Same(scene.Hard, scene.DropDown.Chosen);
    }

    /// <summary>Enter opens the list, Down lights the next choice and Enter chooses it.</summary>
    [Fact]
    public void TheKeysChooseFromTheList()
    {
        var scene = new Scene();
        scene.DropDown.Chosen = scene.Easy;
        scene.Workspace.Input.Focus(scene.DropDown);

        scene.Workspace.Key(MyKeys.Enter, false, false);
        scene.Draw();
        Assert.True(scene.DropDown.IsOpen);

        scene.Workspace.Key(MyKeys.Down, false, false);
        scene.Workspace.Key(MyKeys.Enter, false, false);
        scene.Draw();

        Assert.False(scene.DropDown.IsOpen);
        Assert.Same(scene.Normal, scene.DropDown.Chosen);
        Assert.Equal(1, scene.Changes);
        Assert.True(scene.DropDown.IsFocused, "the keyboard did not come back to the box");
    }

    /// <summary>The box is as wide as its widest choice, so changing the choice moves nothing.</summary>
    [Fact]
    public void ChangingTheChoiceKeepsTheWidth()
    {
        var scene = new Scene();
        scene.DropDown.Chosen = scene.Easy;
        scene.Draw();
        var wide = scene.DropDown.Bounds.Width;

        scene.DropDown.Chosen = scene.Custom;
        scene.Draw();

        Assert.Equal(wide, scene.DropDown.Bounds.Width);
    }

    /// <summary>With nothing chosen the box shows its hint in the hint's color.</summary>
    [Fact]
    public void NothingChosenShowsTheHint()
    {
        var scene = new Scene();
        scene.DropDown.Hint = "Pick one";
        scene.Draw();

        Assert.Equal("Pick one", Shown(scene.DropDown));
        Assert.Equal(GameColors.Faint, Words(scene.DropDown).Color);

        scene.DropDown.Chosen = scene.Easy;
        Assert.Equal(GameColors.Ink, Words(scene.DropDown).Color);
    }

    /// <summary>Only one of its own items can be chosen.</summary>
    [Fact]
    public void AnotherDropDownsItemCannotBeChosen()
    {
        var dropDown = new DropDown();

        Assert.Throws<ArgumentException>(() => dropDown.Chosen = new DropDownItem { Text = "Elsewhere" });
    }

    /// <summary>Another menu item in the list runs its own command and leaves the choice alone.</summary>
    [Fact]
    public void AnotherItemRunsItsOwnCommand()
    {
        var scene = new Scene();
        var ran = false;
        var edit = new MenuItem { Text = "Edit list", Command = new RelayCommand(() => ran = true) };
        scene.DropDown.Items.Add(edit);
        scene.DropDown.Chosen = scene.Easy;
        scene.Press(scene.DropDown);

        scene.Press(edit);

        Assert.True(ran);
        Assert.Same(scene.Easy, scene.DropDown.Chosen);
        Assert.Equal(0, scene.Changes);
    }

    static string Shown(DropDown dropDown)
    {
        return Words(dropDown).Text;
    }

    static Text.Label Words(DropDown dropDown)
    {
        return (Text.Label)dropDown.GetVisualChild(0);
    }

    /// <summary>A drop-down of four difficulties in a window on a workspace, counting the player's changes.</summary>
    sealed class Scene
    {
        public readonly DropDown DropDown;
        public readonly DropDownItem Easy;
        public readonly DropDownItem Normal;
        public readonly DropDownItem Hard;
        public readonly DropDownItem Custom;
        public readonly Workspace Workspace = new Workspace();

        public Scene()
        {
            DropDown = new DropDown
            {
                Items =
                {
                    new DropDownItem { Text = "Easy" }.As(out Easy),
                    new DropDownItem { Text = "Normal" }.As(out Normal),
                    new DropDownItem { Text = "Hard" }.As(out Hard),
                    new MenuSeparator(),
                    new DropDownItem { Text = "Custom" }.As(out Custom)
                }
            };
            DropDown.ChoiceChanged += () => Changes++;
            Workspace.Windows.Open(new Window { Placement = FixedSize.Width(400f), Content = new HStack { Children = { DropDown } } });
            Draw();
        }

        public int Changes { get; private set; }

        public void Draw()
        {
            Workspace.DrawScreen(new Recording(Area), Area);
        }

        public void Type(string words)
        {
            foreach (var c in words)
                Workspace.Text(c);

            Draw();
        }

        public void Press(Element element)
        {
            var middle = new Vector2(element.Bounds.X + element.Bounds.Width / 2f, element.Bounds.Y + element.Bounds.Height / 2f);
            Workspace.PointerDown(middle, PointerButton.Left, 1, false, false);
            Workspace.PointerUp(middle, PointerButton.Left);
            Draw();
        }
    }
}
