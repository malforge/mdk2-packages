using Lumel.Styling;

namespace Lumel.Windows
{
    /// <summary>The style of a dialog, which a theme holds separately from a window's.</summary>
    public class DialogStyle : Style<Dialog, WindowLook>
    {
        /// <summary>Creates a style that sets nothing, leaving the dialog at its neutral values.</summary>
        public DialogStyle()
        {
        }

        /// <summary>
        ///     Creates a style that inherits everything from <paramref name="basedOn" /> and overrides whatever this
        ///     style sets.
        /// </summary>
        public DialogStyle(IStyle<Window> basedOn) : base(basedOn)
        {
        }
    }
}
