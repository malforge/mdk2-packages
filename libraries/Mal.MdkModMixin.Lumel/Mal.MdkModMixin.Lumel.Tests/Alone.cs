using Xunit;

namespace Lumel.Tests;

/// <summary>Tests that must not run alongside any other, because they count what the whole process does.</summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class Alone
{
    /// <summary>The name of the collection.</summary>
    public const string Name = "Alone";
}
