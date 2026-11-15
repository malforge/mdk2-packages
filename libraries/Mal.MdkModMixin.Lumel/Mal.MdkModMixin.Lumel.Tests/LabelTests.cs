using Lumel.Text;
using Xunit;

namespace Lumel.Tests;

/// <summary>What a label draws when its words do not fit.</summary>
public class LabelTests
{
    /// <summary>A label that stops wrapping and starts trimming at the same width draws one line, cut short.</summary>
    [Fact]
    public void ALabelThatStopsWrappingTrimsInstead()
    {
        var label = new Label { Text = "the quick brown fox jumps over the lazy dog", TextWrapping = TextWrapping.Wrap };
        var easel = new Easel(label, 120f, 400f);

        Assert.True(easel.Written.Count() > 1, "the words did not wrap to begin with");

        label.TextWrapping = TextWrapping.Trim;
        easel.Draw(120f, 400f);

        Assert.EndsWith("…", Assert.Single(easel.Written).Text);
    }
}
