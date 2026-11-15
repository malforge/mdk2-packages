using Lumel.Controls;
using Lumel.Drawing;
using Lumel.Input;
using Lumel.Layout;
using Lumel.Screens;
using Lumel.Windows;
using VRage.Input;
using VRageMath;
using Xunit;

namespace Lumel.Tests;

/// <summary>Radio buttons: one of a group checked at a time, by the player or the application, and how a button draws.</summary>
public class RadioButtonTests
{
    static readonly RectangleF Area = new RectangleF(0f, 0f, 800f, 600f);

    /// <summary>Pressing a button checks it, unchecks the one checked before, and tells the application.</summary>
    [Fact]
    public void PressingAButtonChecksOnlyIt()
    {
        var scene = new Scene();
        scene.Small.IsChecked = true;

        scene.Press(scene.Large);

        Assert.True(scene.Large.IsChecked);
        Assert.False(scene.Small.IsChecked);
        Assert.Same(scene.Large, scene.Group.Chosen);
        Assert.Equal(1, scene.Changes);
    }

    /// <summary>Pressing the checked button leaves it checked and tells nobody.</summary>
    [Fact]
    public void PressingTheCheckedButtonChangesNothing()
    {
        var scene = new Scene();
        scene.Small.IsChecked = true;

        scene.Press(scene.Small);

        Assert.True(scene.Small.IsChecked);
        Assert.Equal(0, scene.Changes);
    }

    /// <summary>Checking a button in code unchecks the one checked before, and tells nobody.</summary>
    [Fact]
    public void CheckingInCodeTellsNobody()
    {
        var scene = new Scene();
        scene.Group.Chosen = scene.Small;

        scene.Medium.IsChecked = true;

        Assert.False(scene.Small.IsChecked);
        Assert.Same(scene.Medium, scene.Group.Chosen);
        Assert.Equal(0, scene.Changes);

        scene.Medium.IsChecked = false;
        Assert.Null(scene.Group.Chosen);
    }

    /// <summary>Space checks the button that holds the keyboard.</summary>
    [Fact]
    public void SpaceChecksTheFocusedButton()
    {
        var scene = new Scene();
        scene.Workspace.Input.Focus(scene.Medium);

        scene.Workspace.Key(MyKeys.Space, false, false);

        Assert.Same(scene.Medium, scene.Group.Chosen);
        Assert.Equal(1, scene.Changes);
    }

    /// <summary>A disabled button cannot be checked by a press.</summary>
    [Fact]
    public void ADisabledButtonIsNotChecked()
    {
        var scene = new Scene();
        scene.Large.IsEnabled = false;

        scene.Press(scene.Large);

        Assert.False(scene.Large.IsChecked);
        Assert.Equal(0, scene.Changes);
    }

    /// <summary>A checked button joining a group unchecks the one checked there; one leaving its group stays checked.</summary>
    [Fact]
    public void JoiningAndLeavingAGroup()
    {
        var scene = new Scene();
        scene.Small.IsChecked = true;
        var other = new RadioButton { IsChecked = true };

        other.Group = scene.Group;
        Assert.False(scene.Small.IsChecked);
        Assert.Same(other, scene.Group.Chosen);

        other.Group = null;
        Assert.True(other.IsChecked);
        Assert.Null(scene.Group.Chosen);
    }

    /// <summary>A group checks only its own buttons.</summary>
    [Fact]
    public void AGroupChecksOnlyItsOwnButtons()
    {
        var group = new RadioGroup();

        Assert.Throws<ArgumentException>(() => group.Chosen = new RadioButton());
    }

    /// <summary>A checked button draws a ring and a dot inside it in the theme's colors; an unchecked one the ring alone.</summary>
    [Fact]
    public void ACheckedButtonDrawsADotInItsRing()
    {
        var scene = new Scene();
        scene.Small.IsChecked = true;
        var recording = new Recording(Area);
        scene.Workspace.DrawScreen(recording, Area);

        var discs = recording.Pieces.Where(p => p.Material == LumelMaterials.Disc).ToList();
        var rings = recording.Pieces.Where(p => p.Material == LumelMaterials.Ring).ToList();
        Assert.Equal(3, rings.Count);
        Assert.Equal(4, discs.Count);
        Assert.Single(discs, p => p.Destination.Width == 8f);
    }

    /// <summary>Three sizes, a button each, in one group, in a window on a workspace, counting the player's changes.</summary>
    sealed class Scene
    {
        public readonly RadioGroup Group = new RadioGroup();
        public readonly RadioButton Small;
        public readonly RadioButton Medium;
        public readonly RadioButton Large;
        public readonly Workspace Workspace = new Workspace();

        public Scene()
        {
            Workspace.Windows.Open(new Window
            {
                Placement = FixedSize.Width(400f),
                Content = new VStack
                {
                    Children =
                    {
                        new RadioButton { Text = "Small", Group = Group }.As(out Small),
                        new RadioButton { Text = "Medium", Group = Group }.As(out Medium),
                        new RadioButton { Text = "Large", Group = Group }.As(out Large)
                    }
                }
            });
            Group.ChoiceChanged += () => Changes++;
            Draw();
        }

        public int Changes { get; private set; }

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
