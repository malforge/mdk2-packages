using Xunit;
using Lumel.Text;

namespace Lumel.Tests;

/// <summary>Text with named slots: read into a document once, or filled as plain text.</summary>
public class TemplateTests
{
    /// <summary>A slot becomes a run of its own, set apart as the words around it would be.</summary>
    [Fact]
    public void ASlotIsARunOfItsOwn()
    {
        var runs = Template.Parse("Made from **{name}** blueprint").Blocks[0].Runs;

        Assert.Equal("Made from ", runs[0].Text);
        Assert.Equal("name", runs[1].Slot);
        Assert.Equal(RunKind.Strong, runs[1].Kind);
        Assert.Equal(" blueprint", runs[2].Text);
    }

    /// <summary>Stars close around a slot with a letter hard against them, as they do around a word.</summary>
    [Fact]
    public void StarsCloseAroundASlotAsAroundAWord()
    {
        var runs = Template.Parse("**{name}**の設計図").Blocks[0].Runs;

        Assert.Equal("name", runs[0].Slot);
        Assert.Equal(RunKind.Strong, runs[0].Kind);
        Assert.Equal("の設計図", runs[1].Text);
        Assert.Equal(RunKind.Plain, runs[1].Kind);
    }

    /// <summary>Doubled braces are a brace, and braces around anything but a name are only text.</summary>
    [Fact]
    public void BracesThatAreNoSlotAreText()
    {
        var runs = Template.Parse("{{name}} and { spaced } and {1st}").Blocks[0].Runs;

        var run = Assert.Single(runs);
        Assert.Null(run.Slot);
        Assert.Equal("{name} and { spaced } and {1st}", run.Text);
    }

    /// <summary>Every slot of the name is filled, and a slot of another name is left alone.</summary>
    [Fact]
    public void ASlotIsFilledWhereverItIs()
    {
        Assert.Equal("Airlock and Airlock, {other}", Template.Fill("{name} and {name}, {other}", "name", "Airlock"));
    }

    /// <summary>Filling reads a template as parsing does: a doubled brace is a brace and no slot, and a value is put in as it is, never read for slots.</summary>
    [Fact]
    public void FillingReadsBracesAsParsingDoes()
    {
        Assert.Equal("{name} is Air{lock}", Template.Fill("{{name}} is {name}", "name", "Air{lock}"));
        Assert.Equal("{ spaced } and {1st}", Template.Fill("{ spaced } and {1st}", "spaced", "x"));
    }

    /// <summary>Parsing a template leaves every block whole, a slot inside a quote or a table split out like any other.</summary>
    [Fact]
    public void SlotsInsideHeldBlocksAreSplitOut()
    {
        var document = Template.Parse("> Open {name}\n\n| a |\n|--|\n| {count} |");

        Assert.Equal("name", document.Blocks[0].Blocks[0].Runs[1].Slot);
        Assert.Equal("count", document.Blocks[1].Blocks[1].Blocks[0].Runs[0].Slot);
    }

}
