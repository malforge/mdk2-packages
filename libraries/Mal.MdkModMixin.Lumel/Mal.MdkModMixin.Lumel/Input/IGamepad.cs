namespace Lumel.Input
{
    /// <summary>The state of the gamepad in one frame, as a host reads it.</summary>
    public interface IGamepad
    {
        /// <summary>Whether the gamepad was used more recently than the mouse and keyboard.</summary>
        bool IsLastUsed { get; }

        /// <summary>Whether <paramref name="control" /> went down in this frame.</summary>
        bool IsNewPressed(PadControl control);

        /// <summary>Whether <paramref name="control" /> is held down.</summary>
        bool IsHeld(PadControl control);
    }
}
