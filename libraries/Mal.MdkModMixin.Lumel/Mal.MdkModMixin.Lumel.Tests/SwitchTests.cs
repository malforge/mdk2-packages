using Lumel.Controls;
using Lumel.Input;
using Lumel.Layout;
using Lumel.Sim;
using VRageMath;
using Xunit;

namespace Lumel.Tests;

/// <summary>A switch: the word for its state beside a thumb that moves, sized for the longer word, and its look from its style.</summary>
public class SwitchTests
{
    const float Wide = 400f;
    const float High = 200f;

    static readonly Color Ink = new Color(220, 245, 253);
    static readonly Color Body = new Color(91, 115, 123);
    static readonly Color Well = new Color(32, 39, 45);
    static readonly Color Dim = new Color(95, 114, 120);
    static readonly Color Muted = new Color(162, 195, 205);

    static readonly SwitchStyle Plain = new SwitchStyle
    {
        Base =
        {
            FontSize = 16f,
            Color = Ink,
            Background = Body,
            Track = Well,
            Thumb = Ink,
            WordPadding = new Thickness(8f, 4f),
            Padding = new Thickness(4f),
            Border = Dim
        },
        On = { Track = Color.Transparent },
        PointerOver = { Border = Muted }
    };

    /// <summary>Only the word for the state it is in is written.</summary>
    [Fact]
    public void ItSaysOnlyTheStateItIsIn()
    {
        var control = new Switch { Style = Plain, IsOn = true };
        var easel = new Easel(new VStack { Children = { control } }, Wide, High);

        Assert.Single(easel.Written, command => command.Text == "ON");
        Assert.DoesNotContain(easel.Written, command => command.Text == "OFF");

        control.IsOn = false;
        easel.Draw(Wide, High);

        Assert.Single(easel.Written, command => command.Text == "OFF");
        Assert.DoesNotContain(easel.Written, command => command.Text == "ON");
    }

    /// <summary>The thumb is at the right while on and at the left while off.</summary>
    [Fact]
    public void TheThumbIsAtTheRightWhenOn()
    {
        var control = new Switch { Style = Plain, IsOn = true };
        var easel = new Easel(new VStack { Children = { control } }, Wide, High);
        var onAt = Thumb(easel).X;

        control.IsOn = false;
        easel.Draw(Wide, High);
        var offAt = Thumb(easel).X;

        Assert.True(onAt > offAt);
    }

    /// <summary>The thumb takes the color its style gives the state the switch is in.</summary>
    [Fact]
    public void TheThumbTakesTheColorOfItsState()
    {
        var amber = new Color(255, 170, 40);
        var style = new SwitchStyle(Plain) { Base = { Thumb = amber }, On = { Thumb = Ink } };
        var control = new Switch { Style = style, IsOn = false };
        var easel = new Easel(new VStack { Children = { control } }, Wide, High);
        Assert.Single(easel.Sprites, sprite => sprite.Color == amber);

        control.IsOn = true;
        easel.Draw(Wide, High);
        Assert.DoesNotContain(easel.Sprites, sprite => sprite.Color == amber);
        Assert.Equal(Thumb(easel).Width, Thumb(easel).Height);
    }

    /// <summary>The room inside is filled with the off track only while off.</summary>
    [Fact]
    public void TheTrackIsFilledOnlyWhileOff()
    {
        var control = new Switch { Style = Plain, IsOn = true };
        var easel = new Easel(new VStack { Children = { control } }, Wide, High);

        Assert.DoesNotContain(easel.Sprites, sprite => sprite.Color == Well);

        control.IsOn = false;
        easel.Draw(Wide, High);

        Assert.Contains(easel.Sprites, sprite => sprite.Color == Well);
    }

    /// <summary>It is as wide as the longer word needs whichever it is showing, so it keeps its size when switched.</summary>
    [Fact]
    public void ItIsSizedForTheLongerWord()
    {
        var control = new Switch { Style = Plain, IsOn = true };
        control.OnText = "On";
        control.OffText = "Isključeno";
        _ = new Easel(new VStack { Children = { control } }, Wide, High);
        var on = control.DesiredSize;

        var other = new Switch { Style = Plain, IsOn = false };
        other.OnText = "On";
        other.OffText = "Isključeno";
        _ = new Easel(new VStack { Children = { other } }, Wide, High);

        var shorter = new Switch { Style = Plain, IsOn = true };
        shorter.OnText = "On";
        shorter.OffText = "Off";
        _ = new Easel(new VStack { Children = { shorter } }, Wide, High);

        Assert.Equal(on, other.DesiredSize);
        Assert.True(on.X > shorter.DesiredSize.X);
    }

    /// <summary>Under the pointer, it takes its border for that; left, it goes back.</summary>
    [Fact]
    public void UnderThePointerItTakesItsBorder()
    {
        var control = new Switch { Style = Plain };
        control.Restyle();

        ((IInputTarget)control).IsPointerOver = true;
        Assert.Equal(Muted, control.Border);

        ((IInputTarget)control).IsPointerOver = false;
        Assert.Equal(Dim, control.Border);
    }

    /// <summary>Its height is its words' size, the room round them and the room round the inside, from its style.</summary>
    [Fact]
    public void ItIsSizedByItsStyle()
    {
        var control = new Switch { Style = Plain };
        _ = new Easel(new VStack { Children = { control } }, Wide, High);

        Assert.Equal(16f + 4f * 2f + 4f * 2f, control.DesiredSize.Y, 3);
    }

    /// <summary>Switching measures no text, since only what is drawn changes.</summary>
    [Fact]
    public void SwitchingMeasuresNoText()
    {
        var control = new Switch { Style = Plain };
        var easel = new Easel(new VStack { Children = { control } }, Wide, High);
        var settled = easel.Measured;

        control.IsOn = true;
        easel.Draw(Wide, High);
        control.IsOn = false;
        easel.Draw(Wide, High);

        Assert.Equal(settled, easel.Measured);
    }

    /// <summary>The single fill drawn in the thumb's color.</summary>
    /// <summary>Flipped, its thumb slides across over the slide's ticks, and is at rest at the other end once they are over.</summary>
    [Fact]
    public void FlippedItsThumbSlidesAcross()
    {
        var control = new Switch { Style = Plain, IsOn = false };
        var easel = new Easel(new VStack { Children = { control } }, Wide, High);
        var offAt = Thumb(easel).X;
        Assert.False(easel.IsFrameRequested);

        control.IsOn = true;
        var start = easel.Now;
        easel.Draw(Wide, High, start);
        Assert.Equal(offAt, Thumb(easel).X);
        Assert.True(easel.IsFrameRequested);

        easel.Draw(Wide, High, start + Switch.SlideTicks / 2);
        var partway = Thumb(easel).X;
        Assert.True(easel.IsFrameRequested);

        easel.Draw(Wide, High, start + Switch.SlideTicks);
        var onAt = Thumb(easel).X;
        Assert.False(easel.IsFrameRequested);

        Assert.True(offAt < partway && partway < onAt);
    }

    /// <summary>Partway across, both words ride beside the thumb, cut off at the edges of the room inside.</summary>
    [Fact]
    public void PartwayBothWordsAreDrawnClippedToTheInside()
    {
        var control = new Switch { Style = Plain, IsOn = false };
        var easel = new Easel(new VStack { Children = { control } }, Wide, High);

        control.IsOn = true;
        var start = easel.Now;
        easel.Draw(Wide, High, start);
        easel.Draw(Wide, High, start + Switch.SlideTicks / 2);

        var on = Assert.Single(easel.Written, command => command.Text == "ON");
        var off = Assert.Single(easel.Written, command => command.Text == "OFF");
        var thumb = Thumb(easel);
        Assert.True(on.Position.X < thumb.X);
        Assert.True(off.Position.X > thumb.X);

        var commands = easel.Commands.ToList();
        var clip = commands.Single(command => command.Kind == DrawKind.SetClip);
        var wide = (int)Math.Round(control.ContentBounds.Width * easel.Pixels);
        Assert.InRange(clip.Clip.Width, wide - 1, wide + 1);

        var from = commands.FindIndex(command => command.Kind == DrawKind.SetClip);
        var to = commands.FindIndex(command => command.Kind == DrawKind.ClearClip);
        Assert.InRange(commands.FindIndex(command => command.Text == "ON"), from, to);
        Assert.InRange(commands.FindIndex(command => command.Text == "OFF"), from, to);
    }

    /// <summary>Partway across, the track of the state it left stays behind the thumb on the side it is leaving.</summary>
    [Fact]
    public void PartwayTheLeftTrackStaysOnTheSideItLeaves()
    {
        var control = new Switch { Style = Plain, IsOn = false };
        var easel = new Easel(new VStack { Children = { control } }, Wide, High);

        control.IsOn = true;
        var start = easel.Now;
        easel.Draw(Wide, High, start);
        easel.Draw(Wide, High, start + Switch.SlideTicks / 2);

        var well = Assert.Single(easel.Sprites, sprite => sprite.Color == Well).Destination;
        var thumb = Thumb(easel);
        Assert.Equal(thumb.X + thumb.Width / 2f, well.X, 1f);
    }

    /// <summary>Flipped back partway, it slides back from where it had got to.</summary>
    [Fact]
    public void FlippedBackPartwayItSlidesBackFromThere()
    {
        var control = new Switch { Style = Plain, IsOn = false };
        var easel = new Easel(new VStack { Children = { control } }, Wide, High);
        var offAt = Thumb(easel).X;

        control.IsOn = true;
        var start = easel.Now;
        easel.Draw(Wide, High, start);
        easel.Draw(Wide, High, start + Switch.SlideTicks / 2);
        var partway = Thumb(easel).X;

        control.IsOn = false;
        easel.Draw(Wide, High, start + Switch.SlideTicks / 2);
        Assert.Equal(partway, Thumb(easel).X);

        easel.Draw(Wide, High, start + Switch.SlideTicks / 2 + Switch.SlideTicks);
        Assert.Equal(offAt, Thumb(easel).X);
        Assert.False(easel.IsFrameRequested);
    }

    /// <summary>Shown for the first time already on, it is drawn at rest with its thumb at the right.</summary>
    [Fact]
    public void FirstDrawnItIsAtRest()
    {
        var control = new Switch { Style = Plain, IsOn = true };
        var easel = new Easel(new VStack { Children = { control } }, Wide, High);

        Assert.False(easel.IsFrameRequested);
        Assert.Single(easel.Written);
    }

    static Rectangle Thumb(Easel easel)
    {
        return Assert.Single(easel.Sprites, sprite => sprite.Color == Ink).Destination;
    }
}
