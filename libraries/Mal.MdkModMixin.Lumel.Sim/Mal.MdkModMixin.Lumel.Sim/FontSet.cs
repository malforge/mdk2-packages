using System.Collections.Generic;
using VRageMath;

namespace Lumel.Sim
{
    /// <summary>Routes each measurement to the metrics registered for the requested font.</summary>
    /// <remarks>
    /// The game keeps every loaded font in one registry and picks by subtype, so a screen may name any of
    /// them on any call. This is the smallest thing that reproduces that: a set of named faces, and a rule
    /// for what happens when a name is none of them.
    /// <para>
    /// An unknown name falls back to the text font rather than measuring zero, because zero is the answer
    /// that makes a layout collapse silently. A run in a font that is not there should come out the wrong
    /// width, not the wrong shape.
    /// </para>
    /// </remarks>
    public sealed class FontSet : IFontMetrics
    {
        readonly IFontMetrics _text;
        readonly Dictionary<string, IFontMetrics> _named = new Dictionary<string, IFontMetrics>();

        /// <summary>
        ///     Creates a set that uses <paramref name="text" /> to measure every font not registered with it.
        /// </summary>
        public FontSet(IFontMetrics text)
        {
            _text = text;
        }

        /// <summary>
        ///     Registers <paramref name="metrics" /> for the font subtype <paramref name="font" />, the name a screen
        ///     refers to it by; a null <paramref name="font" /> or <paramref name="metrics" /> is ignored.
        /// </summary>
        public FontSet And(string? font, IFontMetrics? metrics)
        {
            if (!string.IsNullOrEmpty(font) && metrics != null)
                _named[font] = metrics;

            return this;
        }

        /// <inheritdoc />
        public Vector2 MeasurePixels(string? font, string? text, float scale)
        {
            return For(font).MeasurePixels(font, text, scale);
        }

        /// <inheritdoc />
        public float LineHeightPixels(string? font, float scale)
        {
            return For(font).LineHeightPixels(font, scale);
        }

        /// <inheritdoc />
        public int LineUnits(string? font)
        {
            return For(font).LineUnits(font);
        }

        IFontMetrics For(string? font)
        {
            IFontMetrics found;
            return font != null && _named.TryGetValue(font, out found) ? found : _text;
        }
    }
}
