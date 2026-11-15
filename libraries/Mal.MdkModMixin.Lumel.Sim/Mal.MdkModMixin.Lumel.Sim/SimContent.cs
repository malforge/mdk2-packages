using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Xml;
using System.Xml.Linq;
using Lumel.Drawing;
using Lumel.Images;
using Color = VRageMath.Color;

namespace Lumel.Sim
{
    /// <summary>
    ///     The content the simulated screen draws from: the game's content and the content of every mod registered with
    ///     <see cref="Mod" />.
    /// </summary>
    /// <remarks>
    ///     The definitions are read the first time something is asked of them and kept, so mods are registered before the
    ///     first frame is drawn.
    /// </remarks>
    public sealed class SimContent
    {
        const string ObjectBuilder = "MyObjectBuilder_";

        readonly List<string> _mods = new List<string>();
        readonly Dictionary<string, Action<DrawingContext, Rect, Color>> _drawn =
            new Dictionary<string, Action<DrawingContext, Rect, Color>>(StringComparer.Ordinal);

        readonly List<Func<string, string?>> _pictures = new List<Func<string, string?>>();
        Func<IImageCatalog, IImageCatalog> _images = images => images;
        readonly Dictionary<string, string?> _resolved = new Dictionary<string, string?>(StringComparer.Ordinal);
        readonly HashSet<string> _blocks = new HashSet<string>(StringComparer.Ordinal);

        Dictionary<string, string>? _gameSprites;
        Dictionary<string, string>? _modSprites;
        Dictionary<string, string>? _gameIcons;
        Dictionary<string, string>? _modIcons;
        List<SimFont>? _fonts;

        /// <summary>
        ///     Adds the mod in <paramref name="folder" />, which is any folder holding a Data folder: a mod's Content
        ///     folder in its source, or the mod as the game has it.
        /// </summary>
        /// <remarks>Its transparent materials, LCD textures, block definitions and fonts are read from <c>Data</c>, and every path in them from <paramref name="folder" />.</remarks>
        public SimContent Mod(string folder)
        {
            _mods.Add(Path.GetFullPath(folder));
            return this;
        }

        /// <summary>
        ///     Draws <paramref name="material" /> with <paramref name="draw" /> instead of from a picture;
        ///     <paramref name="draw" /> receives the drawing context, the box and the tint.
        /// </summary>
        public SimContent Draws(string material, Action<DrawingContext, Rect, Color> draw)
        {
            _drawn[material] = draw;
            return this;
        }

        /// <summary>
        ///     Registers <paramref name="picture" /> to look up where under the game's content the block picture of a
        ///     material is when no definition names one; it returns null for a material it does not know.
        /// </summary>
        public SimContent Pictures(Func<string, string?> picture)
        {
            _pictures.Add(picture);
            return this;
        }

        /// <summary>
        ///     Wraps the catalog a screen finds its pictures in with <paramref name="images" />, for a mod that has
        ///     pictures of its own to name.
        /// </summary>
        public SimContent Images(Func<IImageCatalog, IImageCatalog> images)
        {
            _images = images;
            return this;
        }

        /// <summary>Returns the catalog that pictures on <paramref name="screen" /> are found in.</summary>
        public IImageCatalog ImagesFor(IScreenSpace screen)
        {
            return _images(new MaterialImages(screen.TextureSize));
        }

        /// <summary>Gets every font the registered mods declare in their Data/Fonts*.sbc.</summary>
        public IReadOnlyList<SimFont> Fonts
        {
            get
            {
                if (_fonts == null)
                    _fonts = ReadFonts();

                return _fonts;
            }
        }

        /// <summary>
        ///     Gets how <paramref name="material" /> is drawn by code and returns true, or returns false if it is not
        ///     drawn by code.
        /// </summary>
        public bool TryDrawn(string material, out Action<DrawingContext, Rect, Color> draw)
        {
            return _drawn.TryGetValue(material, out draw);
        }

        /// <summary>
        ///     Returns a picture file to draw for <paramref name="material" />, decoded if necessary, or null if
        ///     nothing names one; <paramref name="block" /> is true if it is a block's picture, which the game draws as
        ///     it is rather than tinted.
        /// </summary>
        /// <remarks>
        ///     In order: a block's own icon for a definition id, <c>MyObjectBuilder_Door/Subtype</c>, from the game's block
        ///     definitions and the mods'; a picture <see cref="Pictures" /> names; a transparent material or LCD texture a
        ///     registered mod defines; one the game defines. Each answer is kept, a miss included.
        /// </remarks>
        public string? PictureFor(string material, out bool block)
        {
            string? found;
            if (!_resolved.TryGetValue(material, out found))
            {
                found = Find(material);
                _resolved.Add(material, found);
            }

            block = _blocks.Contains(material);
            return found;
        }

        string? Find(string material)
        {
            if (material.IndexOf('/') >= 0)
            {
                var icon = Icon(material);
                if (icon != null)
                {
                    _blocks.Add(material);
                    return icon;
                }
            }

            foreach (var picture in _pictures)
            {
                var relative = picture(material);
                if (relative == null)
                    continue;

                var resolved = GameImages.TryResolve(relative);
                if (resolved != null)
                {
                    _blocks.Add(material);
                    return resolved;
                }
            }

            if (_modSprites == null)
            {
                _modSprites = new Dictionary<string, string>(StringComparer.Ordinal);
                foreach (var mod in _mods)
                {
                    Sprites(mod, true, _modSprites);
                    Materials(mod, true, _modSprites);
                }
            }

            string file;
            if (_modSprites.TryGetValue(material, out file))
                return Picture(file);

            if (_gameSprites == null)
            {
                _gameSprites = new Dictionary<string, string>(StringComparer.Ordinal);
                var content = GameContent.ContentFolder;
                if (content != null)
                {
                    Sprites(content, false, _gameSprites);
                    Materials(content, false, _gameSprites);
                }
            }

            return _gameSprites.TryGetValue(material, out file) ? Picture(file) : null;
        }

        /// <summary>
        ///     Returns the icon of the block <paramref name="id" /> (MyObjectBuilder_Type/Subtype), or null if no
        ///     definition gives one.
        /// </summary>
        string? Icon(string id)
        {
            if (_modIcons == null)
            {
                _modIcons = new Dictionary<string, string>(StringComparer.Ordinal);
                foreach (var mod in _mods)
                {
                    var data = Path.Combine(mod, "Data");
                    if (Directory.Exists(data))
                        Icons(Directory.GetFiles(data, "*.sbc", SearchOption.AllDirectories), mod, true, _modIcons);
                }
            }

            string file;
            if (_modIcons.TryGetValue(id, out file))
                return Picture(file);

            if (_gameIcons == null)
            {
                _gameIcons = new Dictionary<string, string>(StringComparer.Ordinal);
                var content = GameContent.ContentFolder;
                var root = content ?? GameContent.CacheDirectory;
                var folder = Path.Combine(root, "Data", "CubeBlocks");
                if (Directory.Exists(folder))
                    Icons(Directory.GetFiles(folder, "*.sbc"), root, false, _gameIcons);
            }

            return _gameIcons.TryGetValue(id, out file) ? Picture(file) : null;
        }

        /// <summary>
        ///     Returns a picture file for <paramref name="file" />: an absolute path for a mod's own file, or a path
        ///     under the game's content for the game's. A DDS is decoded to a cached PNG; anything else is drawn as it
        ///     is.
        /// </summary>
        static string? Picture(string file)
        {
            if (Path.IsPathRooted(file))
            {
                if (!File.Exists(file))
                    return null;

                if (!file.EndsWith(".dds", StringComparison.OrdinalIgnoreCase))
                    return file;

                return GameImages.ResolveFile(file, Path.Combine("Mods", Cached(file)));
            }

            if (!file.EndsWith(".dds", StringComparison.OrdinalIgnoreCase))
            {
                var content = GameContent.ContentFolder;
                var direct = content == null ? null : Path.Combine(content, file);
                return direct != null && File.Exists(direct) ? direct : null;
            }

            return GameImages.TryResolve(file);
        }

        /// <summary>
        ///     Returns where the decoded copy of a mod's own picture <paramref name="file" /> is kept under the cache,
        ///     by its folder and name.
        /// </summary>
        static string Cached(string file)
        {
            var folder = Path.GetDirectoryName(file) ?? "";
            return Path.GetFileName(folder) + "-" + ((uint)StringComparer.OrdinalIgnoreCase.GetHashCode(folder)).ToString("x8") + "/" + Path.GetFileName(file);
        }

        /// <summary>
        ///     Adds the sprite that each Data/LCDTextures*.sbc under <paramref name="root" /> names to
        ///     <paramref name="into" />, as an absolute path if <paramref name="absolute" /> is true.
        /// </summary>
        static void Sprites(string root, bool absolute, Dictionary<string, string> into)
        {
            foreach (var definition in Definitions(root, "LCDTextures*.sbc", "LCDTextureDefinition"))
            {
                var id = definition.Element("Id");
                var subtype = (string)id?.Attribute("Subtype") ?? (string)id?.Element("SubtypeId");
                var picture = (string)definition.Element("SpritePath") ?? (string)definition.Element("TexturePath");
                if (!string.IsNullOrEmpty(subtype) && !string.IsNullOrEmpty(picture))
                    into[subtype] = Placed(root, picture, absolute);
            }
        }

        /// <summary>
        ///     Adds the picture of each transparent material that a Data/TransparentMaterials*.sbc under
        ///     <paramref name="root" /> declares to <paramref name="into" />.
        /// </summary>
        static void Materials(string root, bool absolute, Dictionary<string, string> into)
        {
            foreach (var definition in Definitions(root, "*TransparentMaterials*.sbc", "TransparentMaterial"))
            {
                var subtype = (string)definition.Element("Id")?.Element("SubtypeId");
                var picture = (string)definition.Element("Texture");
                if (!string.IsNullOrEmpty(subtype) && !string.IsNullOrEmpty(picture))
                    into[subtype] = Placed(root, picture, absolute);
            }
        }

        /// <summary>
        ///     Adds the icon of every block definition in <paramref name="files" /> to <paramref name="into" />, with
        ///     its paths under <paramref name="root" />.
        /// </summary>
        static void Icons(IEnumerable<string> files, string root, bool absolute, Dictionary<string, string> into)
        {
            foreach (var file in files)
            {
                foreach (var definition in Read(file, "Definition"))
                {
                    var id = definition.Element("Id");
                    var icon = (string)definition.Element("Icon");
                    if (id == null || string.IsNullOrEmpty(icon))
                        continue;

                    var type = (string)id.Element("TypeId");
                    if (string.IsNullOrEmpty(type))
                        continue;

                    if (!type.StartsWith(ObjectBuilder, StringComparison.Ordinal))
                        type = ObjectBuilder + type;

                    into[type + "/" + ((string)id.Element("SubtypeId") ?? "")] = Placed(root, icon, absolute);
                }
            }
        }

        /// <summary>
        ///     Returns where <paramref name="picture" /> is: under <paramref name="root" /> if
        ///     <paramref name="absolute" /> is true and the file is there, otherwise under the game's content, where
        ///     the game looks for a mod's picture that its own folder lacks.
        /// </summary>
        static string Placed(string root, string picture, bool absolute)
        {
            var relative = picture.Replace('\\', '/');
            if (!absolute)
                return relative;

            var own = Path.Combine(root, relative);
            return File.Exists(own) ? own : relative;
        }

        /// <summary>
        ///     Returns every element called <paramref name="element" /> in the files matching
        ///     <paramref name="pattern" /> in the Data folder of <paramref name="root" />.
        /// </summary>
        static IEnumerable<XElement> Definitions(string root, string pattern, string element)
        {
            var data = Path.Combine(root, "Data");
            if (!Directory.Exists(data))
                yield break;

            foreach (var file in Directory.GetFiles(data, pattern))
            {
                foreach (var found in Read(file, element))
                    yield return found;
            }
        }

        static IEnumerable<XElement> Read(string file, string element)
        {
            XDocument document;
            try
            {
                document = XDocument.Load(file);
            }
            catch (Exception error) when (error is IOException || error is XmlException)
            {
                yield break;
            }

            foreach (var found in document.Descendants(element))
                yield return found;
        }

        List<SimFont> ReadFonts()
        {
            var fonts = new List<SimFont>();
            foreach (var mod in _mods)
            {
                foreach (var font in Definitions(mod, "Fonts*.sbc", "Font"))
                {
                    var name = (string)font.Element("Id")?.Element("SubtypeId");
                    var table = (string)font.Element("Path");
                    if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(table))
                        fonts.Add(new SimFont(name, Path.Combine(mod, table.Replace('\\', '/'))));
                }
            }

            return fonts;
        }
    }
}
