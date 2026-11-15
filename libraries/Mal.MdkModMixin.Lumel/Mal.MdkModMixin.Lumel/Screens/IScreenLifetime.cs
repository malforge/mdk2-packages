namespace Lumel.Screens
{
    /// <summary>
    ///     What a <see cref="GameScreen" /> tells the workspace it shows when the game opens and closes it.
    /// </summary>
    /// <remarks>Implemented explicitly, so only the screen showing the workspace calls it.</remarks>
    public interface IScreenLifetime
    {
        /// <summary>Called when the screen has opened.</summary>
        void Opened();

        /// <summary>
        ///     Called when the screen has closed; <paramref name="isUnloading" /> is true if the game is unloading the
        ///     world.
        /// </summary>
        void Closed(bool isUnloading);
    }
}
