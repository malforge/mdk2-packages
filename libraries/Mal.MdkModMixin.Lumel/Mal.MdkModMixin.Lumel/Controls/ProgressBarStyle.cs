using Lumel.Styling;

namespace Lumel.Controls
{
    /// <summary>The style of a <see cref="ProgressBar" />: its track, as its background, and its fill.</summary>
    public class ProgressBarStyle : Style<ProgressBar, ProgressBarLook>
    {
        /// <summary>Creates a style that sets nothing, leaving the bar at its neutral values.</summary>
        public ProgressBarStyle()
        {
        }

        /// <summary>
        ///     Creates a style that includes everything in <paramref name="basedOn" />, with this style's own settings
        ///     applied over it.
        /// </summary>
        public ProgressBarStyle(IStyle<ProgressBar> basedOn) : base(basedOn)
        {
        }
    }
}
