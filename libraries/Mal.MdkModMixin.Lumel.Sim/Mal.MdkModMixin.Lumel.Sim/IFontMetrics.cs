using VRageMath;

namespace Lumel.Sim
{
    /// <summary>Reports how big text is. The renderer supplies it, since only the renderer has a font.</summary>
    public interface IFontMetrics
    {
        /// <summary>Returns the size the text would occupy, in window pixels.</summary>
        Vector2 MeasurePixels(string? font, string? text, float scale);

        /// <summary>Returns the height of one line in window pixels, including leading.</summary>
        float LineHeightPixels(string? font, float scale);

        /// <summary>Returns the height of one line in the font's own units, as its file states.</summary>
        int LineUnits(string? font);
    }
}
