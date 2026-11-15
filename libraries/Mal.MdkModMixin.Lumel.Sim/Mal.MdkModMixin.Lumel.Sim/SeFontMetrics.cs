using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Linq;
using VRageMath;

namespace Lumel.Sim
{
    /// <summary>
    /// The game's own text measurements, read from the bitmap font it actually draws with.
    /// </summary>
    /// <remarks>
    /// Worth being exact about, because the simulator is self-consistent: it measures with whatever it
    /// draws with, so a wrong font still looks right here and only goes wrong in the game. Measured against
    /// this font's real advances, a desktop face was coming out between nine and seventeen percent narrow,
    /// which is the difference between a row that fits and a row that runs off the panel.
    /// <para>
    /// The numbers are integers baked by BMFontGen from <c>space_engineers.ttf</c> at size 23, which is why
    /// they cannot be re-derived from the TrueType file: rounding at bake time and the game's own
    /// per-character spacing are both outside what the outline knows.
    /// </para>
    /// </remarks>
    public sealed class SeFontMetrics : IFontMetrics
    {
        /// <summary>The space added between two glyphs, in font units.</summary>
        /// <remarks>The game's <c>MyFont</c> constructor default, which every font definition takes.</remarks>
        public const int Spacing = 1;

        /// <summary>
        ///     The factor the game multiplies a scale by before it measures text, draws it or gets the height of a
        ///     line.
        /// </summary>
        /// <remarks>The game's <c>MyRenderGuiConstants.FONT_SCALE</c>.</remarks>
        public const float FontScale = 28.8f / 37f;

        /// <summary>How far below the top of its line the game draws a glyph's cell, in font units, before the glyph's own offsets.</summary>
        /// <remarks>The game's <c>MyRenderGuiConstants.FONT_TOP_SIDE_BEARING</c>.</remarks>
        public const float TopSideBearing = 3f * 23f / 18f;

        /// <summary>The reference height the game's HUD scaling is defined against.</summary>
        public const float GameReferenceHeight = 1080f;

        /// <summary>A line break, which is left alone because it is not a character the font is asked for.</summary>
        public const char Newline = (char)10;

        /// <summary>The character the game draws in place of one it does not have.</summary>
        public const char Replacement = '□';

        const int UsualGlyphHeight = 45;

        /// <summary>Where one glyph sits on the font page, and what it costs to draw.</summary>
        public struct Glyph
        {
            /// <summary>The X position where the cell starts on the font page, padding included.</summary>
            public int X;

            /// <summary>The Y position where the cell starts on the font page.</summary>
            public int Y;

            /// <summary>The width of the cell on the font page.</summary>
            public int Width;

            /// <summary>The height of the cell on the font page.</summary>
            public int Height;

            /// <summary>How far the pen moves after the glyph, before the spacing.</summary>
            public int Advance;

            /// <summary>
            ///     The font file's lsb: how far right of the pen the cell starts, and also how far down, since the game
            ///     adds it to both.
            /// </summary>
            public int Bearing;

            /// <summary>The font file's ho: how far down the cell is moved, or 0 if the file leaves it out.</summary>
            public int HeightOffset;
        }

        readonly Dictionary<char, Glyph> _glyphs = new Dictionary<char, Glyph>();
        readonly Dictionary<long, int> _kerning = new Dictionary<long, int>();
        readonly ScreenSpaceDevice _device;
        readonly int _lineHeight;

        int _usualHeight = UsualGlyphHeight;

        SeFontMetrics(ScreenSpaceDevice device, int lineHeight, string source, bool fromGame)
        {
            _device = device;
            _lineHeight = lineHeight;
            Source = source;
            FromGame = fromGame;
        }

        /// <summary>Gets the page the glyphs are on, as named by the font file.</summary>
        public string? PageFile { get; private set; }

        /// <summary>Whether the measurements are the game's; always true.</summary>
        public bool IsExact => true;

        /// <summary>
        ///     Gets where the numbers came from, for the line in the window that says how accurate they are.
        /// </summary>
        public string Source { get; private set; }

        /// <summary>Whether the numbers were read from an installed game rather than from the cache.</summary>
        public bool FromGame { get; private set; }

        /// <summary>Gets the height of a line in font units, which every advance is relative to.</summary>
        public int FontLineHeight => _lineHeight;

        /// <inheritdoc />
        public int LineUnits(string? font)
        {
            return _lineHeight;
        }

        /// <summary>
        ///     Returns how far below the top of its line the game draws the cell of <paramref name="glyph" />, in font
        ///     units.
        /// </summary>
        public static float CellTop(Glyph glyph)
        {
            return glyph.Bearing + glyph.HeightOffset + TopSideBearing;
        }

        /// <summary>
        ///     Gets the glyph the game draws for <paramref name="c" /> and returns true, or returns false if the game
        ///     draws nothing for it.
        /// </summary>
        /// <remarks>
        ///     The replacement glyph for a character the font lacks, unless that character is white space or a
        ///     control character, which the game skips outright.
        /// </remarks>
        public bool TryDrawn(char c, out Glyph glyph)
        {
            if (_glyphs.TryGetValue(c, out glyph))
                return true;

            return !char.IsWhiteSpace(c) && !char.IsControl(c) && _glyphs.TryGetValue(Replacement, out glyph);
        }

        /// <summary>Returns how much a kerned pair pulls together, in font units; 0 for most pairs.</summary>
        public int Kerning(char left, char right)
        {
            int adjust;
            return _kerning.TryGetValue(Key(left, right), out adjust) ? adjust : 0;
        }

        /// <summary>
        ///     Loads the font the game's white UI text uses, from the game or the cache, or returns null if neither has
        ///     it so that callers can fall back instead of failing.
        /// </summary>
        public static SeFontMetrics? Load(ScreenSpaceDevice device)
        {
            string path = GameContent.Resolve(Path.Combine("Fonts", "white", "FontDataPA.xml"));
            if (path == null)
                return null;

            return LoadFrom(device, path, GameContent.HasGame ? "the game" : "cache", GameContent.HasGame);
        }

        /// <summary>
        /// Reads any XNA bitmapfont, not only the game's.
        /// </summary>
        /// <remarks>
        /// Split out when this project started shipping a font of its own. The format is the same one
        /// either way - it is what the game reads - so a second reader would have been a second place for
        /// the same parsing to be subtly different.
        /// </remarks>
        public static SeFontMetrics? LoadFrom(ScreenSpaceDevice device, string? path, string source, bool live)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
                return null;

            try
            {
                XDocument document;
                using (var reader = new StreamReader(path, true))
                    document = XDocument.Load(reader);

                XElement font = document.Root;
                if (font == null)
                    return null;

                XNamespace ns = font.Name.Namespace;
                int lineHeight = (int?)font.Attribute("height") ?? 37;

                var metrics = new SeFontMetrics(device, lineHeight, source, live);

                XElement page = font.Descendants(ns + "bitmap").FirstOrDefault();
                metrics.PageFile = page == null ? null : (string)page.Attribute("name");

                foreach (XElement glyph in font.Descendants(ns + "glyph"))
                {
                    string? character = (string?)glyph.Attribute("ch");
                    int? advance = (int?)glyph.Attribute("aw");

                    Glyph cell;
                    if (string.IsNullOrEmpty(character) || !advance.HasValue || !ReadCell(glyph, advance.Value, out cell))
                        continue;

                    metrics._glyphs[character[0]] = cell;

                    // The game's MyFont.AverageFontLineOffset: the height of "a" less its height offset, and 45 where a
                    // font has no "a".
                    if (character[0] == 'a')
                        metrics._usualHeight = cell.Height - cell.HeightOffset;
                }

                if (metrics._glyphs.Count == 0)
                    return null;

                foreach (XElement pair in font.Descendants(ns + "kernpair"))
                {
                    string? left = (string?)pair.Attribute("left");
                    string? right = (string?)pair.Attribute("right");
                    int? adjust = (int?)pair.Attribute("adjust");

                    if (!string.IsNullOrEmpty(left) && !string.IsNullOrEmpty(right) && adjust.HasValue)
                        metrics._kerning[Key(left[0], right[0])] = adjust.Value;
                }

                return metrics;
            }
            catch (Exception error) when (error is IOException || error is XmlException)
            {
                return null;
            }
        }

        /// <summary>Returns what the game's MyFont.MeasureString returns, in pixels.</summary>
        /// <remarks>
        ///     The width is the widest line, with <see cref="Spacing" /> between glyphs and none after the last.
        ///     The height is a line per line break, plus half of how far any glyph stands above its line on
        ///     every line but the last, plus how far the last line's lowest glyph reaches below the font's
        ///     <c>a</c>.
        /// </remarks>
        public Vector2 MeasurePixels(string? font, string? text, float scale)
        {
            text = text ?? string.Empty;

            var lines = 1;
            var pen = 0;
            var widest = 0;
            var raised = 0;
            var raisedHere = 0;
            var below = 0;
            var previous = '\0';

            for (var i = 0; i < text.Length; i++)
            {
                var c = text[i];

                if (c == Newline)
                {
                    lines++;
                    pen = 0;
                    previous = '\0';
                    below = 0;
                    raised += raisedHere;
                    raisedHere = 0;
                    continue;
                }

                Glyph glyph;
                if (!TryDrawn(c, out glyph))
                    continue;

                var above = glyph.Height + glyph.HeightOffset - _lineHeight;
                if (above > raisedHere)
                    raisedHere = above;

                var under = glyph.Height - glyph.HeightOffset - _usualHeight;
                if (under > below)
                    below = under;

                pen += Kerning(previous, c) + glyph.Advance;
                previous = c;

                if (i < text.Length - 1)
                    pen += Spacing;

                if (pen > widest)
                    widest = pen;
            }

            var unit = DrawScale(scale);
            return new Vector2(widest * unit, (lines * _lineHeight + raised / 2f + below) * unit);
        }

        /// <summary>
        ///     Returns what the game's screen-space GetLineHeight returns, in pixels: the height of the line it draws.
        /// </summary>
        public float LineHeightPixels(string? font, float scale)
        {
            return _lineHeight * DrawScale(scale);
        }

        /// <summary>
        ///     Returns <paramref name="text" /> as the game shows it, with every character the game would not draw
        ///     replaced by the replacement character.
        /// </summary>
        /// <remarks>For the desktop face drawn where the page is missing, which has glyphs the game does not.</remarks>
        public string AsDrawn(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            char[]? copy = null;

            for (var i = 0; i < text.Length; i++)
            {
                var c = text[i];
                if (c == Newline || _glyphs.ContainsKey(c) || char.IsWhiteSpace(c) || char.IsControl(c))
                    continue;

                if (copy == null)
                    copy = text.ToCharArray();

                copy[i] = Replacement;
            }

            return copy == null ? text : new string(copy);
        }

        /// <summary>
        ///     Returns the pixels per font unit in the game's line-height arithmetic, including its HUD scaling.
        /// </summary>
        public float EffectiveScale(float scale)
        {
            var height = _device.SafeArea.Height;
            var hud = height > 0f ? height / GameReferenceHeight : 1f;
            return scale * hud;
        }

        /// <summary>
        ///     Returns the pixels per font unit where the game measures and draws, which is
        ///     <see cref="EffectiveScale" /> times <see cref="FontScale" />.
        /// </summary>
        public float DrawScale(float scale)
        {
            return EffectiveScale(scale) * FontScale;
        }

        /// <summary>
        ///     Reads one glyph's place on the page, returning false for anything malformed, which is treated as the
        ///     font not having that character rather than as a reason to abandon the file.
        /// </summary>
        static bool ReadCell(XElement glyph, int advance, out Glyph cell)
        {
            cell = new Glyph();

            // The game reads "loc" and falls back to the older "origin".
            int[]? origin = Pair((string)glyph.Attribute("loc") ?? (string)glyph.Attribute("origin"), ',');
            int[]? size = Pair((string)glyph.Attribute("size"), 'x');

            if (origin == null || size == null)
                return false;

            cell.X = origin[0];
            cell.Y = origin[1];
            cell.Width = size[0];
            cell.Height = size[1];
            cell.Advance = advance;
            cell.Bearing = (int?)glyph.Attribute("lsb") ?? 0;
            cell.HeightOffset = (int?)glyph.Attribute("ho") ?? 0;
            return true;
        }

        /// <summary>
        ///     Parses two numbers separated by <paramref name="between" />, which is how this file writes both.
        /// </summary>
        static int[]? Pair(string? text, char between)
        {
            if (string.IsNullOrEmpty(text))
                return null;

            int at = text.IndexOf(between);
            if (at <= 0)
                return null;

            int first;
            int second;

            if (!int.TryParse(text.Substring(0, at), out first))
                return null;

            if (!int.TryParse(text.Substring(at + 1), out second))
                return null;

            return new[] { first, second };
        }

        static long Key(char left, char right)
        {
            return ((long)left << 16) | right;
        }
    }
}
