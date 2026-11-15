using Lumel.Styling;

namespace Lumel.Controls
{
    /// <summary>
    ///     The style of a <see cref="TabView" />: its box and the space between its headers and its page.
    /// </summary>
    public class TabViewStyle : Style<TabView, TabViewLook>
    {
        /// <summary>Creates a style that sets nothing, leaving the view at its neutral values.</summary>
        public TabViewStyle()
        {
        }

        /// <summary>
        ///     Creates a style that includes everything in <paramref name="basedOn" />, with this style's own settings
        ///     applied over it.
        /// </summary>
        public TabViewStyle(IStyle<Element> basedOn) : base(basedOn)
        {
        }
    }
}
