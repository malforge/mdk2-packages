namespace Lumel.Input
{
    /// <summary>What a gamepad does on a screen, as the game's own screens read it.</summary>
    public enum PadControl
    {
        /// <summary>Move focus up: the D-pad or the left stick.</summary>
        Up,

        /// <summary>Move focus down: the D-pad or the left stick.</summary>
        Down,

        /// <summary>Move focus left: the D-pad or the left stick.</summary>
        Left,

        /// <summary>Move focus right: the D-pad or the left stick.</summary>
        Right,

        /// <summary>Press what has focus: A.</summary>
        Accept,

        /// <summary>Switch to the previous page of tabs: the left trigger.</summary>
        PreviousPage,

        /// <summary>Switch to the next page of tabs: the right trigger.</summary>
        NextPage,

        /// <summary>Scroll up: the right stick.</summary>
        ScrollUp,

        /// <summary>Scroll down: the right stick.</summary>
        ScrollDown
    }
}
