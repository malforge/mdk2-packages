using System;
using System.Globalization;
using VRageMath;

namespace Lumel.Text
{
    /// <summary>The colors a span in markdown-lite can name, and the reading of one written as <c>#rrggbb</c> or <c>#aarrggbb</c>.</summary>
    public static class SpanColors
    {
        // CSS's sixteen basic colors and its orange, by the values CSS gives them (W3C CSS Color Module Level 4, 6.1).
        static readonly string[] Names =
        {
            "black", "white", "gray", "silver", "red", "maroon", "orange", "yellow", "olive", "lime", "green", "aqua", "teal", "blue", "navy",
            "fuchsia", "purple"
        };

        static readonly Color[] Values =
        {
            new Color(0, 0, 0), new Color(255, 255, 255), new Color(128, 128, 128), new Color(192, 192, 192), new Color(255, 0, 0),
            new Color(128, 0, 0), new Color(255, 165, 0), new Color(255, 255, 0), new Color(128, 128, 0), new Color(0, 255, 0),
            new Color(0, 128, 0), new Color(0, 255, 255), new Color(0, 128, 128), new Color(0, 0, 255), new Color(0, 0, 128),
            new Color(255, 0, 255), new Color(128, 0, 128)
        };

        /// <summary>
        ///     Returns true if <paramref name="written" /> is a color's name, in any case, <c>#rrggbb</c> or
        ///     <c>#aarrggbb</c>, and sets
        ///     <paramref name="color" /> to it.
        /// </summary>
        public static bool TryRead(string written, out Color color)
        {
            color = default(Color);
            if (string.IsNullOrEmpty(written))
                return false;

            if (written[0] == '#')
            {
                uint value;
                if (written.Length != 7 && written.Length != 9 || !IsHex(written) ||
                    !uint.TryParse(written.Substring(1), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out value))
                    return false;

                var alpha = written.Length == 9 ? (int)(value >> 24) : 255;
                color = new Color((int)(value >> 16) & 0xFF, (int)(value >> 8) & 0xFF, (int)value & 0xFF, alpha);
                return true;
            }

            for (var i = 0; i < Names.Length; i++)
            {
                if (!string.Equals(Names[i], written, StringComparison.OrdinalIgnoreCase))
                    continue;

                color = Values[i];
                return true;
            }

            return false;
        }

        static bool IsHex(string written)
        {
            for (var i = 1; i < written.Length; i++)
            {
                var c = written[i];
                if (!(c >= '0' && c <= '9' || c >= 'a' && c <= 'f' || c >= 'A' && c <= 'F'))
                    return false;
            }

            return true;
        }
    }
}
