namespace Lumel.Input
{
    /// <summary>
    ///     A turn of the wheel about to be offered to the element under the pointer, which a handler can stop.
    /// </summary>
    public sealed class WheelEventArgs
    {
        /// <summary>
        ///     Creates event data for a wheel turn of <paramref name="amount" /> UI units that is about to be offered
        ///     to <paramref name="target" />.
        /// </summary>
        public WheelEventArgs(Element target, float amount)
        {
            Target = target;
            Amount = amount;
        }

        /// <summary>
        ///     Gets the innermost element under the pointer, which is offered the wheel first, or null if there is
        ///     none.
        /// </summary>
        public Element Target { get; }

        /// <summary>Gets how far the wheel turned, in UI units, where positive is down.</summary>
        public float Amount { get; }

        /// <summary>
        ///     Gets or sets whether a handler has handled the wheel, so that no element under the pointer is offered
        ///     it.
        /// </summary>
        public bool Handled { get; set; }
    }
}
