using Lumel.Layout;
using Lumel.Screens;
using Lumel.Styling;
using Lumel.Windows;
using VRageMath;
using Xunit;

namespace Lumel.Tests;

/// <summary>What the game's own theme gives windows a host shows with no theme of its own.</summary>
public class GameThemeTests
{
    static readonly RectangleF Area = new RectangleF(0f, 0f, 800f, 600f);

    /// <summary>A dialog casts the short shadow and leaves the world behind it unveiled.</summary>
    [Fact]
    public void ADialogCastsAShadowAndVeilsNothing()
    {
        var workspace = new Workspace().With(new VStack());
        var dialog = new Dialog { Content = new VStack() };
        workspace.Windows.Open(dialog);

        workspace.DrawScreen(new Recording(Area), Area);

        Assert.Same(GameTheme.ShortShadow, dialog.Shadow);
        Assert.Equal(Color.Transparent, dialog.Backdrop);
    }

    /// <summary>A popup casts the short shadow, closer under it than a window's.</summary>
    [Fact]
    public void APopupCastsTheShortShadow()
    {
        var popup = new Popup();
        popup.Restyle();

        Assert.Same(GameTheme.ShortShadow, popup.Shadow);
    }
}
