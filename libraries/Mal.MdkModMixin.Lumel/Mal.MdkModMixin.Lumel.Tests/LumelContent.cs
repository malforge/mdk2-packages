using System.IO;
using Lumel.Sim;

namespace Lumel.Tests;

/// <summary>The content the tests draw from: the game's own, and Lumel's, copied beside the tests.</summary>
static class LumelContent
{
    /// <summary>The content every test in the process draws from, read once, since it is the same files for every frame.</summary>
    public static readonly SimContent Instance = new SimContent().Mod(Path.Combine(AppContext.BaseDirectory, "Content", "Mods", "Lumel"));
}
