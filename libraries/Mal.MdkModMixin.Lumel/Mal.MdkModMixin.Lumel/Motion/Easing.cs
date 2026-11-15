namespace Lumel.Motion
{
    /// <summary>Easing curves for transitions, each mapping 0 to 0 and 1 to 1.</summary>
    /// <remarks>The game's <c>MathHelper.SmoothStep</c> is the gentle ease in and out.</remarks>
    public static class Easing
    {
        /// <summary>A cubic ease out: quick to start, then settling into place.</summary>
        public static float EaseOut(float progress)
        {
            var left = 1f - progress;
            return 1f - left * left * left;
        }

        /// <summary>
        ///     A cubic ease in and out: slow to start, quick through the middle, then settling into place.
        /// </summary>
        public static float EaseInOut(float progress)
        {
            if (progress < 0.5f)
                return 4f * progress * progress * progress;

            var left = 2f - 2f * progress;
            return 1f - left * left * left / 2f;
        }

        /// <summary>
        ///     An ease out that overshoots the end slightly before settling back into place, for something that pops
        ///     in.
        /// </summary>
        public static float EaseOutBack(float progress)
        {
            // The usual back curve's constant, which goes about a tenth past the end.
            const float back = 1.70158f;
            var left = progress - 1f;
            return 1f + left * left * ((back + 1f) * left + back);
        }
    }
}
