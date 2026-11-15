namespace Lumel.Windows
{
    /// <summary>An action waiting to run once, made by <see cref="WindowLayer.After" />.</summary>
    public interface ICountdown
    {
        /// <summary>Whether the action is still to run: it has neither run nor been canceled.</summary>
        bool IsPending { get; }

        /// <summary>Keeps the action from running, if it has not run yet.</summary>
        void Cancel();
    }
}
