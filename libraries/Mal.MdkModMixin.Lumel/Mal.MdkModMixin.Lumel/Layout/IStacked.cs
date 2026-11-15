namespace Lumel.Layout
{
    /// <summary>A placement in a stack that can ask for a share of the room the other children leave.</summary>
    public interface IStacked : ISized
    {
        /// <summary>
        ///     Gets this child's weight when the room left along the stack is shared out, or zero to take only its own
        ///     size.
        /// </summary>
        float Grow { get; }

        /// <summary>Gets how a growing child takes the room it grows into.</summary>
        FlexFit Fit { get; }
    }
}
