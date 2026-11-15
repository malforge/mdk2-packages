using VRageMath;

namespace Lumel.Windows
{
    /// <summary>
    ///     What Lumel tells a floating window: the screen it is kept on, from the layer it is open in, and the end of a
    ///     move, from its title bar.
    /// </summary>
    /// <remarks>Implemented explicitly by <see cref="FloatingWindow" />, so only the layer and the title bar reach these.</remarks>
    public interface IFloatingWindow
    {
        /// <summary>
        ///     Sets the whole screen the window is kept on, or empty to keep it within the area it is placed in.
        /// </summary>
        RectangleF Screen { set; }

        /// <summary>Signals that a move has ended with the window at its current position.</summary>
        void EndMove();
    }
}
