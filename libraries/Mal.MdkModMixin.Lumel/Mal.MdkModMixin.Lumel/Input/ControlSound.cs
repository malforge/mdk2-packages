namespace Lumel.Input
{
    /// <summary>
    ///     An event that makes a control play a sound: the pointer moving onto it, or a press running its command.
    /// </summary>
    public enum ControlSound
    {
        /// <summary>The pointer moved onto an element whose command a press would run.</summary>
        Hover,

        /// <summary>A press, by pointer or key, is about to run an element's command.</summary>
        Click
    }
}
