using VRageMath;

namespace Lumel.Input
{
    /// <summary>A pointer that points at a place on the screen and can press it.</summary>
    public interface IPointer
    {
        /// <summary>Whether a pointer is on the surface this frame.</summary>
        bool IsAvailable { get; }

        /// <summary>Gets the pointer's position, in the same UI units everything else is laid out in.</summary>
        Vector2 Position { get; }

        /// <summary>Whether the press is held down right now.</summary>
        bool IsPressed { get; }

        /// <summary>Whether the press began this frame.</summary>
        bool WasPressed { get; }
    }
}