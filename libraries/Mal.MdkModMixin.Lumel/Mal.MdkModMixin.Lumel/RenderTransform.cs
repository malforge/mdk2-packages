using System;
using VRageMath;

namespace Lumel
{
    /// <summary>
    ///     How an element is drawn and hit-tested away from where layout put it: translated, and scaled about its
    ///     top-left corner.
    /// </summary>
    /// <remarks>The default is no transform at all.</remarks>
    public struct RenderTransform : IEquatable<RenderTransform>
    {
        // The scale less one, so the default is a scale of one.
        readonly float _grown;

        /// <summary>
        ///     Creates a transform that moves by <paramref name="translate" /> and scales by <paramref name="scale" />.
        /// </summary>
        public RenderTransform(Vector2 translate, float scale)
        {
            Translate = translate;
            _grown = scale - 1f;
        }

        /// <summary>Gets the transform that does nothing.</summary>
        public static RenderTransform Identity => default(RenderTransform);

        /// <summary>Gets how far the element is moved, in UI units.</summary>
        public Vector2 Translate { get; }

        /// <summary>Gets how much the element is scaled, about its top-left corner.</summary>
        public float Scale => 1f + _grown;

        /// <summary>Whether this moves and scales nothing.</summary>
        public bool IsIdentity => _grown == 0f && Translate.X == 0f && Translate.Y == 0f;

        /// <summary>Creates a transform that moves by <paramref name="translate" /> and does not scale.</summary>
        public static RenderTransform Moved(Vector2 translate)
        {
            return new RenderTransform(translate, 1f);
        }

        /// <summary>
        ///     Returns where <paramref name="point" />, as laid out, is drawn, for an element whose top-left corner is
        ///     <paramref name="origin" />.
        /// </summary>
        public Vector2 Apply(Vector2 point, Vector2 origin)
        {
            return (point - origin) * Scale + origin + Translate;
        }

        /// <summary>
        ///     Returns where <paramref name="point" />, as drawn, was laid out; the reverse of <see cref="Apply" />.
        /// </summary>
        public Vector2 Inverse(Vector2 point, Vector2 origin)
        {
            var scale = Scale;
            return scale == 0f ? origin : (point - Translate - origin) / scale + origin;
        }

        /// <summary>
        ///     Returns true if <paramref name="a" /> and <paramref name="b" /> move and scale by the same amounts.
        /// </summary>
        public static bool operator ==(RenderTransform a, RenderTransform b)
        {
            return a.Equals(b);
        }

        /// <summary>Returns true if <paramref name="a" /> and <paramref name="b" /> differ.</summary>
        public static bool operator !=(RenderTransform a, RenderTransform b)
        {
            return !a.Equals(b);
        }

        /// <inheritdoc />
        public bool Equals(RenderTransform other)
        {
            return _grown == other._grown && Translate.X == other.Translate.X && Translate.Y == other.Translate.Y;
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            return obj is RenderTransform && Equals((RenderTransform)obj);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return (_grown.GetHashCode() * 397) ^ Translate.X.GetHashCode() ^ (Translate.Y.GetHashCode() * 31);
        }
    }
}
