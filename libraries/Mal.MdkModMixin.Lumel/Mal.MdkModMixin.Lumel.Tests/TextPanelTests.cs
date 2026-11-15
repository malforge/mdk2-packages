using Lumel.Images;
using Lumel.Screens;
using Lumel.Text;
using VRageMath;
using Xunit;

namespace Lumel.Tests;

/// <summary>A text panel: its element laid out across the surface, handed over one frame at a time, and unloaded when it goes.</summary>
public class TextPanelTests
{
    /// <summary>Each draw lays the element out across the surface, placed by its own alignment, and hands the surface one frame.</summary>
    [Fact]
    public void ADrawHandsTheSurfaceOneFrame()
    {
        var surface = new PanelSurface("White");
        var label = new Label { Text = "Hello", Align = Align.Center };
        var panel = new TextPanel(surface, new MaterialImages(_ => new Vector2(512f, 512f)), () => 0) { Content = label };

        panel.Draw();
        panel.Draw();

        Assert.Equal(2, surface.Frames);
        Assert.Equal(256f, label.Bounds.Center.X, 1);
        Assert.Equal(256f, label.Bounds.Center.Y, 1);
    }

    /// <summary>A frame in which things only move, by their render transforms, measures nothing.</summary>
    [Fact]
    public void AFrameThatOnlyMovesMeasuresNothing()
    {
        var surface = new PanelSurface("White");
        var label = new Label { Text = "Hello", Align = Align.Center };
        var panel = new TextPanel(surface, new MaterialImages(_ => new Vector2(512f, 512f)), () => 0) { Content = label };
        panel.Draw();
        var measured = surface.Measured;

        label.RenderTransform = new RenderTransform(new Vector2(40f, 0f), 1f);
        panel.Draw();
        panel.Draw();

        Assert.Equal(measured, surface.Measured);
        Assert.Equal(3, surface.Frames);
    }

    /// <summary>A panel draws into a list it is handed, and hands its last frame to the surface again without drawing.</summary>
    [Fact]
    public void AFrameCanGoIntoAListAndBeSentAgain()
    {
        var surface = new PanelSurface("White");
        var panel = new TextPanel(surface, new MaterialImages(_ => new Vector2(512f, 512f)), () => 0) { Content = new Label { Text = "Hello" } };
        var output = new List<VRage.Game.GUI.TextPanel.MySprite>();

        panel.Draw(output);
        Assert.Equal(0, surface.Frames);
        Assert.Equal(panel.LastFrame, output);
        Assert.NotEmpty(output);

        var measured = surface.Measured;
        panel.Resend();
        Assert.Equal(1, surface.Frames);
        Assert.Equal(measured, surface.Measured);
    }

    /// <summary>What a panel shows is loaded from the start, and unloaded when the panel is disposed.</summary>
    [Fact]
    public void DisposingThePanelUnloadsWhatItShows()
    {
        var unloaded = 0;
        var label = new Label { Text = "Hello" };
        label.Unloaded += () => unloaded++;
        var panel = new TextPanel(new PanelSurface("White"), new MaterialImages(_ => new Vector2(512f, 512f)), () => 0) { Content = label };

        Assert.True(label.IsLoaded);

        panel.Dispose();

        Assert.False(label.IsLoaded);
        Assert.Equal(1, unloaded);
    }
}
