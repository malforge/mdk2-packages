using System;

namespace Lumel
{
    /// <summary>The space on each side of an element.</summary>
    public struct Thickness : IEquatable<Thickness>
    {
        /// <summary>The space on the left.</summary>
        public readonly float Left;

        /// <summary>The space on the top.</summary>
        public readonly float Top;

        /// <summary>The space on the right.</summary>
        public readonly float Right;

        /// <summary>The space on the bottom.</summary>
        public readonly float Bottom;

        /// <summary>Creates a thickness with <paramref name="all" /> on every side.</summary>
        public Thickness(float all)
        {
            Left = all;
            Top = all;
            Right = all;
            Bottom = all;
        }

        /// <summary>
        ///     Creates a thickness with <paramref name="horizontal" /> on the left and right and
        ///     <paramref name="vertical" /> on the top and bottom.
        /// </summary>
        public Thickness(float horizontal, float vertical)
        {
            Left = horizontal;
            Top = vertical;
            Right = horizontal;
            Bottom = vertical;
        }

        /// <summary>Creates a thickness with a separate value for each side.</summary>
        public Thickness(float left, float top, float right, float bottom)
        {
            Left = left;
            Top = top;
            Right = right;
            Bottom = bottom;
        }

        /// <summary>Gets the left and right space combined.</summary>
        public float Horizontal => Left + Right;

        /// <summary>Gets the top and bottom space combined.</summary>
        public float Vertical => Top + Bottom;

        /// <summary>Whether all four sides are 0.</summary>
        public bool IsEmpty => Left == 0f && Top == 0f && Right == 0f && Bottom == 0f;

        /// <summary>Returns true if <paramref name="a" /> and <paramref name="b" /> are equal on every side.</summary>
        public static bool operator ==(Thickness a, Thickness b)
        {
            return a.Equals(b);
        }

        /// <summary>Returns true if <paramref name="a" /> and <paramref name="b" /> differ on any side.</summary>
        public static bool operator !=(Thickness a, Thickness b)
        {
            return !a.Equals(b);
        }

        /// <summary>Returns true if <paramref name="other" /> is equal on every side.</summary>
        public bool Equals(Thickness other)
        {
            return Left == other.Left && Top == other.Top && Right == other.Right && Bottom == other.Bottom;
        }

        /// <inheritdoc />
        public override bool Equals(object other)
        {
            return other is Thickness && Equals((Thickness)other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            var hash = Left.GetHashCode();
            hash = (hash * 397) ^ Top.GetHashCode();
            hash = (hash * 397) ^ Right.GetHashCode();
            hash = (hash * 397) ^ Bottom.GetHashCode();
            return hash;
        }
    }
}