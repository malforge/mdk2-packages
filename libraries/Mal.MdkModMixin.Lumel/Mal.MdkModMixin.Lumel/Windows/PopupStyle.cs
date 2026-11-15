using Lumel.Styling;

namespace Lumel.Windows
{
    /// <summary>
    ///     The style of a popup, which a theme holds separately from a window's: a window's look and the distance from
    ///     its anchor.
    /// </summary>
    public class PopupStyle : Style<Popup, PopupLook>
    {
        /// <summary>Creates a style that sets nothing, leaving the popup at its neutral values.</summary>
        public PopupStyle()
        {
        }

        /// <summary>
        ///     Creates a style that inherits everything from <paramref name="basedOn" /> and overrides whatever this
        ///     style sets.
        /// </summary>
        public PopupStyle(IStyle<Window> basedOn) : base(basedOn)
        {
        }
    }
}
