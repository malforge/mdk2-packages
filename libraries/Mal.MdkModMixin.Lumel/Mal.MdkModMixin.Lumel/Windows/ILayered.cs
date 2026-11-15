namespace Lumel.Windows
{
    /// <summary>An element that a window layer covers or holds open, whose layer only the layer sets.</summary>
    /// <remarks>Implemented explicitly by <see cref="Element" />.</remarks>
    public interface ILayered
    {
        /// <summary>Sets the layer the element is the top of, or null once it is removed.</summary>
        WindowLayer Layer { set; }
    }
}
