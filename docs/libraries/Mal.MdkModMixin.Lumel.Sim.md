# Mal.MdkModMixin.Lumel.Sim

> [!NOTE]
> **Version:** 1.0.0-beta.1  
> **Authors:** Malware  
> **Package:** `Mal.MdkModMixin.Lumel.Sim`
> 
> **Description:** The game's screen drawn on a desktop, for the test harness of a mod built on Lumel: what the mod draws, without the game running.
>
> **[Installation Instructions →](./Mal.MdkModMixin.Lumel.Sim-Installation.md)** | **[Release Notes →](./Mal.MdkModMixin.Lumel.Sim-ReleaseNotes.md)**

---
# Lumel.Sim

The game's screen drawn on a desktop, for testing a mod built on Lumel (`Mal.MdkModMixin.Lumel`). It takes pictures
of your windows and LCD apps without the game running, with the game's own fonts.

This is for a test harness or a test project, _never_ for the mod itself.

## Quickstart

1. **Make a desktop project** for your harness: .NET Framework 4.8, x64, WPF.
2. **Install the NuGet packages** `Mal.MdkModMixin.Lumel.Sim` and `DirectXTexNet`, and reference the game's
   assemblies (`Mal.Mdk2.References` does that). Lumel itself comes from either your mod project or the
   `Mal.MdkModMixin.Lumel` package.
3. **Take a picture.**

```csharp
[STAThread]
static void Main()
{
    var content = new SimContent().Mod(@"path\to\Lumel's Content");
    var workspace = new Workspace();
    workspace.Open(new Window { Content = new Label { Text = "Hello" } });
    SimShot.Save("hello.png", 1920, 1080, content, workspace);
}
```

`Mod` takes any folder that holds a Data folder: your mod's Content folder in its source, or the mod as the game has
it. Register Lumel's own too, or its shadows are missing. With `GeneratePathProperty="true"` on the Lumel package
reference, it's at `$(PkgMal_MdkModMixin_Lumel)\contentFiles\cs\any\Shared\Content`.

Rendering needs a single-threaded apartment, hence the `[STAThread]`.

## The game's content

The fonts and the game's own pictures are read from the game's install, found through your MDK settings or Steam. It also
keeps a copy of the little it needs in `%LocalAppData%\Lumel\SimContent`, and on a machine without the game (a build
server, say) it reads that instead. So copy that folder over from a machine that has the game.

---

*Documentation auto-generated from package metadata. Last updated: 2026-11-24*
