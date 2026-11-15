using System;

namespace Lumel.Layout
{
    /// <summary>A placement that holds an element to a size of its own, whatever it would ask for.</summary>
    public sealed class FixedSize : ISized
    {
        readonly float _height;
        readonly float _width;

        FixedSize(float width, float height)
        {
            _width = width;
            _height = height;
        }

        float ISized.Width => _width;

        float ISized.Height => _height;

        event Action IPlacement.Changed
        {
            add { }
            remove { }
        }

        /// <summary>
        ///     Creates a placement that holds the element to <paramref name="width" /> and lets it be as tall as it
        ///     asks.
        /// </summary>
        public static FixedSize Width(float width)
        {
            return new FixedSize(width, float.NaN);
        }

        /// <summary>
        ///     Creates a placement that holds the element to <paramref name="height" /> and lets it be as wide as it
        ///     asks.
        /// </summary>
        public static FixedSize Height(float height)
        {
            return new FixedSize(float.NaN, height);
        }

        /// <summary>
        ///     Creates a placement that holds the element to <paramref name="width" /> and <paramref name="height" />.
        /// </summary>
        public static FixedSize Size(float width, float height)
        {
            return new FixedSize(width, height);
        }
    }
}
