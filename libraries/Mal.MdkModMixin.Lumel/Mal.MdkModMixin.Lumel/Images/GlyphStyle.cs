using Lumel.Styling;

namespace Lumel.Images
{
    /// <summary>The style for a glyph: its font, size and tint, at rest, on hover and when disabled.</summary>
    public class GlyphStyle : Style<Glyph, GlyphLook>
    {
        /// <summary>Creates a style that sets nothing, leaving the glyph at its neutral values.</summary>
        public GlyphStyle()
        {
        }

        /// <summary>
        ///     Creates a style that includes everything in <paramref name="basedOn" />, with whatever this style sets
        ///     applied on top.
        /// </summary>
        public GlyphStyle(IStyle<Glyph> basedOn) : base(basedOn)
        {
        }
    }
}
