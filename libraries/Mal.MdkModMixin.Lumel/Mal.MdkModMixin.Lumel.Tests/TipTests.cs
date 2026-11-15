using Xunit;
using Lumel.Text;

namespace Lumel.Tests;

/// <summary>A tip: read once when it is made, kept while its words stay the same, and carrying the words for its slots.</summary>
public class TipTests
{
    /// <summary>Read from markdown-lite, it keeps the words it was read from and the document they make.</summary>
    [Fact]
    public void ItIsReadOnceWhenMade()
    {
        var tip = new Tip("Opens **this**");

        Assert.Equal("Opens **this**", tip.Text);
        Assert.NotNull(tip.Document);
        Assert.Null(tip.Arguments);
    }

    /// <summary>Asked for the same words again, the tip already saying them is kept, so nothing is read again.</summary>
    [Fact]
    public void TheSameWordsKeepTheSameTip()
    {
        var tip = Tip.For(null, "Switch off");

        Assert.Same(tip, Tip.For(tip, "Switch off"));
        Assert.NotSame(tip, Tip.For(tip, "Switch on"));
    }

    /// <summary>No words is no tip.</summary>
    [Fact]
    public void NoWordsIsNoTip()
    {
        Assert.Null(Tip.For(new Tip("Something"), null));
        Assert.Null(Tip.For(null, ""));
    }

    /// <summary>A template keeps the arguments its slots are said by, and the same template with them is kept too.</summary>
    [Fact]
    public void ATemplateCarriesItsArguments()
    {
        var named = new Arguments();
        named.Set("name", "Airlock");

        var tip = Tip.For(null, "Add another **{name}**", named);

        Assert.Same(named, tip!.Arguments);
        Assert.Same(tip, Tip.For(tip, "Add another **{name}**", named));
        Assert.NotSame(tip, Tip.For(tip, "Add another **{name}**", new Arguments()));
    }

    /// <summary>Built in code, it has a document and no words it was read from.</summary>
    [Fact]
    public void BuiltInCodeItHasNoText()
    {
        var tip = new Tip(Markdown.Parse("Built"));

        Assert.Null(tip.Text);
        Assert.NotNull(tip.Document);
    }
}
