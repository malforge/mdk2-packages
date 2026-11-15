using Lumel.Styling;

namespace Lumel.Windows
{
    /// <summary>
    ///     The style of a window: its panel, edge, padding, shadow, and what a modal window paints over the screen
    ///     beneath.
    /// </summary>
    public class WindowStyle : Style<Window, WindowLook>
    {
        /// <summary>Creates a style that sets nothing, leaving the window at its neutral values.</summary>
        public WindowStyle()
        {
        }

        /// <summary>
        ///     Creates a style that inherits everything from <paramref name="basedOn" /> and overrides whatever this
        ///     style sets.
        /// </summary>
        public WindowStyle(IStyle<Window> basedOn) : base(basedOn)
        {
        }
    }
}
