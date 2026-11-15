using VRageMath;

namespace Lumel.Controls
{
    /// <summary>Implemented by a <see cref="ColorPanel" /> so its palettes can tell it a color was chosen from them.</summary>
    /// <remarks>Implemented explicitly by <see cref="ColorPanel" />, so only its palettes call it.</remarks>
    public interface IColorPanel
    {
        /// <summary>Tells the panel the player chose <paramref name="color" /> from one of its palettes.</summary>
        void Picked(Color color);
    }
}
