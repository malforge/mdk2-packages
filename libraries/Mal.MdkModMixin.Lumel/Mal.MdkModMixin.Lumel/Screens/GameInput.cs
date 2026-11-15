using Sandbox.ModAPI;
using VRageMath;

namespace Lumel.Screens
{
    /// <summary>Reads the state of the game's input as a whole.</summary>
    /// <remarks>
    ///     Static because the game's input is one per process, read through the game's own static
    ///     <see cref="MyAPIGateway.Input" />.
    /// </remarks>
    public static class GameInput
    {
        /// <summary>
        ///     Returns true if the game has cleared its input for this frame, as it does on every frame the game window
        ///     is out of focus.
        /// </summary>
        /// <remarks>
        ///     On the first such frame the wheel delta is minus every notch since launch, so nothing read from a cleared
        ///     frame is input.
        /// </remarks>
        public static bool IsCleared => IsClearedAt(MyAPIGateway.Input.GetMousePosition());

        /// <summary>
        ///     Returns true if <paramref name="mousePosition" /> is where the game puts the mouse when it clears its
        ///     input.
        /// </summary>
        public static bool IsClearedAt(Vector2 mousePosition)
        {
            // Space Engineers, observed in game 2026-11-02: a cleared frame puts the mouse at (-1, -1), and in play the
            // mouse never reads negative.
            return mousePosition.X < 0 && mousePosition.Y < 0;
        }
    }
}
