namespace Lumel.Styling
{
    /// <summary>The style each kind of control takes when it is given none, read-only.</summary>
    public interface ITheme
    {
        /// <summary>
        ///     Returns the style for the nearest kind of control that <paramref name="element" /> is and this theme has
        ///     a style for, or null if there is none.
        /// </summary>
        IStyle StyleFor(Element element);
    }
}
