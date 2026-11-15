using VRageMath;

namespace Lumel
{
    /// <summary>An element that draws text in a font, size and color set by its looks.</summary>
    public interface ITextual
    {
        /// <summary>Gets the font the text is drawn in.</summary>
        string Font { get; }

        /// <summary>Gets the height of a line of text, in UI units.</summary>
        float FontSize { get; }

        /// <summary>Gets the color the text is drawn in.</summary>
        Color Color { get; }
    }
}
