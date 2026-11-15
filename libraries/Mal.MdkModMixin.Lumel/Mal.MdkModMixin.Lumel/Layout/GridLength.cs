
namespace Lumel.Layout
{
    /// <summary>
    ///     The size of a grid column or row: a fixed size, as big as its largest cell, or a share of the remaining
    ///     room.
    /// </summary>
    public struct GridLength
    {
        /// <summary>A length that is as big as the largest cell in the track.</summary>
        public static readonly GridLength Auto = new GridLength(0f, 0f, true);

        readonly bool _auto;
        readonly float _size;
        readonly float _weight;

        GridLength(float size, float weight, bool auto)
        {
            _size = size;
            _weight = weight;
            _auto = auto;
        }

        /// <summary>Gets the size of a fixed length.</summary>
        public float Size => _size;

        /// <summary>Gets the weight of the share for a flex length.</summary>
        public float Weight => _weight;

        /// <summary>Whether this is as big as its largest cell.</summary>
        public bool IsAuto => _auto;

        /// <summary>Whether this takes a share of what the fixed and auto lengths leave.</summary>
        public bool IsFlex => !_auto && _weight > 0f;

        /// <summary>Whether this is a size given outright.</summary>
        public bool IsFixed => !_auto && !(_weight > 0f);

        /// <summary>
        ///     Creates a flex length that takes a share of the remaining room with the given
        ///     <paramref name="weight" />, relative to the other flex lengths.
        /// </summary>
        public static GridLength Flex(float weight)
        {
            return new GridLength(0f, weight, false);
        }

        /// <summary>Converts a float to a fixed length of <paramref name="size" />.</summary>
        public static implicit operator GridLength(float size)
        {
            return new GridLength(size, 0f, false);
        }
    }
}
