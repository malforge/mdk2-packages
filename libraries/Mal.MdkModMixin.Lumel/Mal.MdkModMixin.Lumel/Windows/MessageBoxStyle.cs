using Lumel.Styling;

namespace Lumel.Windows
{
    /// <summary>The style of a message box: its panel, message and buttons.</summary>
    public class MessageBoxStyle : Style<MessageBox, MessageBoxLook>
    {
        /// <summary>Creates a style that sets nothing, leaving the message box at its neutral values.</summary>
        public MessageBoxStyle()
        {
        }

        /// <summary>
        ///     Creates a style that inherits everything from <paramref name="basedOn" /> and overrides whatever this
        ///     style sets.
        /// </summary>
        public MessageBoxStyle(IStyle<Dialog> basedOn) : base(basedOn)
        {
        }
    }
}
