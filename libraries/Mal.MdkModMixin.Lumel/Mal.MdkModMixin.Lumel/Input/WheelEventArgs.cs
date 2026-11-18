using VRageMath;

namespace Lumel.Input
{
    /// <summary>
    ///     A turn of the wheel about to be offered to the element under the pointer, which a handler can stop.
    /// </summary>
    public sealed class WheelEventArgs
    {
        /// <summary>
        ///     Creates event data for a wheel turn of <paramref name="amount" /> UI units, positive down along Y and right along X, that is about to be offered
        ///     to <paramref name="target" />.
        /// </summary>
        public WheelEventArgs(Element target, Vector2 amount)
        {
            Target = target;
            Amount = amount;
        }

        /// <summary>
        ///     Gets the innermost element under the pointer, which is offered the wheel first, or null if there is
        ///     none.
        /// </summary>
        public Element Target { get; }

        /// <summary>Gets how far the wheel turned, in UI units, positive down along Y and right along X.</summary>
        public Vector2 Amount { get; }

        /// <summary>
        ///     Gets or sets whether a handler has handled the wheel, so that no element under the pointer is offered
        ///     it.
        /// </summary>
        public bool Handled { get; set; }
    }
}
