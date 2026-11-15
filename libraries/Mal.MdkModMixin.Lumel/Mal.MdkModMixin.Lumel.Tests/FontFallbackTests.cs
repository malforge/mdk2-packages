using Lumel.Layout;
using Lumel.Text;
using Xunit;

namespace Lumel.Tests;

/// <summary>Words in a font the game has not loaded, drawn in the game's own face instead.</summary>
public class FontFallbackTests
{
    /// <summary>A font the game has is drawn in as it is named.</summary>
    [Fact]
    public void ALoadedFontIsDrawnIn()
    {
        var easel = new Easel(new VStack { Children = { new Label { Base = { Font = "SpaceEngineersBold" }, Text = "Strong" } } }, 400f, 100f);

        easel.Draw(400f, 100f);

        Assert.Equal("SpaceEngineersBold", easel.Written.Single().Font);
    }

    /// <summary>A font the game has not loaded, such as one whose mod is missing, is drawn in the game's own face.</summary>
    [Fact]
    public void AMissingFontFallsBackToTheGamesFace()
    {
        var column = new VStack();
        var easel = new Easel(column, 400f, 100f);
        easel.MissingFonts.Add("SpaceEngineersBold");
        column.Children.Add(new Label { Base = { Font = "SpaceEngineersBold" }, Text = "Strong" });

        easel.Draw(400f, 100f);

        Assert.Equal("White", easel.Written.Single().Font);
    }
}
