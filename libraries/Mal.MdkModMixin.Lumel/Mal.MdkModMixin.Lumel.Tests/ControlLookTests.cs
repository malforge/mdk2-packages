using Lumel.Controls;
using Lumel.Images;
using Lumel.Input;
using Lumel.Layout;
using Lumel.Styling;
using Lumel.Text;
using VRageMath;
using Xunit;

namespace Lumel.Tests;

/// <summary>What the controls draw with comes from their looks: the game's where no style is given, and the neutral values where a style says nothing.</summary>
public class ControlLookTests
{
    const float Wide = 400f;
    const float High = 400f;

    static readonly Color Live = new Color(58, 204, 0);

    /// <summary>A separator is a hairline in the game's line color where no style is given, and as thick and colored as its look says.</summary>
    [Fact]
    public void ASeparatorIsItsLooksLine()
    {
        var game = new Separator();
        var own = new Separator { Base = { Thickness = 4f, Color = Live } };
        var easel = new Easel(new VStack { Children = { game, own } }, Wide, High);

        Assert.Equal(1f, game.DesiredSize.Y);
        Assert.Equal(GameColors.Line, game.Color);
        Assert.Equal(4f, own.DesiredSize.Y);
        Assert.Contains(easel.Sprites, sprite => sprite.Color == Live);
    }

    /// <summary>A separator with a style that says nothing is a white line one unit thick.</summary>
    [Fact]
    public void ASeparatorsNeutralLineIsWhiteAndOneThick()
    {
        var line = new Separator { Style = new SeparatorStyle() };
        line.Restyle();

        Assert.Equal(1f, line.Thickness);
        Assert.Equal(Color.White, line.Color);
    }

    /// <summary>A tree line is two wide and turns in by twelve in the game's line color where no style is given.</summary>
    [Fact]
    public void ATreeLineTakesTheGamesLook()
    {
        var line = new TreeLine();
        line.Restyle();

        Assert.Equal(2f, line.Thickness);
        Assert.Equal(12f, line.Turn);
        Assert.Equal(GameColors.Line, line.Color);
    }

    /// <summary>A tree line is as wide as its look says, and turns in at what it ends at by as much as its look says.</summary>
    [Fact]
    public void ATreeLineTurnsInByItsLook()
    {
        Label end;
        var line = new TreeLine { Base = { Thickness = 4f, Turn = 20f, Color = Live }, Placement = FixedSize.Width(4f) };
        var row = new HStack { Children = { line, new VStack { Children = { new Label { Text = "end" }.As(out end) } } } };
        line.Ends = end;
        var easel = new Easel(new VStack { Children = { row } }, Wide, High);

        var turn = Assert.Single(easel.Sprites, sprite => sprite.Color == Live && sprite.Destination.Width > sprite.Destination.Height).Destination;
        Assert.Equal(20f * easel.Pixels, turn.Width, 1f);
        Assert.Equal(4f * easel.Pixels, turn.Height, 1f);
    }

    /// <summary>A tree line turns in at the middle of what it ends at, or along its bottom edge when told to.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ATreeLineTurnsInAtTheMiddleOrTheBottom(bool atBottom)
    {
        Label end;
        var line = new TreeLine { Base = { Thickness = 4f, Turn = 20f, Color = Live }, Placement = FixedSize.Width(4f), TurnsAtBottom = atBottom };
        var row = new HStack { Children = { line, new VStack { Children = { new Label { Text = "end", Placement = FixedSize.Height(100f) }.As(out end) } } } };
        line.Ends = end;
        var easel = new Easel(new VStack { Children = { row } }, Wide, High);

        var turn = Assert.Single(easel.Sprites, sprite => sprite.Color == Live && sprite.Destination.Width > sprite.Destination.Height).Destination;
        var expected = atBottom ? end.Bounds.Y + 96f : end.Bounds.Y + 50f;
        Assert.Equal(expected * easel.Pixels, turn.Y, 1f);
    }

    /// <summary>A progress bar takes the game's well as its track and its accent as its fill where no style is given.</summary>
    [Fact]
    public void AProgressBarTakesTheGamesLook()
    {
        var bar = new ProgressBar();
        bar.Restyle();

        Assert.Equal(GameColors.Well, bar.Background);
        Assert.Equal(GameColors.Accent, bar.Fill);
    }

    /// <summary>A progress bar fills its share from the left, holds its value between empty and full, and fills in the color its state's look gives.</summary>
    [Theory]
    [InlineData(0.25f, 0.25f)]
    [InlineData(1.5f, 1f)]
    [InlineData(-1f, 0f)]
    public void AProgressBarFillsItsShare(float value, float shown)
    {
        var low = State.Declare("TestLow", 400);
        var bar = new ProgressBar { Base = { Fill = Color.Gray }, [low] = { Fill = Live }, Placement = FixedSize.Size(200f, 8f), Value = value };
        bar.Set(low, true);
        var easel = new Easel(new VStack { Children = { bar } }, Wide, High);

        Assert.Equal(shown, bar.Value);
        var fills = easel.Sprites.Where(sprite => sprite.Color == Live).ToList();
        if (shown == 0f)
        {
            Assert.Empty(fills);
            return;
        }

        var fill = Assert.Single(fills).Destination;
        Assert.Equal(bar.Bounds.X * easel.Pixels, fill.X, 1f);
        Assert.Equal(200f * shown * easel.Pixels, fill.Width, 1f);
    }

    /// <summary>A dial's needle takes its dragging look while it is turned, and gives an angle only once it is turned.</summary>
    [Fact]
    public void ADialsNeedleTakesItsDraggingLookWhileTurned()
    {
        var dial = new Dial { Style = new DialStyle { Base = { NeedleColor = Color.Gray }, Dragging = { NeedleColor = Live } } };
        var root = new VStack { Children = { dial } };
        _ = new Easel(root, Wide, High);
        var input = new InputRoot(root);
        var turned = 0;
        dial.AngleChanged += () => turned++;
        Assert.Null(dial.Angle);

        var right = dial.ToScreen(new Vector2(dial.ContentBounds.Right - 2f, dial.ContentBounds.Center.Y));
        input.PointerDown(right, PointerButton.Left, 1, false, false);

        Assert.True(dial.IsDragging);
        Assert.Equal(Live, dial.NeedleColor);
        Assert.Equal(90f, dial.Angle);
        Assert.Equal(1, turned);

        input.PointerUp(right, PointerButton.Left);
        Assert.False(dial.IsDragging);
        Assert.Equal(Color.Gray, dial.NeedleColor);
    }

    /// <summary>A dial's angle is held within its reach either way, and can be taken away again.</summary>
    [Fact]
    public void ADialsAngleIsHeldWithinItsReach()
    {
        var dial = new Dial { MaximumAngle = 90f, Angle = 135f };
        Assert.Equal(90f, dial.Angle);

        dial.Angle = -200f;
        Assert.Equal(-90f, dial.Angle);

        dial.Angle = null;
        Assert.Null(dial.Angle);
    }

    /// <summary>A slider's value runs from its minimum at the left to its maximum at the right, and is held between them, the maximum never below the minimum.</summary>
    [Fact]
    public void ASlidersValueRunsFromItsMinimumToItsMaximum()
    {
        var slider = new Slider { Minimum = 10f, Maximum = 20f, Placement = FixedSize.Width(208f) };
        var root = new VStack { Children = { slider } };
        _ = new Easel(root, Wide, High);
        var input = new InputRoot(root);
        var moved = 0;
        slider.ValueChanged += () => moved++;
        Assert.Equal(10f, slider.Value);

        var middle = slider.ToScreen(new Vector2(slider.ContentBounds.Center.X, slider.ContentBounds.Center.Y));
        input.PointerDown(middle, PointerButton.Left, 1, false, false);
        Assert.Equal(15f, slider.Value, 2);
        Assert.Equal(1, moved);

        slider.Value = 40f;
        Assert.Equal(20f, slider.Value);
        slider.Minimum = 30f;
        Assert.Equal(30f, slider.Maximum);
        Assert.Equal(30f, slider.Value);
    }

    /// <summary>
    ///     A slider takes its track, fill and thumb from its looks: the game's sunken track with no fill and an ink thumb
    ///     where no style is given.
    /// </summary>
    [Fact]
    public void ASliderTakesItsTrackAndThumbFromItsLooks()
    {
        var game = new Slider();
        var own = new Slider { Base = { TrackTint = Live, TrackHeight = 4f, Fill = Color.Gray, Thumb = Color.Gray, ThumbWidth = 8f, ThumbHeight = 0f } };
        game.Restyle();
        own.Restyle();

        Assert.Equal(GameColors.Well, game.TrackTint);
        Assert.Equal(8f, game.TrackHeight);
        Assert.Equal(Color.Transparent, game.Fill);
        Assert.Equal(GameColors.Ink, game.Thumb);
        Assert.Equal(12f, game.ThumbWidth);
        Assert.Equal(20f, game.ThumbHeight);
        Assert.Equal(Live, own.TrackTint);
        Assert.Equal(4f, own.TrackHeight);
        Assert.Equal(Color.Gray, own.Fill);
        Assert.Equal(Color.Gray, own.Thumb);
        Assert.Equal(8f, own.ThumbWidth);
        Assert.Equal(0f, own.ThumbHeight);
    }

    /// <summary>
    ///     A slider shows its dragging look from the press on it until the release, its track lit and its thumb white
    ///     meanwhile.
    /// </summary>
    [Fact]
    public void ASliderIsDraggingWhileItsThumbIsHeld()
    {
        var slider = new Slider();
        var root = new VStack { Children = { slider } };
        _ = new Easel(root, 200f, 100f);
        var input = new InputRoot(root);
        var middle = new Vector2(slider.Bounds.X + slider.Bounds.Width / 2f, slider.Bounds.Y + slider.Bounds.Height / 2f);

        input.PointerDown(middle, PointerButton.Left, 1, false, false);
        slider.Restyle();
        Assert.True(slider.IsDragging);
        Assert.Equal(Color.White, slider.Thumb);
        Assert.Equal(GameColors.Line, slider.TrackTint);

        input.PointerUp(middle, PointerButton.Left);
        Assert.False(slider.IsDragging);
    }

    /// <summary>A spectrum's marker is drawn as its look says: a ring of the marker's color inside an outline of the outline's.</summary>
    [Fact]
    public void ASpectrumsMarkerIsItsLook()
    {
        var spectrum = new ColorSpectrum
        {
            Base = { MarkerSize = 20f, MarkerThickness = 4f, MarkerOutlineThickness = 2f, MarkerColor = Live, MarkerOutlineColor = Color.Gray },
            Placement = FixedSize.Size(200f, 200f),
            Saturation = 0.5f,
            Value = 0.5f
        };
        var easel = new Easel(new VStack { Children = { spectrum } }, Wide, High);

        var ring = easel.Sprites.Where(sprite => sprite.Color == Live).Select(sprite => sprite.Destination).ToList();
        var outline = easel.Sprites.Where(sprite => sprite.Color == Color.Gray).Select(sprite => sprite.Destination).ToList();
        Assert.Equal(4, ring.Count);
        Assert.Equal(4, outline.Count);
        Assert.Equal(20f * easel.Pixels, ring.Max(r => r.Width), 1f);
        Assert.Equal(4f * easel.Pixels, ring.Min(r => r.Height), 1f);
        Assert.Equal(24f * easel.Pixels, outline.Max(r => r.Width), 1f);
    }

    /// <summary>A hue slider's marker is a bar of the marker's color on a wider outline, as thick as its look says.</summary>
    [Fact]
    public void AHueSlidersMarkerIsItsLook()
    {
        var slider = new HueSlider
        {
            Base = { MarkerThickness = 4f, MarkerOutlineThickness = 3f, MarkerColor = Live, MarkerOutlineColor = Color.Gray },
            Placement = FixedSize.Size(20f, 200f),
            Hue = 0.5f
        };
        var easel = new Easel(new VStack { Children = { slider } }, Wide, High);

        var bar = Assert.Single(easel.Sprites, sprite => sprite.Color == Live).Destination;
        var outline = Assert.Single(easel.Sprites, sprite => sprite.Color == Color.Gray).Destination;
        Assert.Equal(4f * easel.Pixels, bar.Height, 1f);
        Assert.Equal(10f * easel.Pixels, outline.Height, 1f);
        Assert.Equal(26f * easel.Pixels, outline.Width, 1f);
    }

    /// <summary>An image's tint is a look: its style's, or one set on the image itself from data.</summary>
    [Fact]
    public void AnImagesTintIsItsLook()
    {
        var styled = new Image { Style = new ImageStyle { Base = { Tint = Color.Gray } } };
        var own = new Image { Style = new ImageStyle { Base = { Tint = Color.Gray } } };
        own.Base.Tint = Live;
        var plain = new Image();
        styled.Restyle();
        own.Restyle();
        plain.Restyle();

        Assert.Equal(Color.Gray, styled.Tint);
        Assert.Equal(Live, own.Tint);
        Assert.Equal(Color.White, plain.Tint);
    }

    /// <summary>A button is pressed while a left press begun on it is held on it, and not once the pointer leaves it or lets go.</summary>
    [Fact]
    public void AButtonIsPressedWhileAPressIsHeldOnIt()
    {
        var button = new Button { Text = "Go", Command = new RelayCommand(() => { }) };
        var root = new VStack { Children = { button } };
        _ = new Easel(root, Wide, High);
        var input = new InputRoot(root);
        var on = button.ToScreen(button.Bounds.Center);

        input.PointerDown(on, PointerButton.Left, 1, false, false);
        Assert.True(button.IsPressed);
        Assert.Equal(GameColors.Line, button.Background);

        input.PointerMove(on + new Vector2(0f, 300f));
        Assert.False(button.IsPressed);

        input.PointerMove(on);
        Assert.True(button.IsPressed);

        input.PointerUp(on, PointerButton.Left);
        Assert.False(button.IsPressed);
    }

    /// <summary>A switch's words for on and off are what it was given, and read back.</summary>
    [Fact]
    public void ASwitchsWordsReadBack()
    {
        var control = new Switch { OnText = "Yes", OffText = "No" };

        Assert.Equal("Yes", control.OnText);
        Assert.Equal("No", control.OffText);
    }
}
