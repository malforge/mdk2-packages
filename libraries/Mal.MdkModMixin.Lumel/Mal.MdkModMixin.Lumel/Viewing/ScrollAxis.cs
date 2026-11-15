
namespace Lumel.Viewing
{
    /// <summary>
    ///     One axis of scrolling: the total size of the content, the size of the visible part, and the position of the
    ///     view along it.
    /// </summary>
    /// <remarks>
    ///     An offset it works out is returned and not stored: the control assigns it through its own property, which
    ///     invalidates.
    /// </remarks>
    public sealed class ScrollAxis
    {
        /// <summary>Gets or sets the total length of the content.</summary>
        public float Extent { get; set; }

        /// <summary>Gets or sets the length of the visible part.</summary>
        public float Viewport { get; set; }

        /// <summary>Gets or sets how far along the content the view is scrolled.</summary>
        /// <remarks>
        ///     Not clamped when set; the control calls <see cref="Clamp" /> once <see cref="Extent" /> and
        ///     <see cref="Viewport" /> are known.
        /// </remarks>
        public float Offset { get; set; }

        /// <summary>Gets or sets the minimum length of the thumb.</summary>
        public float MinThumbLength { get; set; }

        /// <summary>Gets the largest offset, or 0 if everything fits.</summary>
        public float Max
        {
            get
            {
                var max = Extent - Viewport;
                return max > 0f ? max : 0f;
            }
        }

        /// <summary>Whether any of the content is out of view.</summary>
        public bool CanMove => Max > 0f;

        /// <summary>Gets how far along the content the view is, from 0 to 1.</summary>
        public float Fraction
        {
            get
            {
                var max = Max;
                return max > 0f ? Offset / max : 0f;
            }
        }

        /// <summary>Gets the length of the thumb.</summary>
        public float ThumbLength
        {
            get
            {
                if (Extent <= 0f || Viewport <= 0f)
                    return Viewport;

                var length = Viewport * Viewport / Extent;

                if (length < MinThumbLength)
                    length = MinThumbLength;
                if (length > Viewport)
                    length = Viewport;

                return length;
            }
        }

        /// <summary>Gets the distance the thumb can travel.</summary>
        public float Travel
        {
            get
            {
                var travel = Viewport - ThumbLength;
                return travel > 0f ? travel : 0f;
            }
        }

        /// <summary>Gets the position where the thumb starts, measured from the start of the track.</summary>
        public float ThumbStart => Travel * Fraction;

        /// <summary>Returns the offset after moving by <paramref name="amount" />.</summary>
        public float Moved(float amount)
        {
            return Clamp(Offset + amount);
        }

        /// <summary>Returns the offset after dragging the thumb by <paramref name="distance" />.</summary>
        public float Dragged(float distance)
        {
            var travel = Travel;
            return travel > 0f ? Clamp(Offset + distance * Max / travel) : Offset;
        }

        /// <summary>Returns the offset that puts the middle of the thumb at <paramref name="at" />.</summary>
        /// <param name="at">Where along the axis, in the same units as <paramref name="start" />.</param>
        /// <param name="start">Where the track begins.</param>
        public float ThumbedTo(float at, float start)
        {
            var travel = Travel;
            if (travel <= 0f)
                return Offset;

            return Clamp((at - start - ThumbLength * 0.5f) * Max / travel);
        }

        /// <summary>Clamps <paramref name="value" /> between 0 and <see cref="Max" />.</summary>
        public float Clamp(float value)
        {
            var max = Max;

            if (value > max)
                value = max;
            if (value < 0f)
                value = 0f;

            return value;
        }
    }
}