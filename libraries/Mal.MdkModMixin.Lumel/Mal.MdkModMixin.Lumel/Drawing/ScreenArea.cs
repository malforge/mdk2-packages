namespace Lumel.Drawing
{
    /// <summary>The part of the game's window that a screen-space display lays itself out in.</summary>
    public enum ScreenArea
    {
        /// <summary>The size and position of the game's own terminal screen, centered in the safe area.</summary>
        Terminal,

        /// <summary>The whole window, edge to edge, which is wider than the safe area on anything but 4:3.</summary>
        Window
    }
}
