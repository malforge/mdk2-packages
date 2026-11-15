using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Lumel.Sim
{
    /// <summary>
    ///     Locates the game's content on a machine that has it, and keeps a copy of the little the simulator draws from
    ///     for a machine that does not.
    /// </summary>
    /// <remarks>
    /// Everything the simulator needs from Space Engineers is small, static and derived from files rather
    /// than from a running game, so it is fetched once where the game is and read anywhere afterwards.
    /// <para>
    /// The game is found the way MDK finds it: the binary path in the MDK project's local ini, then in its ini, then the
    /// binary path in MDK's own settings, and otherwise the game's Steam install. The harness names its MDK project with
    /// assembly metadata under <see cref="ProjectKey" />.
    /// </para>
    /// <para>
    /// The cache lives outside any repository: it is Keen's content, which no mod may redistribute.
    /// </para>
    /// </remarks>
    public static class GameContent
    {
        /// <summary>
        ///     The name of the setting that, when pointed at a Content folder, overrides every other lookup.
        /// </summary>
        public const string OverrideVariable = "LUMEL_SE_CONTENT";

        /// <summary>The assembly metadata key naming the MDK project file whose ini says where the game is.</summary>
        public const string ProjectKey = "Lumel.MdkProject";

        /// <summary>
        ///     Gets the directory where fetched files are kept, so that a machine without the game still has them.
        /// </summary>
        public static string CacheDirectory
        {
            get
            {
                string root = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                return Path.Combine(root, "Lumel", "SimContent");
            }
        }

        /// <summary>
        ///     Returns a path to <paramref name="relativePath" />, taking it from the game and caching it if the game
        ///     is present, or from the cache otherwise; returns null if neither has it.
        /// </summary>
        public static string? Resolve(string relativePath)
        {
            string cached = Path.Combine(CacheDirectory, relativePath);

            string content = FindContentFolder();
            if (content != null)
            {
                string live = Path.Combine(content, relativePath);
                if (File.Exists(live))
                {
                    TryCache(live, cached);
                    return live;
                }
            }

            return File.Exists(cached) ? cached : null;
        }

        /// <summary>Whether content is read from the game itself rather than from the cache.</summary>
        public static bool HasGame => FindContentFolder() != null;

        /// <summary>
        ///     Gets the folder the game's content was found in, or null on a machine that does not have the game.
        /// </summary>
        public static string? ContentFolder => FindContentFolder();

        // Searched once for the process: where the game is installed does not change while it runs, and the search reads
        // every Steam library on the machine.
        static string? _contentFolder;
        static bool _searched;

        static string? FindContentFolder()
        {
            if (_searched)
                return _contentFolder;

            _searched = true;
            _contentFolder = Search();
            return _contentFolder;
        }

        static string? Search()
        {
            string overridden = Environment.GetEnvironmentVariable(OverrideVariable);
            if (!string.IsNullOrEmpty(overridden) && Directory.Exists(overridden))
                return overridden;

            var binaries = FromProject() ?? FromMdkSettings();
            if (binaries != null)
            {
                // The game's Content folder stands beside its Bin64.
                var content = Path.GetFullPath(Path.Combine(binaries, "..", "Content"));
                if (Directory.Exists(content))
                    return content;
            }

            foreach (string library in SteamLibraries())
            {
                // The Mod SDK carries the same fonts and is a smaller install, so it is worth accepting
                // either. Whichever is present first is as good as the other for what is taken from it.
                string[] candidates =
                {
                    Path.Combine(library, "steamapps", "common", "SpaceEngineers", "Content"),
                    Path.Combine(library, "steamapps", "common", "SpaceEngineersModSDK", "OriginalContent")
                };

                foreach (string candidate in candidates)
                    if (Directory.Exists(candidate))
                        return candidate;
            }

            return null;
        }

        /// <summary>
        ///     Returns the binary path that the harness's MDK project names in its local ini or its ini, or null if
        ///     neither names one.
        /// </summary>
        /// <remarks>Each ini is looked for as MDK looks: mdk.local.ini or mdk.ini beside the project, then named for the project.</remarks>
        static string? FromProject()
        {
            var project = typeof(GameContent).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
                .FirstOrDefault(entry => entry.Key == ProjectKey)?.Value;
            if (string.IsNullOrEmpty(project))
                return null;

            var folder = Path.GetDirectoryName(project);
            if (string.IsNullOrEmpty(folder))
                return null;

            return BinaryPath(Path.Combine(folder, "mdk.local.ini"))
                   ?? BinaryPath(Path.ChangeExtension(project, ".mdk.local.ini"))
                   ?? BinaryPath(Path.Combine(folder, "mdk.ini"))
                   ?? BinaryPath(Path.ChangeExtension(project, ".mdk.ini"));
        }

        /// <summary>
        ///     Returns the binarypath from the [mdk] section of the ini at <paramref name="ini" />, or null if it is
        ///     missing, empty or auto.
        /// </summary>
        static string? BinaryPath(string ini)
        {
            if (!File.Exists(ini))
                return null;

            var section = "";
            foreach (var raw in File.ReadAllLines(ini))
            {
                var line = raw.Trim();
                if (line.Length == 0 || line[0] == ';')
                    continue;

                if (line[0] == '[')
                {
                    section = line.Trim('[', ']').Trim();
                    continue;
                }

                var equals = line.IndexOf('=');
                if (equals < 0 || !string.Equals(section, "mdk", StringComparison.OrdinalIgnoreCase))
                    continue;

                if (!string.Equals(line.Substring(0, equals).Trim(), "binarypath", StringComparison.OrdinalIgnoreCase))
                    continue;

                return UnlessAuto(line.Substring(equals + 1));
            }

            return null;
        }

        /// <summary>Returns the binary path from MDK's own settings, or null if they name none or say auto.</summary>
        /// <remarks>Read from <c>CustomAutoBinaryPath</c> in <c>%AppData%\MDK2\settings.json</c>, the setting MDK's hub writes.</remarks>
        static string? FromMdkSettings()
        {
            var settings = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MDK2", "settings.json");
            if (!File.Exists(settings))
                return null;

            var json = File.ReadAllText(settings);
            var key = json.IndexOf("\"CustomAutoBinaryPath\"", StringComparison.Ordinal);
            var colon = key < 0 ? -1 : json.IndexOf(':', key);
            var open = colon < 0 ? -1 : json.IndexOf('"', colon + 1);
            var close = open < 0 ? -1 : json.IndexOf('"', open + 1);
            if (close < 0)
                return null;

            return UnlessAuto(json.Substring(open + 1, close - open - 1).Replace("\\\\", "\\"));
        }

        /// <summary>Returns <paramref name="value" /> as a path, or null if it is empty or auto.</summary>
        static string? UnlessAuto(string value)
        {
            var path = value.Trim();
            return path.Length == 0 || string.Equals(path, "auto", StringComparison.OrdinalIgnoreCase) ? null : path;
        }

        static IEnumerable<string> SteamLibraries()
        {
            var roots = new List<string>();

            foreach (Environment.SpecialFolder folder in new[]
                     {
                         Environment.SpecialFolder.ProgramFilesX86,
                         Environment.SpecialFolder.ProgramFiles
                     })
            {
                string steam = Path.Combine(Environment.GetFolderPath(folder), "Steam");
                if (Directory.Exists(steam))
                    roots.Add(steam);
            }

            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (string steam in roots)
            {
                if (seen.Add(steam))
                    yield return steam;

                // libraryfolders.vdf lists the other drives Steam was told about. Read as plain text
                // rather than parsed: only the quoted paths are wanted from it.
                string manifest = Path.Combine(steam, "steamapps", "libraryfolders.vdf");
                if (!File.Exists(manifest))
                    continue;

                string[] lines;
                try
                {
                    lines = File.ReadAllLines(manifest);
                }
                catch (IOException)
                {
                    continue;
                }

                foreach (string line in lines)
                {
                    int marker = line.IndexOf("\"path\"", StringComparison.OrdinalIgnoreCase);
                    if (marker < 0)
                        continue;

                    int open = line.IndexOf('"', marker + 6);
                    int close = open < 0 ? -1 : line.IndexOf('"', open + 1);
                    if (open < 0 || close < 0)
                        continue;

                    string path = line.Substring(open + 1, close - open - 1).Replace("\\\\", "\\");
                    if (Directory.Exists(path) && seen.Add(path))
                        yield return path;
                }
            }
        }

        static void TryCache(string source, string destination)
        {
            try
            {
                if (File.Exists(destination)
                    && File.GetLastWriteTimeUtc(destination) >= File.GetLastWriteTimeUtc(source))
                    return;

                string folder = Path.GetDirectoryName(destination);
                if (folder != null)
                    Directory.CreateDirectory(folder);

                File.Copy(source, destination, true);
            }
            catch (IOException)
            {
                // Caching is a convenience for another machine, never a requirement for this one.
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }
}
