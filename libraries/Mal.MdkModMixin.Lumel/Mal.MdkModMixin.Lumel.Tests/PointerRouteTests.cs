using Lumel.Controls;
using Lumel.Input;
using Lumel.Layout;
using Lumel.Screens;
using Lumel.Text;
using Lumel.Viewing;
using Lumel.Windows;
using VRage.Input;
using VRageMath;
using Xunit;

namespace Lumel.Tests;

/// <summary>The wheel bubbling from what is under the pointer, input confined to one element, a press announced before it lands, and the arrows moving the keyboard.</summary>
public class PointerRouteTests
{
    static readonly RectangleF Area = new RectangleF(0f, 0f, 800f, 600f);

    /// <summary>The wheel over what a scroll view holds moves the scroll view.</summary>
    [Fact]
    public void TheWheelOverWhatAScrollViewHoldsScrollsIt()
    {
        var (view, first) = Tall();
        var workspace = Laid(new VStack { Children = { view } });

        workspace.Wheel(Middle(first), 40f);

        Assert.Equal(40f, view.VerticalOffset);
    }

    /// <summary>The wheel is shown to the application first, with what it will reach and how far it turned.</summary>
    [Fact]
    public void TheWheelIsPreviewedBeforeItReachesAnything()
    {
        var (view, first) = Tall();
        var workspace = Laid(new VStack { Children = { view } });
        WheelEventArgs seen = null;
        workspace.Input.PreviewWheel += turned => seen = turned;

        workspace.Wheel(Middle(first), 40f);

        Assert.Same(first, seen.Target);
        Assert.Equal(40f, seen.Amount);
        Assert.Equal(40f, view.VerticalOffset);
    }

    /// <summary>A wheel the application marks handled reaches nothing under the pointer.</summary>
    [Fact]
    public void AWheelMarkedHandledScrollsNothing()
    {
        var (view, first) = Tall();
        var workspace = Laid(new VStack { Children = { view } });
        workspace.Input.PreviewWheel += turned => turned.Handled = true;

        workspace.Wheel(Middle(first), 40f);

        Assert.Equal(0f, view.VerticalOffset);
    }

    /// <summary>The wheel over a pinned tip scrolls what lies under the tip.</summary>
    [Fact]
    public void TheWheelOverAPinnedTipScrollsWhatIsUnderIt()
    {
        var lines = new VStack();
        for (var i = 0; i < 100; i++)
            lines.Children.Add(new Label { Text = "Line" });

        var view = new ScrollView { Placement = FixedSize.Size(800f, 600f), Content = lines };
        var workspace = Laid(view);
        var line = lines.Children[0];
        var tip = new CodexTip(new Codex(), new Tip("A few words about it.")) { Placement = FixedSize.Width(100f), Base = { ShowDelay = 0 } };
        line.Tip = tip;
        workspace.Pointer = Middle(line);
        workspace.DrawScreen(new Recording(Area), Area);
        var onTip = Middle(tip);
        workspace.Pointer = onTip;
        workspace.DrawScreen(new Recording(Area), Area);
        Assert.True(tip.Covers(onTip), "the tip does not hold the pointer");

        workspace.Wheel(onTip, 40f);

        Assert.Equal(40f, view.VerticalOffset);
    }

    /// <summary>A wheel nothing inside can take goes on outwards to whatever takes it.</summary>
    [Fact]
    public void AWheelNothingInsideTakesGoesOutwards()
    {
        var words = new Label { Text = "Words" };
        var view = new ScrollView { Placement = FixedSize.Size(200f, 100f), Content = words };
        var outer = new Listener { Children = { view } };
        var workspace = Laid(outer);

        workspace.Wheel(Middle(view), 40f);

        Assert.Equal(new[] { 40f }, outer.Wheels);
    }

    /// <summary>Confined, a press outside reaches nothing and leaves the keyboard where it is; a press inside reaches what it lands on.</summary>
    [Fact]
    public void ConfinedAPressOutsideReachesNothing()
    {
        var pressed = 0;
        var inside = new TextBox { Placement = FixedSize.Width(200f) };
        var outside = new Button { Text = "Go", Command = new RelayCommand(() => pressed++) };
        var workspace = Laid(new VStack { Children = { inside, outside } });
        workspace.Input.Focus(inside);
        workspace.Input.Confined = inside;

        Click(workspace, Middle(outside));

        Assert.Equal(0, pressed);
        Assert.Same(inside, workspace.Input.Focused);
        Assert.Null(workspace.HitTest(Middle(outside)));
        Assert.Same(inside, workspace.HitTest(Middle(inside)));
    }

    /// <summary>Confined, the wheel outside moves nothing.</summary>
    [Fact]
    public void ConfinedTheWheelOutsideMovesNothing()
    {
        var (view, first) = Tall();
        var inside = new TextBox { Placement = FixedSize.Width(200f) };
        var workspace = Laid(new VStack { Children = { inside, view } });
        workspace.Input.Confined = inside;

        workspace.Wheel(Middle(first), 40f);

        Assert.Equal(0f, view.VerticalOffset);
    }

    /// <summary>A press is announced, with what it will reach, before that is told of it.</summary>
    [Fact]
    public void APressIsAnnouncedBeforeItLands()
    {
        var said = "";
        var button = new Button { Text = "Go", Command = new RelayCommand(() => said += "carried") };
        var workspace = Laid(new VStack { Children = { button } });
        workspace.Input.PreviewPointerDown += target => said += target == button ? "pressing " : "elsewhere ";

        Click(workspace, Middle(button));

        Assert.Equal("pressing carried", said);
    }

    /// <summary>The arrows move the keyboard to the nearest box that way, and nowhere past the last one.</summary>
    [Fact]
    public void TheArrowsMoveTheKeyboardToTheNearestBoxThatWay()
    {
        var topLeft = Box();
        var topRight = Box();
        var bottomLeft = Box();
        var workspace = Laid(new VStack
        {
            Base = { Spacing = 20f },
            Children =
            {
                new HStack { Base = { Spacing = 20f }, Children = { topLeft, topRight } },
                new HStack { Children = { bottomLeft } }
            }
        });
        workspace.Input.Focus(topRight);

        workspace.Key(MyKeys.Down, false, false);
        Assert.Same(bottomLeft, workspace.Input.Focused);

        workspace.Key(MyKeys.Up, false, false);
        Assert.Same(topLeft, workspace.Input.Focused);

        Assert.False(workspace.Key(MyKeys.Up, false, false));
        Assert.Same(topLeft, workspace.Input.Focused);
    }

    /// <summary>A text box keeps left and right for its caret.</summary>
    [Fact]
    public void ATextBoxKeepsLeftAndRight()
    {
        var left = Box();
        var right = Box();
        left.Text = "abc";
        var workspace = Laid(new HStack { Base = { Spacing = 20f }, Children = { left, right } });
        workspace.Input.Focus(left);

        workspace.Key(MyKeys.Right, false, false);

        Assert.Same(left, workspace.Input.Focused);
    }

    /// <summary>In a dialog, the arrows move the keyboard only among what the dialog holds, and do not cancel it.</summary>
    [Fact]
    public void InADialogTheArrowsStayInside()
    {
        var canceled = 0;
        var behind = Box();
        var first = Box();
        var second = Box();
        var workspace = Laid(new VStack { Children = { behind } });
        workspace.Windows.Open(new Dialog { CancelCommand = new RelayCommand(() => canceled++), Content = new VStack { Base = { Spacing = 20f }, Children = { first, second } } });
        workspace.DrawScreen(new Recording(Area), Area);
        workspace.Input.Focus(second);

        workspace.Key(MyKeys.Up, false, false);
        Assert.Same(first, workspace.Input.Focused);

        workspace.Key(MyKeys.Up, false, false);
        Assert.Same(first, workspace.Input.Focused);
        Assert.Equal(0, canceled);
    }

    /// <summary>Tab in a dialog just opened, with nothing holding the keyboard, moves it into the dialog.</summary>
    [Fact]
    public void TabInADialogJustOpenedStaysInside()
    {
        var behind = Box();
        var inside = Box();
        var workspace = Laid(new VStack { Children = { behind } });
        workspace.Input.Focus(behind);
        workspace.Windows.Open(new Dialog { Content = new VStack { Children = { inside } } });
        workspace.DrawScreen(new Recording(Area), Area);

        workspace.Key(MyKeys.Tab, false, false);

        Assert.Same(inside, workspace.Input.Focused);
    }

    /// <summary>
    ///     Creates a scroll view a hundred units high holding forty lines, and returns it with the first line.
    /// </summary>
    static (ScrollView, Label) Tall()
    {
        var lines = new VStack();
        for (var i = 0; i < 40; i++)
            lines.Children.Add(new Label { Text = "Line" });

        return (new ScrollView { Placement = FixedSize.Size(200f, 100f), Content = lines }, (Label)lines.Children[0]);
    }

    static TextBox Box()
    {
        return new TextBox { Placement = FixedSize.Width(120f) };
    }

    static Workspace Laid(Element view)
    {
        var workspace = new Workspace().With(view);
        workspace.DrawScreen(new Recording(Area), Area);
        return workspace;
    }

    static void Click(Workspace workspace, Vector2 point)
    {
        workspace.PointerDown(point, PointerButton.Left, 1, false, false);
        workspace.PointerUp(point, PointerButton.Left);
    }

    static Vector2 Middle(Element element)
    {
        var bounds = element.Bounds;
        return new Vector2(bounds.X + bounds.Width / 2f, bounds.Y + bounds.Height / 2f);
    }

    sealed class Listener : VStack
    {
        public List<float> Wheels { get; } = new List<float>();

        protected override bool OnWheel(float amount)
        {
            Wheels.Add(amount);
            return true;
        }
    }
}
