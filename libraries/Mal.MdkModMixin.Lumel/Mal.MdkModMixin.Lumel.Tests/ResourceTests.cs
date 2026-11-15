using Lumel.Layout;
using Lumel.Screens;
using Lumel.Text;
using Lumel.Windows;
using VRageMath;
using Xunit;

namespace Lumel.Tests;

/// <summary>
///     Resources: found by walking up from an element to its workspace, the nearest first, under their type's full name or
///     a key of their own, through popups to their anchors and tips to their elements.
/// </summary>
public class ResourceTests
{
    static readonly RectangleF Area = new RectangleF(0f, 0f, 800f, 600f);

    /// <summary>An element in a window finds what its workspace holds, and a resource set lower down takes its place.</summary>
    [Fact]
    public void TheNearestResourceWins()
    {
        var label = new Label();
        var column = new VStack { Children = { label } };
        var window = new Window { Content = column };
        var workspace = new Workspace();
        var outer = new Codex();
        var inner = new Codex();
        workspace.Resources.Add(outer);
        workspace.Open(window);

        Assert.Same(outer, label.FindResource<Codex>());

        column.Resources.Add(inner);
        Assert.Same(inner, label.FindResource<Codex>());
    }

    /// <summary>Adding under a key that is taken replaces what was there, and a resource can be kept under a key of its own.</summary>
    [Fact]
    public void AKeyCanBeReplacedOrChosen()
    {
        var label = new Label();
        var workspace = new Workspace { BelowHud = label };
        var first = new Codex();
        var second = new Codex();
        var admin = new Codex();

        workspace.Resources.Add(first);
        workspace.Resources.Add(second);
        workspace.Resources.Add("Admin", admin);

        Assert.Same(second, label.FindResource<Codex>());
        Assert.Same(admin, label.FindResource<Codex>("Admin"));
    }

    /// <summary>A missing resource throws, unless asked for with TryFindResource, and one of the wrong kind throws either way.</summary>
    [Fact]
    public void AMissingOrWrongResourceIsAnError()
    {
        var label = new Label();
        var workspace = new Workspace { BelowHud = label };
        workspace.Resources.Add("Admin", "not a codex");

        Assert.Throws<InvalidOperationException>(() => label.FindResource<Codex>());
        Assert.Null(label.TryFindResource<Codex>());
        Assert.Throws<InvalidOperationException>(() => label.TryFindResource<Codex>("Admin"));
    }

    /// <summary>What a popup shows finds the resources above its anchor first, and what a tip shows those above its element.</summary>
    [Fact]
    public void PopupsAndTipsLookAboveWhatTheyHangFrom()
    {
        var anchor = new Label();
        var near = new Codex();
        var column = new VStack { Children = { anchor } };
        column.Resources.Add(near);
        var workspace = new Workspace();
        workspace.Resources.Add(new Codex());
        workspace.Open(new Window { Content = column });

        var inPopup = new Label();
        var popup = new Popup { Anchor = anchor, Content = inPopup };
        popup.Open();
        Assert.Same(near, inPopup.FindResource<Codex>());

        var tip = new Tip("Words.");
        ((ITipLifetime)tip).Open(anchor, Vector2.Zero);
        Assert.Same(near, tip.FindResource<Codex>());
    }

    /// <summary>CodexTip.For keeps the tip it is given only where that is a codex tip saying the same, and a plain tip saying it is replaced.</summary>
    [Fact]
    public void ForKeepsOnlyACodexTipSayingTheSame()
    {
        var same = new CodexTip("Words.");
        Tip plain = new Tip("Words.");

        Assert.Same(same, CodexTip.For(same, "Words."));
        Assert.NotSame(same, CodexTip.For(same, "Other words."));
        Assert.IsType<CodexTip>(CodexTip.For(plain, "Words."));
        Assert.Null(CodexTip.For(plain, ""));
    }

    /// <summary>A codex tip made with no codex finds the one above its element as it opens, and follows its links there.</summary>
    [Fact]
    public void ACodexTipFindsItsCodexAsItOpens()
    {
        var page = new Tip("Page b.");
        var codex = new Codex { FnResolvePage = id => id == "b" ? page : null };
        var tip = new CodexTip("See [b](b).") { Base = { ShowDelay = 0 } };
        var button = new Label { Text = "Vent", Tip = tip, Placement = FixedSize.Size(100f, 40f) };
        var workspace = new Workspace().With(new VStack { Children = { button } });
        workspace.Resources.Add(codex);
        workspace.DrawScreen(new Recording(Area), Area);

        workspace.Pointer = button.Bounds.Center;
        workspace.DrawScreen(new Recording(Area), Area);

        Assert.True(tip.IsOpen);
        Assert.Same(codex, tip.Codex);
        Assert.True(tip.Follow("b"));
        Assert.Same(page, tip.Page);
    }
}
