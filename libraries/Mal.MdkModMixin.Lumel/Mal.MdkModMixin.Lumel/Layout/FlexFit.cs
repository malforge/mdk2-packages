namespace Lumel.Layout
{
    /// <summary>How a growing stack child takes the room it grows into.</summary>
    public enum FlexFit
    {
        /// <summary>
        ///     The child asks for its share when the stack is measured, so the stack takes all the room it is offered.
        /// </summary>
        Tight,

        /// <summary>
        ///     The child asks for no room when the stack is measured and is given what is left when the stack is
        ///     arranged.
        /// </summary>
        Loose
    }
}
