namespace Lumel.Input
{
    /// <summary>Stores copied text until something pastes it.</summary>
    /// <remarks>Not the operating system's clipboard, which the mod API does not offer; the host decides where it is.</remarks>
    public interface IClipboard
    {
        /// <summary>Returns the copied text, or an empty string if nothing has been copied.</summary>
        string Get();

        /// <summary>Replaces the copied text with <paramref name="text" />.</summary>
        void Set(string text);
    }
}
