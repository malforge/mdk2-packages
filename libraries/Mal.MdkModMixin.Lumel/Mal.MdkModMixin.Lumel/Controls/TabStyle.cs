using Lumel.Styling;

namespace Lumel.Controls
{
    /// <summary>
    ///     The style of a <see cref="Tab" />: its text and plate, when idle, hovered, selected and disabled.
    /// </summary>
    public class TabStyle : Style<Tab, TabLook>
    {
        /// <summary>Creates a style that sets nothing, leaving the tab at its neutral values.</summary>
        public TabStyle()
        {
        }

        /// <summary>
        ///     Creates a style that includes everything in <paramref name="basedOn" />, with this style's own settings
        ///     applied over it.
        /// </summary>
        public TabStyle(IStyle<Tab> basedOn) : base(basedOn)
        {
        }

        /// <summary>The look applied while the tab's page is the one shown.</summary>
        public TabLook Selected => this[Tab.Selected];
    }
}
