namespace Lumel.Controls
{
    /// <summary>The way a color is typed in a <see cref="ColorPanel" />, one tab each.</summary>
    public enum ColorEntry
    {
        /// <summary>By its hue, saturation and value.</summary>
        Hsv,

        /// <summary>By its red, green and blue.</summary>
        Rgb,

        /// <summary>By its code, written <c>#RRGGBB</c> or <c>#RRGGBBAA</c>.</summary>
        Hex
    }
}
