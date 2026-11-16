# Mal.MdkModMixin.Lumel

> [!NOTE]
> **Version:** 1.0.0-beta.1  
> **Authors:** Malware  
> **Package:** `Mal.MdkModMixin.Lumel`
> 
> **Description:** A UI framework for Space Engineers mods: the controls, layout, text, drawing and screens an interface is built from.
>
> **[Installation Instructions →](./Mal.MdkModMixin.Lumel-Installation.md)** | **[Release Notes →](./Mal.MdkModMixin.Lumel-ReleaseNotes.md)**

---
# Lumel

A UI framework for Space Engineers mods. Windows on the game's screen that take the mouse, the keyboard and the
gamepad, HUD layers, and apps on LCD panels, all built from the same controls: labels, buttons, switches, sliders,
text boxes, drop-downs, menus, tabs, color pickers and so on, laid out in stacks and grids, and styled by themes.

It's what my own mods are built on.

## Quickstart

1. **Install the NuGet package** `Mal.MdkModMixin.Lumel` in your mod project. It comes from the Malforge feed on
   GitHub Packages, so if you haven't set that up yet,
   [do that first](https://github.com/malforge/mdk2-packages/blob/main/docs/Setting-Up-GitHub-Packages.md).
2. **Build a view.** A view is one object initializer, the way a XAML file is one tree.
3. **Give it a host:** a `Workspace` for the game's screen, or a `TextPanel` for an LCD.

### A window on the game's screen

```csharp
[MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
public class HelloSession : MySessionComponentBase
{
    Workspace _workspace;
    Window _hello;

    public override void BeforeStart()
    {
        _workspace = new Workspace("MyMod_Hello");
        _hello = new Window
        {
            Content = new VStack
            {
                Children =
                {
                    new Label { Text = "Hello" },
                    new Switch()
                }
            }
        };
    }

    public override void UpdateAfterSimulation()
    {
        if (MyAPIGateway.Input.IsNewKeyPressed(MyKeys.F8))
            _workspace.Open(_hello);
    }

    public override void Draw()
    {
        _workspace?.Draw();
    }

    protected override void UnloadData()
    {
        _workspace?.Dispose();
        _workspace = null;
    }
}
```

F8 opens the window, with the mouse free. Escape closes it. You never open or close the game's screen yourself: it
comes up with the first window and goes with the last.

`Draw` has to be called on _every_ frame the game draws, since what's drawn only lasts that one frame. And give the
workspace a name of your own mod's, the game knows the screen by it.

### An app on an LCD

```csharp
[MyTextSurfaceScript("MyMod_Hello", "Hello")]
public class HelloApp : MyTextSurfaceScriptBase
{
    readonly TextPanel _panel;

    public HelloApp(IMyTextSurface surface, IMyCubeBlock block, Vector2 size) : base(surface, block, size)
    {
        _panel = new TextPanel(surface)
        {
            Content = new VStack
            {
                Align = Align.Center,
                Children =
                {
                    new Label { Text = "Hello" },
                    new Switch { IsOn = true }
                }
            }
        };
    }

    public override ScriptUpdate NeedsUpdate => ScriptUpdate.Update10;

    public override void Run()
    {
        base.Run();
        _panel.Draw();
    }

    public override void Dispose()
    {
        base.Dispose();
        _panel.Dispose();
    }
}
```

Pick "Hello" in an LCD's script list. A few things to keep in mind:

- `IMyCubeBlock` has to be the in-game one (`using IMyCubeBlock = VRage.Game.ModAPI.Ingame.IMyCubeBlock;`), or the
  game can't create the app.
- The script id, `MyMod_Hello`, is stored on the screen. Make it your mod's own, and never change it.
- On a panel, one unit is one of its pixels.

Fair warning: LCD apps are display only. There's no input on text panels, so a button on an LCD looks like a button,
but nobody can press it. That _may_ come as an extension in the future.

## Content

Lumel brings a few pictures and materials of its own (the shadows and such). They're packed into your mod along with
the code, so there's nothing to copy.

## Testing

`Mal.MdkModMixin.Lumel.Sim` draws Lumel on a desktop, without the game running, so a test harness can take pictures of
your screens.

---

*Documentation auto-generated from package metadata. Last updated: 2026-11-16*
