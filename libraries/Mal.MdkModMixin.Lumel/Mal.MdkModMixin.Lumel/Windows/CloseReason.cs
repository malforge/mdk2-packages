namespace Lumel.Windows
{
    /// <summary>Why a window closed, as WinForms tells a form.</summary>
    public enum CloseReason
    {
        /// <summary>Closed while the world goes on: by itself, by a press elsewhere, by Escape, or by the mod.</summary>
        Closed,

        /// <summary>Closed by <see cref="Window.Accept" />: the player finished with it.</summary>
        Accepted,

        /// <summary>Closed because the world is unloading, by the game or by its workspace being disposed.</summary>
        WorldUnloading
    }
}
