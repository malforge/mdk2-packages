namespace Lumel.Windows
{
    /// <summary>
    ///     A window that a layer opens and closes, raising its <see cref="Window.Opened" /> and
    ///     <see cref="Window.Closed" /> events as it does.
    /// </summary>
    /// <remarks>Implemented explicitly by <see cref="Window" />, so only the layer it is open in calls it.</remarks>
    public interface IWindowLifetime
    {
        /// <summary>Called when the layer has put the window up.</summary>
        void PutUp();

        /// <summary>Called when the layer has removed the window, for <paramref name="reason" />.</summary>
        void TakenAway(CloseReason reason);
    }
}
