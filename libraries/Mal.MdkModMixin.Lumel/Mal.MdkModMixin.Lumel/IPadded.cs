namespace Lumel
{
    /// <summary>An element with space kept clear between its edge and what it draws inside, set by its looks.</summary>
    public interface IPadded
    {
        /// <summary>Gets the space kept clear inside the bounds, within which content is measured and placed.</summary>
        Thickness Padding { get; }

        /// <summary>Sets the space to keep clear inside the bounds; called by the element's looks.</summary>
        void Pad(Thickness padding);
    }
}
