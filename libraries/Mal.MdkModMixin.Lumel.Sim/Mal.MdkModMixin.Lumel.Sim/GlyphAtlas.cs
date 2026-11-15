using System;
using System.IO;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Lumel.Sim
{
    /// <summary>The game's own glyph images, ready to be copied one glyph at a time.</summary>
    /// <remarks>
    /// The last approximation in the drawing path. Widths were already the game's own, taken from the
    /// font file, and only the shapes were a desktop face stretched sideways onto them - which was the
    /// right trade while it lasted, since a wrong width is invisible here and expensive in the game while
    /// a wrong shape is merely ugly. With the page decoded there is no trade left to make.
    /// <para>
    /// The page stores coverage in the alpha channel and a color in the rest, and the game draws a glyph in the
    /// page's color multiplied by the text's: white for nearly every glyph, so the text's color as it is, and
    /// darker where a glyph is drawn darker, such as the chip behind the installation's mark. Tinted copies are
    /// made once per color and kept, which costs four megabytes each and saves doing it per glyph per frame; a
    /// screen uses under a dozen colors and this is a development tool on a desktop.
    /// </para>
    /// </remarks>
    public sealed class GlyphAtlas
    {
        readonly int _width;
        readonly int _height;
        readonly byte[] _coverage;
        readonly byte[]? _colors;
        readonly Dictionary<uint, BitmapSource> _tinted = new Dictionary<uint, BitmapSource>();

        /// <summary>
        ///     Creates a page of <paramref name="width" /> by <paramref name="height" /> pixels with each pixel's
        ///     <paramref name="coverage" />, and with <paramref name="colors" /> as blue, green and red when the page
        ///     has any color other than white.
        /// </summary>
        GlyphAtlas(int width, int height, byte[] coverage, byte[]? colors)
        {
            _width = width;
            _height = height;
            _coverage = coverage;
            _colors = colors;
        }

        /// <summary>Loads the page named by a font file, or returns null if it cannot be found.</summary>
        /// <remarks>
        /// Null rather than throwing. A machine without the game and without a cached page is the case
        /// this whole arrangement exists to survive, and it falls back to a desktop face that has been
        /// good enough to build the entire screen against.
        /// </remarks>
        public static GlyphAtlas? Load(string? pageFile)
        {
            return Load("white", pageFile);
        }

        /// <summary>
        ///     Loads the page named by a font file in the game's font folder <paramref name="folder" />, or returns
        ///     null if it cannot be found.
        /// </summary>
        public static GlyphAtlas? Load(string folder, string? pageFile)
        {
            if (string.IsNullOrEmpty(pageFile))
                return null;

            string relative = "Fonts/" + folder + "/" + pageFile;
            string? png;

            try
            {
                png = GameImages.Resolve(relative);
            }
            catch (Exception)
            {
                // A decoder that will not run is a reason to fall back, not a reason to stop. It is only
                // ever reachable on a machine that has the game, where the fallback is still correct.
                return null;
            }

            return FromPng(png);
        }

        /// <summary>
        ///     Loads a page from a PNG, with its color stored straight rather than multiplied by its coverage, or
        ///     returns null if there is no such file.
        /// </summary>
        public static GlyphAtlas? FromPng(string? png)
        {
            if (png == null || !File.Exists(png))
                return null;

            var decoded = new FormatConvertedBitmap(
                new BitmapImage(new Uri(png, UriKind.Absolute)), PixelFormats.Bgra32, null, 0d);

            int width = decoded.PixelWidth;
            int height = decoded.PixelHeight;

            var bgra = new byte[width * height * 4];
            decoded.CopyPixels(bgra, width * 4, 0);

            var coverage = new byte[width * height];
            for (int i = 0; i < coverage.Length; i++)
                coverage[i] = bgra[i * 4 + 3];

            return new GlyphAtlas(width, height, coverage, Colors(bgra, width, height, width * 4));
        }

        /// <summary>Loads a page directly from a DDS, for a font this project ships rather than borrows.</summary>
        /// <remarks>
        /// No cache in front of it, unlike the game's own page. That one is decoded once and kept because
        /// the machine reading it may not have the game at all; this one is in the repository, is a
        /// fortieth of the size, and decoding it costs less than deciding whether to.
        /// </remarks>
        public static GlyphAtlas? FromDds(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
                return null;

            DdsImage.Pixels? pixels;
            try
            {
                pixels = DdsImage.Read(path);
            }
            catch (Exception)
            {
                return null;
            }

            if (pixels == null)
                return null;

            var coverage = new byte[pixels.Width * pixels.Height];
            for (int y = 0; y < pixels.Height; y++)
                for (int x = 0; x < pixels.Width; x++)
                    coverage[y * pixels.Width + x] = pixels.Bgra[y * pixels.Stride + x * 4 + 3];

            return new GlyphAtlas(pixels.Width, pixels.Height, coverage, Colors(pixels.Bgra, pixels.Width, pixels.Height, pixels.Stride));
        }

        /// <summary>
        ///     Returns each pixel's color as blue, green and red, or null if every covered pixel is white, which holds
        ///     for every page the game ships and avoids keeping a copy for nothing.
        /// </summary>
        static byte[]? Colors(byte[] bgra, int width, int height, int stride)
        {
            byte[]? colors = null;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int at = y * stride + x * 4;
                    if (bgra[at + 3] != 0 && (bgra[at] != 255 || bgra[at + 1] != 255 || bgra[at + 2] != 255))
                    {
                        colors = new byte[width * height * 3];
                        break;
                    }
                }

                if (colors != null)
                    break;
            }

            if (colors == null)
                return null;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int from = y * stride + x * 4;
                    int to = (y * width + x) * 3;
                    colors[to] = bgra[from];
                    colors[to + 1] = bgra[from + 1];
                    colors[to + 2] = bgra[from + 2];
                }
            }

            return colors;
        }

        /// <summary>
        ///     Returns the page in one color, multiplied by the page's own, created the first time that color is
        ///     requested.
        /// </summary>
        public BitmapSource Tinted(byte r, byte g, byte b)
        {
            uint key = (uint)((r << 16) | (g << 8) | b);

            BitmapSource cached;
            if (_tinted.TryGetValue(key, out cached))
                return cached;

            var bitmap = new WriteableBitmap(_width, _height, 96d, 96d, PixelFormats.Bgra32, null);

            bitmap.Lock();
            try
            {
                unsafe
                {
                    var row = (byte*)bitmap.BackBuffer;

                    for (int y = 0; y < _height; y++)
                    {
                        int from = y * _width;

                        for (int x = 0; x < _width; x++)
                        {
                            byte* pixel = row + x * 4;
                            if (_colors == null)
                            {
                                pixel[0] = b;
                                pixel[1] = g;
                                pixel[2] = r;
                            }
                            else
                            {
                                int at = (from + x) * 3;
                                pixel[0] = (byte)(b * _colors[at] / 255);
                                pixel[1] = (byte)(g * _colors[at + 1] / 255);
                                pixel[2] = (byte)(r * _colors[at + 2] / 255);
                            }

                            pixel[3] = _coverage[from + x];
                        }

                        row += bitmap.BackBufferStride;
                    }
                }

                bitmap.AddDirtyRect(new Int32Rect(0, 0, _width, _height));
            }
            finally
            {
                bitmap.Unlock();
            }

            // Frozen, so the canvas can draw it from whatever thread asks and nothing has to copy it
            // again: a page is four megabytes and a color is asked for once.
            bitmap.Freeze();

            _tinted.Add(key, bitmap);
            return bitmap;
        }
    }
}
