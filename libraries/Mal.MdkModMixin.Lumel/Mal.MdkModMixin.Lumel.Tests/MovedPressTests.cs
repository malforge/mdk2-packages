using Lumel.Controls;
using Lumel.Input;
using Lumel.Layout;
using Lumel.Text;
using Lumel.Viewing;
using VRageMath;
using Xunit;

namespace Lumel.Tests;

/// <summary>Presses through render transforms: what is pressed is told where, in its own layout, the press is drawn over it.</summary>
public class MovedPressTests
{
    const float Wide = 400f;
    const float High = 400f;

    /// <summary>A point carried out to the screen and back lands where it began, through a move and a scale above it.</summary>
    [Fact]
    public void AScreenPointRoundTrips()
    {
        var inner = new Label { Text = "Airlock", RenderTransform = new RenderTransform(new Vector2(5f, 7f), 2f) };
        var outer = new VStack { RenderTransform = RenderTransform.Moved(new Vector2(30f, 40f)), Children = { inner } };
        _ = new Easel(new VStack { Children = { outer } }, Wide, High);

        var point = new Vector2(inner.Bounds.X + 3f, inner.Bounds.Y + 4f);
        var drawn = inner.ToScreen(point);

        Assert.NotEqual(point, drawn);
        Assert.Equal(point.X, inner.FromScreen(drawn).X, 3);
        Assert.Equal(point.Y, inner.FromScreen(drawn).Y, 3);
    }

    /// <summary>A link in a moved row follows when pressed where it is drawn.</summary>
    [Fact]
    public void ALinkInAMovedRowFollowsWherePressed()
    {
        Link? followed = null;
        var text = new RichText { Document = Markdown.Parse("[codex](codex) and more"), Base = { FontSize = 20f } };
        text.LinkClicked += l => followed = l;
        var row = new VStack { RenderTransform = RenderTransform.Moved(new Vector2(0f, 60f)), Children = { text } };
        var root = new VStack { Children = { row } };
        _ = new Easel(root, Wide, High);

        Press(new InputRoot(root), text.ToScreen(new Vector2(text.ContentBounds.X + 4f, text.ContentBounds.Y + 10f)));

        Assert.Equal("codex", followed?.Target);
    }

    /// <summary>A link scaled up by a viewbox follows when pressed where it is drawn.</summary>
    [Fact]
    public void ALinkInAViewboxFollowsWherePressed()
    {
        Link? followed = null;
        var text = new RichText { Document = Markdown.Parse("[codex](codex) and more"), Base = { FontSize = 20f } };
        text.LinkClicked += l => followed = l;
        var box = new Viewbox { Content = text, Placement = FixedSize.Size(Wide, 300f) };
        var root = new VStack { Children = { box } };
        _ = new Easel(root, Wide, High);

        Press(new InputRoot(root), text.ToScreen(new Vector2(text.ContentBounds.X + 4f, text.ContentBounds.Y + 10f)));

        Assert.True(box.Scale > 1f, $"scaled by {box.Scale}");
        Assert.Equal("codex", followed?.Target);
    }

    /// <summary>A moved row's command runs when the press ends over where it is drawn, and not over where it was laid out.</summary>
    [Fact]
    public void AMovedRowsCommandRunsWhereItIsDrawn()
    {
        var pressed = 0;
        var row = new HStack
        {
            Command = new RelayCommand(() => pressed++),
            RenderTransform = RenderTransform.Moved(new Vector2(0f, 100f)),
            Children = { new Label { Text = "Airlock" } }
        };
        var root = new VStack { Children = { row } };
        _ = new Easel(root, Wide, High);
        var input = new InputRoot(root);

        Press(input, row.ToScreen(new Vector2(row.Bounds.X + 2f, row.Bounds.Y + 2f)));
        Assert.Equal(1, pressed);

        Press(input, new Vector2(row.Bounds.X + 2f, row.Bounds.Y + 2f));
        Assert.Equal(1, pressed);
    }

    /// <summary>A drag handle in a row that moves tells where the pointer is on the screen, so dragging it does not chase its own row.</summary>
    [Fact]
    public void ADragHandleInAMovedRowSaysWhereThePointerIs()
    {
        var handle = new DragHandle { CanDrag = true, Placement = FixedSize.Size(24f, 24f) };
        var row = new HStack { RenderTransform = RenderTransform.Moved(new Vector2(0f, 50f)), Children = { handle } };
        var root = new VStack { Children = { row } };
        _ = new Easel(root, Wide, High);
        var input = new InputRoot(root);

        var said = new List<Vector2>();
        handle.DragStarted += said.Add;
        handle.DragDelta += said.Add;

        var at = handle.ToScreen(new Vector2(handle.Bounds.X + 4f, handle.Bounds.Y + 4f));
        input.PointerDown(at, PointerButton.Left, 1, false, false);
        input.PointerMove(at + new Vector2(0f, 30f));

        Assert.Equal(new[] { at, at + new Vector2(0f, 30f) }, said);
    }

    static void Press(InputRoot input, Vector2 at)
    {
        input.PointerDown(at, PointerButton.Left, 1, false, false);
        input.PointerUp(at, PointerButton.Left);
    }
}
