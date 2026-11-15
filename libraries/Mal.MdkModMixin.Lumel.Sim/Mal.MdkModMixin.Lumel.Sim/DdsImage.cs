using System;
using System.IO;
using System.Runtime.InteropServices;
using DirectXTexNet;

namespace Lumel.Sim
{
    /// <summary>Converts one of the game's DDS textures into a plain pixel buffer.</summary>
    /// <remarks>
    /// The decompress-then-unpremultiply pair is lifted from Malforge's <c>DocGen.TextureLoader</c>, which
    /// does this same job against the same game and has been right for longer than this project has
    /// existed. Writing a third BC1 unpacker would have been a third place to get it subtly wrong.
    /// <para>
    /// Unpremultiplying matters more than it looks: the game stores glyph and icon alpha premultiplied,
    /// and a page drawn without reversing that has dark haloes around every letter - which reads as bad
    /// font rendering rather than as a missing step.
    /// </para>
    /// </remarks>
    public static class DdsImage
    {
        /// <summary>Pixel data as BGRA bytes, one row after another, top row first.</summary>
        public sealed class Pixels
        {
            /// <summary>How many pixels wide the picture is.</summary>
            public int Width;

            /// <summary>How many pixels tall the picture is.</summary>
            public int Height;

            /// <summary>How many bytes one row takes.</summary>
            public int Stride;

            /// <summary>Every pixel as blue, green, red and alpha bytes, row by row.</summary>
            public byte[] Bgra = Array.Empty<byte>();
        }

        /// <summary>
        ///     Reads the DDS at <paramref name="path" /> and returns its pixels, or null if it cannot be read.
        /// </summary>
        /// <remarks>
        /// Null rather than throwing, because every caller here has a fallback and a machine without the
        /// game is an ordinary case rather than a fault. What is not ordinary - a file that is present
        /// and will not decode - is worth hearing about, so that one is left to throw.
        /// </remarks>
        public static Pixels? Read(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
                return null;

            ScratchImage scratch = TexHelper.Instance.LoadFromDDSFile(path, DDS_FLAGS.FORCE_RGB);

            try
            {
                scratch = Decompress(scratch);
                scratch = UnPremultiply(scratch);

                Image image = scratch.GetImage(0);

                var pixels = new Pixels();
                pixels.Width = image.Width;
                pixels.Height = image.Height;
                pixels.Stride = (int)image.RowPitch;
                pixels.Bgra = new byte[image.RowPitch * image.Height];

                Marshal.Copy(image.Pixels, pixels.Bgra, 0, pixels.Bgra.Length);
                return pixels;
            }
            finally
            {
                scratch.Dispose();
            }
        }

        static ScratchImage Decompress(ScratchImage scratch)
        {
            TexMetadata meta = scratch.GetMetadata();

            ScratchImage converted = IsBlockCompressed(meta.Format)
                ? scratch.Decompress(DXGI_FORMAT.B8G8R8A8_UNORM)
                : scratch.Convert(DXGI_FORMAT.B8G8R8A8_UNORM, TEX_FILTER_FLAGS.FANT, 0.5f);

            scratch.Dispose();
            return converted;
        }

        static ScratchImage UnPremultiply(ScratchImage scratch)
        {
            if (scratch.GetMetadata().GetAlphaMode() != TEX_ALPHA_MODE.PREMULTIPLIED)
                return scratch;

            ScratchImage straight = scratch.PremultiplyAlpha(TEX_PMALPHA_FLAGS.REVERSE);
            scratch.Dispose();
            return straight;
        }

        /// <summary>
        ///     Returns true if <paramref name="format" /> is block-compressed, which must be unpacked rather than
        ///     converted.
        /// </summary>
        /// <remarks>
        /// By range rather than by listing twenty enum members. The BC formats occupy one contiguous run
        /// of the DXGI enumeration and always have, and a range says what is meant - "the compressed
        /// ones" - where a list of names only says it to somebody who already knows.
        /// </remarks>
        static bool IsBlockCompressed(DXGI_FORMAT format)
        {
            return (format >= DXGI_FORMAT.BC1_TYPELESS && format <= DXGI_FORMAT.BC5_SNORM)
                || (format >= DXGI_FORMAT.BC6H_TYPELESS && format <= DXGI_FORMAT.BC7_UNORM_SRGB);
        }
    }
}
