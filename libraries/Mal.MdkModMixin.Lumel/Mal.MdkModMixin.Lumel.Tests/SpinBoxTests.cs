using Lumel.Controls;
using Lumel.Input;
using Lumel.Layout;
using Lumel.Screens;
using Lumel.Styling;
using Lumel.Windows;
using VRage.Input;
using VRageMath;
using Xunit;

namespace Lumel.Tests;

/// <summary>A spin box: its buttons step its number by the step it is given, within its bounds.</summary>
public class SpinBoxTests
{
    static readonly RectangleF Area = new RectangleF(0f, 0f, 800f, 600f);

    /// <summary>Plus and minus step the number by the box's step, as an edit.</summary>
    [Fact]
    public void TheButtonsStepTheNumber()
    {
        var scene = new Scene(new SpinBox { Box = { SmallChange = 0.5, Value = 2 } });
        var edits = 0;
        scene.Spin.Box.Edited += () => edits++;

        scene.Press(Plus(scene.Spin));
        Assert.Equal(2.5, scene.Spin.Box.Value);

        scene.Press(Minus(scene.Spin));
        scene.Press(Minus(scene.Spin));
        Assert.Equal(1.5, scene.Spin.Box.Value);
        Assert.Equal(3, edits);
    }

    /// <summary>A changed step takes effect at the next press, as a unit changing from seconds to ticks changes it.</summary>
    [Fact]
    public void TheStepCanChange()
    {
        var scene = new Scene(new SpinBox { Box = { Value = 20 } });

        scene.Spin.Box.SmallChange = 10;
        scene.Press(Plus(scene.Spin));

        Assert.Equal(30, scene.Spin.Box.Value);
    }

    /// <summary>The button stepping toward a bound the number stands at is disabled, and the number stays within its bounds.</summary>
    [Fact]
    public void TheNumberStaysWithinItsBounds()
    {
        var scene = new Scene(new SpinBox { Box = { Minimum = 0, Maximum = 2, Value = 1 } });

        scene.Press(Minus(scene.Spin));
        Assert.Equal(0, scene.Spin.Box.Value);
        Assert.False(Minus(scene.Spin).IsEnabled);
        Assert.True(Plus(scene.Spin).IsEnabled);

        scene.Press(Minus(scene.Spin));
        Assert.Equal(0, scene.Spin.Box.Value);

        scene.Press(Plus(scene.Spin));
        scene.Press(Plus(scene.Spin));
        scene.Press(Plus(scene.Spin));
        Assert.Equal(2, scene.Spin.Box.Value);
        Assert.False(Plus(scene.Spin).IsEnabled);
        Assert.True(Minus(scene.Spin).IsEnabled);
    }

    /// <summary>The buttons take the theme's step style, as tall as the number box between them.</summary>
    [Fact]
    public void TheButtonsAreAsTallAsTheBox()
    {
        var scene = new Scene(new SpinBox { Box = { Value = 1 } });

        Assert.Same(GameTheme.StepButton, Plus(scene.Spin).Style);
        Assert.Equal(scene.Spin.Box.Bounds.Height, Plus(scene.Spin).Bounds.Height);
        Assert.Equal(scene.Spin.Box.Bounds.Height, Minus(scene.Spin).Bounds.Height);
    }

    /// <summary>A press on a button leaves the keyboard in the number box, so typing follows a step.</summary>
    [Fact]
    public void AStepLeavesTheKeyboardInTheBox()
    {
        var scene = new Scene(new SpinBox { Box = { Value = 1 } });

        scene.Press(Plus(scene.Spin));
        scene.Workspace.Text('5');

        Assert.True(scene.Spin.Box.IsFocused);
        Assert.Contains("5", scene.Spin.Box.Text);
    }

    /// <summary>A press on a button while the number box is disabled leaves nothing holding the keyboard.</summary>
    [Fact]
    public void AStepLeavesNothingFocusedWhileTheBoxIsDisabled()
    {
        var scene = new Scene(new SpinBox { Box = { Value = 1 } });
        scene.Spin.Box.IsEnabled = false;
        scene.Draw();

        scene.Press(Plus(scene.Spin));

        Assert.Null(scene.Workspace.Input.Focused);
    }

    /// <summary>Tab passes over the buttons to the number box.</summary>
    [Fact]
    public void TabPassesOverTheButtons()
    {
        var scene = new Scene(new SpinBox { Box = { Value = 1 } });

        scene.Workspace.Key(MyKeys.Tab, false, false);

        Assert.True(scene.Spin.Box.IsFocused);
    }

    /// <summary>Each button carries the tip the application gives it.</summary>
    [Fact]
    public void TheButtonsCarryTheirTips()
    {
        var shorter = Tip.For(null, "Shorter");
        var longer = Tip.For(null, "Longer");

        var spin = new SpinBox { LessTip = shorter, MoreTip = longer };

        Assert.Same(shorter, Minus(spin).Tip);
        Assert.Same(longer, Plus(spin).Tip);
    }

    static Button Minus(SpinBox spin)
    {
        return (Button)spin.Children[0];
    }

    static Button Plus(SpinBox spin)
    {
        return (Button)spin.Children[2];
    }

    /// <summary>A spin box in a window on a workspace.</summary>
    sealed class Scene
    {
        public readonly SpinBox Spin;
        public readonly Workspace Workspace = new Workspace();

        public Scene(SpinBox spin)
        {
            Spin = spin;
            Workspace.Windows.Open(new Window { Placement = FixedSize.Width(400f), Content = new HStack { Children = { spin } } });
            Draw();
        }

        public void Draw()
        {
            Workspace.DrawScreen(new Recording(Area), Area);
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
