using Lumel.Text;
using Xunit;

namespace Lumel.Tests;

/// <summary>The matching of a search box.</summary>
public class TextSearchTests
{
    /// <summary>Each word is found anywhere, in the order typed and any case; no words find everything.</summary>
    [Theory]
    [InlineData("Airlock 10", "airl 1", true)]
    [InlineData("Airlock Door 1", "airl 1", true)]
    [InlineData("1 Airlock", "airl 1", false)]
    [InlineData("Airlock", "AIRLOCK", true)]
    [InlineData("Airlock", "  lock  ", true)]
    [InlineData("Airlock", "locks", false)]
    [InlineData("Iron Ore", "", true)]
    [InlineData("Iron Ore", "   ", true)]
    [InlineData("", "ore", false)]
    [InlineData(null, "ore", false)]
    public void WordsAreFoundInOrder(string? text, string search, bool found)
    {
        Assert.Equal(found, TextSearch.Matches(text!, search));
    }
}
