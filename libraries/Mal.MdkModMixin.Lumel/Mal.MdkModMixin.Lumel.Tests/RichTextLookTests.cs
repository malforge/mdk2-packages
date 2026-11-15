using Lumel.Layout;
using Lumel.Text;
using VRageMath;
using Xunit;
using Lumel.Sim;

namespace Lumel.Tests;

/// <summary>The sizes, gaps and colors of rich text's own marks, each a look of its own: rules, bullets, quote bars, the room round headings, quotes, tables and callouts.</summary>
public class RichTextLookTests
{
    const float Wide = 400f;
    const float High = 600f;

    static readonly Color Body = new Color(162, 195, 205);
    static readonly Color Rule = new Color(91, 115, 123);

    /// <summary>A rule, and the line under a heading, are as thick as the rule thickness says.</summary>
    [Fact]
    public void RulesAreAsThickAsTheirLook()
    {
        var easel = Laid("# Title\n\n---", text =>
        {
            text.Base.UnderlinedHeadingLevel = 1;
            text.Base.RuleThickness = 4f;
        });

        var rules = easel.Sprites.Where(s => s.Color == Rule).ToList();
        Assert.Equal(2, rules.Count);
        Assert.All(rules, r => Assert.Equal(4f * easel.Pixels, r.Destination.Height, 1f));
    }

    /// <summary>A bullet is as wide and in the color its looks say, apart from the rules.</summary>
    [Fact]
    public void ABulletTakesItsOwnSizeAndColor()
    {
        var easel = Laid("- item", text =>
        {
            text.Base.BulletColor = Color.Orange;
            text.Base.BulletSize = 8f;
        });

        var bullet = Assert.Single(easel.Sprites, s => s.Color == Color.Orange);
        Assert.Equal(8f * easel.Pixels, bullet.Destination.Width, 1f);
        Assert.DoesNotContain(easel.Sprites, s => s.Color == Rule);
    }

    /// <summary>Bullets and quote bars whose looks give them no color of their own are drawn in the rule color.</summary>
    [Fact]
    public void BulletsAndQuoteBarsTakeTheRuleColorUnlessSet()
    {
        var text = new RichText
        {
            Style = new RichTextStyle { Base = { RuleColor = Color.Orange } },
            Document = Markdown.Parse("- item\n\n> Quoted"),
            Placement = FixedSize.Width(Wide)
        };
        var easel = new Easel(new VStack { Children = { text } }, Wide, High);

        Assert.Equal(2, easel.Sprites.Count(s => s.Color == Color.Orange));
    }

    /// <summary>A quote's bar is as wide and in the color its looks say, and its blocks stand in as far as its indent says.</summary>
    [Fact]
    public void AQuoteTakesItsOwnBarAndIndent()
    {
        var easel = Laid("Before\n\n> Quoted", text =>
        {
            text.Base.QuoteBarColor = Color.Orange;
            text.Base.QuoteBarWidth = 6f;
            text.Base.QuoteIndent = 40f;
        });

        var bar = Assert.Single(easel.Sprites, s => s.Color == Color.Orange);
        Assert.Equal(6f * easel.Pixels, bar.Destination.Width, 1f);
        Assert.Equal(40f * easel.Pixels, Word(easel, "Quoted").Position.X - Word(easel, "Before").Position.X, 1f);
    }

    /// <summary>A heading stands as far below the block before it as the heading spacing says, and its rule as far below it as the heading rule spacing says.</summary>
    [Fact]
    public void AHeadingTakesItsOwnSpacing()
    {
        var spaced = Laid("Before\n\n# Title", text => text.Base.HeadingSpacing = 40f);
        var plain = Laid("Before\n\n# Title", _ => { });
        Assert.Equal((40f - 16f) * spaced.Pixels, Word(spaced, "Title").Position.Y - Word(plain, "Title").Position.Y, 1f);

        var ruled = Laid("# Title", text =>
        {
            text.Base.UnderlinedHeadingLevel = 1;
            text.Base.HeadingRuleSpacing = 12f;
        });
        var halfBlock = Laid("# Title", text => text.Base.UnderlinedHeadingLevel = 1);
        var rule = Assert.Single(ruled.Sprites, s => s.Color == Rule).Destination.Y;
        var near = Assert.Single(halfBlock.Sprites, s => s.Color == Rule).Destination.Y;
        Assert.Equal((12f - 4f) * ruled.Pixels, rule - near, 1f);
    }

    /// <summary>A table's columns stand as far apart as the table column spacing says, whatever the list indent.</summary>
    [Fact]
    public void ATablesColumnsTakeTheirOwnSpacing()
    {
        var easel = Laid("| | |\n|--|--|\n| a | b |", text => text.Base.TableColumnSpacing = 60f);

        Assert.Equal(easel.WidthOf("a") + 60f * easel.Pixels, Word(easel, "b").Position.X - Word(easel, "a").Position.X, 1f);
    }

    /// <summary>A callout's blocks stand inside it by its padding, whatever the list indent.</summary>
    [Fact]
    public void ACalloutTakesItsOwnPadding()
    {
        var easel = Laid("Before\n\n::: note\nInside\n:::", text =>
        {
            text.Base.Callouts["note"].Background = Color.Purple;
            text.Base.CalloutPadding = new Thickness(32f, 12f, 0f, 12f);
        });

        var flush = Laid("Before\n\n::: note\nInside\n:::", text =>
        {
            text.Base.Callouts["note"].Background = Color.Purple;
            text.Base.CalloutPadding = new Thickness(0f);
        });

        var background = Assert.Single(easel.Sprites, s => s.Color == Color.Purple).Destination;
        Assert.Equal(32f * easel.Pixels, Word(easel, "Inside").Position.X - Word(easel, "Before").Position.X, 1f);
        Assert.Equal(12f * easel.Pixels, Word(easel, "Inside").Position.Y - Word(flush, "Inside").Position.Y, 1f);
        Assert.Equal((20f + 24f) * easel.Pixels, background.Height, 1f);

    }

    static Easel Laid(string markdown, Action<RichText> styled)
    {
        var text = new RichText
        {
            Document = Markdown.Parse(markdown),
            Base = { FontSize = 20f, Heading1Size = 20f, Color = Body, RuleColor = Rule, BulletColor = Rule, QuoteBarColor = Rule, BlockSpacing = 8f, Indent = 20f },
            Placement = FixedSize.Width(Wide)
        };
        styled(text);

        return new Easel(new VStack { Children = { text } }, Wide, High);
    }

    static DrawCommand Word(Easel easel, string text) =>
        Assert.Single(easel.Written, command => command.Text?.Trim() == text);
}
