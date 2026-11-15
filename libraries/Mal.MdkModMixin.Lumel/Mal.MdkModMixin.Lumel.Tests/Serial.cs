using Xunit;

// Lumel works on the game's one thread, and its shared styles and themes keep what they work out without locks; the tests
// run one at a time, as the game does.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
