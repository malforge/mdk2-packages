
namespace Lumel.Layout
{
    /// <summary>A placement that gives an element its own width and height.</summary>
    public interface ISized : IPlacement
    {
        /// <summary>Gets the element's width, or float.NaN for as wide as it asks.</summary>
        float Width { get; }

        /// <summary>Gets the element's height, or float.NaN for as tall as it asks.</summary>
        float Height { get; }
    }
}
