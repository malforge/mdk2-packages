namespace Lumel.Input
{
    /// <summary>An <see cref="IClipboard" /> that keeps the copied text in memory.</summary>
    public class Clipboard : IClipboard
    {
        string _value = string.Empty;

        /// <inheritdoc />
        public string Get()
        {
            return _value;
        }

        /// <inheritdoc />
        public void Set(string text)
        {
            _value = text ?? string.Empty;
        }
    }
}
