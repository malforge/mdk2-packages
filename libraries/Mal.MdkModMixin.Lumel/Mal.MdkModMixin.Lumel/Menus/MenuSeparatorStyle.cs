using Lumel.Controls;
using Lumel.Styling;

namespace Lumel.Menus
{
    /// <summary>The style of a <see cref="MenuSeparator" />, which a theme holds separately from a separator's.</summary>
    public class MenuSeparatorStyle : Style<MenuSeparator, SeparatorLook>
    {
        /// <summary>Creates a style that sets nothing, leaving the separator at its neutral values.</summary>
        public MenuSeparatorStyle()
        {
        }

        /// <summary>
        ///     Creates a style that includes everything in <paramref name="basedOn" />, with whatever this style sets
        ///     layered over it.
        /// </summary>
        public MenuSeparatorStyle(IStyle<Separator> basedOn) : base(basedOn)
        {
        }
    }
}
