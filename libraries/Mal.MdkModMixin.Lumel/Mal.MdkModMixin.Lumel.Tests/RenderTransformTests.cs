using Lumel.Input;
using Lumel.Layout;
using Lumel.Text;
using VRageMath;
using Xunit;

namespace Lumel.Tests;

/// <summary>A render transform: an element drawn and pressed moved and scaled, while layout leaves it where it put it.</summary>
public class RenderTransformTests
{
    const float Wide = 1440f;
    const float High = 1080f;

    static readonly Color Paint = new Color(200, 40, 40);

    /// <summary>Moved, it is drawn moved, and its bounds stay where layout put it.</summary>
    [Fact]
    public void MovedItIsDrawnMovedAndLaidOutInPlace()
    {
        var box = Box();
        var easel = new Easel(new VStack { Children = { box } }, Wide, High);

        box.RenderTransform = RenderTransform.Moved(new Vector2(10f, 20f));
        easel.Draw(Wide, High);

        Assert.Equal(new RectangleF(0f, 0f, 100f, 40f), box.Bounds);
        var drawn = Painted(easel);
        Assert.Equal(10, drawn.X);
        Assert.Equal(20, drawn.Y);
    }

    /// <summary>Setting it measures nothing and moves nothing else, because layout never sees it.</summary>
    [Fact]
    public void SettingItMeasuresNothing()
    {
        var box = Box();
        var neighbor = Box();
        var easel = new Easel(new VStack { Children = { box, neighbor } }, Wide, High);
        var settled = easel.Measured;
        var beside = neighbor.Bounds;

        box.RenderTransform = RenderTransform.Moved(new Vector2(0f, 30f));
        easel.Draw(Wide, High);

        Assert.Equal(settled, easel.Measured);
        Assert.Equal(beside, neighbor.Bounds);
    }

    /// <summary>A press lands on it where it is drawn, and not where it was laid out.</summary>
    [Fact]
    public void APressLandsWhereItIsDrawn()
    {
        var box = Box();
        box.Command = new RelayCommand(() => { });
        var root = new VStack { Children = { box } };
        var easel = new Easel(root, Wide, High);

        box.RenderTransform = RenderTransform.Moved(new Vector2(200f, 0f));
        easel.Draw(Wide, High);

        Assert.Same(box, root.HitTest(new Vector2(250f, 20f)));
        Assert.Null(root.HitTest(new Vector2(50f, 20f)));
        Assert.True(box.Reaches(new Vector2(250f, 20f)));
    }

    /// <summary>Scaled, it grows from its top-left corner, and its words are drawn larger with it.</summary>
    [Fact]
    public void ScaledItGrowsFromItsCorner()
    {
        var box = Box();
        box.Text = "Scaled";
        var easel = new Easel(new VStack { Children = { box } }, Wide, High);
        var words = easel.Written.Single().Scale;

        box.RenderTransform = new RenderTransform(Vector2.Zero, 2f);
        easel.Draw(Wide, High);

        var drawn = Painted(easel);
        Assert.Equal(0, drawn.X);
        Assert.Equal(200, drawn.Width);
        Assert.Equal(80, drawn.Height);
        Assert.Equal(words * 2f, easel.Written.Single().Scale, 3);
    }

    /// <summary>A transform inside another is moved by both.</summary>
    [Fact]
    public void TransformsInsideEachOtherAddUp()
    {
        var box = Box();
        var holder = new VStack { Children = { box } };
        var easel = new Easel(new VStack { Children = { holder } }, Wide, High);

        holder.RenderTransform = RenderTransform.Moved(new Vector2(10f, 0f));
        box.RenderTransform = RenderTransform.Moved(new Vector2(0f, 10f));
        easel.Draw(Wide, High);

        var drawn = Painted(easel);
        Assert.Equal(10, drawn.X);
        Assert.Equal(10, drawn.Y);
        Assert.True(box.Reaches(new Vector2(20f, 20f)));
        Assert.False(box.Reaches(new Vector2(5f, 5f)));
    }

    static Label Box() => new Label { Base = { Background = Paint }, Placement = FixedSize.Size(100f, 40f) };

    static Rectangle Painted(Easel easel) => easel.Sprites.Single(sprite => sprite.Color == Paint).Destination;
}
