using Lumel.Layout;
using Lumel.Screens;
using Lumel.Styling;
using Lumel.Windows;

namespace Lumel.Tests;

/// <summary>Puts the elements a test works with on a workspace, in a window of their own that adds nothing around them.</summary>
static class Showing
{
    /// <summary>A window with no padding, border, backdrop or shadow, so what it holds is laid out as if on its own.</summary>
    public static readonly IStyle<Window> Bare = new WindowStyle();

    /// <summary>
    ///     Opens <paramref name="content" /> on <paramref name="workspace" /> in a bare window, layered as given where
    ///     there are several, and returns the workspace.
    /// </summary>
    public static Workspace With(this Workspace workspace, params Element[] content)
    {
        workspace.Open(Window(content));
        return workspace;
    }

    /// <summary>Returns a bare window holding <paramref name="content" />, layered as given where there are several.</summary>
    public static Window Window(params Element[] content)
    {
        if (content.Length == 1)
            return new Window { Style = Bare, Content = content[0] };

        var layers = new ZStack();
        foreach (var element in content)
            layers.Children.Add(element);

        return new Window { Style = Bare, Content = layers };
    }
}
