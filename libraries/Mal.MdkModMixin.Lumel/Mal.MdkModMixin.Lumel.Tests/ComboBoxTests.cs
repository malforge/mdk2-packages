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
///     A combo box: its list narrowed by typing while the field keeps the keyboard, a choice picked by pointer or keys,
///     and what is typed given as the answer on Enter or as the field loses the keyboard.
/// </summary>
public class ComboBoxTests
{
    static readonly RectangleF Area = new RectangleF(0f, 0f, 800f, 600f);

    /// <summary>Typing opens the list with the matching choices alone and nothing lit, and the field keeps the keyboard.</summary>
    [Fact]
    public void TypingNarrowsTheListAndTheFieldKeepsTheKeyboard()
    {
        var scene = new Scene();
        scene.Press(scene.ComboBox);

        scene.Type("min");

        Assert.True(scene.ComboBox.IsOpen);
        Assert.True(scene.ComboBox.Is(ComboBox.Expanded));
        Assert.True(scene.Mining.IsVisible);
        Assert.True(scene.MiningTwo.IsVisible);
        Assert.False(scene.Default.IsVisible);
        Assert.Null(scene.Mining.Menu.Highlight);
        Assert.Same(scene.ComboBox.Field, scene.Workspace.Input.Focused);
        Assert.Equal(0, scene.Commits);
    }

    /// <summary>Typing what no choice matches closes the list.</summary>
    [Fact]
    public void TypingWhatNothingMatchesClosesTheList()
    {
        var scene = new Scene();
        scene.Press(scene.ComboBox);
        scene.Type("min");

        scene.Type("q");

        Assert.False(scene.ComboBox.IsOpen);
    }

    /// <summary>Enter with no choice lit gives what is typed, once, and closes the list.</summary>
    [Fact]
    public void EnterGivesWhatIsTyped()
    {
        var scene = new Scene();
        scene.Press(scene.ComboBox);
        scene.Type("Min");

        scene.Key(MyKeys.Enter);

        Assert.False(scene.ComboBox.IsOpen);
        Assert.Equal("Min", scene.ComboBox.Text);
        Assert.Equal(1, scene.Commits);
    }

    /// <summary>Pressing a choice gives its words alone, closes the list, and leaves the keyboard in the field.</summary>
    [Fact]
    public void PressingAChoiceGivesIt()
    {
        var scene = new Scene();
        scene.Press(scene.ComboBox);
        scene.Type("Min");

        scene.Press(scene.MiningTwo);

        Assert.False(scene.ComboBox.IsOpen);
        Assert.Equal("Mining 2", scene.ComboBox.Text);
        Assert.Equal(1, scene.Commits);
        Assert.Same(scene.ComboBox.Field, scene.Workspace.Input.Focused);
    }

    /// <summary>Down lights the first match, and Enter picks it.</summary>
    [Fact]
    public void DownLightsAChoiceAndEnterPicksIt()
    {
        var scene = new Scene();
        scene.Press(scene.ComboBox);
        scene.Type("Min");

        scene.Key(MyKeys.Down);
        Assert.Same(scene.Mining, scene.Mining.Menu.Highlight);

        scene.Key(MyKeys.Enter);

        Assert.Equal("Mining", scene.ComboBox.Text);
        Assert.Equal(1, scene.Commits);
    }

    /// <summary>Escape closes an open list and is taken, leaving what is typed.</summary>
    [Fact]
    public void EscapeClosesTheList()
    {
        var scene = new Scene();
        scene.Press(scene.ComboBox);
        scene.Type("Min");

        Assert.True(scene.Key(MyKeys.Escape));

        Assert.False(scene.ComboBox.IsOpen);
        Assert.Equal("Min", scene.ComboBox.Text);
        Assert.Same(scene.ComboBox.Field, scene.Workspace.Input.Focused);
    }

    /// <summary>The field losing the keyboard gives what is typed, and nothing where it is the answer already.</summary>
    [Fact]
    public void LosingTheKeyboardGivesWhatIsTyped()
    {
        var scene = new Scene();
        scene.ComboBox.Text = "Default";
        scene.Press(scene.ComboBox);
        scene.Workspace.Input.Focus(null);
        Assert.Equal(0, scene.Commits);

        scene.Press(scene.ComboBox);
        scene.Key(MyKeys.End);
        scene.Type(" crew");
        scene.Workspace.Input.Focus(null);

        Assert.Equal("Default crew", scene.ComboBox.Text);
        Assert.Equal(1, scene.Commits);
    }

    /// <summary>Pressing the arrow opens every choice, with the one in the field marked and lit, and the keyboard in the field.</summary>
    [Fact]
    public void PressingTheArrowOpensEveryChoice()
    {
        var scene = new Scene();
        scene.ComboBox.Text = "mining";

        var box = scene.ComboBox.ContentBounds;
        scene.PressAt(new Vector2(box.X + box.Width - scene.ComboBox.ArrowSize / 2f, box.Y + box.Height / 2f));

        Assert.True(scene.ComboBox.IsOpen);
        Assert.True(scene.Default.IsVisible);
        Assert.True(scene.Mining.IsChosen);
        Assert.Same(scene.Mining, scene.Mining.Menu.Highlight);
        Assert.Same(scene.ComboBox.Field, scene.Workspace.Input.Focused);
        Assert.Equal(0, scene.Commits);
    }

    /// <summary>A combo box of three group names in a window on a workspace, counting the answers given.</summary>
    sealed class Scene
    {
        public readonly ComboBox ComboBox;
        public readonly ComboBoxItem Default;
        public readonly ComboBoxItem Mining;
        public readonly ComboBoxItem MiningTwo;
        public readonly Workspace Workspace = new Workspace();

        public Scene()
        {
            ComboBox = new ComboBox
            {
                Placement = FixedSize.Width(300f),
                Items =
                {
                    new ComboBoxItem { Text = "Default" }.As(out Default),
                    new ComboBoxItem { Text = "Mining" }.As(out Mining),
                    new ComboBoxItem { Text = "Mining 2" }.As(out MiningTwo)
                }
            };
            ComboBox.Committed += () => Commits++;
            Workspace.Windows.Open(new Window { Placement = FixedSize.Width(400f), Content = new HStack { Children = { ComboBox } } });
            Draw();
        }

        public int Commits { get; private set; }

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

        public bool Key(MyKeys key)
        {
            var taken = Workspace.Key(key, false, false);
            Draw();
            return taken;
        }

        public void Press(Element element)
        {
            PressAt(new Vector2(element.Bounds.X + element.Bounds.Width / 4f, element.Bounds.Y + element.Bounds.Height / 2f));
        }

        public void PressAt(Vector2 point)
        {
            Workspace.PointerDown(point, PointerButton.Left, 1, false, false);
            Workspace.PointerUp(point, PointerButton.Left);
            Draw();
        }
    }
}
