using Lumel.Styling;

namespace Lumel.Windows
{
    /// <summary>The style of a coach mark: a popup's look, with its widest width and the style of its text.</summary>
    public class CoachMarkStyle : Style<CoachMark, CoachMarkLook>
    {
        /// <summary>Creates a style that sets nothing, leaving the coach mark at its neutral values.</summary>
        public CoachMarkStyle()
        {
        }

        /// <summary>
        ///     Creates a style that inherits everything from <paramref name="basedOn" /> and overrides whatever this
        ///     style sets.
        /// </summary>
        public CoachMarkStyle(IStyle<Popup> basedOn) : base(basedOn)
        {
        }
    }
}
