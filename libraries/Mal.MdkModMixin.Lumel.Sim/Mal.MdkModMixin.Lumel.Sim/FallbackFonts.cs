using System.Globalization;
using System.Windows;
using System.Windows.Media;
using VRageMath;

namespace Lumel.Sim
{
    /// <summary>
    ///     Approximate text metrics that are good enough to lay out against, and make clear they are not the game's.
    /// </summary>
    /// <remarks>
    /// The line height is real: 37 pixels at the reference height, read off the game's own
    /// <c>FontData</c>, which is an XNA bitmap font declaring <c>height="37" base="30" size="23"</c>. The
    /// widths are not - they come from whatever face the desktop has, stretched so its line height lands on
    /// that number. So a layout built here has the right number of lines in the right places, and every
    /// individual run is the wrong width by a few percent.
    /// <para>
    /// That is the lower rung deliberately, and the upper one exists: <see cref="SeFontMetrics" />
    /// reads the same font file's per-glyph advances and is exact. It needs the game or a warmed cache, so
    /// it is what the window uses and not what a recorded frame can depend on - a frame has to reproduce
    /// on a machine that has never had the game. Anything asking whether text fits should be read with
    /// suspicion here, and <see cref="IsExact" /> says so out loud rather than leaving it to be discovered.
    /// </para>
    /// </remarks>
    public sealed class FallbackFonts : IFontMetrics
    {
        /// <summary>From the game's FontData: the height of a line at scale 1, at the reference height.</summary>
        public const float GameLineHeight = 37f;

        /// <summary>The height the game's own UI and its font scaling are designed for.</summary>
        public const float GameReferenceHeight = 1080f;

        const double PerPixel = 1d;

        readonly Typeface _typeface;
        readonly ScreenSpaceDevice _device;

        double _heightPerSize;

        /// <summary>
        ///     Creates metrics that measure and draw in the desktop typeface <paramref name="family" />, at the scale
        ///     set by the screen of <paramref name="device" />.
        /// </summary>
        public FallbackFonts(ScreenSpaceDevice device, string family)
        {
            _device = device;
            _typeface = new Typeface(new FontFamily(family), FontStyles.Normal, FontWeights.Normal,
                FontStretches.Normal);
        }

        /// <summary>Whether the measurements are the game's; always false for these.</summary>
        public bool IsExact => false;

        /// <summary>
        ///     Returns the size of <paramref name="text" />: an approximate width, and the game's height for one line,
        ///     including <see cref="SeFontMetrics.FontScale" />.
        /// </summary>
        public Vector2 MeasurePixels(string? font, string? text, float scale)
        {
            float line = DrawnLineHeight(scale);

            if (string.IsNullOrEmpty(text))
                return new Vector2(0f, line);

            FormattedText formatted = Build(text, FontSizeFor(font, scale));
            return new Vector2((float)formatted.WidthIncludingTrailingWhitespace, line);
        }

        /// <summary>
        ///     Returns the line height the game's screen-space GetLineHeight would return, including
        ///     <see cref="SeFontMetrics.FontScale" />.
        /// </summary>
        public float LineHeightPixels(string? font, float scale)
        {
            return GameLineHeight * EffectiveScale(scale) * SeFontMetrics.FontScale;
        }

        /// <summary>Returns the game's line height in font units, since this face has no file to provide it.</summary>
        public int LineUnits(string? font)
        {
            return (int)GameLineHeight;
        }

        /// <summary>
        ///     Returns the scale the game would actually draw at: <paramref name="scale" /> multiplied by the game's
        ///     HUD scale. The multiplication is reproduced rather than ignored, because the backend under test corrects
        ///     for it and a simulator that skipped it would make that correction look like a bug.
        /// </summary>
        public float EffectiveScale(float scale)
        {
            float height = _device.SafeArea.Height;
            float gameScale = height > 0f ? height / GameReferenceHeight : 1f;
            return scale * gameScale;
        }

        /// <summary>Returns the point size at which this face's line height matches the game's.</summary>
        public double FontSizeFor(string? font, float scale)
        {
            if (_heightPerSize <= 0d)
            {
                FormattedText probe = Build("Mg", 100d);
                _heightPerSize = probe.Height / 100d;
                if (_heightPerSize <= 0d)
                    _heightPerSize = 1d;
            }

            return DrawnLineHeight(scale) / _heightPerSize;
        }

        /// <summary>
        ///     Returns <paramref name="text" /> set in the typeface at <paramref name="size" />, in white.
        /// </summary>
        public FormattedText Build(string text, double size)
        {
            return new FormattedText(text, CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                _typeface, size, Brushes.White, PerPixel);
        }

        /// <summary>Returns the height in pixels of a line the game draws at <paramref name="scale" />.</summary>
        float DrawnLineHeight(float scale)
        {
            return LineHeightPixels(null, scale);
        }
    }
}
