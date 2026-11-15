namespace Lumel.Styling
{
    /// <summary>How a <typeparamref name="TControl" /> looks, read-only.</summary>
    /// <remarks>A style for a derived control is a style for its base too: it is that style and more.</remarks>
    public interface IStyle<out TControl> : IStyle where TControl : Element
    {
    }
}
