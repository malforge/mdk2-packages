using Lumel.Input;
using Lumel.Layout;
using Lumel.Text;
using VRageMath;
using Xunit;
using Lumel.Sim;

namespace Lumel.Tests;

/// <summary>Links in markdown-lite: words with a target only the host reads, which can be pressed where the host asks.</summary>
public class LinkTests
{
    const float Wide = 400f;
    const float High = 400f;

    static readonly Color Body = new Color(162, 195, 205);
    static readonly Color Stressed = new Color(220, 245, 253);

    /// <summary>A link is its words and its target; every run its words become shares one link.</summary>
    [Fact]
    public void ALinkIsWordsAndATarget()
    {
        var runs = Markdown.Parse("Read [the installation](installation) first").Blocks[0].Runs;

        var link = Assert.IsType<LinkRun>(runs[1]);
        Assert.Equal("the installation", link.Text);
        Assert.Equal("installation", link.Link.Target);
        Assert.Equal(new[] { "Read ", "the installation", " first" }, runs.Select(r => r.Text));
    }

    /// <summary>A link's words honor backslash escapes, a bracket among them included.</summary>
    [Fact]
    public void ALinksWordsHonorEscapes()
    {
        var link = Assert.IsType<LinkRun>(Assert.Single(Markdown.Parse("[a \\] b](x)").Blocks[0].Runs));

        Assert.Equal("a ] b", link.Text);
    }

    /// <summary>Brackets with no target, a reference, an empty target or one with a space are the text they are.</summary>
    [Theory]
    [InlineData("[words]")]
    [InlineData("[words][ref]")]
    [InlineData("[words]()")]
    [InlineData("[words](two parts)")]
    [InlineData("[words](unclosed")]
    public void NotALinkIsText(string source)
    {
        var runs = Markdown.Parse(source).Blocks[0].Runs;

        Assert.DoesNotContain(runs, r => r is LinkRun);
        Assert.Equal(source, string.Concat(runs.Select(r => r.Text)));
    }

    /// <summary>A slot among a link's words stays part of the link.</summary>
    [Fact]
    public void ASlotInALinkStaysInIt()
    {
        var runs = Template.Parse("[Open {name}](system)").Blocks[0].Runs;

        Assert.All(runs, r => Assert.IsType<LinkRun>(r));
        Assert.Same(((LinkRun)runs[0]).Link, ((LinkRun)runs[1]).Link);
        Assert.Equal("name", runs[1].Slot);
    }

    /// <summary>A link is drawn in the link color, and in its own once it is given one.</summary>
    [Fact]
    public void ALinkIsDrawnInTheLinkColor()
    {
        var (text, easel) = Laid("See [the codex](codex) now");
        Assert.Equal(Color.White, Word(easel, "the codex").Color);
        Assert.Equal(Body, Word(easel, "now").Color);

        text.Base.LinkColor = Color.Orange;
        easel.Draw(Wide, High);
        Assert.Equal(Color.Orange, Word(easel, "the codex").Color);
    }

    /// <summary>A link's fill is drawn behind its words, and stops where they do.</summary>
    [Fact]
    public void ALinksFillStopsAtItsWords()
    {
        var (text, easel) = Laid("[codex](codex) and more words after it");
        text.Base.LinkBackground = Color.Purple;
        easel.Draw(Wide, High);

        var fill = Assert.Single(easel.Sprites, s => s.Color == Color.Purple).Destination;
        Assert.True(fill.X + fill.Width <= Word(easel, "and more words after it").Position.X + 1f, "the fill reaches past the link");
    }

    /// <summary>A press on a link follows it; a press beside it reaches the row the text stands in.</summary>
    [Fact]
    public void APressOnALinkFollowsItAndOneBesideItReachesTheRow()
    {
        Link followed = null!;
        var rowPressed = 0;
        var text = new RichText { Document = Markdown.Parse("[codex](codex) and more"), Base = { FontSize = 20f } };
        text.LinkClicked += l => followed = l;
        var row = new HStack { Command = new RelayCommand(() => rowPressed++), Children = { text } };
        var input = Input(row);

        var onLink = new Vector2(text.ContentBounds.X + 4f, text.ContentBounds.Y + 10f);
        input.PointerDown(onLink, PointerButton.Left, 1, false, false);
        input.PointerUp(onLink, PointerButton.Left);
        Assert.Equal("codex", followed.Target);
        Assert.Equal(0, rowPressed);

        var beside = new Vector2(text.ContentBounds.X + text.ContentBounds.Width - 2f, text.ContentBounds.Y + 10f);
        input.PointerDown(beside, PointerButton.Left, 1, false, false);
        input.PointerUp(beside, PointerButton.Left);
        Assert.Equal(1, rowPressed);
    }

    /// <summary>Without anything to follow them, links take no press at all.</summary>
    [Fact]
    public void LinksWithNothingToFollowThemAreWords()
    {
        var text = new RichText { Document = Markdown.Parse("[codex](codex)"), Base = { FontSize = 20f } };
        Input(new VStack { Children = { text } });

        Assert.NotNull(text.LinkAt(new Vector2(text.ContentBounds.X + 4f, text.ContentBounds.Y + 10f)));
        Assert.Null(text.HitTest(new Vector2(text.ContentBounds.X + 4f, text.ContentBounds.Y + 10f)));
    }

    /// <summary>Text with a command of its own and nothing to follow links takes a press anywhere on it, its links included.</summary>
    [Fact]
    public void TextWithACommandTakesPressesEverywhere()
    {
        var pressed = 0;
        var text = new RichText { Document = Markdown.Parse("[codex](codex) and more"), Base = { FontSize = 20f }, Command = new RelayCommand(() => pressed++) };
        var input = Input(new VStack { Children = { text } });

        var beside = new Vector2(text.ContentBounds.X + text.ContentBounds.Width - 2f, text.ContentBounds.Y + 10f);
        input.PointerDown(beside, PointerButton.Left, 1, false, false);
        input.PointerUp(beside, PointerButton.Left);

        Assert.Equal(1, pressed);
    }

    /// <summary>Resting on a link says what the link's tip says, and anywhere else what the text's own tip says.</summary>
    [Fact]
    public void ALinkSaysItsOwnTip()
    {
        var own = new Tip("The text");
        var linked = new Tip("The codex");
        var text = new RichText { Document = Markdown.Parse("[codex](codex) and more"), Base = { FontSize = 20f }, Tip = own, FnResolveLinkTip = _ => linked };
        Input(new VStack { Children = { text } });

        Assert.Same(linked, text.TipAt(new Vector2(text.ContentBounds.X + 4f, text.ContentBounds.Y + 10f)));
        Assert.Same(own, text.TipAt(new Vector2(text.ContentBounds.X + text.ContentBounds.Width - 2f, text.ContentBounds.Y + 10f)));
    }

    /// <summary>Once nothing handles a press on a link, its links take no press and a press on one reaches what is beneath.</summary>
    [Fact]
    public void LinksStopTakingPressesWhenTheirHandlerIsRemoved()
    {
        Link? followed = null;
        var rowPressed = 0;
        Action<Link> follow = l => followed = l;
        var text = new RichText { Document = Markdown.Parse("[codex](codex) and more"), Base = { FontSize = 20f } };
        text.LinkClicked += follow;
        var input = Input(new HStack { Command = new RelayCommand(() => rowPressed++), Children = { text } });
        var onLink = new Vector2(text.ContentBounds.X + 4f, text.ContentBounds.Y + 10f);

        text.LinkClicked -= follow;
        input.PointerDown(onLink, PointerButton.Left, 1, false, false);
        input.PointerUp(onLink, PointerButton.Left);

        Assert.Null(followed);
        Assert.Equal(1, rowPressed);
    }


    static (RichText, Easel) Laid(string markdown)
    {
        var text = new RichText
        {
            Document = Markdown.Parse(markdown),
            Base = { FontSize = 20f, Color = Body, EmphasisColor = Stressed },
            Placement = FixedSize.Width(Wide)
        };

        var easel = new Easel(new VStack { Children = { text } }, Wide, High);
        return (text, easel);
    }

    static InputRoot Input(Element root)
    {
        _ = new Easel(new VStack { Children = { root } }, Wide, High);
        return new InputRoot(root);
    }

    static DrawCommand Word(Easel easel, string text) =>
        Assert.Single(easel.Written, command => command.Text?.Trim() == text);
}
