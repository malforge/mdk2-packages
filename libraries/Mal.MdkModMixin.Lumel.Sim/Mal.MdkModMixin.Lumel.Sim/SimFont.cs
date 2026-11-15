using System.IO;

namespace Lumel.Sim
{
    /// <summary>A font a mod declares: the name a screen draws in, and its bitmap font table.</summary>
    public sealed class SimFont
    {
        /// <summary>
        ///     Creates a font named <paramref name="name" /> with its table at <paramref name="table" />.
        /// </summary>
        public SimFont(string name, string table)
        {
            Name = name;
            Table = table;
        }

        /// <summary>Gets the subtype a screen names the font by.</summary>
        public string Name { get; }

        /// <summary>Gets the bitmap font's XML table, which names its page.</summary>
        public string Table { get; }

        /// <summary>Gets the folder the table is in, where the pages it names are.</summary>
        public string Folder => Path.GetDirectoryName(Table)!; // the table is a path to a file, so it has a folder

        /// <summary>
        ///     Returns the font's measurements read for <paramref name="device" />, or null if the table cannot be
        ///     read.
        /// </summary>
        public SeFontMetrics? Metrics(ScreenSpaceDevice device)
        {
            return SeFontMetrics.LoadFrom(device, Table, "the mod's own content", true);
        }
    }
}
